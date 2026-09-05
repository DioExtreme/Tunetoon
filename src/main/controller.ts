import { app, dialog, ipcMain, screen, type BrowserWindow } from 'electron';
import type {
  AccountDraft,
  AppState,
  Cmd,
  DisplayInfo,
  EvChannel,
  LivePatch,
  RendererApi,
  SaveAccountResult,
  ServerId,
  ToastMessage
} from '@shared/types';
import { Store } from './accounts/store';
import { TransferManager } from './accounts/transfer';
import { GameManager } from './launcher/game';
import { PlayController } from './launcher/play';
import { sweepRunDirs } from './launcher/runDirs';
import { revokeLauncherToken, validateClashCredentials } from './login/clash';
import { readClashToons } from './clash/toons';
import { getText } from './util/http';
import { recordVersion } from './version';
import { IS_MAC } from './util/platform';

const CHANGELOG_URL = process.env['TUNETOON_CHANGELOG_URL'] ?? 'https://tunetoon.dioextreme.com/changelog.md';
const MANUAL_ARRANGE_TIMEOUT_SEC = 15;

type HandlerMap = {
  [K in Cmd]: (...args: Parameters<RendererApi[K]>) => ReturnType<RendererApi[K]> | Awaited<ReturnType<RendererApi[K]>>;
};

const saved = (warning?: string): SaveAccountResult => (warning ? { ok: true, warning } : { ok: true });

export class AppController {
  private store = new Store();
  private games = new GameManager();
  private player = new PlayController(this.store, this.games, (channel, payload) => this.send(channel, payload));
  private transfer = new TransferManager(this.store, () => this.window);

  private window: BrowserWindow | null = null;
  private closeConfirmed = false;
  private startupToast: ToastMessage | null = null;
  private updatedFrom: string | null = null;

  init(): void {
    this.store.unsealVault();
    this.updatedFrom = recordVersion();
    if (this.store.loadError) {
      this.startupToast = {
        kind: 'error',
        text: 'Stored accounts could not be decrypted. The file was left in place.'
      };
      return;
    }
    this.transfer
      .migrateOldAppData()
      .then((toast) => {
        if (toast) {
          this.startupToast = toast;
        }
      })
      .catch(() => {
        this.startupToast = { kind: 'error', text: 'Importing data from the old Tunetoon failed.' };
      });
  }

  attachWindow(window: BrowserWindow): void {
    this.window = window;
    window.on('close', (event) => {
      if (this.closeConfirmed || !this.games.anyRunning()) {
        return;
      }
      event.preventDefault();
      this.send('onConfirmClose', null);
    });
    window.webContents.on('render-process-gone', () => {
      this.closeConfirmed = true;
    });
  }

  private send(channel: EvChannel, payload: unknown): void {
    if (this.window && !this.window.isDestroyed()) {
      this.window.webContents.send(channel, payload);
    }
  }

  register(): void {
    this.wireModelEvents();
    const handlers: HandlerMap = {
      ...this.appHandlers(),
      ...this.vaultHandlers(),
      ...this.accountHandlers(),
      ...this.groupHandlers(),
      ...this.playHandlers(),
      ...this.transferHandlers()
    };
    for (const [channel, fn] of Object.entries(handlers)) {
      ipcMain.handle(channel, (_e, ...args) => (fn as (...handlerArgs: unknown[]) => unknown)(...args));
    }
  }

  private wireModelEvents(): void {
    this.store.onAccountsChanged = (accounts) => this.send('onStatePatch', { accounts } satisfies LivePatch);
    this.store.onGroupsChanged = (groups) => this.send('onStatePatch', { groups } satisfies LivePatch);
    this.games.onStatusChanged = (statuses) => this.send('onStatePatch', { statuses } satisfies LivePatch);
    this.games.onGameExited = (server, gameDir) => {
      sweepRunDirs(server, gameDir, this.games.runningIds());
      if (server === 'clash') {
        this.send('onToonsChanged', null);
      }
    };
  }

  private appHandlers(): Pick<HandlerMap, 'getState' | 'getChangelog' | 'setConfig' | 'pickFolder' | 'confirmClose'> {
    return {
      getState: () => this.buildState(),
      getChangelog: () => getText(CHANGELOG_URL).catch(() => null),
      setConfig: (patch) => this.store.setConfig(patch),
      pickFolder: (title) => this.pickFolder(title),
      confirmClose: () => {
        this.closeConfirmed = true;
        this.window?.close();
      }
    };
  }

  private vaultHandlers(): Pick<HandlerMap, 'unlock' | 'setupMasterPassword' | 'disableEncryption'> {
    return {
      unlock: (password) => this.store.unlock(password),
      setupMasterPassword: (password) => this.store.setupMasterPassword(password),
      disableEncryption: () => this.store.disableEncryption()
    };
  }

  private accountHandlers(): Pick<
    HandlerMap,
    | 'addAccount'
    | 'updateAccount'
    | 'deleteAccount'
    | 'reorderAccounts'
    | 'setLoginWanted'
    | 'setLoginWantedMany'
    | 'setToonSlot'
    | 'getClashToons'
  > {
    return {
      addAccount: (server, draft) => this.addAccount(server, draft),
      updateAccount: (id, draft) => this.updateAccount(id, draft),
      deleteAccount: (id) => this.deleteAccount(id),
      reorderAccounts: (server, orderedIds) => this.store.reorderAccounts(server, orderedIds),
      setLoginWanted: (id, wanted) => this.store.setLoginWanted(id, wanted),
      setLoginWantedMany: (ids, wanted) => this.store.setLoginWantedMany(ids, wanted),
      setToonSlot: (id, slot) => this.store.setToonSlot(id, slot),
      getClashToons: (username) => readClashToons(this.store.config.clashPath, username)
    };
  }

