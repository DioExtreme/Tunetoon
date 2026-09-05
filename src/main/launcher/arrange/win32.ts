import koffi from 'koffi';

interface Rect {
  left: number;
  top: number;
  right: number;
  bottom: number;
}

interface FrameInfo {
  frame: Rect;
  insets: { left: number; top: number; right: number; bottom: number };
}

export type Hwnd = unknown;

const user32 = koffi.load('user32.dll');
const dwmapi = koffi.load('dwmapi.dll');

const RECT = koffi.struct('RECT', { left: 'int32', top: 'int32', right: 'int32', bottom: 'int32' });
const RECT_SIZE = koffi.sizeof(RECT);

koffi.proto('bool EnumWindowsProc(void *hwnd, intptr_t lparam)');
koffi.proto(
  'void WinEventProc(void *hook, uint32 event, void *hwnd, int32 idObject, int32 idChild, uint32 idEventThread, uint32 time)'
);

const EnumWindows = user32.func('bool __stdcall EnumWindows(EnumWindowsProc *cb, intptr_t lparam)');
const GetWindowThreadProcessId = user32.func(
  'uint32 __stdcall GetWindowThreadProcessId(void *hwnd, _Out_ uint32 *pid)'
);
const IsWindowVisible = user32.func('bool __stdcall IsWindowVisible(void *hwnd)');
const GetWindow = user32.func('void * __stdcall GetWindow(void *hwnd, uint32 cmd)');
const GetWindowTextLengthW = user32.func('int __stdcall GetWindowTextLengthW(void *hwnd)');
const GetWindowRect = user32.func('bool __stdcall GetWindowRect(void *hwnd, _Out_ RECT *rect)');
const DwmGetWindowAttribute = dwmapi.func(
  'int32 __stdcall DwmGetWindowAttribute(void *hwnd, uint32 attr, _Out_ RECT *rect, uint32 cb)'
);

export const SetWindowPos = user32.func(
  'bool __stdcall SetWindowPos(void *hwnd, void *after, int x, int y, int cx, int cy, uint32 flags)'
);
export const SetWinEventHook = user32.func(
  'void * __stdcall SetWinEventHook(uint32 eventMin, uint32 eventMax, void *mod, WinEventProc *cb, uint32 idProcess, uint32 idThread, uint32 flags)'
);
export const UnhookWinEvent = user32.func('bool __stdcall UnhookWinEvent(void *hook)');

const GW_OWNER = 4;
const DWMWA_EXTENDED_FRAME_BOUNDS = 9;

export const SWP_NOZORDER_NOACTIVATE = 0x14;
export const OBJID_WINDOW = 0;
export const EVENT_OBJECT_SHOW = 0x8002;
export const EVENT_OBJECT_NAMECHANGE = 0x800c;
export const WINEVENT_OUTOFCONTEXT = 0;

export function pidOf(hwnd: Hwnd): number {
  const out: [number] = [0];
  GetWindowThreadProcessId(hwnd, out);
  return out[0];
}

export function isTopLevelAppWindow(hwnd: Hwnd): boolean {
  return IsWindowVisible(hwnd) && GetWindow(hwnd, GW_OWNER) === null && GetWindowTextLengthW(hwnd) > 0;
}

export function findWindows(pids: ReadonlySet<number>): Map<number, Hwnd> {
  const found = new Map<number, Hwnd>();
  EnumWindows((hwnd: Hwnd) => {
    const pid = pidOf(hwnd);
    if (!pids.has(pid) || found.has(pid) || !isTopLevelAppWindow(hwnd)) {
      return true;
    }
    found.set(pid, hwnd);
    return found.size < pids.size;
  }, 0);
  return found;
}

export function frameOf(hwnd: Hwnd): FrameInfo | null {
  const windowRect = {} as Rect;
  if (!GetWindowRect(hwnd, windowRect)) {
    return null;
  }
  const extended = {} as Rect;
  if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, extended, RECT_SIZE) === 0) {
    return {
      frame: extended,
      insets: {
        left: extended.left - windowRect.left,
        top: extended.top - windowRect.top,
        right: windowRect.right - extended.right,
        bottom: windowRect.bottom - extended.bottom
      }
    };
  }
  return { frame: windowRect, insets: { left: 0, top: 0, right: 0, bottom: 0 } };
}
