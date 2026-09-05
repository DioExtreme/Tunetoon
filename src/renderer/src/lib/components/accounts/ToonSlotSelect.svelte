<script lang="ts">
  import type { ClashToonInfo, PublicClashAccount } from '@shared/types';
  import { MAIN_MENU_SLOT } from '@shared/types';
  import { getClashToons } from '../../clashToons.svelte';

  let { account }: { account: PublicClashAccount } = $props();

  let toons = $state<ClashToonInfo[]>([]);

  $effect(() => {
    if (account.username) {
      void getClashToons(account.username).then((result) => {
        toons = result;
      });
    }
  });

  const slotOptions = $derived.by(() => {
    const options = [{ slot: MAIN_MENU_SLOT, name: 'Main Menu' }, ...toons];
    if (!options.some((o) => o.slot === account.toonSlot)) {
      options.push({ slot: account.toonSlot, name: `Slot ${account.toonSlot}` });
    }
    return options;
  });
</script>

<select
  class="field-input w-32 shrink-0 bg-panel px-2 py-1 text-xs"
  title="Toon slot to enter with"
  value={String(account.toonSlot)}
  onchange={(e) => void window.api.setToonSlot(account.id, Number(e.currentTarget.value))}>
  {#each slotOptions as option (option.slot)}
    <option value={String(option.slot)}>{option.name}</option>
  {/each}
</select>
