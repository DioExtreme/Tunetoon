import { app } from 'electron';
import {
  copyFileSync,
  existsSync,
  linkSync,
  mkdirSync,
  readFileSync,
  readdirSync,
  renameSync,
  rmdirSync,
  rmSync,
  statSync,
  symlinkSync,
  unlinkSync,
  utimesSync,
  writeFileSync
} from 'fs';
import { createHash } from 'crypto';
import path from 'path';
import type { ServerId } from '@shared/types';
import { IS_MAC } from '../util/platform';

interface RunSpec {
  syncFiles: string[];
  mergeFile?: string;
  sharedDirs: string[];
}

const SPECS: Record<ServerId, RunSpec> = {
  rewritten: { syncFiles: ['settings.json'], sharedDirs: ['logs', 'screenshots'] },
  clash: { syncFiles: ['preferences.json'], mergeFile: 'toons.json', sharedDirs: ['logs'] }
};

const SEED_MANIFEST = 'tunetoon-seed.json';
const GC_SUFFIX = '.gc';

interface SeedManifest {
  hashes: Record<string, string>;
  toons?: string;
}

function runRoot(server: ServerId): string {
  return path.join(app.getPath('userData'), `${server}-run`);
}

function hashBuffer(buf: Buffer): string {
  return createHash('sha256').update(buf).digest('hex');
}

function seedFiles(
  names: string[],
  mergeFile: string | undefined,
  gameDir: string,
  dir: string
): { hashes: Record<string, string>; mergeText?: string } {
  const hashes: Record<string, string> = {};
  let mergeText: string | undefined;
  for (const file of names) {
    const src = path.join(gameDir, file);
    if (existsSync(src)) {
      const buf = readFileSync(src);
      writeFileSync(path.join(dir, file), buf);
      hashes[file] = hashBuffer(buf);
      if (file === mergeFile) {
        mergeText = buf.toString('utf8');
      }
    }
  }
  return mergeText !== undefined ? { hashes, mergeText } : { hashes };
}

function linkRemaining(gameDir: string, dir: string, handled: Set<string>): void {
  for (const entry of readdirSync(gameDir, { withFileTypes: true })) {
    if (handled.has(entry.name)) {
      continue;
    }
    const src = path.join(gameDir, entry.name);
    const dest = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      symlinkSync(src, dest, 'junction');
    } else if (entry.isFile()) {
      linkSync(src, dest);
    }
  }
}

export function prepareRunDir(server: ServerId, accountId: string, gameDir: string): string {
  const spec = SPECS[server];
  const dir = path.join(runRoot(server), `${accountId}-${Date.now()}`);
  mkdirSync(dir, { recursive: true });

  for (const shared of spec.sharedDirs) {
    mkdirSync(path.join(gameDir, shared), { recursive: true });
    symlinkSync(path.join(gameDir, shared), path.join(dir, shared), 'junction');
  }

  const seedNames = spec.mergeFile ? [...spec.syncFiles, spec.mergeFile] : spec.syncFiles;
  const seeded = seedFiles(seedNames, spec.mergeFile, gameDir, dir);
  linkRemaining(gameDir, dir, new Set([...seedNames, ...spec.sharedDirs]));

  const manifest: SeedManifest = { hashes: seeded.hashes };
  if (seeded.mergeText !== undefined) {
    manifest.toons = seeded.mergeText;
  }
  writeFileSync(path.join(dir, SEED_MANIFEST), JSON.stringify(manifest));
  return dir;
}

function wasModified(runFile: string, seedHash: string | undefined): boolean {
  return existsSync(runFile) && (!seedHash || hashBuffer(readFileSync(runFile)) !== seedHash);
}

function mergeJsonKeys(seedText: string | undefined, runFile: string, destFile: string): void {
  try {
    const seed = (seedText ? JSON.parse(seedText) : {}) as Record<string, unknown>;
    const run = JSON.parse(readFileSync(runFile, 'utf8')) as Record<string, unknown>;
    const dest = (existsSync(destFile) ? JSON.parse(readFileSync(destFile, 'utf8')) : {}) as Record<string, unknown>;
    for (const key of new Set([...Object.keys(seed), ...Object.keys(run)])) {
      if (JSON.stringify(run[key]) === JSON.stringify(seed[key])) {
        continue;
      }
      if (key in run) {
        dest[key] = run[key];
      } else {
        delete dest[key];
      }
    }
    writeFileSync(destFile, JSON.stringify(dest));
  } catch {
    copyFileSync(runFile, destFile);
  }
}

function syncBack(syncFiles: string[], mergeFile: string | undefined, dir: string, gameDir: string): void {
  const manifestFile = path.join(dir, SEED_MANIFEST);
  if (!existsSync(manifestFile)) {
    return;
  }
  const manifest = JSON.parse(readFileSync(manifestFile, 'utf8')) as SeedManifest;
  for (const file of syncFiles) {
    const runFile = path.join(dir, file);
    if (!wasModified(runFile, manifest.hashes[file])) {
      continue;
    }
    const dest = path.join(gameDir, file);
    const stat = statSync(runFile);
    if (!existsSync(dest) || stat.mtimeMs > statSync(dest).mtimeMs) {
      copyFileSync(runFile, dest);
      utimesSync(dest, stat.atime, stat.mtime);
    }
  }
  if (mergeFile) {
    const runFile = path.join(dir, mergeFile);
    if (wasModified(runFile, manifest.hashes[mergeFile])) {
      mergeJsonKeys(manifest.toons, runFile, path.join(gameDir, mergeFile));
    }
  }
}

function unlinkJunctions(dir: string): void {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isSymbolicLink()) {
      if (IS_MAC) {
        unlinkSync(p);
      } else {
        rmdirSync(p);
      }
    } else if (entry.isDirectory()) {
      unlinkJunctions(p);
    }
  }
}

function accountIdOf(entry: string): string {
  const name = entry.endsWith(GC_SUFFIX) ? entry.slice(0, -GC_SUFFIX.length) : entry;
  return name.slice(0, name.lastIndexOf('-'));
}

export function sweepRunDirs(server: ServerId, gameDir: string, runningIds: ReadonlySet<string>): void {
  try {
    const root = runRoot(server);
    if (!existsSync(root)) {
      return;
    }
    const spec = SPECS[server];
    for (const entry of readdirSync(root)) {
      if (runningIds.has(accountIdOf(entry))) {
        continue;
      }
      const dir = path.join(root, entry);
      const needsSyncBack = !entry.endsWith(GC_SUFFIX);
      const graveDir = needsSyncBack ? `${dir}${GC_SUFFIX}` : dir;
      try {
        if (needsSyncBack) {
          renameSync(dir, graveDir);
          syncBack(spec.syncFiles, spec.mergeFile, graveDir, gameDir);
        }
        unlinkJunctions(graveDir);
        rmSync(graveDir, { recursive: true, force: true });
      } catch {}
    }
  } catch {}
}