  private groupHandlers(): Pick<
    HandlerMap,
    | 'getDisplays'
    | 'addGroup'
    | 'updateGroup'
    | 'deleteGroup'
    | 'reorderGroups'
    | 'playGroup'
    | 'arrangeGroup'
    | 'endGroup'
  > {
    return {
      getDisplays: () => this.listDisplays(),
      addGroup: (group) => this.store.addGroup(group),
      updateGroup: (group) => this.store.updateGroup(group),
      deleteGroup: (id) => this.store.deleteGroup(id),
      reorderGroups: (server, orderedIds) => this.store.reorderGroups(server, orderedIds),
      playGroup: (id) => this.player.play(id),
      arrangeGroup: (id) => this.player.arrangeGroup(id, MANUAL_ARRANGE_TIMEOUT_SEC),
      endGroup: (id) => this.endGroup(id)
    };
  }

  private playHandlers(): Pick<
    HandlerMap,
    'play' | 'cancelPatch' | 'submitTwoStep' | 'cancelTwoStep' | 'endGame' | 'endAll'
  > {
    return {
      play: () => this.player.play(),
      cancelPatch: () => this.player.cancelPatch(),
      submitTwoStep: (codes) => this.player.submitTwoStep(codes),
      cancelTwoStep: (accountId) => this.player.cancelTwoStep(accountId),
      endGame: (id) => this.games.end(id),
      endAll: () => this.endAll()
    };
  }

  private transferHandlers(): Pick<HandlerMap, 'importLegacy' | 'exportAccounts' | 'importAccounts'> {
    return {
      importLegacy: (folder, password) => this.transfer.importLegacy(folder, password),
      exportAccounts: () => this.transfer.exportAccounts(),
      importAccounts: () => this.transfer.importAccounts()
    };
  }

  private buildState(): AppState {
    this.flushStartupToast();
    return {
      config: this.store.config,
      accounts: this.store.locked ? { rewritten: [], clash: [] } : this.store.publicAccounts(),
      groups: this.store.locked ? [] : this.store.groups,
      statuses: this.games.getStatuses(),
      loginFailures: this.player.loginFailures,
      locked: this.store.locked,
      freshInstall: this.store.freshInstall,
      passwordProtected: this.store.passwordProtected,
      version: app.getVersion(),
      updatedFrom: this.updatedFrom
    };
  }

  private flushStartupToast(): void {
    if (this.startupToast) {
      const toast = this.startupToast;
      this.startupToast = null;
      setTimeout(() => this.send('onToast', toast), 0);
    }
  }

  private listDisplays(): DisplayInfo[] {
    const primaryId = screen.getPrimaryDisplay().id;
    return screen
      .getAllDisplays()
      .map((d, i) => ({
        id: d.id,
        label: d.label || `Display ${i + 1}`,
        bounds: d.bounds,
        workArea: d.workArea,
        primary: d.id === primaryId
      }));
  }

  private endGroup(id: string): void {
    const group = this.store.findGroup(id);
    if (group) {
      this.games.endMany(group.members);
    }
  }

  sweepStartup(): void {
    sweepRunDirs('clash', this.store.config.clashPath, this.games.runningIds());
    sweepRunDirs('rewritten', this.store.config.rewrittenPath, this.games.runningIds());
  }

  private async addAccount(server: ServerId, draft: AccountDraft): Promise<SaveAccountResult> {
    let token: string | null = null;
    let warning: string | undefined;
    if (server === 'clash' && draft.username && draft.password) {
      const validation = await validateClashCredentials(draft.username, draft.password);
      if (!validation.ok) {
        return validation;
      }
      token = validation.token;
      warning = validation.warning;
    }
    this.store.addAccount(server, draft, token);
    return saved(warning);
  }

  private async updateAccount(id: string, draft: AccountDraft): Promise<SaveAccountResult> {
    const account = this.store.findAccount(id);
    let token: string | null | undefined;
    let warning: string | undefined;
    if (account?.server === 'clash') {
      const nextPassword = draft.password ?? account.password;
      const credsChanged = account.username !== draft.username || nextPassword !== account.password;
      if (credsChanged && draft.username && nextPassword) {
        const validation = await validateClashCredentials(draft.username, nextPassword);
        if (!validation.ok) {
          return validation;
        }
        token = validation.token;
        warning = validation.warning;
      }
    }
    this.store.updateAccount(id, draft, token);
    return saved(warning);
  }

  private deleteAccount(id: string): void {
    const account = this.store.findAccount(id);
    if (account?.server === 'clash' && account.loginToken) {
      revokeLauncherToken(account.loginToken);
    }
    this.player.clearFailure(id);
    this.games.dropAccount(id);
    this.store.deleteAccount(id);
  }

  private endAll(): void {
    if (this.store.config.globalEndAll) {
      this.games.endAll();
    } else {
      const ids = this.store.accounts[this.store.config.gameServer].map((a) => a.id);
      this.games.endMany(ids);
    }
  }

  private async pickFolder(title: string): Promise<string | null> {
    if (!this.window) {
      return null;
    }
    const result = await dialog.showOpenDialog(this.window, { title, properties: ['openDirectory'] });
    return result.canceled ? null : (result.filePaths[0] ?? null);
  }

  async shutdown(): Promise<void> {
    if (IS_MAC) {
      this.games.endAll();
    }
    await this.store.flush();
    await this.player.destroy();
  }
}
