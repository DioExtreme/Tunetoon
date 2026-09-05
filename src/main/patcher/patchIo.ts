import { createHash } from 'crypto';
import { createReadStream, createWriteStream } from 'fs';
import { Readable, Transform } from 'stream';
import { pipeline } from 'stream/promises';
import { baseHeaders } from '../util/http';

export class PatchError extends Error {}

export const PATCH_ERRORS = {
  gameDir: 'Unable to create the game directory. Check permissions or change the target directory.',
  manifest: 'Could not retrieve the patch manifest. The game has not been updated.',
  mirrors: 'Could not retrieve the download mirrors. The game has not been updated.',
  download: 'An error occurred downloading update files. The game has not been updated.',
  apply: 'An error occurred applying game patches. The game has not been updated.'
} as const;

export async function sha1File(filePath: string): Promise<string | null> {
  try {
    const hash = createHash('sha1');
    await pipeline(createReadStream(filePath, { highWaterMark: 1 << 20 }), hash);
    return hash.digest('hex');
  } catch {
    return null;
  }
}

export async function orFail<T>(message: string, load: () => Promise<T>): Promise<T> {
  try {
    return await load();
  } catch {
    throw new PatchError(message);
  }
}

export function sha1Tap(): { tap: Transform; digest: () => string } {
  const hash = createHash('sha1');
  const tap = new Transform({
    transform(chunk, _encoding, callback) {
      hash.update(chunk as Buffer);
      callback(null, chunk);
    }
  });
  return { tap, digest: () => hash.digest('hex') };
}

export async function downloadTo(url: string, dest: string, signal: AbortSignal): Promise<string | null> {
  try {
    const res = await fetch(url, { headers: baseHeaders(), signal });
    if (!res.ok || !res.body) {
      return null;
    }
    const { tap, digest } = sha1Tap();
    await pipeline(Readable.fromWeb(res.body as never), tap, createWriteStream(dest), { signal });
    return digest();
  } catch {
    return null;
  }
}
