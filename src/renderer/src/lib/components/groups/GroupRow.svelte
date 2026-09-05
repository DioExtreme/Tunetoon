<script lang="ts">
  import type { AccountGroup } from '@shared/types';
  import { isBusy, play, runningCount, ui } from '../../state.svelte';

  let {
    group,
    index,
    dragTarget,
    ondragbegin,
    ondraghover,
    ondragfinish
  }: {
    group: AccountGroup;
    index: number;
    dragTarget: boolean;
    ondragbegin: (index: number) => void;
    ondraghover: (index: number) => void;
    ondragfinish: (commit: boolean) => void;
  } = $props();

  let draggable = $state(false);
  const running = $derived(runningCount(group.members));
</script>

<div
  class="list-row transition-colors {dragTarget ? 'list-row-target' : 'list-row-idle'}"
  role="listitem"
  {draggable}
  ondragstart={() => ondragbegin(index)}
  ondragover={(e) => {
    e.preventDefault();
    ondraghover(index);
  }}
  ondrop={(e) => {
    e.preventDefault();
    ondragfinish(true);
  }}
  ondragend={() => {
    draggable = false;
    ondragfinish(false);
  }}>
  <span
    class="cursor-grab text-mute/50 hover:text-mute"
    title="Drag to reorder"
    onmousedown={() => (draggable = true)}
    onmouseup={() => (draggable = false)}
    role="presentation">
    ⠿
  </span>
  <button class="min-w-0 flex-1 text-left" title="Edit group" onclick={() => (ui.group = group)}>
    <div class="truncate text-sm font-medium">
      {group.name || 'Unnamed group'}
    </div>
    <div class="hint">
      {group.members.length} account{group.members.length === 1 ? '' : 's'}
      {#if running > 0}
        · <span class="text-ok">{running} in game</span>
      {/if}
    </div>
  </button>
  <button
    class="btn-primary shrink-0 px-3 py-1 text-xs"
    disabled={isBusy()}
    title="Log in and launch this group, then arrange its windows"
    onclick={() => void play(group.id)}>
    Play
  </button>
  <button
    class="shrink-0 rounded-md border px-2.5 py-1 text-xs transition-colors disabled:cursor-not-allowed
      {running > 0 ? 'border-accent/60 bg-accent/30 text-ink hover:bg-accent/45' : 'border-line text-mute'}"
    disabled={running === 0}
    title="Re-apply the window layout to running games"
    onclick={() => void window.api.arrangeGroup(group.id)}>
    Arrange
  </button>
  <button
    class="btn-danger shrink-0 px-2.5 py-1 text-xs"
    disabled={running === 0}
    title="End this group's games"
    onclick={() => void window.api.endGroup(group.id)}>
    End
  </button>
</div>
