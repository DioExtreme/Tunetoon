import type { PatchProgress } from '@shared/types';

export interface PatchContext {
  onProgress: (progress: PatchProgress) => void;
  signal: AbortSignal;
}

export interface Patcher {
  run(gamePath: string, ctx: PatchContext): Promise<void>;
}

export const PATCH_TMP_SUFFIX = '.tmp';
export const EXTRACTED_SUFFIX = '.extracted';
export const CLASH_STAGED_SUFFIX = '~';
