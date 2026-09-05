<script lang="ts">
  import type { ServerId } from '@shared/types';
  import { app, isBusy, patchConfig, runningCount, ui } from '../../state.svelte';

  const servers: { id: ServerId; label: string; sub: string }[] = [
    { id: 'rewritten', label: 'Rewritten', sub: 'Toontown Rewritten' },
    { id: 'clash', label: 'Clash', sub: 'Corporate Clash' }
  ];

  const busy = $derived(isBusy());
</script>

<aside class="flex w-44 shrink-0 flex-col border-r border-line bg-panel">
  <nav class="flex flex-col gap-1 px-2 pt-4">
    {#each servers as server (server.id)}
      {@const active = app.config.gameServer === server.id}
      {@const running = runningCount(app.accounts[server.id].map((a) => a.id))}
      <button
        class="flex items-center justify-between rounded-lg px-3 py-2 text-left transition-colors
          disabled:cursor-not-allowed disabled:opacity-60
          {active ? 'bg-accent/25 text-ink' : 'text-mute hover:bg-hover hover:text-ink'}"
        disabled={busy}
        title={server.sub}
        onclick={() => void patchConfig({ gameServer: server.id })}>
        <span class="text-sm font-medium">{server.label}</span>
        {#if running > 0}
          <span class="rounded-full bg-ok/20 px-2 py-0.5 text-xs text-ok">{running}</span>
        {/if}
      </button>
    {/each}
  </nav>

  <div class="mt-auto flex flex-col gap-1 px-2 pb-3">
    <button
      class="rounded-lg px-3 py-2 text-left text-sm text-mute hover:bg-hover hover:text-ink"
      onclick={() => void window.api.endAll()}>
      End All
    </button>
    <button
      class="rounded-lg px-3 py-2 text-left text-sm text-mute hover:bg-hover hover:text-ink"
      onclick={() => (ui.options = true)}>
      Options
    </button>
    <div class="px-3 pt-2 text-xs text-mute/60">v{app.version}</div>
  </div>
</aside>
