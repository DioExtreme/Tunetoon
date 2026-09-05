<script lang="ts">
  import { refreshAccounts, toast } from '../../state.svelte';
  import Modal from './Modal.svelte';
  import BusyDots from '../common/BusyDots.svelte';

  let { onclose }: { onclose: () => void } = $props();

  let password = $state('');
  let confirm = $state('');
  let error = $state('');
  let submitting = $state(false);

  async function submit(e: SubmitEvent): Promise<void> {
    e.preventDefault();
    error = '';
    if (password.length === 0) {
      error = 'Enter a password.';
      return;
    }
    if (password !== confirm) {
      error = 'Passwords do not match.';
      return;
    }
    submitting = true;
    try {
      await window.api.setupMasterPassword(password);
      await refreshAccounts();
      toast('success', 'Accounts are now encrypted.');
      onclose();
    } finally {
      submitting = false;
    }
  }
</script>

<Modal title="Set a password" {onclose}>
  <form class="flex flex-col gap-3" onsubmit={submit}>
    <p class="hint">Your account list will be encrypted with this password. It cannot be recovered if forgotten.</p>
    <input type="password" class="field-input" placeholder="Password" bind:value={password} />
    <input type="password" class="field-input" placeholder="Confirm password" bind:value={confirm} />
    {#if error}
      <p class="text-xs text-bad">{error}</p>
    {/if}
    <button type="submit" class="btn-primary mt-1 py-2" disabled={submitting}>
      {#if submitting}
        <BusyDots />
      {:else}
        Encrypt accounts
      {/if}
    </button>
  </form>
</Modal>
