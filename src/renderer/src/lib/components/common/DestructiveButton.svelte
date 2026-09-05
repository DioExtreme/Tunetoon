<script lang="ts">
  let {
    label,
    armedLabel,
    class: className = '',
    onconfirm
  }: { label: string; armedLabel: string; class?: string; onconfirm: () => void } = $props();

  let armed = $state(false);
</script>

<button
  type="button"
  class="btn-danger {className}"
  onclick={() => {
    if (armed) {
      onconfirm();
    } else {
      armed = true;
    }
  }}>
  {armed ? armedLabel : label}
</button>
