import type { ClashToonInfo } from '@shared/types';
import { SvelteMap } from 'svelte/reactivity';

const clashToonCache = new SvelteMap<string, ClashToonInfo[]>();
const toonFetches = new Map<string, Promise<ClashToonInfo[]>>();

export function getClashToons(username: string): Promise<ClashToonInfo[]> {
  const key = username.toLowerCase();
  const cached = clashToonCache.get(key);
  if (cached) {
    return Promise.resolve(cached);
  }
  const pending = toonFetches.get(key);
  if (pending) {
    return pending;
  }
  const fetched = window.api.getClashToons(username).then(
    (toons) => {
      toonFetches.delete(key);
      clashToonCache.set(key, toons);
      return toons;
    },
    (err) => {
      toonFetches.delete(key);
      throw err;
    }
  );
  toonFetches.set(key, fetched);
  return fetched;
}

export function clearClashToonCache(): void {
  clashToonCache.clear();
}
