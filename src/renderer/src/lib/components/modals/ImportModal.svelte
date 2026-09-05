<script lang="ts">
  import { reportLegacyImport } from '../../state.svelte';
  import Modal from './Modal.svelte';
  import ModalFooter from './ModalFooter.svelte';

  let { folder, onclose }: { folder: string; onclose: () => void } = $props();

  let password = $state('');
  let error = $state('');
  let submitting = $state(false);

  async function runImport(e: SubmitEvent): Promise<void> {
    e.preventDefault();
    submitting = true;
    error = '';
    try {
      const result = await window.api.importLegacy(folder, password);
      if (result.needsPassword || result.error) {
        error = result.error ?? 'Wrong password.';
        return;
      }
      await reportLegacyImport(result);
      onclose();
    } finally {
      submitting = false;
    }
  }
</script>

<Modal title="Import from old Tunetoon" {onclose}>
  <form class="flex flex-col gap-3" onsubmit={runImport}>
    <p class="hint">The old account files are encrypted, enter the old password:</p>

    <input type="password" class="field-input" bind:value={password} />

    {#if error}
      <p class="text-xs text-bad">{error}</p>
    {/if}

    <ModalFooter
      submit
      confirmLabel={submitting ? 'Importing…' : 'Import'}
      confirmDisabled={!password || submitting}
      oncancel={onclose} />
  </form>
</Modal>
