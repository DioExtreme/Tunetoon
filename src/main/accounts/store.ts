import { app, safeStorage } from 'electron';
import { randomUUID } from 'crypto';
import { existsSync, readFileSync } from 'fs';
import { mkdir } from 'fs/promises';
import path from 'path';
import { atomicWrite } from '../util/fsAtomic';
import { IS_MAC } from '../util/platform';
import type {
  Account,
  AccountDraft,
  AccountGroup,
  AccountLists,
  AppConfig,
  PublicAccountLists,
  ServerId
} from '@shared/types';
import { DEFAULT_CONFIG, MAIN_MENU_SLOT } from '@shared/types';
import { parseAccountExport } from './importFile';
import {
  generateDataKey,
  openVault,
  sealVault,
  unwrapKeyPassword,
  VAULT_VERSION,
  type VaultEnvelope,
  type WrapHeader,
  wrapKeyPassword
} from './vault';

const CONFIG_FILE = 'config.json';
const VAULT_FILE = 'accounts.vault';

function defaultConfig(): AppConfig {
  if (IS_MAC) {
    return {
      ...DEFAULT_CONFIG,
      rewrittenPath: path.join(app.getPath('appData'), 'Toontown Rewritten'),
      clashPath: path.join(app.getPath('appData'), 'Corporate Clash'),
      hideUsernames: true
    };
  }
  const pf86 = process.env['ProgramFiles(x86)'] ?? 'C:\\Program Files (x86)';
  const localAppData = process.env['LOCALAPPDATA'] ?? app.getPath('appData');
  return {
    ...DEFAULT_CONFIG,
    rewrittenPath: path.join(pf86, 'Toontown Rewritten'),
    clashPath: path.join(localAppData, 'Corporate Clash'),
    hideUsernames: true
  };
}

interface PersistedData extends AccountLists {
  groups: AccountGroup[];
}

function byRank(orderedIds: string[]): (a: { id: string }, b: { id: string }) => number {
  const rank = new Map(orderedIds.map((id, index) => [id, index]));
  return (a, b) => (rank.get(a.id) ?? orderedIds.length) - (rank.get(b.id) ?? orderedIds.length);
}

export class Store {
  config: AppConfig;
  accounts: AccountLists = { rewritten: [], clash: [] };
  groups: AccountGroup[] = [];

  locked = false;
  freshInstall = false;
  loadError = false;

  private dataKey: Buffer | null = null;
  private wrapHeader: WrapHeader | null = null;
  private pendingEnvelope: VaultEnvelope | null = null;
  private saveTimer: NodeJS.Timeout | null = null;
  private saveChain: Promise<void> = Promise.resolve();
  private dir: string;

  onAccountsChanged: (accounts: PublicAccountLists) => void = () => {};
  onGroupsChanged: (groups: AccountGroup[]) => void = () => {};

  constructor() {
    this.dir = app.getPath('userData');
    this.config = this.loadConfig();

    if (existsSync(this.vaultPath)) {
      try {
        const envelope = JSON.parse(readFileSync(this.vaultPath, 'utf8')) as VaultEnvelope;
        if (envelope.version === VAULT_VERSION) {
          this.pendingEnvelope = envelope;
        } else {
          this.loadError = true;
        }
      } catch {
        this.loadError = true;
      }
      this.locked = this.pendingEnvelope?.wrap === 'password';
    } else {
      this.freshInstall = true;
    }
  }

  private get configPath(): string {
    return path.join(this.dir, CONFIG_FILE);
  }

  private get vaultPath(): string {
    return path.join(this.dir, VAULT_FILE);
  }

  private loadConfig(): AppConfig {
    const defaults = defaultConfig();
    try {
      const loaded = JSON.parse(readFileSync(this.configPath, 'utf8'));
      return { ...defaults, ...loaded };
    } catch {
      return defaults;
    }
  }

  setConfig(patch: Partial<AppConfig>): AppConfig {
    this.config = { ...this.config, ...patch };
    this.saveConfigNow().catch(() => {});
    return this.config;
  }

  unsealVault(): void {
    if (this.pendingEnvelope?.wrap === 'device') {
      this.openDeviceVault(this.pendingEnvelope);
      this.pendingEnvelope = null;
      return;
    }
    if (this.locked || this.loadError) {
      return;
    }
    this.dataKey = generateDataKey();
    this.wrapHeader = this.deviceWrap(this.dataKey);
  }

