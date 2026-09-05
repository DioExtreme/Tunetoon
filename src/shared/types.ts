export const MAIN_MENU_SLOT = -1;

export const DEFAULT_CONFIG = {
  gameServer: 'rewritten' as ServerId,
  rewrittenPath: '',
  clashPath: '',
  skipUpdates: false,
  globalEndAll: false,
  hideUsernames: false,
  protectGameSettings: true
};

export type ServerId = 'rewritten' | 'clash';

export type AppConfig = typeof DEFAULT_CONFIG;

export interface AccountBase {
  id: string;
  toon: string;
  username: string;
  password: string;
  loginWanted: boolean;
}

export interface RewrittenAccount extends AccountBase {
  server: 'rewritten';
}

export interface ClashAccount extends AccountBase {
  server: 'clash';
  loginToken: string | null;
  toonSlot: number;
}

export type Account = RewrittenAccount | ClashAccount;

export interface AccountLists {
  rewritten: RewrittenAccount[];
  clash: ClashAccount[];
}

type PublicRewrittenAccount = Omit<RewrittenAccount, 'password'>;
export type PublicClashAccount = Omit<ClashAccount, 'password' | 'loginToken'>;
export type PublicAccount = PublicRewrittenAccount | PublicClashAccount;

export interface PublicAccountLists {
  rewritten: PublicRewrittenAccount[];
  clash: PublicClashAccount[];
}

export interface WindowRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface AccountGroup {
  id: string;
  server: ServerId;
  name: string;
  members: string[];
  layout: Record<string, WindowRect>;
}

export interface DisplayInfo {
  id: number;
  label: string;
  bounds: WindowRect;
  workArea: WindowRect;
  primary: boolean;
}

interface AccountStatus {
  loggedIn: boolean;
  running: boolean;
}

export type StatusMap = Record<string, AccountStatus>;

export type PatchPhase = 'checking' | 'downloading' | 'patching';

export interface PatchProgress {
  phase: PatchPhase;
  processed: number;
  total: number;
}

export type PlayPhase = 'idle' | 'updating' | 'logging-in';

export interface TwoStepRequest {
  accountId: string;
  toon: string;
  kind: 'toonguard' | 'two-step' | 'toonstep';
}

export interface LiveState {
  accounts: PublicAccountLists;
  groups: AccountGroup[];
  statuses: StatusMap;
  loginFailures: Record<string, string>;
  patchProgress: PatchProgress | null;
  playPhase: PlayPhase;
  twoStepRequests: TwoStepRequest[];
}

export type LivePatch = Partial<LiveState>;

export interface AppState extends Pick<LiveState, 'accounts' | 'groups' | 'statuses' | 'loginFailures'> {
  config: AppConfig;
  locked: boolean;
  freshInstall: boolean;
  passwordProtected: boolean;
  version: string;
  updatedFrom: string | null;
}

export type AccountDraft = { toon: string; username: string; password?: string };

export type SaveAccountResult = { ok: true; warning?: string } | { ok: false; error: string };

export interface PlayResult {
  failures: { toon: string; reason: string }[];
  error?: string;
}

export interface LegacyImportResult {
  rewritten: number;
  clash: number;
  configImported: boolean;
  needsPassword: boolean;
  error?: string;
}

export type ImportOutcome =
  { kind: 'file'; added: number } | { kind: 'legacy'; result: LegacyImportResult; folder: string } | null;

export interface ClashToonInfo {
  slot: number;
  name: string;
}

export type ToastKind = 'error' | 'info' | 'success';

export interface ToastMessage {
  kind: ToastKind;
  text: string;
}

export interface RendererApi {
  getState(): Promise<AppState>;
  getChangelog(): Promise<string | null>;
  unlock(password: string): Promise<boolean>;
  setupMasterPassword(password: string): Promise<void>;
  disableEncryption(): Promise<void>;

  setConfig(patch: Partial<AppConfig>): Promise<AppConfig>;
  pickFolder(title: string): Promise<string | null>;

  addAccount(server: ServerId, draft: AccountDraft): Promise<SaveAccountResult>;
  updateAccount(id: string, draft: AccountDraft): Promise<SaveAccountResult>;
  deleteAccount(id: string): Promise<void>;
  reorderAccounts(server: ServerId, orderedIds: string[]): Promise<void>;
  setLoginWanted(id: string, wanted: boolean): Promise<void>;
  setLoginWantedMany(ids: string[], wanted: boolean): Promise<void>;
  setToonSlot(id: string, slot: number): Promise<void>;

  getClashToons(username: string): Promise<ClashToonInfo[]>;

  getDisplays(): Promise<DisplayInfo[]>;
  addGroup(group: Omit<AccountGroup, 'id'>): Promise<void>;
  updateGroup(group: AccountGroup): Promise<void>;
  deleteGroup(id: string): Promise<void>;
  reorderGroups(server: ServerId, orderedIds: string[]): Promise<void>;
  playGroup(id: string): Promise<PlayResult>;
  arrangeGroup(id: string): Promise<void>;
  endGroup(id: string): Promise<void>;

  play(): Promise<PlayResult>;
  cancelPatch(): Promise<void>;
  submitTwoStep(codes: Record<string, string>): Promise<void>;
  cancelTwoStep(accountId?: string): Promise<void>;

  endGame(id: string): Promise<void>;
  endAll(): Promise<void>;
  confirmClose(): Promise<void>;

  importLegacy(folder: string, password?: string): Promise<LegacyImportResult>;
  exportAccounts(): Promise<boolean>;
  importAccounts(): Promise<ImportOutcome>;

  onStatePatch: (cb: (patch: LivePatch) => void) => void;
  onToast: (cb: (toast: ToastMessage) => void) => void;
  onConfirmClose: (cb: () => void) => void;
  onToonsChanged: (cb: () => void) => void;
  onUpdateProgress: (cb: (percent: number) => void) => void;
}

export type EvChannel = Extract<keyof RendererApi, `on${string}`>;
export type Cmd = Exclude<keyof RendererApi, EvChannel>;
