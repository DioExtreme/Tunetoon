<script lang="ts">
  import { app } from '../../state.svelte';
  import { parseChangelog } from '../../changelog';
  import Modal from './Modal.svelte';

  let { text, onclose }: { text: string; onclose: () => void } = $props();

  const blocks = $derived(parseChangelog(text));
</script>

<Modal title="What's new in {app.version}" {onclose}>
  <div class="flex max-h-[60vh] flex-col gap-1.5 overflow-y-auto pr-1 text-sm outline-none">
    {#each blocks as block, i (i)}
      {#if block.kind === 'li'}
        <li class="ml-4 list-disc text-mute">{block.text}</li>
      {:else}
        <p class="text-mute">{block.text}</p>
      {/if}
    {:else}
      <p class="text-mute">Tunetoon was updated to a new version.</p>
    {/each}
  </div>
  <div class="mt-4 flex justify-end">
    <button class="btn-primary" onclick={onclose}>Close</button>
  </div>
</Modal>
