import type { PatchPhase } from '@shared/types';
import type { PatchContext } from './types';

const EMIT_INTERVAL_MS = 33;

export class ProgressTracker {
  private processed = 0;
  private total = 0;
  private phase: PatchPhase = 'checking';
  private lastEmit = 0;

  constructor(private ctx: PatchContext) {}

  get signal(): AbortSignal {
    return this.ctx.signal;
  }

  newPhase(phase: PatchPhase, total: number): void {
    this.phase = phase;
    this.total = total;
    this.processed = 0;
    this.emit();
  }

  fileProcessed(): void {
    this.processed++;
    if (this.processed === this.total || Date.now() - this.lastEmit >= EMIT_INTERVAL_MS) {
      this.emit();
    }
  }

  private emit(): void {
    this.lastEmit = Date.now();
    this.ctx.onProgress({ phase: this.phase, processed: this.processed, total: this.total });
  }
}
