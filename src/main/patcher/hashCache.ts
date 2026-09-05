import { app } from 'electron';
import { mkdir, readFile, rename, rm, stat } from 'fs/promises';
import path from 'path';
import type { ServerId } from '@shared/types';
import { atomicWrite } from '../util/fsAtomic';
import type { PatchWorkerPool } from './worker/workerPool';

interface CacheEntry {
  size: number;
  mtimeMs: number;
  sha1: string;
}

interface CacheFile {
  v: 1;
  files: Record<string, CacheEntry>;
}

export class HashCache {
  private touched = new Set<string>();
  private madeDirs = new Set<string>();
  private dirty = false;

  private constructor(
    private file: string,
    private gamePath: string,
    private workers: PatchWorkerPool,
    private entries: Record<string, CacheEntry>
  ) {}

  static async load(server: ServerId, gamePath: string, workers: PatchWorkerPool): Promise<HashCache> {
    const file = path.join(app.getPath('userData'), `patch-hash-cache-${server}.json`);
    let entries: Record<string, CacheEntry> = {};
    try {
      const parsed = JSON.parse(await readFile(file, 'utf8')) as CacheFile;
      if (parsed.v === 1) {
        for (const key of Object.keys(parsed.files)) {
          const entry = parsed.files[key];
          if (
            !entry ||
            typeof entry.size !== 'number' ||
            typeof entry.mtimeMs !== 'number' ||
            typeof entry.sha1 !== 'string'
          ) {
            delete parsed.files[key];
          }
        }
        entries = parsed.files;
      }
    } catch {}
    return new HashCache(file, gamePath, workers, entries);
  }

  async sha1(relKey: string): Promise<string | null> {
    this.touched.add(relKey);
    const filePath = path.join(this.gamePath, relKey);
    const before = await stat(filePath).catch(() => null);
    if (!before) {
      if (this.entries[relKey]) {
        delete this.entries[relKey];
        this.dirty = true;
      }
      return null;
    }
    const entry = this.entries[relKey];
    if (entry && entry.size === before.size && entry.mtimeMs === before.mtimeMs) {
      return entry.sha1;
    }
    const reply = await this.workers.submit({ op: 'sha1', input: filePath });
    const hash = reply.ok ? (reply.sha1 ?? null) : null;
    if (hash === null) {
      return null;
    }
    const after = await stat(filePath).catch(() => null);
    if (after && after.size === before.size && after.mtimeMs === before.mtimeMs) {
      this.entries[relKey] = { size: before.size, mtimeMs: before.mtimeMs, sha1: hash };
      this.dirty = true;
    }
    return hash;
  }

  async install(relKey: string, from: string, sha1: string): Promise<void> {
    const dest = path.join(this.gamePath, relKey);
    const dir = path.dirname(dest);
    if (!this.madeDirs.has(dir)) {
      await mkdir(dir, { recursive: true });
      this.madeDirs.add(dir);
    }
    await rm(dest, { force: true });
    await rename(from, dest);
    const st = await stat(dest).catch(() => null);
    if (st) {
      this.entries[relKey] = { size: st.size, mtimeMs: st.mtimeMs, sha1 };
      this.touched.add(relKey);
      this.dirty = true;
    }
  }

  async save(prune: boolean): Promise<void> {
    if (prune) {
      for (const key of Object.keys(this.entries)) {
        if (!this.touched.has(key)) {
          delete this.entries[key];
          this.dirty = true;
        }
      }
    }
    if (!this.dirty) {
      return;
    }
    try {
      await atomicWrite(this.file, JSON.stringify({ v: 1, files: this.entries } satisfies CacheFile));
    } catch {}
  }
}
