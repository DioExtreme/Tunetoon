<script lang="ts">
  import { app, ui } from './lib/state.svelte';
  import Sidebar from './lib/components/frame/Sidebar.svelte';
  import AccountList from './lib/components/accounts/AccountList.svelte';
  import GroupsPanel from './lib/components/groups/GroupsPanel.svelte';
  import PlayBar from './lib/components/frame/PlayBar.svelte';
  import Toasts from './lib/components/frame/Toasts.svelte';
  import GroupEditModal from './lib/components/modals/GroupEditModal.svelte';
  import AccountEditModal from './lib/components/modals/AccountEditModal.svelte';
  import OptionsModal from './lib/components/modals/OptionsModal.svelte';
  import TwoStepModal from './lib/components/modals/TwoStepModal.svelte';
  import UnlockModal from './lib/components/modals/UnlockModal.svelte';
  import SetPasswordModal from './lib/components/modals/SetPasswordModal.svelte';
  import ImportModal from './lib/components/modals/ImportModal.svelte';
  import ConfirmCloseModal from './lib/components/modals/ConfirmCloseModal.svelte';
  import ChangelogModal from './lib/components/modals/ChangelogModal.svelte';
</script>

<div class="flex h-full flex-col">
  {#if app.ready}
    <div class="flex min-h-0 flex-1">
      <Sidebar />
      <main class="flex min-w-0 flex-1 flex-col bg-base">
        <GroupsPanel />
        <AccountList />
        <PlayBar />
      </main>
    </div>

    {#if app.locked}
      <UnlockModal />
    {:else}
      {#if ui.edit}
        <AccountEditModal account={ui.edit === 'new' ? null : ui.edit} onclose={() => (ui.edit = null)} />
      {/if}

      {#if ui.options}
        <OptionsModal onclose={() => (ui.options = false)} />
      {/if}

      {#if ui.setupPassword}
        <SetPasswordModal onclose={() => (ui.setupPassword = false)} />
      {/if}

      {#if ui.import}
        <ImportModal folder={ui.import} onclose={() => (ui.import = null)} />
      {/if}

      {#if ui.group}
        <GroupEditModal group={ui.group === 'new' ? null : ui.group} onclose={() => (ui.group = null)} />
      {/if}

      {#if ui.changelog}
        <ChangelogModal text={ui.changelog} onclose={() => (ui.changelog = null)} />
      {/if}
    {/if}

    {#if app.twoStepRequests.length > 0}
      <TwoStepModal />
    {/if}

    {#if ui.confirmClose}
      <ConfirmCloseModal onclose={() => (ui.confirmClose = false)} />
    {/if}

    <Toasts />
  {/if}
</div>
