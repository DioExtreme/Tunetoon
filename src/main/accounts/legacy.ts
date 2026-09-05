import { createDecipheriv, pbkdf2, randomUUID } from 'crypto';
import { promisify } from 'util';
import { existsSync } from 'fs';
import { readFile } from 'fs/promises';
import path from 'path';
import type {
  AccountBase,
  AccountLists,
  AppConfig,
  ClashAccount,
  LegacyImportResult,
  RewrittenAccount
} from '@shared/types';
import { MAIN_MENU_SLOT } from '@shared/types';

const FILES = {
  config: 'Config.json',
  rewritten: { plain: 'AccListRewritten.nully', encrypted: 'AccListRewritten.nully2' },
  clash: { plain: 'AccListClash.nully', encrypted: 'AccListClash.nully2' }
} as const;

const ALL_LEGACY_FILES = [
  FILES.config,
  FILES.rewritten.plain,
  FILES.rewritten.encrypted,
  FILES.clash.plain,
  FILES.clash.encrypted
];

export function isLegacyImportFile(file: string): boolean {
  const base = path.basename(file).toLowerCase();
  return base === 'config.json' || base.endsWith('.nully') || base.endsWith('.nully2');
}

interface LegacyAccount {
  LoginWanted?: boolean;
  Toon?: string | null;
  Username?: string | null;
  Password?: string | null;
  LoginToken?: string | null;
  ToonSlot?: number;
}

interface LegacyConfig {
  RewrittenPath?: string;
  ClashPath?: string;
  SkipUpdates?: boolean;
  GlobalEndAll?: boolean;
}

async function readLegacyFile(file: string): Promise<string> {
  const text = await readFile(file, 'utf8');
  return text.charCodeAt(0) === 0xfeff ? text.slice(1) : text;
}

const pbkdf2Async = promisify(pbkdf2);

async function decryptLegacyFile(contents: string, password: string): Promise<string> {
  const raw = Buffer.from(contents, 'base64');
  const salt = raw.subarray(0, 16);
  const derived = await pbkdf2Async(password, salt, 10000, 48, 'sha256');
  const iv = derived.subarray(0, 16);
  const key = derived.subarray(16, 48);
  const decipher = createDecipheriv('aes-256-cbc', key, iv);
  return Buffer.concat([decipher.update(raw.subarray(16)), decipher.final()]).toString('utf8');
}

function parseLegacyAccounts(json: string): LegacyAccount[] {
  const parsed = JSON.parse(json);
  if (!Array.isArray(parsed)) {
    return [];
  }
  return parsed.filter((a): a is LegacyAccount => a !== null && typeof a === 'object');
}

function hasIdentity(a: LegacyAccount): boolean {
  return Boolean(a.Toon || a.Username);
}

function commonFields(a: LegacyAccount): AccountBase {
  return {
    id: randomUUID(),
    toon: a.Toon ?? '',
    username: a.Username ?? '',
    password: a.Password ?? '',
    loginWanted: a.LoginWanted ?? false
  };
}

function toRewritten(a: LegacyAccount): RewrittenAccount {
  return { ...commonFields(a), server: 'rewritten' };
}

function toClash(a: LegacyAccount): ClashAccount {
  return {
    ...commonFields(a),
    server: 'clash',
    loginToken: a.LoginToken ?? null,
    toonSlot: a.ToonSlot == null || a.ToonSlot === -2 ? MAIN_MENU_SLOT : a.ToonSlot
  };
}

async function readLegacyConfig(folder: string): Promise<Partial<AppConfig> | null> {
  const configPath = path.join(folder, FILES.config);
  if (!existsSync(configPath)) {
    return null;
  }
  try {
    const legacy = JSON.parse(await readLegacyFile(configPath)) as LegacyConfig;
    const patch: Partial<AppConfig> = {};
    if (typeof legacy.RewrittenPath === 'string' && legacy.RewrittenPath) {
      patch.rewrittenPath = legacy.RewrittenPath;
    }
    if (typeof legacy.ClashPath === 'string' && legacy.ClashPath) {
      patch.clashPath = legacy.ClashPath;
    }
    if (typeof legacy.SkipUpdates === 'boolean') {
      patch.skipUpdates = legacy.SkipUpdates;
    }
    if (typeof legacy.GlobalEndAll === 'boolean') {
      patch.globalEndAll = legacy.GlobalEndAll;
    }
    return patch;
  } catch {
    return null;
  }
}

interface ListReadOutcome {
  accounts: LegacyAccount[];
  needsPassword?: boolean;
  error?: string;
}

async function readAccountList(
  folder: string,
  names: { plain: string; encrypted: string },
  password?: string
): Promise<ListReadOutcome> {
  const encryptedPath = path.join(folder, names.encrypted);
  if (existsSync(encryptedPath)) {
    if (!password) {
      return { accounts: [], needsPassword: true };
    }
    let contents: string;
    try {
      contents = await readLegacyFile(encryptedPath);
    } catch {
      return { accounts: [] };
    }
    try {
      return { accounts: parseLegacyAccounts(await decryptLegacyFile(contents, password)) };
    } catch {
      return { accounts: [], needsPassword: true, error: 'Wrong password for the old encrypted files.' };
    }
  }
  const plainPath = path.join(folder, names.plain);
  if (existsSync(plainPath)) {
    try {
      return { accounts: parseLegacyAccounts(await readLegacyFile(plainPath)) };
    } catch {
      return { accounts: [] };
    }
  }
  return { accounts: [] };
}

interface ParsedLegacyData {
  summary: LegacyImportResult;
  accounts: AccountLists;
  configPatch: Partial<AppConfig>;
}

export async function importLegacyData(folder: string, password?: string): Promise<ParsedLegacyData> {
  const summary: LegacyImportResult = { rewritten: 0, clash: 0, configImported: false, needsPassword: false };
  const accounts: AccountLists = { rewritten: [], clash: [] };

  if (!ALL_LEGACY_FILES.some((name) => existsSync(path.join(folder, name)))) {
    summary.error = 'No Tunetoon data found in that folder.';
    return { summary, accounts, configPatch: {} };
  }

  const configPatch = await readLegacyConfig(folder);
  summary.configImported = configPatch !== null;

  const rewrittenList = await readAccountList(folder, FILES.rewritten, password);
  const clashList = await readAccountList(folder, FILES.clash, password);
  summary.needsPassword = Boolean(rewrittenList.needsPassword || clashList.needsPassword);
  const error = rewrittenList.error ?? clashList.error;
  if (error !== undefined) {
    summary.error = error;
  }

  accounts.rewritten = rewrittenList.accounts.filter(hasIdentity).map(toRewritten);
  accounts.clash = clashList.accounts.filter(hasIdentity).map(toClash);
  summary.rewritten = accounts.rewritten.length;
  summary.clash = accounts.clash.length;

  return { summary, accounts, configPatch: configPatch ?? {} };
}
