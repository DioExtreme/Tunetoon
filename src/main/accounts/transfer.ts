import { app, dialog, type BrowserWindow } from 'electron';
import { readFile, writeFile } from 'fs/promises';
import path from 'path';
import type { ImportOutcome, LegacyImportResult, ToastMessage } from '@shared/types';
import type { Store } from './store';
import { importLegacyData, isLegacyImportFile } from './legacy';
import { IS_MAC } from '../util/platform';

export class TransferManager {
  constructor(
    private store: Store,
    private getWindow: () => BrowserWindow | null
  ) {}

  async migrateOldAppData(): Promise<ToastMessage | null> {
    if (IS_MAC || !this.store.freshInstall) {
      return null;
    }
    const result = await this.importLegacy(path.dirname(app.getPath('exe')));
    if (result.needsPassword) {
      return {
        kind: 'info',
        text: 'Accounts from the old Tunetoon found, but they are encrypted. Import them from Options.'
      };
    }
    if (result.rewritten + result.clash > 0) {
      return { kind: 'success', text: `Imported ${result.rewritten + result.clash} account(s) from the old Tunetoon.` };
    }
    return null;
  }

  async exportAccounts(): Promise<boolean> {
    const window = this.getWindow();
    if (this.store.locked || this.store.loadError || !window) {
      return false;
    }
    const result = await dialog.showSaveDialog(window, {
      title: 'Export accounts',
      defaultPath: 'tunetoon-accounts.json',
      filters: [{ name: 'JSON', extensions: ['json'] }]
    });
    if (result.canceled || !result.filePath) {
      return false;
    }
    try {
      await writeFile(result.filePath, this.store.exportData(), 'utf8');
      return true;
    } catch {
      return false;
    }
  }

  async importAccounts(): Promise<ImportOutcome> {
    const window = this.getWindow();
    if (this.store.locked || this.store.loadError || !window) {
      return null;
    }
    const picked = await dialog.showOpenDialog(window, {
      title: 'Import accounts',
      filters: [
        { name: 'Tunetoon data', extensions: ['json', 'nully', 'nully2'] },
        { name: 'All files', extensions: ['*'] }
      ],
      properties: ['openFile']
    });
    const file = picked.filePaths[0];
    if (picked.canceled || !file) {
      return null;
    }
    if (isLegacyImportFile(file)) {
      const folder = path.dirname(file);
      return { kind: 'legacy', result: await this.importLegacy(folder), folder };
    }
    const parsed = JSON.parse(await readFile(file, 'utf8'));
    return { kind: 'file', added: this.store.mergeExported(parsed) };
  }

  async importLegacy(folder: string, password?: string): Promise<LegacyImportResult> {
    const { summary, accounts, configPatch } = await importLegacyData(folder, password);
    if (summary.needsPassword || summary.error) {
      return summary;
    }
    this.store.setConfig(configPatch);
    this.store.mergeExported(accounts);
    return summary;
  }
}
