import type { DisplayInfo, WindowRect } from '@shared/types';

export const PREVIEW_W = 560;
export const PREVIEW_H = 300;
export const SNAP_PX = 12;
const MIN_W = 200;
const MIN_H = 150;
const FALLBACK_DESKTOP: WindowRect = { x: 0, y: 0, width: 1920, height: 1080 };

export type Preset = 'grid' | 'columns' | 'rows';

interface SnapContext {
  displays: DisplayInfo[];
  others: WindowRect[];
  bounds: WindowRect;
  threshold: number;
}

export function unionBounds(displays: DisplayInfo[]): WindowRect {
  if (displays.length === 0) {
    return FALLBACK_DESKTOP;
  }
  const bounds = displays.map((d) => d.bounds);
  const x = Math.min(...bounds.map((r) => r.x));
  const y = Math.min(...bounds.map((r) => r.y));
  return {
    x,
    y,
    width: Math.max(...bounds.map((r) => r.x + r.width)) - x,
    height: Math.max(...bounds.map((r) => r.y + r.height)) - y
  };
}

export function tileRects(area: WindowRect, ids: string[], preset: Preset): Record<string, WindowRect> {
  const n = ids.length;
  const cols = preset === 'columns' ? n : preset === 'rows' ? 1 : Math.ceil(Math.sqrt(n));
  const rows = Math.ceil(n / cols);
  const rects: Record<string, WindowRect> = {};
  ids.forEach((id, i) => {
    const c = i % cols;
    const r = Math.floor(i / cols);
    const x0 = Math.round(area.x + (c * area.width) / cols);
    const x1 = Math.round(area.x + ((c + 1) * area.width) / cols);
    const y0 = Math.round(area.y + (r * area.height) / rows);
    const y1 = Math.round(area.y + ((r + 1) * area.height) / rows);
    rects[id] = { x: x0, y: y0, width: x1 - x0, height: y1 - y0 };
  });
  return rects;
}

const snap = (value: number, candidates: number[], threshold: number): number =>
  candidates.find((c) => Math.abs(value - c) <= threshold) ?? value;

function moveCandidates(rect: WindowRect, displays: DisplayInfo[], others: WindowRect[]): { x: number[]; y: number[] } {
  const x: number[] = [];
  const y: number[] = [];
  for (const { workArea: wa } of displays) {
    x.push(wa.x, wa.x + wa.width - rect.width, wa.x + (wa.width - rect.width) / 2, wa.x + wa.width / 2);
    y.push(wa.y, wa.y + wa.height - rect.height, wa.y + (wa.height - rect.height) / 2, wa.y + wa.height / 2);
  }
  for (const o of others) {
    x.push(o.x + o.width, o.x - rect.width, o.x, o.x + o.width - rect.width);
    y.push(o.y + o.height, o.y - rect.height, o.y, o.y + o.height - rect.height);
  }
  return { x, y };
}

function resizeCandidates(displays: DisplayInfo[], others: WindowRect[]): { right: number[]; bottom: number[] } {
  const right: number[] = [];
  const bottom: number[] = [];
  for (const { workArea: wa } of displays) {
    right.push(wa.x + wa.width, wa.x + wa.width / 2);
    bottom.push(wa.y + wa.height, wa.y + wa.height / 2);
  }
  for (const o of others) {
    right.push(o.x, o.x + o.width);
    bottom.push(o.y, o.y + o.height);
  }
  return { right, bottom };
}

export function snapMove(
  orig: WindowRect,
  rect: WindowRect,
  dx: number,
  dy: number,
  ctx: SnapContext
): { x: number; y: number } {
  const candidates = moveCandidates(rect, ctx.displays, ctx.others);
  const x = snap(orig.x + dx, candidates.x, ctx.threshold);
  const y = snap(orig.y + dy, candidates.y, ctx.threshold);
  return {
    x: Math.round(Math.min(Math.max(x, ctx.bounds.x), ctx.bounds.x + ctx.bounds.width - rect.width)),
    y: Math.round(Math.min(Math.max(y, ctx.bounds.y), ctx.bounds.y + ctx.bounds.height - rect.height))
  };
}

export function snapResize(
  rect: WindowRect,
  orig: WindowRect,
  dx: number,
  dy: number,
  ctx: SnapContext
): { width: number; height: number } {
  const candidates = resizeCandidates(ctx.displays, ctx.others);
  const width = snap(rect.x + orig.width + dx, candidates.right, ctx.threshold) - rect.x;
  const height = snap(rect.y + orig.height + dy, candidates.bottom, ctx.threshold) - rect.y;
  return {
    width: Math.round(Math.max(MIN_W, Math.min(width, ctx.bounds.x + ctx.bounds.width - rect.x))),
    height: Math.round(Math.max(MIN_H, Math.min(height, ctx.bounds.y + ctx.bounds.height - rect.y)))
  };
}
