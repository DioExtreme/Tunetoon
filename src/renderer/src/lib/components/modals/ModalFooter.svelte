<script lang="ts">
  import DestructiveButton from '../common/DestructiveButton.svelte';

  let {
    confirmLabel = 'Save',
    confirmDisabled = false,
    cancelLabel = 'Cancel',
    submit = false,
    danger = false,
    onconfirm,
    oncancel,
    remove
  }: {
    confirmLabel?: string;
    confirmDisabled?: boolean;
    cancelLabel?: string;
    submit?: boolean;
    danger?: boolean;
    onconfirm?: () => void;
    oncancel: () => void;
    remove?: { label?: string; onclick: () => void } | undefined;
  } = $props();
</script>

<div class="mt-2 flex items-center {remove ? 'justify-between' : 'justify-end'} gap-2">
  {#if remove}
    <DestructiveButton label={remove.label ?? 'Delete'} armedLabel="Really delete?" onconfirm={remove.onclick} />
  {/if}
  <div class="flex gap-2">
    <button type="button" class="btn-secondary" onclick={oncancel}>{cancelLabel}</button>
    <button
      type={submit ? 'submit' : 'button'}
      class={danger ? 'btn-danger' : 'btn-primary'}
      disabled={confirmDisabled}
      onclick={onconfirm}>
      {confirmLabel}
    </button>
  </div>
</div>
