import { createHash } from 'crypto';
import { createReadStream, createWriteStream } from 'fs';
import { readdir, rm } from 'fs/promises';
import path from 'path';
import { createGunzip } from 'zlib';
import { pipeline } from 'stream/promises';
import { getJson } from '../util/http';
import { CLASH_PLATFORM } from '../util/platform';
import { CLASH_URLS } from '../clash/urls';
import { PATCH_ERRORS, PatchError, downloadTo, orFail, sha1File, sha1Tap } from './patchIo';
import { checkAll, downloadAll, eachFile } from './phases';
import { runPatch } from './run';
import type { HashCache } from './hashCache';
import type { ProgressTracker } from './progress';
import { CLASH_STAGED_SUFFIX, type PatchContext, type Patcher } from './types';
import type { PatchWorkerPool } from './worker/workerPool';

interface ClashManifestFile {
  filePath: string;
  sha1: string;
  compressed_sha1: string;
}

interface ClashManifest {
  files: ClashManifestFile[];
}

interface NeededFile {
  file: ClashManifestFile;
  platform: 'windows' | 'macos' | 'resources';
  stagedPath?: string;
}

const INSTALL_CONCURRENCY = 4;

const ORPHAN_RE = new RegExp(`^[0-9a-f]{40}${CLASH_STAGED_SUFFIX}?$`);

export class ClashPatcher implements Patcher {
  constructor(private workers: PatchWorkerPool) {}

  run(gamePath: string, ctx: PatchContext): Promise<void> {
    return runPatch('clash', gamePath, this.workers, ctx, (cache, tracker) =>
      this.runInner(gamePath, ctx, cache, tracker)
    );
  }

  private async runInner(
    gamePath: string,
    ctx: PatchContext,
    cache: HashCache,
    tracker: ProgressTracker
  ): Promise<void> {
    const needed = await checkAll(await this.fetchManifests(ctx), tracker, async (entry) => {
      const localHash = await cache.sha1(entry.file.filePath);
      return localHash === entry.file.sha1 ? null : entry;
    });
    if (needed.length > 0) {
      await downloadAll(needed, tracker, (entry) => this.acquireFile(gamePath, entry, ctx));
      await eachFile(needed, 'patching', INSTALL_CONCURRENCY, tracker, (entry) =>
        cache.install(entry.file.filePath, entry.stagedPath!, entry.file.sha1)
      );
    }
    await this.sweepOrphans(gamePath);
  }

  private fetchManifests(ctx: PatchContext): Promise<NeededFile[]> {
    return orFail(PATCH_ERRORS.manifest, async (): Promise<NeededFile[]> => {
      const [main, resources] = await Promise.all([
        getJson<ClashManifest>(CLASH_URLS.mainManifest, ctx.signal),
        getJson<ClashManifest>(CLASH_URLS.resourceManifest, ctx.signal)
      ]);
      return [
        ...main.files.map((file) => ({ file, platform: CLASH_PLATFORM })),
        ...resources.files.map((file) => ({ file, platform: 'resources' as const }))
      ];
    });
  }

  private async acquireFile(gamePath: string, entry: NeededFile, ctx: PatchContext): Promise<boolean> {
    const stagingName = createHash('sha1')
      .update(entry.file.filePath + entry.platform, 'utf8')
      .digest('hex');
    const downloadPath = path.join(gamePath, stagingName);
    const stagedPath = downloadPath + CLASH_STAGED_SUFFIX;

    if ((await sha1File(stagedPath)) === entry.file.sha1) {
      entry.stagedPath = stagedPath;
      return true;
    }

    const url = `${CLASH_URLS.patchServer}${entry.platform}/${entry.file.filePath}.gz`;
    if ((await downloadTo(url, downloadPath, ctx.signal)) !== entry.file.compressed_sha1) {
      await rm(downloadPath, { force: true });
      return false;
    }

    if ((await this.gunzipTo(downloadPath, stagedPath, ctx.signal)) !== entry.file.sha1) {
      await rm(stagedPath, { force: true });
      return false;
    }

    entry.stagedPath = stagedPath;
    return true;
  }

  private async gunzipTo(from: string, to: string, signal: AbortSignal): Promise<string> {
    const { tap, digest } = sha1Tap();
    try {
      await pipeline(createReadStream(from), createGunzip(), tap, createWriteStream(to), { signal });
    } catch (err) {
      await rm(to, { force: true });
      throw err instanceof Error && err.name === 'AbortError'
        ? err
        : new PatchError('Failed to decompress an update file.');
    } finally {
      await rm(from, { force: true });
    }
    return digest();
  }

  private async sweepOrphans(gamePath: string): Promise<void> {
    let entries: string[];
    try {
      entries = await readdir(gamePath);
    } catch {
      return;
    }
    for (const name of entries) {
      if (ORPHAN_RE.test(name)) {
        await rm(path.join(gamePath, name), { force: true });
      }
    }
  }
}
