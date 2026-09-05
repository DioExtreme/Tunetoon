import { app, BrowserWindow } from 'electron';
import { existsSync, readFileSync, rmSync, writeFileSync } from 'fs';
import path from 'path';
import { setTimeout as delay } from 'timers/promises';
import type { AppUpdater, UpdateCheckResult } from 'electron-updater';

interface StartupUpdate {
  installing: boolean;
  splash: BrowserWindow | null;
}

const CHECK_TIMEOUT_MS = 3000;

function createSplash(): BrowserWindow {
  const splash = new BrowserWindow({
    width: 300,
    height: 180,
    frame: false,
    resizable: false,
    maximizable: false,
    fullscreenable: false,
    backgroundColor: '#0d0a16',
    title: 'Tunetoon',
    webPreferences: {
      preload: path.join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      spellcheck: false,
      enableWebSQL: false,
      webgl: false,
      devTools: false
    }
  });
  void splash.loadFile(path.join(__dirname, '../renderer/splash.html'));
  return splash;
}

async function configureAutoUpdater(): Promise<AppUpdater> {
  const { autoUpdater } = (await import('electron-updater')).default;
  autoUpdater.logger = null;
  autoUpdater.autoInstallOnAppQuit = false;
  autoUpdater.on('error', () => {});
  return autoUpdater;
}

function checkWithin(autoUpdater: AppUpdater): Promise<UpdateCheckResult | null> {
  const checkPromise = autoUpdater.checkForUpdates().catch(() => null);
  void checkPromise.then((r) => r?.downloadPromise?.catch(() => {}));
  return Promise.race([checkPromise, delay(CHECK_TIMEOUT_MS, null)]);
}

function attemptAlreadyFailed(marker: string, version: string): boolean {
  try {
    if (existsSync(marker) && readFileSync(marker, 'utf8') === version) {
      rmSync(marker);
      return true;
    }
    return false;
  } catch {
    return true;
  }
}

function pipeProgressToSplash(autoUpdater: AppUpdater, splash: BrowserWindow): void {
  autoUpdater.on('download-progress', (progress) => {
    if (!splash.isDestroyed()) {
      splash.webContents.send('onUpdateProgress', progress.percent);
    }
  });
}

export async function runStartupUpdate(): Promise<StartupUpdate> {
  if (!app.isPackaged || process.env['PORTABLE_EXECUTABLE_DIR']) {
    return { installing: false, splash: null };
  }
  const autoUpdater = await configureAutoUpdater();
  const splash = createSplash();
  const result = await checkWithin(autoUpdater);
  const download = result?.downloadPromise;
  if (!download) {
    return { installing: false, splash };
  }

  const marker = path.join(app.getPath('userData'), 'update-attempt');
  const version = result.updateInfo.version;
  if (attemptAlreadyFailed(marker, version)) {
    return { installing: false, splash };
  }

  pipeProgressToSplash(autoUpdater, splash);
  try {
    await download;
  } catch {
    return { installing: false, splash };
  }
  try {
    writeFileSync(marker, version);
  } catch {
    return { installing: false, splash };
  }
  autoUpdater.quitAndInstall(true, true);
  return { installing: true, splash };
}
