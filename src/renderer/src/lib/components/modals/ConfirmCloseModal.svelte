<script lang="ts">
  import Modal from './Modal.svelte';
  import ModalFooter from './ModalFooter.svelte';

  let { onclose }: { onclose: () => void } = $props();
</script>

<Modal title="Close Tunetoon?" {onclose}>
  <div class="flex flex-col gap-3">
    <p class="hint">Running games will close together with the launcher.</p>
    <ModalFooter
      confirmLabel="Close anyway"
      danger
      onconfirm={() => void window.api.confirmClose()}
      oncancel={onclose} />
  </div>
</Modal>
