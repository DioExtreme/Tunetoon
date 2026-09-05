import type { EvChannel, LivePatch, TwoStepRequest } from '@shared/types';

export interface PendingChallenge {
  request: TwoStepRequest;
  needsCode: boolean;
  retry(code: string): Promise<'done' | 'pending'>;
}

export class TwoStepQueue {
  private resolver: ((codes: Record<string, string> | null) => void) | null = null;
  private pending: PendingChallenge[] = [];

  constructor(private send: (channel: EvChannel, payload: unknown) => void) {}

  submit(codes: Record<string, string>): void {
    this.resolver?.(codes);
    this.resolver = null;
  }

  cancel(accountId?: string): void {
    if (accountId === undefined) {
      this.resolver?.(null);
      this.resolver = null;
      return;
    }
    const index = this.pending.findIndex((p) => p.request.accountId === accountId);
    if (index < 0) {
      return;
    }
    this.pending.splice(index, 1);
    if (this.pending.length === 0) {
      this.resolver?.(null);
      this.resolver = null;
    } else {
      this.send('onStatePatch', { twoStepRequests: this.pending.map((p) => p.request) } satisfies LivePatch);
    }
  }

  async resolve(pending: PendingChallenge[]): Promise<void> {
    this.pending = pending;
    while (pending.length > 0) {
      this.send('onStatePatch', { twoStepRequests: pending.map((p) => p.request) } satisfies LivePatch);

      const codes = await new Promise<Record<string, string> | null>((resolve) => {
        this.resolver = resolve;
      });
      if (codes === null) {
        break;
      }

      const round = pending.splice(0);
      await Promise.all(
        round.map(async (entry) => {
          const code = codes[entry.request.accountId]?.trim() ?? '';
          if (entry.needsCode && !code) {
            pending.push(entry);
            return;
          }
          if ((await entry.retry(code)) === 'pending') {
            pending.push(entry);
          }
        })
      );
    }
    this.pending = [];
    this.send('onStatePatch', { twoStepRequests: [] } satisfies LivePatch);
  }
}
