import koffi from 'koffi';
import type { WindowRect } from '@shared/types';

interface MacFrame {
  x: number;
  y: number;
  width: number;
  height: number;
}

export type TickOutcome = { kind: 'noWindow' } | { kind: 'frame'; corrected: boolean };

function loadFirst(paths: string[]): ReturnType<typeof koffi.load> {
  for (const candidate of paths) {
    try {
      return koffi.load(candidate);
    } catch {}
  }
  throw new Error(`Could not load macOS framework (tried: ${paths.join(', ')})`);
}

const hiServices = loadFirst([
  '/System/Library/Frameworks/ApplicationServices.framework/Versions/A/Frameworks/HIServices.framework/Versions/A/HIServices',
  '/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices',
  '/System/Library/Frameworks/ApplicationServices.framework/Versions/A/ApplicationServices'
]);
const coreFoundation = loadFirst([
  '/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation',
  '/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation'
]);

koffi.pointer('CFRef', koffi.opaque());
koffi.struct('CGPoint', { x: 'double', y: 'double' });
koffi.struct('CGSize', { width: 'double', height: 'double' });

const AXUIElementCreateApplication = hiServices.func('CFRef AXUIElementCreateApplication(int32 pid)');
const AXUIElementSetMessagingTimeout = hiServices.func('int32 AXUIElementSetMessagingTimeout(CFRef el, float seconds)');
const AXUIElementCopyAttributeValue = hiServices.func(
  'int32 AXUIElementCopyAttributeValue(CFRef el, CFRef attr, _Out_ CFRef *value)'
);
const AXUIElementSetAttributeValue = hiServices.func(
  'int32 AXUIElementSetAttributeValue(CFRef el, CFRef attr, CFRef value)'
);
const AXValueCreatePoint = hiServices.func('CFRef AXValueCreate(uint32 type, CGPoint *value)');
const AXValueCreateSize = hiServices.func('CFRef AXValueCreate(uint32 type, CGSize *value)');
const AXValueGetValuePoint = hiServices.func('bool AXValueGetValue(CFRef value, uint32 type, _Out_ CGPoint *out)');
const AXValueGetValueSize = hiServices.func('bool AXValueGetValue(CFRef value, uint32 type, _Out_ CGSize *out)');

const CFStringCreateWithCString = coreFoundation.func(
  'CFRef CFStringCreateWithCString(CFRef alloc, str s, uint32 encoding)'
);
const CFArrayGetCount = coreFoundation.func('long CFArrayGetCount(CFRef arr)');
const CFArrayGetValueAtIndex = coreFoundation.func('CFRef CFArrayGetValueAtIndex(CFRef arr, long idx)');
const CFGetTypeID = coreFoundation.func('ulong CFGetTypeID(CFRef cf)');
const CFArrayGetTypeID = coreFoundation.func('ulong CFArrayGetTypeID()');
const CFRelease = coreFoundation.func('void CFRelease(CFRef cf)');

const kAXValueTypeCGPoint = 1;
const kAXValueTypeCGSize = 2;
const kCFStringEncodingUTF8 = 0x08000100;
const AX_SUCCESS = 0;
const AX_MESSAGING_TIMEOUT_SEC = 0.25;

function cfStr(s: string): unknown {
  const ref: unknown = CFStringCreateWithCString(null, s, kCFStringEncodingUTF8);
  if (ref === null) {
    throw new Error(`CFStringCreateWithCString failed for ${s}`);
  }
  return ref;
}

const attrWindows = cfStr('AXWindows');
const attrPosition = cfStr('AXPosition');
const attrSize = cfStr('AXSize');

function copyAttr(el: unknown, attr: unknown, released: unknown[]): unknown {
  const out: [unknown] = [null];
  if (AXUIElementCopyAttributeValue(el, attr, out) !== AX_SUCCESS || out[0] === null) {
    return null;
  }
  released.push(out[0]);
  return out[0];
}

function readFrame(win: unknown, released: unknown[]): MacFrame | null {
  const posValue = copyAttr(win, attrPosition, released);
  const sizeValue = copyAttr(win, attrSize, released);
  if (posValue === null || sizeValue === null) {
    return null;
  }
  const pt = {} as { x: number; y: number };
  const sz = {} as { width: number; height: number };
  if (
    !AXValueGetValuePoint(posValue, kAXValueTypeCGPoint, pt) ||
    !AXValueGetValueSize(sizeValue, kAXValueTypeCGSize, sz)
  ) {
    return null;
  }
  return { x: pt.x, y: pt.y, width: sz.width, height: sz.height };
}

function pickLargest(windows: unknown, count: number, released: unknown[]): unknown {
  if (count === 1) {
    return CFArrayGetValueAtIndex(windows, 0);
  }
  let best: unknown = null;
  let bestArea = -1;
  for (let i = 0; i < count; i++) {
    const win: unknown = CFArrayGetValueAtIndex(windows, i);
    if (win === null) {
      continue;
    }
    const frame = readFrame(win, released);
    const area = frame ? frame.width * frame.height : 0;
    if (area > bestArea) {
      bestArea = area;
      best = win;
    }
  }
  return best;
}

function setSize(win: unknown, width: number, height: number, released: unknown[]): void {
  const value: unknown = AXValueCreateSize(kAXValueTypeCGSize, { width, height });
  if (value === null) {
    return;
  }
  released.push(value);
  AXUIElementSetAttributeValue(win, attrSize, value);
}

function setPosition(win: unknown, x: number, y: number, released: unknown[]): void {
  const value: unknown = AXValueCreatePoint(kAXValueTypeCGPoint, { x, y });
  if (value === null) {
    return;
  }
  released.push(value);
  AXUIElementSetAttributeValue(win, attrPosition, value);
}

export function pollPid(pid: number, target: WindowRect, tolerance: number): TickOutcome {
  const released: unknown[] = [];
  try {
    const app: unknown = AXUIElementCreateApplication(pid);
    if (app === null) {
      return { kind: 'noWindow' };
    }
    released.push(app);
    AXUIElementSetMessagingTimeout(app, AX_MESSAGING_TIMEOUT_SEC);

    const windows = copyAttr(app, attrWindows, released);
    if (windows === null || Number(CFGetTypeID(windows)) !== Number(CFArrayGetTypeID())) {
      return { kind: 'noWindow' };
    }
    const count = Number(CFArrayGetCount(windows));
    if (count === 0) {
      return { kind: 'noWindow' };
    }

    const win = pickLargest(windows, count, released);
    if (win === null) {
      return { kind: 'noWindow' };
    }
    const frame = readFrame(win, released);
    if (frame === null) {
      return { kind: 'noWindow' };
    }

    const sizeOff =
      Math.abs(frame.width - target.width) > tolerance || Math.abs(frame.height - target.height) > tolerance;
    const posOff = Math.abs(frame.x - target.x) > tolerance || Math.abs(frame.y - target.y) > tolerance;
    if (sizeOff) {
      setSize(win, target.width, target.height, released);
    }
    if (posOff) {
      setPosition(win, target.x, target.y, released);
    }

    return { kind: 'frame', corrected: sizeOff || posOff };
  } finally {
    for (let i = released.length - 1; i >= 0; i--) {
      CFRelease(released[i]);
    }
  }
}
