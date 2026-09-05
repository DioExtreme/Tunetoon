<script lang="ts">
  import { app, currentAccounts, currentGroups, importAccounts, ui } from '../../state.svelte';
  import { createReorder } from '../../reorder.svelte';
  import AccountRow from './AccountRow.svelte';

  const accounts = $derived(currentAccounts());
  const groups = $derived(currentGroups());
  const hasGroups = $derived(groups.length > 0);

  let filter = $state('');
  let lastCheckedId = $state<string | null>(null);
  const needle = $derived(filter.trim().toLowerCase());
  const visible = $derived(
    needle
      ? accounts.filter((a) => a.toon.toLowerCase().includes(needle) || a.username.toLowerCase().includes(needle))
      : accounts
  );
  const allChecked = $derived(visible.length > 0 && visible.every((a) => a.loginWanted));

  const reorder = createReorder((orderedIds) => void window.api.reorderAccounts(app.config.gameServer, orderedIds));

  function toggleLoginWanted(id: string, checked: boolean, shiftKey: boolean): void {
    const from = shiftKey && lastCheckedId ? visible.findIndex((a) => a.id === lastCheckedId) : -1;
    const to = visible.findIndex((a) => a.id === id);
    if (from >= 0 && to >= 0 && from !== to) {
      const ids = visible.slice(Math.min(from, to), Math.max(from, to) + 1).map((a) => a.id);
      void window.api.setLoginWantedMany(ids, checked);
    } else {
      void window.api.setLoginWanted(id, checked);
    }
    lastCheckedId = id;
  }

  function toggleAllVisible(): void {
    void window.api.setLoginWantedMany(
      visible.map((a) => a.id),
      !allChecked
    );
  }
</script>

<section class="flex min-h-0 flex-1 flex-col">
  <header class="flex items-center justify-between gap-2 px-5 pt-4 pb-3">
    <h1 class="shrink-0 text-sm font-semibold text-mute">
      Accounts
      <span class="count-pill">{needle ? `${visible.length}/${accounts.length}` : accounts.length}</span>
    </h1>
    <div class="flex min-w-0 items-center gap-2">
      {#if accounts.length > 0}
        <input class="field-input w-36 min-w-0 px-2 py-1 text-xs" placeholder="Search" bind:value={filter} />
        <button
          class="btn-secondary shrink-0 rounded-lg px-2.5 py-1 text-xs text-mute hover:text-ink"
          disabled={visible.length === 0}
          onclick={toggleAllVisible}>
          {allChecked ? 'Uncheck all' : 'Check all'}
        </button>
      {/if}
      {#if !hasGroups}
        <button
          class="btn-secondary shrink-0 rounded-lg text-mute hover:text-ink"
          title="Create an account group with a window layout"
          onclick={() => (ui.group = 'new')}>
          + Group
        </button>
      {/if}
      <button class="btn-primary shrink-0 rounded-lg px-3" onclick={() => (ui.edit = 'new')}> + Add </button>
    </div>
  </header>

  <div class="min-h-0 flex-1 overflow-y-auto pr-2.5 pb-4 pl-5 [scrollbar-gutter:stable]" role="list">
    {#if accounts.length === 0}
      <div class="mt-16 flex flex-col items-center gap-3 text-center text-mute">
        <div class="text-4xl">🎩</div>
        <p class="text-sm">No accounts yet for this server.</p>
        <div class="flex gap-2">
          <button class="btn-secondary rounded-lg" onclick={() => (ui.edit = 'new')}> Add an account </button>
          {#if app.freshInstall}
            <button class="btn-secondary rounded-lg" onclick={() => void importAccounts()}> Import accounts </button>
          {/if}
        </div>
      </div>
    {:else if visible.length === 0}
      <p class="mt-8 text-center hint">No accounts match the search.</p>
    {:else}
      <div class="flex flex-col gap-1.5">
        {#each visible as account, index (account.id)}
          <AccountRow
            {account}
            {index}
            canDrag={needle === ''}
            dragTarget={reorder.isTarget(index)}
            oncheck={toggleLoginWanted}
            ondragbegin={reorder.begin}
            ondraghover={reorder.hover}
            ondragfinish={(apply) =>
              reorder.finish(
                accounts.map((a) => a.id),
                apply
              )} />
        {/each}
      </div>
    {/if}
  </div>
</section>
