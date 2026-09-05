<script lang="ts">
  import { refreshAccounts } from '../../state.svelte';
  import Modal from './Modal.svelte';
  import BusyDots from '../common/BusyDots.svelte';

  let password = $state('');
  let error = $state('');
  let submitting = $state(false);

  async function submit(e: SubmitEvent): Promise<void> {
    e.preventDefault();
    error = '';
    if (password.length === 0) {
      error = 'Enter a password.';
      return;
    }
    submitting = true;
    try {
      if (!(await window.api.unlock(password))) {
        error = 'Wrong password.';
        return;
      }
      await refreshAccounts();
    } finally {
      submitting = false;
    }
  }
</script>

<Modal title="Unlock accounts" dismissable={false}>
  <form class="flex flex-col gap-3" onsubmit={submit}>
    <p class="hint">Your account list is encrypted. Enter your password.</p>
    <input type="password" class="field-input" placeholder="Password" bind:value={password} />
    {#if error}
      <p class="text-xs text-bad">{error}</p>
    {/if}
    <button type="submit" class="btn-primary mt-1 py-2" disabled={submitting}>
      {#if submitting}
        <BusyDots />
      {:else}
        Unlock
      {/if}
    </button>
  </form>
</Modal>
