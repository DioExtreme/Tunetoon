import { systemPreferences } from 'electron';
import type { WindowRect } from '@shared/types';
import type { ArrangeResult, ArrangeTarget } from './arranger';
import { pollPid } from './darwin';

const STABLE_MS = 2000;
const CHECK_INTERVAL_MS = 300;
const POSITION_TOLERANCE_PX = 2;

interface TargetState {
  target: WindowRect;
  everPlaced: boolean;
  stableSince: number | null;
}

class ArrangeRunMac {
  private states = new Map<number, TargetState>();
  private deadline: number;
  private checkTimer: NodeJS.Timeout | null = null;
  private finished = false;
  private resolve: (result: ArrangeResult) => void = () => {};
  private reject: (err: Error) => void = () => {};

  constructor(targets: ArrangeTarget[], timeoutSec: number) {
    for (const { pid, rect } of targets) {
      this.states.set(pid, { target: rect, everPlaced: false, stableSince: null });
    }
    this.deadline = Date.now() + timeoutSec * 1000;
  }

  run(): Promise<ArrangeResult> {
    return new Promise<ArrangeResult>((resolve, reject) => {
      this.resolve = resolve;
      this.reject = reject;
      if (!systemPreferences.isTrustedAccessibilityClient(true)) {
        this.finished = true;
        reject(
          new Error(
            'Accessibility permission required. Enable Tunetoon under System Settings > Privacy & Security > Accessibility, then try again.'
          )
        );
        return;
      }
      this.tick();
      if (!this.finished) {
        this.checkTimer = setInterval(() => this.tick(), CHECK_INTERVAL_MS);
      }
    });
  }

  private tick(): void {
    try {
      const now = Date.now();
      for (const [pid, state] of this.states) {
        const outcome = pollPid(pid, state.target, POSITION_TOLERANCE_PX);
        if (outcome.kind === 'noWindow') {
          state.stableSince = null;
          continue;
        }
        state.everPlaced = true;
        if (outcome.corrected) {
          state.stableSince = null;
        } else {
          state.stableSince ??= now;
        }
      }
      if (now >= this.deadline || this.allSettled(now)) {
        this.finish();
      }
    } catch (err) {
      this.finishWithError(err as Error);
    }
  }

  private allSettled(now: number): boolean {
    for (const state of this.states.values()) {
      if (state.stableSince === null || now - state.stableSince < STABLE_MS) {
        return false;
      }
    }
    return true;
  }

  private finish(): void {
    if (this.finished) {
      return;
    }
    this.finished = true;
    if (this.checkTimer) {
      clearInterval(this.checkTimer);
    }
    let arranged = 0;
    const missingPids: number[] = [];
    for (const [pid, state] of this.states) {
      if (state.everPlaced) {
        arranged++;
      } else {
        missingPids.push(pid);
      }
    }
    this.resolve({ arranged, missingPids });
  }

  private finishWithError(err: Error): void {
    if (this.finished) {
      return;
    }
    this.finished = true;
    if (this.checkTimer) {
      clearInterval(this.checkTimer);
    }
    this.reject(err);
  }
}

export function arrangeWindows(targets: ArrangeTarget[], timeoutSec: number): Promise<ArrangeResult> {
  return new ArrangeRunMac(targets, timeoutSec).run();
}
