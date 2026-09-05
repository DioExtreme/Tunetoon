interface Reorder {
  isTarget(index: number): boolean;
  begin(index: number): void;
  hover(index: number): void;
  finish(ids: string[], apply: boolean): void;
}

export function createReorder(commit: (orderedIds: string[]) => void): Reorder {
  let from = $state<number | null>(null);
  let to = $state<number | null>(null);
  return {
    isTarget: (index) => to === index && from !== null && from !== index,
    begin(index) {
      from = index;
      to = index;
    },
    hover(index) {
      to = index;
    },
    finish(ids, apply) {
      if (apply && from !== null && to !== null && from !== to) {
        const reordered = [...ids];
        const [moved] = reordered.splice(from, 1);
        reordered.splice(to, 0, moved!);
        commit(reordered);
      }
      from = null;
      to = null;
    }
  };
}
