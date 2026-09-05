import type { PatchPhase } from '@shared/types';
import { PATCH_ERRORS, PatchError } from './patchIo';
import type { ProgressTracker } from './progress';

const CHECK_CONCURRENCY = 8;
const DOWNLOAD_CONCURRENCY = 4;

export async function eachFile<T, R>(
  items: T[],
  phase: PatchPhase,
  limit: number,
  tracker: ProgressTracker,
  fn: (item: T) => Promise<R>
): Promise<R[]> {
  tracker.newPhase(phase, items.length);
  const results = new Array<R>(items.length);
  let next = 0;
  const worker = async (): Promise<void> => {
    for (let i = next++; i < items.length; i = next++) {
      try {
        tracker.signal.throwIfAborted();
        results[i] = await fn(items[i]!);
      } finally {
        tracker.fileProcessed();
      }
    }
  };
  await Promise.all(Array.from({ length: Math.min(limit, items.length) }, worker));
  return results;
}

export async function checkAll<T, R>(
  items: T[],
  tracker: ProgressTracker,
  fn: (item: T) => Promise<R | null>
): Promise<R[]> {
  const results = await eachFile(items, 'checking', CHECK_CONCURRENCY, tracker, fn);
  return results.filter((item): item is R => item !== null);
}

export async function downloadAll<T>(
  items: T[],
  tracker: ProgressTracker,
  acquire: (item: T) => Promise<boolean>
): Promise<void> {
  const results = await eachFile(items, 'downloading', DOWNLOAD_CONCURRENCY, tracker, acquire);
  if (results.some((ok) => !ok)) {
    throw new PatchError(PATCH_ERRORS.download);
  }
}
