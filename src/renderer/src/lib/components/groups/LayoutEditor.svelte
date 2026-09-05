<script lang="ts">
  import type { DisplayInfo, WindowRect } from '@shared/types';
  import {
    PREVIEW_H,
    PREVIEW_W,
    SNAP_PX,
    snapMove,
    snapResize,
    tileRects,
    unionBounds,
    type Preset
  } from '../../layoutGeometry';

  type DragMode = 'move' | 'resize';

  interface Member {
    id: string;
    label: string;
  }

  let { members, layout }: { members: Member[]; layout: Record<string, WindowRect> } = $props();

  const COLORS = ['#8b5cf6', '#38bdf8', '#4ade80', '#fbbf24', '#f87171', '#f472b6', '#34d399', '#fb923c'];

  let displays = $state<DisplayInfo[]>([]);
  let presetDisplayIndex = $state(0);
  let preset = $state<Preset>('grid');

  void window.api.getDisplays().then((d) => {
    displays = d;
    const primaryIndex = d.findIndex((x) => x.primary);
    presetDisplayIndex = primaryIndex >= 0 ? primaryIndex : 0;
  });

  $effect(() => {
    void members.length;
    const target = displays[presetDisplayIndex] ?? displays[0];
    if (target && !members.every((m) => layout[m.id])) {
      applyTile(target);
    }
  });

  const desktopBounds = $derived(unionBounds(displays));
  const scale = $derived(Math.min(PREVIEW_W / desktopBounds.width, PREVIEW_H / desktopBounds.height));

  function applyTile(target: DisplayInfo): void {
    Object.assign(
      layout,
      tileRects(
        target.workArea,
        members.map((m) => m.id),
        preset
      )
    );
  }

  function applyPreset(): void {
    const target = displays[presetDisplayIndex];
    if (target) {
      applyTile(target);
    }
  }

  interface DragState {
    id: string;
    mode: DragMode;
    startClientX: number;
    startClientY: number;
    orig: WindowRect;
  }
  let drag = $state<DragState | null>(null);

  function beginDrag(event: PointerEvent, id: string, mode: DragMode): void {
    event.preventDefault();
    event.stopPropagation();
    drag = { id, mode, startClientX: event.clientX, startClientY: event.clientY, orig: { ...layout[id]! } };
  }

  function onPointerMove(event: PointerEvent): void {
    const active = drag;
    if (!active) {
      return;
    }
    const dx = (event.clientX - active.startClientX) / scale;
    const dy = (event.clientY - active.startClientY) / scale;
    const rect = layout[active.id];
    if (!rect) {
      return;
    }
    const ctx = {
      displays,
      others: members.filter((m) => m.id !== active.id && layout[m.id]).map((m) => layout[m.id]!),
      bounds: desktopBounds,
      threshold: SNAP_PX / scale
    };

    if (active.mode === 'move') {
      const next = snapMove(active.orig, rect, dx, dy, ctx);
      rect.x = next.x;
      rect.y = next.y;
    } else {
      const next = snapResize(rect, active.orig, dx, dy, ctx);
      rect.width = next.width;
      rect.height = next.height;
    }
  }

  function endDrag(): void {
    drag = null;
  }

  const px = (dip: number): number => Math.round(dip * scale);
</script>

<svelte:window onpointermove={onPointerMove} onpointerup={endDrag} />

<div class="flex flex-col gap-2">
  <div class="flex items-center gap-2 text-xs">
    <select class="field-input px-2 py-1 text-xs" bind:value={presetDisplayIndex}>
      {#each displays as d, i (d.id)}
        <option value={i}>{d.label}{d.primary ? ' (primary)' : ''}</option>
      {/each}
    </select>
    <select class="field-input px-2 py-1 text-xs" bind:value={preset}>
      <option value="grid">Grid</option>
      <option value="columns">Side by side</option>
      <option value="rows">Stacked</option>
    </select>
    <button class="btn-secondary px-2.5 py-1 text-xs" onclick={applyPreset}> Apply preset </button>
    <span class="ml-auto text-mute">Drag/resize windows</span>
  </div>

  <div
    class="relative self-center overflow-hidden rounded-lg border border-line bg-base"
    style="width: {px(desktopBounds.width)}px; height: {px(desktopBounds.height)}px">
    {#each displays as d (d.id)}
      <div
        class="absolute rounded border border-line/80 bg-panel"
        style="left: {px(d.bounds.x - desktopBounds.x)}px; top: {px(d.bounds.y - desktopBounds.y)}px;
          width: {px(d.bounds.width)}px; height: {px(d.bounds.height)}px"
        title={d.label}>
        <span class="absolute right-1 bottom-0.5 text-[10px] text-mute/60">{d.label}</span>
      </div>
    {/each}

    {#each members as member, i (member.id)}
      {@const rect = layout[member.id]}
      {#if rect}
        {@const color = COLORS[i % COLORS.length]}
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <div
          class="absolute flex cursor-move items-center justify-center overflow-hidden rounded border text-[10px] font-semibold select-none"
          style="left: {px(rect.x - desktopBounds.x)}px; top: {px(rect.y - desktopBounds.y)}px;
            width: {px(rect.width)}px; height: {px(rect.height)}px;
            border-color: {color}; background: {color}33; color: {color};
            z-index: {drag?.id === member.id ? 10 : 1}"
          onpointerdown={(e) => beginDrag(e, member.id, 'move')}>
          <span class="truncate px-1">{i + 1}. {member.label}</span>
          <!-- svelte-ignore a11y_no_static_element_interactions -->
          <div
            class="absolute right-0 bottom-0 size-3 cursor-nwse-resize"
            style="background: {color}"
            onpointerdown={(e) => beginDrag(e, member.id, 'resize')}>
          </div>
        </div>
      {/if}
    {/each}
  </div>
</div>
