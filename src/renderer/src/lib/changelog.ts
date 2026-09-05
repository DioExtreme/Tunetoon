interface ChangelogBlock {
  kind: 'p' | 'li';
  text: string;
}

export function parseChangelog(text: string): ChangelogBlock[] {
  const out: ChangelogBlock[] = [];
  for (const raw of text.split('\n')) {
    const line = raw.replaceAll('**', '');
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) {
      continue;
    }
    const last = out[out.length - 1];
    if (trimmed.startsWith('- ')) {
      out.push({ kind: 'li', text: trimmed.slice(2) });
    } else if (line.startsWith('  ') && last?.kind === 'li') {
      last.text += ' ' + trimmed;
    } else {
      out.push({ kind: 'p', text: trimmed });
    }
  }
  return out;
}
