<script lang="ts">
  import { app, currentAccounts, isBusy, play } from '../../state.svelte';
  import BusyDots from '../common/BusyDots.svelte';

  const phaseLabel = {
    checking: 'Checking files',
    downloading: 'Downloading files',
    patching: 'Patching files'
  } as const;

  const busy = $derived(isBusy());
  const progress = $derived(app.patchProgress);
  const hasProgress = $derived(progress !== null && progress.total > 0);
  const progressPercent = $derived(progress && progress.total > 0 ? (progress.processed / progress.total) * 100 : 0);

  const checkedCount = $derived(currentAccounts().filter((a) => a.loginWanted).length);

  const statusText = $derived.by(() => {
    if (app.playPhase === 'updating') {
      if (progress && progress.total > 0) {
        return `${phaseLabel[progress.phase]} ${progress.processed}/${progress.total}`;
      }
      return 'Checking for updates…';
    }
    if (app.playPhase === 'logging-in') {
      return 'Logging in…';
    }
    return '';
  });
</script>

<footer class="flex items-center gap-4 border-t border-line bg-panel px-5 py-3">
  <div class="min-w-0 flex-1">
    {#if busy}
      <div class="mb-1 flex items-center gap-3 hint">
        <span class="truncate">{statusText}</span>
        <button class="shrink-0 text-bad hover:underline" onclick={() => void window.api.cancelPatch()}>
          Cancel
        </button>
      </div>
      <div class="h-1.5 w-full overflow-hidden rounded-full bg-raise">
        {#if hasProgress}
          <div
            class="h-full w-full rounded-full bg-accent transition-transform duration-200"
            style="transform: translateX(-{100 - progressPercent}%)">
          </div>
        {:else}
          <div class="h-full w-1/4 animate-pulse rounded-full bg-accent"></div>
        {/if}
      </div>
    {:else}
      <span class="hint">
        {checkedCount > 0
          ? `${checkedCount} account${checkedCount === 1 ? '' : 's'} checked for login`
          : 'Check accounts to log them in'}
      </span>
    {/if}
  </div>

  <button
    class="btn-primary rounded-xl px-10 py-2.5 text-[1rem] font-bold shadow-lg shadow-accent/20 transition-all"
    disabled={busy}
    onclick={() => void play()}>
    {#if busy}
      <BusyDots class="h-6" />
    {:else}
      Play
    {/if}
  </button>
</footer>
