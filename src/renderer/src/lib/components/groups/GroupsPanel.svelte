<script lang="ts">
  import { app, currentGroups, ui } from '../../state.svelte';
  import { createReorder } from '../../reorder.svelte';
  import GroupRow from './GroupRow.svelte';

  const groups = $derived(currentGroups());
  const hasGroups = $derived(groups.length > 0);

  const reorder = createReorder((orderedIds) => void window.api.reorderGroups(app.config.gameServer, orderedIds));
</script>

{#if hasGroups}
  <div class="pt-4">
    <div class="mb-2 flex items-center justify-between px-5">
      <h1 class="text-sm font-semibold text-mute">
        Groups
        <span class="count-pill">{groups.length}</span>
      </h1>
      <button
        class="btn-secondary rounded-lg px-2.5 py-1 text-xs text-mute hover:text-ink"
        onclick={() => (ui.group = 'new')}>
        + Group
      </button>
    </div>
    <div class="flex max-h-[35vh] flex-col gap-1.5 overflow-y-auto pr-2.5 pl-5 [scrollbar-gutter:stable]">
      {#each groups as group, index (group.id)}
        <GroupRow
          {group}
          {index}
          dragTarget={reorder.isTarget(index)}
          ondragbegin={reorder.begin}
          ondraghover={reorder.hover}
          ondragfinish={(apply) =>
            reorder.finish(
              groups.map((g) => g.id),
              apply
            )} />
      {/each}
    </div>
  </div>
{/if}
