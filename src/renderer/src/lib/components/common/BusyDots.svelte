<script lang="ts">
  let { class: className = 'h-5' }: { class?: string } = $props();
</script>

<span class="inline-flex {className} items-center justify-center gap-1.5" aria-label="Working">
  <span class="size-1.5 animate-bounce rounded-full bg-white [animation-delay:-0.3s]"></span>
  <span class="size-1.5 animate-bounce rounded-full bg-white [animation-delay:-0.15s]"></span>
  <span class="size-1.5 animate-bounce rounded-full bg-white"></span>
</span>
