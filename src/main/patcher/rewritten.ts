import { rm } from 'fs/promises';
import path from 'path';
import { getJson } from '../util/http';
import { IS_MAC, TTR_PLATFORM } from '../util/platform';
import { PATCH_ERRORS, PatchError, downloadTo, orFail } from './patchIo';
import { checkAll, downloadAll, eachFile } from './phases';
import { runPatch } from './run';
import type { HashCache } from './hashCache';
import type { ProgressTracker } from './progress';
import { EXTRACTED_SUFFIX, PATCH_TMP_SUFFIX, type PatchContext, type Patcher } from './types';
import type { PatchWorkerPool } from './worker/workerPool';

const TTR_URLS = {
  manifest: 'https://cdn.toontownrewritten.com/content/patchmanifest.txt',
  mirrors: 'https://cdn.toontownrewritten.com/mirrors.txt'
};

interface ManifestPatch {
  filename: string;
  compPatchHash: string;
  patchHash: string;
}

interface ManifestFile {
  dl: string;
  only: string[];
  hash: string;
  compHash: string;
  patches?: Record<string, ManifestPatch>;
}

interface WorkBase {
  fileKey: string;
  file: ManifestFile;
  stagedPath?: string;
}

type WorkItem = (WorkBase & { type: 'full' }) | (WorkBase & { type: 'patch'; patch: ManifestPatch });

export class RewrittenPatcher implements Patcher {
  constructor(private workers: PatchWorkerPool) {}

  run(gamePath: string, ctx: PatchContext): Promise<void> {
    return runPatch('rewritten', gamePath, this.workers, ctx, (cache, tracker) =>
      this.runInner(gamePath, ctx, cache, tracker)
    );
  }

  private async runInner(
    gamePath: string,
    ctx: PatchContext,
    cache: HashCache,
    tracker: ProgressTracker
  ): Promise<void> {
    const work = await checkAll(Object.entries(await this.fetchManifest(ctx)), tracker, ([fileKey, file]) =>
      this.classify(cache, fileKey, file)
    );
    if (work.length === 0) {
      return;
    }

    const mirrors = await orFail(PATCH_ERRORS.mirrors, () => getJson<string[]>(TTR_URLS.mirrors, ctx.signal));
    if (mirrors.length === 0) {
      throw new PatchError(PATCH_ERRORS.mirrors);
    }

    await downloadAll(work, tracker, (item) => this.acquireFile(gamePath, item, mirrors, ctx));
    await this.applyAll(gamePath, work, tracker, ctx, cache);
  }

  private fetchManifest(ctx: PatchContext): Promise<Record<string, ManifestFile>> {
    return orFail(PATCH_ERRORS.manifest, () => getJson<Record<string, ManifestFile>>(TTR_URLS.manifest, ctx.signal));
  }

  private async classify(cache: HashCache, fileKey: string, file: ManifestFile): Promise<WorkItem | null> {
    if (!file.only.includes(TTR_PLATFORM) || (IS_MAC && fileKey === 'Toontown Rewritten.zip')) {
      return null;
    }
    const localHash = await cache.sha1(fileKey);
    if (localHash === file.hash) {
      return null;
    }
    const patch = localHash !== null ? file.patches?.[localHash] : undefined;
    return patch ? { type: 'patch', fileKey, file, patch } : { type: 'full', fileKey, file };
  }

  private async acquireFile(gamePath: string, item: WorkItem, mirrors: string[], ctx: PatchContext): Promise<boolean> {
    const { dlName, compressedHash, extractedHash } =
      item.type === 'patch'
        ? { dlName: item.patch.filename, compressedHash: item.patch.compPatchHash, extractedHash: item.patch.patchHash }
        : { dlName: item.file.dl, compressedHash: item.file.compHash, extractedHash: item.file.hash };
    const downloadPath = path.join(gamePath, dlName);
    const stagedPath = downloadPath + EXTRACTED_SUFFIX;

    for (const mirror of mirrors) {
      ctx.signal.throwIfAborted();
      if ((await downloadTo(mirror + dlName, downloadPath, ctx.signal)) !== compressedHash) {
        await rm(downloadPath, { force: true });
        continue;
      }

      const extracted = await this.workers.submit({ op: 'bunzip', input: downloadPath, output: stagedPath });
      await rm(downloadPath, { force: true });
      if (!extracted.ok || extracted.sha1 !== extractedHash) {
        await rm(stagedPath, { force: true });
        continue;
      }
      item.stagedPath = stagedPath;
      return true;
    }
    return false;
  }

  private async applyAll(
    gamePath: string,
    work: WorkItem[],
    tracker: ProgressTracker,
    ctx: PatchContext,
    cache: HashCache
  ): Promise<void> {
    const capture = async (item: WorkItem): Promise<Error | null> => {
      try {
        await this.applyFile(gamePath, item, cache);
        return null;
      } catch (err) {
        if (ctx.signal.aborted) {
          throw err;
        }
        return err as Error;
      }
    };
    const firstFailure = (await eachFile(work, 'patching', this.workers.size, tracker, capture)).find(Boolean);
    if (firstFailure) {
      throw firstFailure;
    }
  }

  private async applyFile(gamePath: string, item: WorkItem, cache: HashCache): Promise<void> {
    const localPath = path.join(gamePath, item.fileKey);
    let source = item.stagedPath!;

    if (item.type === 'patch') {
      const patched = await this.workers.submit({ op: 'bspatch', input: localPath, output: source });
      await rm(source, { force: true });
      source = localPath + PATCH_TMP_SUFFIX;
      if (!patched.ok || patched.sha1 !== item.file.hash) {
        await rm(source, { force: true });
        const detail = patched.error ? `${item.fileKey}: ${patched.error}` : item.fileKey;
        throw new PatchError(`${PATCH_ERRORS.apply} (${detail})`);
      }
    }
    await cache.install(item.fileKey, source, item.file.hash);
  }
}
