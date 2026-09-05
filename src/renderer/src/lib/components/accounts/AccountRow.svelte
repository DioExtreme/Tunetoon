<script lang="ts">
  import type { PublicAccount } from '@shared/types';
  import { app, displayName, ui } from '../../state.svelte';
  import ToonSlotSelect from './ToonSlotSelect.svelte';

  let {
    account,
    index,
    canDrag,
    dragTarget,
    oncheck,
    ondragbegin,
    ondraghover,
    ondragfinish
  }: {
    account: PublicAccount;
    index: number;
    canDrag: boolean;
    dragTarget: boolean;
    oncheck: (id: string, checked: boolean, shiftKey: boolean) => void;
    ondragbegin: (index: number) => void;
    ondraghover: (index: number) => void;
    ondragfinish: (commit: boolean) => void;
  } = $props();

  const status = $derived(app.statuses[account.id] ?? { loggedIn: false, running: false });
  const failure = $derived(app.loginFailures[account.id]);
  let draggable = $state(false);

  const dot = $derived.by(() => {
    if (status.running) {
      return { class: 'bg-ok shadow-[0_0_6px] shadow-ok/60', title: 'In game' };
    }
    if (status.loggedIn) {
      return { class: 'bg-warn', title: 'Logged in' };
    }
    if (failure) {
      return { class: 'bg-bad', title: failure };
    }
    return { class: 'bg-line', title: 'Offline' };
  });
</script>

<div
  class="list-row group transition-colors {dragTarget ? 'list-row-target' : 'list-row-idle'}"
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
    class={canDrag ? 'cursor-grab text-mute/50 group-hover:text-mute' : 'cursor-default text-mute/30'}
    title={canDrag ? 'Drag to reorder' : 'Clear the search to reorder'}
    onmousedown={() => (draggable = canDrag)}
    onmouseup={() => (draggable = false)}
    role="presentation">
    ⠿
  </span>

  <input
    type="checkbox"
    class="size-4 accent-accent"
    title="Log in this account when you press Play. Shift-click another checkbox to check a whole range."
    checked={account.loginWanted}
    onclick={(e) => oncheck(account.id, e.currentTarget.checked, e.shiftKey)} />

  <span class="size-2.5 shrink-0 rounded-full {dot.class}" title={dot.title}></span>

  <button class="min-w-0 flex-1 cursor-pointer text-left" title="Edit account" onclick={() => (ui.edit = account)}>
    <div class="truncate text-sm font-medium">{displayName(account)}</div>
    {#if account.toon && account.username && !app.config.hideUsernames}
      <div class="truncate hint">{account.username}</div>
    {/if}
  </button>

  {#if account.server === 'clash'}
    <ToonSlotSelect {account} />
  {/if}

  <button
    class="btn-danger shrink-0 px-2.5 py-1 text-xs {status.running ? '' : 'invisible'}"
    onclick={() => void window.api.endGame(account.id)}>
    End
  </button>
</div>
