import { app } from 'electron';
import { readFileSync, writeFileSync } from 'fs';
import path from 'path';

export function recordVersion(): string | null {
  const versionFile = path.join(app.getPath('userData'), 'last-version');
  let previous: string | null = null;
  try {
    previous = readFileSync(versionFile, 'utf8');
  } catch {}
  if (previous === app.getVersion()) {
    return null;
  }
  try {
    writeFileSync(versionFile, app.getVersion());
    return previous;
  } catch {
    return null;
  }
}
