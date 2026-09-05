import type {
  AccountGroup,
  AppConfig,
  AppState,
  LegacyImportResult,
  LiveState,
  PlayResult,
  PublicAccount,
  ToastKind,
  ToastMessage
} from '@shared/types';
import { DEFAULT_CONFIG } from '@shared/types';
import { maskUsername } from '@shared/mask';
import { clearClashToonCache } from './clashToons.svelte';

interface Toast extends ToastMessage {
  id: number;
}

interface AppStateShape extends AppState, Pick<LiveState, 'patchProgress' | 'playPhase' | 'twoStepRequests'> {
  ready: boolean;
  toasts: Toast[];
}

export const app = $state<AppStateShape>({
  ready: false,
  locked: false,
  freshInstall: false,
  passwordProtected: false,
  version: '',
  updatedFrom: null,
  config: DEFAULT_CONFIG,
  accounts: { rewritten: [], clash: [] },
  groups: [],
  statuses: {},
  loginFailures: {},
  patchProgress: null,
  playPhase: 'idle',
  twoStepRequests: [],
  toasts: []
});

export const ui = $state<{
  edit: PublicAccount | 'new' | null;
  group: AccountGroup | 'new' | null;
  options: boolean;
  import: string | null;
  setupPassword: boolean;
  confirmClose: boolean;
  changelog: string | null;
}>({
  edit: null,
  group: null,
  options: false,
  import: null,
  setupPassword: false,
  confirmClose: false,
  changelog: null
});

let toastId = 0;

export function toast(kind: ToastKind, text: string): void {
  const id = ++toastId;
  app.toasts.push({ id, kind, text });
  setTimeout(() => {
    app.toasts = app.toasts.filter((t) => t.id !== id);
  }, 6000);
}

export function isBusy(): boolean {
  return app.playPhase !== 'idle';
}

export function currentAccounts(): PublicAccount[] {
  return app.accounts[app.config.gameServer];
}

export function currentGroups(): AccountGroup[] {
  return app.groups.filter((g) => g.server === app.config.gameServer);
}

export function runningCount(ids: string[]): number {
  return ids.filter((id) => app.statuses[id]?.running).length;
}

export function displayName(account: { toon: string; username: string }): string {
  if (account.toon) {
    return account.toon;
  }
  if (!account.username) {
    return 'Unnamed';
  }
  return app.config.hideUsernames ? maskUsername(account.username) : account.username;
}

export async function refreshAccounts(): Promise<void> {
  Object.assign(app, await window.api.getState());
}

export async function patchConfig(patch: Partial<AppConfig>): Promise<void> {
  app.config = await window.api.setConfig(patch);
}

export async function reportLegacyImport(result: LegacyImportResult): Promise<void> {
  if (result.error) {
    toast('error', result.error);
    return;
  }
  await refreshAccounts();
  toast(
    'success',
    `Imported ${result.rewritten} Rewritten and ${result.clash} Clash account(s)` +
      (result.configImported ? ', plus settings.' : '.')
  );
}

export async function importAccounts(): Promise<void> {
  try {
    const outcome = await window.api.importAccounts();
    if (!outcome) {
      return;
    }
    if (outcome.kind === 'file') {
      toast(
        outcome.added > 0 ? 'success' : 'info',
        outcome.added > 0 ? `Imported ${outcome.added} account(s).` : 'Nothing new to import.'
      );
      return;
    }
    if (outcome.result.needsPassword) {
      ui.import = outcome.folder;
      return;
    }
    await reportLegacyImport(outcome.result);
  } catch {
    toast('error', 'That file could not be read as an account export.');
  }
}

function reportPlayResult(result: PlayResult): void {
  if (result.error) {
    toast('error', result.error);
  }
  for (const failure of result.failures) {
    toast('error', `${failure.toon || 'Account'}: ${failure.reason}`);
  }
}

export async function play(groupId?: string): Promise<void> {
  if (isBusy()) {
    return;
  }
  reportPlayResult(await (groupId ? window.api.playGroup(groupId) : window.api.play()));
}

export async function initAppState(): Promise<void> {
  await refreshAccounts();
  app.ready = true;
  if (app.updatedFrom) {
    void window.api.getChangelog().then((text) => (ui.changelog = text));
  }

  window.api.onStatePatch((patch) => {
    Object.assign(app, patch);
  });
  window.api.onToast((t) => {
    toast(t.kind, t.text);
  });
  window.api.onConfirmClose(() => {
    ui.confirmClose = true;
  });
  window.api.onToonsChanged(() => {
    clearClashToonCache();
  });
}
