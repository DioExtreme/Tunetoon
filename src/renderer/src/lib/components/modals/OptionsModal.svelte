<script lang="ts">
  import { app, importAccounts, patchConfig, refreshAccounts, toast, ui } from '../../state.svelte';
  import Modal from './Modal.svelte';
  import DestructiveButton from '../common/DestructiveButton.svelte';

  let { onclose }: { onclose: () => void } = $props();

  type Folder = { key: 'rewrittenPath' | 'clashPath'; label: string; title: string };

  const FOLDERS: Folder[] = [
    { key: 'rewrittenPath', label: 'Rewritten', title: "Select Toontown Rewritten's folder" },
    { key: 'clashPath', label: 'Clash', title: "Select Corporate Clash's folder" }
  ];

  type Flag = { key: 'skipUpdates' | 'protectGameSettings' | 'globalEndAll' | 'hideUsernames'; caption: string };

  const BEHAVIOR_FLAGS: Flag[] = [
    { key: 'skipUpdates', caption: 'Skip game updates' },
    { key: 'protectGameSettings', caption: 'Protect game settings while launching' },
    { key: 'globalEndAll', caption: '"End All" ends games on both servers' }
  ];

  async function setPath(folder: Folder): Promise<void> {
    const picked = await window.api.pickFolder(folder.title);
    if (picked) {
      await patchConfig({ [folder.key]: picked });
    }
  }

  async function exportAccounts(): Promise<void> {
    if (await window.api.exportAccounts()) {
      toast('success', 'Accounts exported.');
    }
  }

  async function removePassword(): Promise<void> {
    await window.api.disableEncryption();
    await refreshAccounts();
    toast('info', 'Password removed. Accounts stay encrypted by your system.');
  }
</script>

{#snippet flagRows(flags: Flag[])}
  {#each flags as flag (flag.key)}
    <label class="flex items-center gap-2 text-sm">
      <input
        type="checkbox"
        class="size-4 accent-accent"
        checked={app.config[flag.key]}
        onchange={(e) => void patchConfig({ [flag.key]: e.currentTarget.checked })} />
      {flag.caption}
    </label>
  {/each}
{/snippet}

<Modal title="Options" {onclose}>
  <div class="flex flex-col gap-4">
    <div class="flex flex-col gap-2">
      <span class="section-heading">Game folders</span>
      {#each FOLDERS as folder (folder.key)}
        <div class="flex items-center gap-2">
          <span class="w-20 shrink-0 hint">{folder.label}</span>
          <input
            class="field-input min-w-0 flex-1 px-2 py-1.5 text-xs"
            readonly
            value={app.config[folder.key]}
            title={app.config[folder.key]} />
          <button class="btn-secondary px-2 py-1 text-xs" onclick={() => void setPath(folder)}> Browse… </button>
        </div>
      {/each}
    </div>

    <div class="flex flex-col gap-2">
      <span class="section-heading">Behavior</span>
      {@render flagRows(BEHAVIOR_FLAGS)}
    </div>

    <div class="flex flex-col gap-2">
      <span class="section-heading">Security</span>
      {@render flagRows([{ key: 'hideUsernames', caption: 'Hide usernames in the main window' }])}
      <p class="text-sm text-mute">
        {app.passwordProtected
          ? 'Accounts are encrypted with your password.'
          : "Accounts are encrypted by your system's encryption service."}
      </p>
      {#if app.passwordProtected}
        <DestructiveButton
          class="self-start"
          label="Remove password"
          armedLabel="Really remove the password?"
          onconfirm={() => void removePassword()} />
      {:else}
        <button class="btn-secondary self-start" onclick={() => (ui.setupPassword = true)}>
          Protect with a password
        </button>
      {/if}
    </div>

    <div class="flex flex-col gap-2">
      <span class="section-heading">Data</span>
      <div class="flex gap-2">
        <button class="btn-secondary" onclick={() => void exportAccounts()}> Export accounts </button>
        <button class="btn-secondary" onclick={() => void importAccounts()}> Import accounts </button>
      </div>
      <p class="hint">
        Export writes a plain file with all accounts, for moving to another PC. Import takes an exported file, or
        Tunetoon v2 account files.
      </p>
    </div>
  </div>
</Modal>
