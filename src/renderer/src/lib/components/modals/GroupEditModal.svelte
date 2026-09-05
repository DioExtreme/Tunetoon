<script lang="ts">
  import type { AccountGroup, WindowRect } from '@shared/types';
  import { app, currentAccounts, displayName } from '../../state.svelte';
  import Modal from './Modal.svelte';
  import ModalFooter from './ModalFooter.svelte';
  import LayoutEditor from '../groups/LayoutEditor.svelte';

  let { group, onclose }: { group: AccountGroup | null; onclose: () => void } = $props();

  // svelte-ignore state_referenced_locally
  let name = $state(group?.name ?? '');
  // svelte-ignore state_referenced_locally
  let memberIds = $state<string[]>(
    group
      ? [...group.members]
      : currentAccounts()
          .filter((a) => a.loginWanted)
          .map((a) => a.id)
  );
  // svelte-ignore state_referenced_locally
  const layout = $state<Record<string, WindowRect>>(group ? $state.snapshot(group.layout) : {});

  const accounts = $derived(currentAccounts());

  const members = $derived(
    memberIds.flatMap((id) => {
      const account = accounts.find((a) => a.id === id);
      return account ? [{ id, label: displayName(account) }] : [];
    })
  );

  function toggleMember(id: string): void {
    if (memberIds.includes(id)) {
      memberIds = memberIds.filter((m) => m !== id);
      delete layout[id];
    } else {
      memberIds = [...memberIds, id];
    }
  }

  async function save(): Promise<void> {
    const cleanLayout = $state.snapshot(
      Object.fromEntries(memberIds.filter((id) => layout[id]).map((id) => [id, layout[id]!]))
    );
    const data = {
      server: app.config.gameServer,
      name: name.trim() || 'Group',
      members: [...memberIds],
      layout: cleanLayout
    };
    if (group) {
      await window.api.updateGroup({ ...data, id: group.id });
    } else {
      await window.api.addGroup(data);
    }
    onclose();
  }

  async function removeGroup(): Promise<void> {
    if (group) {
      await window.api.deleteGroup(group.id);
    }
    onclose();
  }
</script>

<Modal title={group ? 'Edit group' : 'New group'} {onclose} wide>
  <div class="flex flex-col gap-4">
    <label class="field-label">
      Group name
      <input class="field-input" bind:value={name} placeholder="Group" />
    </label>

    <div class="flex flex-col gap-1">
      <span class="hint">Accounts</span>
      <div class="flex max-h-28 flex-wrap gap-1.5 overflow-y-auto">
        {#each accounts as account (account.id)}
          {@const index = memberIds.indexOf(account.id)}
          <button
            class="rounded-md border px-2.5 py-1 text-xs transition-colors
              {index >= 0
              ? 'border-accent bg-accent/20 text-ink'
              : 'border-line text-mute hover:bg-hover hover:text-ink'}"
            onclick={() => toggleMember(account.id)}>
            {#if index >= 0}<span class="font-bold">{index + 1}.</span>{/if}
            {displayName(account)}
          </button>
        {/each}
        {#if accounts.length === 0}
          <span class="hint">No accounts on this server yet.</span>
        {/if}
      </div>
    </div>

    {#if memberIds.length > 0}
      <div class="flex flex-col gap-1">
        <span class="hint">Window layout</span>
        <LayoutEditor {members} {layout} />
      </div>
    {/if}

    <ModalFooter
      confirmDisabled={memberIds.length === 0}
      onconfirm={() => void save()}
      oncancel={onclose}
      remove={group ? { label: 'Delete group', onclick: () => void removeGroup() } : undefined} />
  </div>
</Modal>
