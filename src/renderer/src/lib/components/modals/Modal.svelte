<script lang="ts">
  import type { Snippet } from 'svelte';

  let {
    title,
    onclose,
    dismissable = true,
    wide = false,
    children
  }: {
    title: string;
    onclose?: (() => void) | undefined;
    dismissable?: boolean;
    wide?: boolean;
    children: Snippet;
  } = $props();

  const openModal = (dialog: HTMLDialogElement): void => {
    if (!dialog.open) {
      dialog.showModal();
    }
  };

  function onCancel(event: Event): void {
    event.preventDefault();
    if (dismissable) {
      onclose?.();
    }
  }

  let pressedBackdrop = false;

  function onBackdropPointerDown(event: PointerEvent): void {
    pressedBackdrop = event.target === event.currentTarget;
  }

  function onBackdropClick(event: MouseEvent): void {
    const onBackdrop = pressedBackdrop && event.target === event.currentTarget;
    pressedBackdrop = false;
    if (dismissable && onBackdrop) {
      onclose?.();
    }
  }
</script>

<dialog
  {@attach openModal}
  class="fixed inset-0 m-0 h-full max-h-none w-full max-w-none bg-transparent p-0 text-ink backdrop:bg-black/60 backdrop:backdrop-blur-[2px]"
  oncancel={onCancel}>
  <div
    class="flex h-full items-center justify-center"
    onpointerdown={onBackdropPointerDown}
    onclick={onBackdropClick}
    role="presentation">
    <div
      class="{wide
        ? 'w-[42rem]'
        : 'w-[28rem]'} relative max-h-[92vh] max-w-[94vw] overflow-y-auto rounded-xl border border-line bg-panel p-5 shadow-2xl">
      <div class="mb-4 pr-8">
        <h2 class="text-[1rem] font-semibold text-ink">{title}</h2>
      </div>
      {@render children()}
      {#if dismissable}
        <button
          class="absolute top-4 right-4 rounded px-2 py-0.5 text-mute hover:bg-hover hover:text-ink"
          onclick={() => onclose?.()}
          aria-label="Close">
          ✕
        </button>
      {/if}
    </div>
  </div>
</dialog>
