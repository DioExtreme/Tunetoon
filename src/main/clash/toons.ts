import { readFile } from 'fs/promises';
import path from 'path';
import type { ClashToonInfo } from '@shared/types';

export async function readClashToons(clashPath: string, username: string): Promise<ClashToonInfo[]> {
  const file = path.join(clashPath, 'toons.json');
  if (!username) {
    return [];
  }
  try {
    const parsed = JSON.parse(await readFile(file, 'utf8')) as Record<string, Record<string, number>>;
    const toons = parsed[username.toLowerCase()];
    if (!toons) {
      return [];
    }
    return Object.entries(toons)
      .map(([name, slot]) => ({ name, slot }))
      .sort((a, b) => a.slot - b.slot);
  } catch {
    return [];
  }
}
