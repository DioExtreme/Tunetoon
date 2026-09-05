<script lang="ts">
  import type { PublicAccount } from '@shared/types';
  import { app, toast } from '../../state.svelte';
  import Modal from './Modal.svelte';
  import ModalFooter from './ModalFooter.svelte';

  let { account, onclose }: { account: PublicAccount | null; onclose: () => void } = $props();

  // svelte-ignore state_referenced_locally
  let toon = $state(account?.toon ?? '');
  // svelte-ignore state_referenced_locally
  let username = $state(account?.username ?? '');
  let password = $state('');
  let submitting = $state(false);
  let error = $state('');

  async function save(e: SubmitEvent): Promise<void> {
    e.preventDefault();
    if (submitting) {
      return;
    }
    submitting = true;
    error = '';
    const draft = { toon: toon.trim(), username: username.trim(), ...(password ? { password } : {}) };
    try {
      const result = account
        ? await window.api.updateAccount(account.id, draft)
        : await window.api.addAccount(app.config.gameServer, draft);
      if (!result.ok) {
        error = result.error;
        return;
      }
      if (result.warning) {
        toast('info', result.warning);
      }
      onclose();
    } finally {
      submitting = false;
    }
  }

  async function removeAccount(): Promise<void> {
    if (account) {
      await window.api.deleteAccount(account.id);
    }
    onclose();
  }
</script>

<Modal title={account ? 'Edit account' : 'Add account'} {onclose}>
  <form class="flex flex-col gap-3" onsubmit={save}>
    <label class="field-label">
      Toon (display name)
      <input class="field-input" bind:value={toon} placeholder="Toon" />
    </label>
    <label class="field-label">
      Username
      <input class="field-input" bind:value={username} placeholder="Username" />
    </label>
    <label class="field-label">
      Password
      <input
        type="password"
        class="field-input"
        placeholder={account ? 'Leave blank to keep current' : ''}
        bind:value={password} />
    </label>

    {#if error}
      <p class="text-sm text-bad">{error}</p>
    {/if}

    <ModalFooter
      submit
      confirmDisabled={submitting}
      oncancel={onclose}
      remove={account ? { onclick: () => void removeAccount() } : undefined} />
  </form>
</Modal>