  private openDeviceVault(envelope: VaultEnvelope): void {
    try {
      const key = Buffer.from(safeStorage.decryptString(Buffer.from(envelope.wrappedKey, 'base64')), 'base64');
      this.loadParsed(openVault(envelope, key));
      this.dataKey = key;
      this.wrapHeader = { wrap: 'device', wrappedKey: envelope.wrappedKey };
    } catch {
      this.loadError = true;
    }
  }

  async unlock(password: string): Promise<boolean> {
    const envelope = this.pendingEnvelope;
    if (!envelope) {
      return false;
    }
    try {
      const key = await unwrapKeyPassword(password, envelope);
      this.loadParsed(openVault(envelope, key));
      const { version: _v, nonce: _n, tag: _t, data: _d, ...header } = envelope;
      this.dataKey = key;
      this.wrapHeader = header;
      this.locked = false;
      this.pendingEnvelope = null;
      return true;
    } catch {
      return false;
    }
  }

  get passwordProtected(): boolean {
    return this.locked || this.wrapHeader?.wrap === 'password';
  }

  private deviceWrap(dataKey: Buffer): WrapHeader {
    return { wrap: 'device', wrappedKey: safeStorage.encryptString(dataKey.toString('base64')).toString('base64') };
  }

  private ensureDataKey(): Buffer {
    this.dataKey ??= generateDataKey();
    return this.dataKey;
  }

  async setupMasterPassword(password: string): Promise<void> {
    if (this.locked) {
      return;
    }
    this.wrapHeader = await wrapKeyPassword(password, this.ensureDataKey());
    await this.saveAccountsNow();
  }

  async disableEncryption(): Promise<void> {
    if (this.locked) {
      return;
    }
    this.wrapHeader = this.deviceWrap(this.ensureDataKey());
    await this.saveAccountsNow();
  }

  findAccount(id: string): Account | undefined {
    return this.accounts.rewritten.find((a) => a.id === id) ?? this.accounts.clash.find((a) => a.id === id);
  }

  addAccount(server: ServerId, draft: AccountDraft, loginToken: string | null = null): void {
    const base = { ...draft, password: draft.password ?? '', id: randomUUID(), loginWanted: false };
    if (server === 'rewritten') {
      this.accounts.rewritten.push({ ...base, server: 'rewritten' });
    } else {
      this.accounts.clash.push({ ...base, server: 'clash', loginToken, toonSlot: MAIN_MENU_SLOT });
    }
    this.commitAccountChanges();
  }

  updateAccount(id: string, draft: AccountDraft, loginToken?: string | null): void {
    const account = this.findAccount(id);
    if (!account) {
      return;
    }
    const passwordChanged = draft.password !== undefined && draft.password !== account.password;
    if (account.server === 'clash' && (account.username !== draft.username || passwordChanged)) {
      account.loginToken = null;
    }
    account.toon = draft.toon;
    account.username = draft.username;
    if (draft.password !== undefined) {
      account.password = draft.password;
    }
    if (loginToken !== undefined && account.server === 'clash') {
      account.loginToken = loginToken;
    }
    this.commitAccountChanges();
  }

  deleteAccount(id: string): void {
    this.accounts.rewritten = this.accounts.rewritten.filter((a) => a.id !== id);
    this.accounts.clash = this.accounts.clash.filter((a) => a.id !== id);
    for (const group of this.groups) {
      if (group.members.includes(id)) {
        group.members = group.members.filter((m) => m !== id);
        delete group.layout[id];
      }
    }
    this.onGroupsChanged(this.groups);
    this.commitAccountChanges();
  }

  reorderAccounts(server: ServerId, orderedIds: string[]): void {
    this.accounts[server].sort(byRank(orderedIds));
    this.commitAccountChanges();
  }

  setLoginWanted(id: string, wanted: boolean): void {
    const account = this.findAccount(id);
    if (account) {
      account.loginWanted = wanted;
      this.commitAccountChanges();
    }
  }

  setLoginWantedMany(ids: string[], wanted: boolean): void {
    for (const id of ids) {
      const account = this.findAccount(id);
      if (account) {
        account.loginWanted = wanted;
      }
    }
    this.commitAccountChanges();
  }

