<script lang="ts">
  import { app } from '../../state.svelte';

  const border = { error: 'border-bad', info: 'border-accent', success: 'border-ok' } as const;
</script>

<div class="pointer-events-none fixed right-4 bottom-20 z-50 flex w-80 flex-col gap-2">
  {#each app.toasts as t (t.id)}
    <div class="pointer-events-auto rounded-lg border-l-4 {border[t.kind]} bg-raise px-3 py-2 text-sm shadow-lg">
      {t.text}
    </div>
  {/each}
</div>
