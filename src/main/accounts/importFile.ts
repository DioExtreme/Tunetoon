import { MAIN_MENU_SLOT } from '@shared/types';
import type { AccountGroup, ClashAccount, RewrittenAccount } from '@shared/types';

interface ParsedAccountExport {
  rewritten: RewrittenAccount[];
  clash: ClashAccount[];
  groups: AccountGroup[];
}

type Raw = Record<string, unknown>;

const asString = (value: unknown): string => (typeof value === 'string' ? value : '');

function hasIdAndUsername(entry: Raw): boolean {
  return typeof entry['id'] === 'string' && typeof entry['username'] === 'string';
}

function listOf(value: unknown): Raw[] {
  const list = value ?? [];
  if (!Array.isArray(list)) {
    throw new Error('Not a Tunetoon account export.');
  }
  return list.filter((entry): entry is Raw => entry !== null && typeof entry === 'object');
}

export function parseAccountExport(parsed: unknown): ParsedAccountExport {
  const source = (parsed && typeof parsed === 'object' ? parsed : {}) as Raw;
  const rewrittenIn = listOf(source['rewritten']);
  const clashIn = listOf(source['clash']);
  const groupsIn = listOf(source['groups']);

  const rewritten = rewrittenIn
    .filter(hasIdAndUsername)
    .map((a): RewrittenAccount => ({
      id: a['id'] as string,
      server: 'rewritten',
      toon: asString(a['toon']),
      username: a['username'] as string,
      password: asString(a['password']),
      loginWanted: a['loginWanted'] === true
    }));
  const clash = clashIn
    .filter(hasIdAndUsername)
    .map((a): ClashAccount => ({
      id: a['id'] as string,
      server: 'clash',
      toon: asString(a['toon']),
      username: a['username'] as string,
      password: asString(a['password']),
      loginWanted: a['loginWanted'] === true,
      loginToken: typeof a['loginToken'] === 'string' ? a['loginToken'] : null,
      toonSlot: typeof a['toonSlot'] === 'number' ? a['toonSlot'] : MAIN_MENU_SLOT
    }));
  const groups = groupsIn
    .filter((g) => typeof g['id'] === 'string')
    .map((g): AccountGroup => ({
      id: g['id'] as string,
      server: g['server'] === 'clash' ? 'clash' : 'rewritten',
      name: asString(g['name']),
      members: Array.isArray(g['members'])
        ? (g['members'] as unknown[]).filter((m): m is string => typeof m === 'string')
        : [],
      layout:
        g['layout'] && typeof g['layout'] === 'object' && !Array.isArray(g['layout'])
          ? (g['layout'] as AccountGroup['layout'])
          : {}
    }));

  return { rewritten, clash, groups };
}
