import { app, BrowserWindow, dialog, Menu, safeStorage, shell } from 'electron';
import path from 'path';
import { AppController } from './controller';
import { IS_MAC } from './util/platform';
import { runStartupUpdate } from './updater';

process.env['UV_THREADPOOL_SIZE'] = '8';
app.setPath('userData', path.join(app.getPath('appData'), 'DioExtreme', 'Tunetoon'));

function main(): void {
  if (!app.requestSingleInstanceLock()) {
    app.quit();
    return;
  }

  const rendererUrl = process.env['ELECTRON_RENDERER_URL'];
  if (!rendererUrl) {
    Menu.setApplicationMenu(
      IS_MAC ? Menu.buildFromTemplate([{ role: 'appMenu' }, { role: 'editMenu' }, { role: 'windowMenu' }]) : null
    );
  }

  let mainWindow: BrowserWindow | null = null;
  let controllerInitialized = false;
  const controller = new AppController();
  controller.register();

  function createWindow(splash: BrowserWindow | null): void {
    mainWindow = new BrowserWindow({
      width: 1030,
      height: 730,
      minWidth: 760,
      minHeight: 520,
      show: false,
      autoHideMenuBar: true,
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
        devTools: !!rendererUrl
      }
    });

    controller.attachWindow(mainWindow);

    mainWindow.on('ready-to-show', () => {
      mainWindow?.show();
      splash?.destroy();
      setImmediate(() => {
        controller.sweepStartup();
      });
    });

    mainWindow.webContents.setWindowOpenHandler(({ url }) => {
      void shell.openExternal(url);
      return { action: 'deny' };
    });

    if (rendererUrl) {
      void mainWindow.loadURL(rendererUrl);
    } else {
      void mainWindow.loadFile(path.join(__dirname, '../renderer/index.html'));
    }
  }

  app.on('second-instance', () => {
    if (mainWindow) {
      if (mainWindow.isMinimized()) {
        mainWindow.restore();
      }
      mainWindow.focus();
    }
  });

  app.whenReady().then(async () => {
    if (!safeStorage.isEncryptionAvailable()) {
      dialog.showErrorBox(
        'Encryption unavailable',
        "Tunetoon requires your system's encryption service to store accounts securely, but it is not available. Tunetoon will now close."
      );
      app.exit(1);
      return;
    }
    try {
      const { installing, splash } = await runStartupUpdate();
      if (installing) {
        return;
      }
      controller.init();
      controllerInitialized = true;
      createWindow(splash);
    } catch (err) {
      dialog.showErrorBox('Tunetoon failed to start', String(err));
      app.exit(1);
    }
  });

  app.on('window-all-closed', () => {
    if (!controllerInitialized) {
      app.quit();
      return;
    }
    void controller
      .shutdown()
      .catch(() => {})
      .finally(() => app.quit());
  });
}

main();
