import { spawn, type ChildProcess } from 'child_process';
import { chmodSync, existsSync } from 'fs';
import path from 'path';
import type { Account, AppConfig, ServerId, StatusMap } from '@shared/types';
import { IS_MAC } from '../util/platform';
import { SERVERS, gamePathFor } from './servers';
import { prepareRunDir } from './runDirs';

export interface LoginSession {
  gameserver: string;
  cookie: string;
}

export class GameManager {
  private processes = new Map<string, ChildProcess>();
  private sessions = new Map<string, LoginSession>();

  onStatusChanged: (statuses: StatusMap) => void = () => {};
  onGameExited: (server: ServerId, gameDir: string) => void = () => {};

  getStatuses(): StatusMap {
    const statuses: StatusMap = {};
    for (const id of new Set([...this.sessions.keys(), ...this.processes.keys()])) {
      statuses[id] = { loggedIn: this.sessions.has(id), running: this.processes.has(id) };
    }
    return statuses;
  }

  private notify(): void {
    this.onStatusChanged(this.getStatuses());
  }

  isLoggedIn(id: string): boolean {
    return this.sessions.has(id);
  }

  isRunning(id: string): boolean {
    return this.processes.has(id);
  }

  anyRunning(): boolean {
    return this.processes.size > 0;
  }

  pidFor(id: string): number | null {
    return this.processes.get(id)?.pid ?? null;
  }

  markLoggedIn(id: string, session: LoginSession): void {
    this.sessions.set(id, session);
    this.notify();
  }

  dropAccount(id: string): void {
    this.end(id);
    this.sessions.delete(id);
    this.notify();
  }

  launch(account: Account, config: AppConfig): 'launched' | 'skipped' | 'missing-exe' {
    const session = this.sessions.get(account.id);
    if (this.processes.has(account.id) || !session) {
      return 'skipped';
    }

    const server = SERVERS[account.server];
    const gameDir = gamePathFor(account.server, config);
    const exe = path.join(gameDir, server.exe);
    if (!existsSync(exe)) {
      this.dropAccount(account.id);
      return 'missing-exe';
    }
    if (IS_MAC) {
      try {
        chmodSync(exe, 0o755);
      } catch {}
    }

    const env: NodeJS.ProcessEnv = { ...process.env, ...server.env(account, session) };

    const child = spawn(exe, [], {
      cwd: this.runCwd(account, gameDir, config.protectGameSettings),
      env,
      stdio: 'ignore',
      windowsHide: true
    });
    child.unref();

    this.processes.set(account.id, child);
    this.notify();
    this.watchExit(account, child, gameDir);
    return 'launched';
  }

  private runCwd(account: Account, gameDir: string, protect: boolean): string {
    if (!protect) {
      return gameDir;
    }
    try {
      return prepareRunDir(account.server, account.id, gameDir);
    } catch {
      return gameDir;
    }
  }

  private watchExit(account: Account, child: ChildProcess, gameDir: string): void {
    const exited = (): void => {
      this.onExit(account.id);
      this.onGameExited(account.server, gameDir);
    };
    child.on('error', exited);
    child.on('exit', exited);
  }

  private onExit(id: string): void {
    this.processes.delete(id);
    this.sessions.delete(id);
    this.notify();
  }

  end(id: string): void {
    this.processes.get(id)?.kill();
  }

  endMany(ids: string[]): void {
    for (const id of ids) {
      this.end(id);
    }
  }

  endAll(): void {
    this.endMany([...this.processes.keys()]);
  }

  runningIds(): ReadonlySet<string> {
    return new Set(this.processes.keys());
  }
}
