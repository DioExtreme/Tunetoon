<script lang="ts">
  import { untrack } from 'svelte';
  import { app } from '../../state.svelte';
  import Modal from './Modal.svelte';
  import ModalFooter from './ModalFooter.svelte';

  const codes = $state<Record<string, string>>({});
  let submitting = $state(false);
  let escArmed = $state(false);
  let escTimer = 0;

  const hasCodeEntries = $derived(app.twoStepRequests.some((r) => r.kind !== 'toonstep'));
  const hasToonstep = $derived(app.twoStepRequests.some((r) => r.kind === 'toonstep'));
  const firstCodeId = $derived(app.twoStepRequests.find((r) => r.kind !== 'toonstep')?.accountId);
  const requestIds = $derived(new Set(app.twoStepRequests.map((r) => r.accountId)));

  function clearCodes(): void {
    for (const key of Object.keys(codes)) {
      delete codes[key];
    }
  }

  function pruneCodes(keep: ReadonlySet<string>): void {
    for (const key of Object.keys(codes)) {
      if (!keep.has(key)) {
        delete codes[key];
      }
    }
  }

  $effect(() => {
    if (untrack(() => submitting)) {
      clearCodes();
      submitting = false;
    } else {
      pruneCodes(requestIds);
    }
  });

  function submit(e: SubmitEvent): void {
    e.preventDefault();
    submitting = true;
    void window.api.submitTwoStep({ ...codes });
  }

  function cancelAll(): void {
    void window.api.cancelTwoStep();
  }

  function requestClose(): void {
    if (Object.values(codes).some((c) => c?.trim()) && !escArmed) {
      escArmed = true;
      clearTimeout(escTimer);
      escTimer = window.setTimeout(() => (escArmed = false), 1500);
      return;
    }
    cancelAll();
  }
</script>

<Modal title="Two-step verification" onclose={requestClose}>
  <p class="mb-3 hint">
    {#if hasCodeEntries}
      These accounts need a verification code. Leave one blank to be asked again.
    {/if}
    {#if hasToonstep}
      Accounts marked ToonStep must be approved from the Corporate Clash website first.
    {/if}
  </p>
  <form class="flex flex-col gap-2" onsubmit={submit}>
    {#each app.twoStepRequests as request (request.accountId)}
      <div class="flex items-center gap-3">
        <span class="w-36 truncate text-sm">{request.toon}</span>
        {#if request.kind === 'toonstep'}
          <span class="flex-1 text-sm text-mute">ToonStep</span>
        {:else}
          <!-- svelte-ignore a11y_autofocus -->
          <input
            class="field-input flex-1 py-1.5"
            placeholder={request.kind === 'toonguard' ? 'ToonGuard code' : 'Authenticator token'}
            autofocus={request.accountId === firstCodeId}
            bind:value={codes[request.accountId]} />
        {/if}
        <button
          type="button"
          class="shrink-0 rounded px-1.5 py-0.5 text-mute hover:bg-hover hover:text-ink"
          title="Cancel this login"
          disabled={submitting}
          onclick={() => void window.api.cancelTwoStep(request.accountId)}>
          ✕
        </button>
      </div>
    {/each}
    {#if escArmed}
      <p class="text-xs text-warn">Press Esc again to cancel all logins.</p>
    {/if}
    <ModalFooter
      submit
      cancelLabel="Cancel logins"
      confirmLabel={hasCodeEntries ? 'Submit codes' : 'Retry'}
      confirmDisabled={submitting}
      oncancel={cancelAll} />
  </form>
</Modal>
