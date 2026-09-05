import { mkdir } from 'fs/promises';
import type { ServerId } from '@shared/types';
import { HashCache } from './hashCache';
import { PATCH_ERRORS, orFail } from './patchIo';
import { ProgressTracker } from './progress';
import type { PatchContext } from './types';
import type { PatchWorkerPool } from './worker/workerPool';

export async function runPatch(
  server: ServerId,
  gamePath: string,
  workers: PatchWorkerPool,
  ctx: PatchContext,
  run: (cache: HashCache, tracker: ProgressTracker) => Promise<void>
): Promise<void> {
  await orFail(PATCH_ERRORS.gameDir, () => mkdir(gamePath, { recursive: true }));
  const cache = await HashCache.load(server, gamePath, workers);
  let completed = false;
  try {
    await run(cache, new ProgressTracker(ctx));
    completed = true;
  } finally {
    await cache.save(completed);
  }
}