  setToonSlot(id: string, slot: number): void {
    const account = this.findAccount(id);
    if (account?.server === 'clash') {
      account.toonSlot = slot;
      this.commitAccountChanges();
    }
  }

  publicAccounts(): PublicAccountLists {
    return {
      rewritten: this.accounts.rewritten.map(({ password: _password, ...rest }) => rest),
      clash: this.accounts.clash.map(({ password: _password, loginToken: _loginToken, ...rest }) => rest)
    };
  }

  commitAccountChanges(): void {
    this.onAccountsChanged(this.publicAccounts());
    this.scheduleSave();
  }

  findGroup(id: string): AccountGroup | undefined {
    return this.groups.find((g) => g.id === id);
  }

  addGroup(group: Omit<AccountGroup, 'id'>): void {
    this.groups.push({ ...group, id: randomUUID() });
    this.commitGroupChanges();
  }

  updateGroup(group: AccountGroup): void {
    const index = this.groups.findIndex((g) => g.id === group.id);
    if (index >= 0) {
      this.groups[index] = group;
      this.commitGroupChanges();
    }
  }

  deleteGroup(id: string): void {
    this.groups = this.groups.filter((g) => g.id !== id);
    this.commitGroupChanges();
  }

  reorderGroups(server: ServerId, orderedIds: string[]): void {
    const sortedForServer = this.groups.filter((g) => g.server === server).sort(byRank(orderedIds));
    let nextSorted = 0;
    this.groups = this.groups.map((g) => (g.server === server ? sortedForServer[nextSorted++]! : g));
    this.commitGroupChanges();
  }

  private commitGroupChanges(): void {
    this.onGroupsChanged(this.groups);
    this.scheduleSave();
  }

  mergeExported(parsed: unknown): number {
    const incoming = parseAccountExport(parsed);
    const known = new Set([...this.accounts.rewritten, ...this.accounts.clash].map((a) => a.id));
    const freshRewritten = incoming.rewritten.filter((a) => !known.has(a.id));
    const freshClash = incoming.clash.filter((a) => !known.has(a.id));
    const knownGroups = new Set(this.groups.map((g) => g.id));
    const freshGroups = incoming.groups.filter((g) => !knownGroups.has(g.id));

    this.accounts.rewritten.push(...freshRewritten);
    this.accounts.clash.push(...freshClash);
    if (freshGroups.length > 0) {
      this.groups.push(...freshGroups);
      this.commitGroupChanges();
    }

    this.freshInstall = false;
    this.commitAccountChanges();
    return freshRewritten.length + freshClash.length;
  }

  exportData(): string {
    return JSON.stringify(this.persistedData(), null, 2);
  }

  private loadParsed(json: string): void {
    const parsed = JSON.parse(json) as Partial<PersistedData>;
    this.accounts = { rewritten: parsed.rewritten ?? [], clash: parsed.clash ?? [] };
    this.groups = parsed.groups ?? [];
  }

  private persistedData(): PersistedData {
    return { ...this.accounts, groups: this.groups };
  }

  private scheduleSave(): void {
    if (this.saveTimer) {
      clearTimeout(this.saveTimer);
    }
    this.saveTimer = setTimeout(() => {
      this.saveAccountsNow().catch(() => {});
    }, 500);
  }

  private enqueueSave(job: () => Promise<void>): Promise<void> {
    const next = this.saveChain.then(job);
    this.saveChain = next.catch(() => {});
    return next;
  }

  private saveConfigNow(): Promise<void> {
    return this.enqueueSave(async () => {
      await mkdir(this.dir, { recursive: true });
      await atomicWrite(this.configPath, JSON.stringify(this.config, null, 2));
    });
  }

  private saveAccountsNow(): Promise<void> {
    if (this.saveTimer) {
      clearTimeout(this.saveTimer);
      this.saveTimer = null;
    }
    return this.enqueueSave(async () => {
      if (this.locked || this.loadError || !this.wrapHeader || !this.dataKey) {
        return;
      }
      await mkdir(this.dir, { recursive: true });
      const envelope = sealVault(this.wrapHeader, this.dataKey, JSON.stringify(this.persistedData()));
      await atomicWrite(this.vaultPath, JSON.stringify(envelope));
    });
  }

  async flush(): Promise<void> {
    await this.saveConfigNow();
    await this.saveAccountsNow();
  }
}
