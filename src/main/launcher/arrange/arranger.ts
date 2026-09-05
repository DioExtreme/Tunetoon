import { screen } from 'electron';
import koffi from 'koffi';
import type { WindowRect } from '@shared/types';
import {
  EVENT_OBJECT_NAMECHANGE,
  EVENT_OBJECT_SHOW,
  OBJID_WINDOW,
  SWP_NOZORDER_NOACTIVATE,
  SetWinEventHook,
  SetWindowPos,
  UnhookWinEvent,
  WINEVENT_OUTOFCONTEXT,
  findWindows,
  frameOf,
  isTopLevelAppWindow,
  pidOf,
  type Hwnd
} from './win32';

export interface ArrangeTarget {
  pid: number;
  rect: WindowRect;
}

export interface ArrangeResult {
  arranged: number;
  missingPids: number[];
}

const STABLE_MS = 2000;
const CHECK_INTERVAL_MS = 300;
const RESCAN_INTERVAL_MS = 1500;
const POSITION_TOLERANCE_PX = 2;

interface TargetState {
  target: WindowRect;
  window: { hwnd: Hwnd; addr: bigint } | null;
  everPlaced: boolean;
  stableSince: number | null;
}

class ArrangeRun {
  private readonly states = new Map<number, TargetState>();
  private readonly deadline: number;
  private readonly hooks: Hwnd[] = [];
  private registered: ReturnType<typeof koffi.register> | null = null;
  private checkTimer: NodeJS.Timeout | null = null;
  private lastRescan = 0;
  private finished = false;
  private resolve!: (result: ArrangeResult) => void;

  constructor(targets: ArrangeTarget[], timeoutSec: number) {
    this.deadline = Date.now() + timeoutSec * 1000;
    for (const target of targets) {
      const physical = screen.dipToScreenRect(null, {
        x: Math.round(target.rect.x),
        y: Math.round(target.rect.y),
        width: Math.round(target.rect.width),
        height: Math.round(target.rect.height)
      });
      this.states.set(target.pid, { target: physical, window: null, everPlaced: false, stableSince: null });
    }
  }

  run(): Promise<ArrangeResult> {
    return new Promise((resolve) => {
      this.resolve = resolve;
      this.installHooks();
      this.rescan();
      this.startPollLoop();
    });
  }

  private finish(): void {
    if (this.finished) {
      return;
    }
    this.finished = true;
    if (this.checkTimer) {
      clearInterval(this.checkTimer);
    }
    for (const hook of this.hooks) {
      UnhookWinEvent(hook);
    }
    if (this.registered) {
      koffi.unregister(this.registered);
    }
    this.resolve(this.tally());
  }

  private tally(): ArrangeResult {
    let arranged = 0;
    const missingPids: number[] = [];
    for (const [pid, state] of this.states) {
      if (state.everPlaced) {
        arranged++;
      } else {
        missingPids.push(pid);
      }
    }
    return { arranged, missingPids };
  }

  private placeWindow(state: TargetState): void {
    if (!state.window) {
      return;
    }
    const frameInfo = frameOf(state.window.hwnd);
    if (!frameInfo) {
      state.window = null;
      state.stableSince = null;
      return;
    }
    state.everPlaced = true;
    const { frame, insets } = frameInfo;
    const wanted = state.target;
    const onTarget =
      Math.abs(frame.left - wanted.x) <= POSITION_TOLERANCE_PX &&
      Math.abs(frame.top - wanted.y) <= POSITION_TOLERANCE_PX &&
      Math.abs(frame.right - frame.left - wanted.width) <= POSITION_TOLERANCE_PX &&
      Math.abs(frame.bottom - frame.top - wanted.height) <= POSITION_TOLERANCE_PX;

    if (onTarget) {
      if (state.stableSince === null) {
        state.stableSince = Date.now();
      }
    } else {
      state.stableSince = null;
      SetWindowPos(
        state.window.hwnd,
        null,
        wanted.x - insets.left,
        wanted.y - insets.top,
        wanted.width + insets.left + insets.right,
        wanted.height + insets.top + insets.bottom,
        SWP_NOZORDER_NOACTIVATE
      );
    }
  }

  private onWinEvent = (_hook: Hwnd, _event: number, hwnd: Hwnd, idObject: number, idChild: number): void => {
    if (this.finished || hwnd === null || idObject !== OBJID_WINDOW || idChild !== 0) {
      return;
    }
    const state = this.states.get(pidOf(hwnd));
    if (!state) {
      return;
    }
    const addr = koffi.address(hwnd);
    if (state.window === null) {
      if (!isTopLevelAppWindow(hwnd)) {
        return;
      }
      state.window = { hwnd, addr };
    } else if (addr !== state.window.addr) {
      return;
    }
    this.placeWindow(state);
  };

  private installHooks(): void {
    this.registered = koffi.register(this.onWinEvent, koffi.pointer('WinEventProc'));
    for (const pid of this.states.keys()) {
      const hook = SetWinEventHook(
        EVENT_OBJECT_SHOW,
        EVENT_OBJECT_NAMECHANGE,
        null,
        this.registered,
        pid,
        0,
        WINEVENT_OUTOFCONTEXT
      );
      if (hook !== null) {
        this.hooks.push(hook);
      }
    }
  }

  private rescan(): void {
    const missing = new Set([...this.states].filter(([, state]) => state.window === null).map(([pid]) => pid));
    if (missing.size === 0) {
      return;
    }
    for (const [pid, hwnd] of findWindows(missing)) {
      const state = this.states.get(pid)!;
      state.window = { hwnd, addr: koffi.address(hwnd) };
      this.placeWindow(state);
    }
  }

  private startPollLoop(): void {
    this.checkTimer = setInterval(() => {
      const now = Date.now();
      if (now >= this.deadline) {
        this.finish();
        return;
      }
      if (now - this.lastRescan >= RESCAN_INTERVAL_MS) {
        this.lastRescan = now;
        this.rescan();
      }
      if (this.allSettled(now)) {
        this.finish();
      }
    }, CHECK_INTERVAL_MS);
  }

  private allSettled(now: number): boolean {
    return [...this.states.values()].every(
      (state) => state.stableSince !== null && now - state.stableSince >= STABLE_MS
    );
  }
}

export function arrangeWindows(targets: ArrangeTarget[], timeoutSec: number): Promise<ArrangeResult> {
  return new ArrangeRun(targets, timeoutSec).run();
}
