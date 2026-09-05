import { existsSync } from 'fs';
import type {
  Account,
  AccountGroup,
  ClashAccount,
  EvChannel,
  LivePatch,
  PlayPhase,
  PlayResult,
  RewrittenAccount,
  ToastKind,
  TwoStepRequest
} from '@shared/types';
import { maskUsername } from '@shared/mask';
import { errorMessage } from '../util/errors';
import type { Store } from '../accounts/store';
import type { GameManager } from './game';
import type { ArrangeTarget } from './arrange/arranger';
import { CLASH_URLS } from '../clash/urls';
import { loginClash, type ClashOutcome } from '../login/clash';
import { continueRewrittenTwoStep, loginRewritten, type RewrittenOutcome } from '../login/rewritten';
import { SERVERS, gamePathFor } from './servers';
import { sweepRunDirs } from './runDirs';
import { TwoStepQueue, type PendingChallenge } from './twoStep';
import { PatchError } from '../patcher/patchIo';
import { PatchWorkerPool } from '../patcher/worker/workerPool';

const PATCH_WORKERS = 4;
const POST_LAUNCH_ARRANGE_TIMEOUT_SEC = 90;

export class PlayController {
  private workers = new PatchWorkerPool(PATCH_WORKERS);
  private twoStep: TwoStepQueue;

  private playing = false;
  private playAbort: AbortController | null = null;
  private failures: Record<string, string> = {};

  constructor(
    private store: Store,
    private games: GameManager,
    private send: (channel: EvChannel, payload: unknown) => void
  ) {
    this.twoStep = new TwoStepQueue(send);
  }

  async play(groupId?: string): Promise<PlayResult> {
    if (this.playing || this.store.locked) {
      return { failures: [] };
    }
    this.playing = true;
    this.playAbort = new AbortController();
    try {
      return await this.playInner(this.playAbort.signal, groupId);
    } finally {
      this.playing = false;
      this.playAbort = null;
      this.setPhase('idle');
      this.send('onStatePatch', { patchProgress: null } satisfies LivePatch);
    }
  }

  async arrangeGroup(groupId: string, timeoutSec: number): Promise<void> {
    const group = this.store.findGroup(groupId);
    if (!group) {
      this.toast('error', 'Window arranging failed: Group not found.');
      return;
    }

    const targets: ArrangeTarget[] = [];
    const pidToToon = new Map<number, string>();
    for (const memberId of group.members) {
      const pid = this.games.pidFor(memberId);
      const rect = group.layout[memberId];
      if (pid === null || !rect) {
        continue;
      }
      const account = this.store.findAccount(memberId);
      pidToToon.set(pid, (account && this.label(account)) || 'Unknown');
      targets.push({ pid, rect });
    }
    if (targets.length === 0) {
      this.toast('error', 'Window arranging failed: No running games with a configured layout.');
      return;
    }

    try {
      const { arrangeWindows } =
        process.platform === 'darwin' ? await import('./arrange/arrangerMac') : await import('./arrange/arranger');
      const { arranged, missingPids } = await arrangeWindows(targets, timeoutSec);
      if (missingPids.length > 0) {
        const missing = missingPids.map((pid) => pidToToon.get(pid) ?? String(pid));
        this.toast('info', `No game window found for: ${missing.join(', ')}`);
      } else {
        this.toast('success', `Arranged ${arranged} window(s).`);
      }
    } catch (err) {
      this.toast('error', `Window arranging failed: ${errorMessage(err)}`);
    }
  }

  cancelPatch(): void {
    this.playAbort?.abort();
    this.twoStep.cancel();
  }

  submitTwoStep(codes: Record<string, string>): void {
    this.twoStep.submit(codes);
  }

  cancelTwoStep(accountId?: string): void {
    this.twoStep.cancel(accountId);
  }

  get loginFailures(): Record<string, string> {
    return { ...this.failures };
  }

  clearFailure(id: string): void {
    if (id in this.failures) {
      delete this.failures[id];
      this.send('onStatePatch', { loginFailures: this.loginFailures } satisfies LivePatch);
    }
  }

  async destroy(): Promise<void> {
    await this.workers.destroy();
  }

