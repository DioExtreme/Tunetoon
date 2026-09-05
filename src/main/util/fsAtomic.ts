import { open, rename } from 'fs/promises';

export async function atomicWrite(file: string, contents: string): Promise<void> {
  const tmp = file + '.tmp';
  const fh = await open(tmp, 'w');
  try {
    await fh.writeFile(contents, 'utf8');
    await fh.sync();
  } finally {
    await fh.close();
  }
  await rename(tmp, file);
}