  private async playInner(signal: AbortSignal, groupId?: string): Promise<PlayResult> {
    const config = this.store.config;
    const group = groupId ? this.store.findGroup(groupId) : undefined;
    const server = group ? group.server : config.gameServer;
    const gamePath = gamePathFor(server, config);
    const result: PlayResult = { failures: [] };

    const anyRunningOnServer = this.store.accounts[server].some((a) => this.games.isRunning(a.id));

    if (!config.skipUpdates && !anyRunningOnServer) {
      this.setPhase('updating');
      this.workers = new PatchWorkerPool(PATCH_WORKERS);
      const patcher = SERVERS[server].createPatcher(this.workers);
      try {
        await patcher.run(gamePath, {
          signal,
          onProgress: (p) => this.send('onStatePatch', { patchProgress: p } satisfies LivePatch)
        });
      } catch (err) {
        if (signal.aborted) {
          return result;
        }
        result.error = err instanceof PatchError ? err.message : 'An unexpected error occurred while updating.';
        return result;
      } finally {
        await this.workers.destroy();
      }
    }

    if (!existsSync(gamePath)) {
      result.error = 'Game directory missing.';
      return result;
    }

    this.setPhase('logging-in');
    if (server === 'rewritten') {
      await this.loginRewrittenAccounts(result, signal, group);
    } else {
      await this.loginClashAccounts(result, signal, group);
    }

    if (signal.aborted) {
      return { failures: [] };
    }

    sweepRunDirs(server, gamePath, this.games.runningIds());

    for (const account of this.store.accounts[server]) {
      if (group && !group.members.includes(account.id)) {
        continue;
      }
      if (this.games.launch(account, config) === 'missing-exe') {
        this.fail(result, account, 'Game executable not found. Check the game folder in Options.');
      }
    }

    this.send('onStatePatch', { loginFailures: this.loginFailures } satisfies LivePatch);

    if (group) {
      void this.arrangeGroup(group.id, POST_LAUNCH_ARRANGE_TIMEOUT_SEC);
    }

    return result;
  }

  private takeLoginCandidates<T extends Account>(accounts: T[], group?: AccountGroup): T[] {
    const picked = accounts.filter(
      (a) =>
        (group ? group.members.includes(a.id) : a.loginWanted) &&
        !this.games.isLoggedIn(a.id) &&
        a.username &&
        a.password
    );
    for (const account of picked) {
      delete this.failures[account.id];
    }
    return picked;
  }

  private async loginRewrittenAccounts(result: PlayResult, signal: AbortSignal, group?: AccountGroup): Promise<void> {
    const candidates = this.takeLoginCandidates(this.store.accounts.rewritten, group);
    const pending: PendingChallenge[] = [];

    const applyOutcome = (account: RewrittenAccount, outcome: RewrittenOutcome): void => {
      switch (outcome.kind) {
        case 'ok':
          this.games.markLoggedIn(account.id, { gameserver: outcome.gameserver, cookie: outcome.cookie });
          break;
        case 'fail':
          this.fail(result, account, outcome.reason);
          break;
        case 'two-step': {
          let token = outcome.responseToken;
          const request: TwoStepRequest = {
            accountId: account.id,
            toon: this.label(account),
            kind: outcome.toonguard ? 'toonguard' : 'two-step'
          };
          pending.push({
            request,
            needsCode: true,
            retry: async (code) => {
              const next = await continueRewrittenTwoStep(token, code, signal);
              if (next.kind === 'two-step') {
                token = next.responseToken;
                request.kind = next.toonguard ? 'toonguard' : 'two-step';
                return 'pending';
              }
              applyOutcome(account, next);
              return 'done';
            }
          });
          break;
        }
      }
    };

    await Promise.all(candidates.map(async (account) => applyOutcome(account, await loginRewritten(account, signal))));
    await this.twoStep.resolve(pending);
  }

  private async loginClashAccounts(result: PlayResult, signal: AbortSignal, group?: AccountGroup): Promise<void> {
    const candidates = this.takeLoginCandidates(this.store.accounts.clash, group);
    const pending: PendingChallenge[] = [];
    let tokensChanged = false;

    const loginTrackingToken = async (account: ClashAccount): Promise<ClashOutcome> => {
      const hadToken = account.loginToken;
      const outcome = await loginClash(account, signal);
      if (account.loginToken !== hadToken) {
        tokensChanged = true;
      }
      return outcome;
    };

    const applyOutcome = (account: ClashAccount, outcome: ClashOutcome): void => {
      switch (outcome.kind) {
        case 'ok':
          this.games.markLoggedIn(account.id, { gameserver: CLASH_URLS.gameServer, cookie: outcome.cookie });
          break;
        case 'fail':
          this.fail(result, account, outcome.reason);
          break;
        case 'toonstep':
          pending.push({
            request: { accountId: account.id, toon: this.label(account), kind: 'toonstep' },
            needsCode: false,
            retry: async () => {
              const next = await loginTrackingToken(account);
              if (next.kind === 'toonstep') {
                return 'pending';
              }
              applyOutcome(account, next);
              return 'done';
            }
          });
          break;
      }
    };

    await Promise.all(candidates.map(async (account) => applyOutcome(account, await loginTrackingToken(account))));
    await this.twoStep.resolve(pending);

    if (tokensChanged) {
      this.store.commitAccountChanges();
    }
  }

  private setPhase(phase: PlayPhase): void {
    this.send('onStatePatch', { playPhase: phase } satisfies LivePatch);
  }

  private toast(kind: ToastKind, text: string): void {
    this.send('onToast', { kind, text });
  }

  private label(account: { toon: string; username: string }): string {
    if (account.toon) {
      return account.toon;
    }
    return this.store.config.hideUsernames ? maskUsername(account.username) : account.username;
  }

  private fail(result: PlayResult, account: Account, reason: string): void {
    result.failures.push({ toon: this.label(account), reason });
    if (!this.playAbort?.signal.aborted) {
      this.failures[account.id] = reason;
    }
  }
}
