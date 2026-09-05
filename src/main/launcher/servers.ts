import path from 'path';
import type { Account, AppConfig, ClashAccount, ServerId } from '@shared/types';
import { MAIN_MENU_SLOT } from '@shared/types';
import { IS_MAC } from '../util/platform';
import type { Patcher } from '../patcher/types';
import { RewrittenPatcher } from '../patcher/rewritten';
import { ClashPatcher } from '../patcher/clash';
import type { PatchWorkerPool } from '../patcher/worker/workerPool';
import type { LoginSession } from './game';

interface ServerDescriptor {
  exe: string;
  createPatcher(workers: PatchWorkerPool): Patcher;
  env(account: Account, session: LoginSession): Record<string, string>;
}

export const SERVERS: Record<ServerId, ServerDescriptor> = {
  rewritten: {
    exe: IS_MAC ? 'Toontown Rewritten' : 'TTREngine64.exe',
    createPatcher: (workers) => new RewrittenPatcher(workers),
    env: (_account, session) => ({ TTR_GAMESERVER: session.gameserver, TTR_PLAYCOOKIE: session.cookie })
  },
  clash: {
    exe: IS_MAC ? path.join('CorporateClash.app', 'Contents', 'MacOS', 'corporateclash') : 'CorporateClash.exe',
    createPatcher: (workers) => new ClashPatcher(workers),
    env: (account, session) => {
      const clash = account as ClashAccount;
      const env: Record<string, string> = {
        TT_GAMESERVER: session.gameserver,
        TT_PLAYCOOKIE: session.cookie,
        LAUNCHER_USER: clash.username,
        REALM: 'production',
        SENTRY_ENVIRONMENT: 'production'
      };
      if (clash.toonSlot !== MAIN_MENU_SLOT) {
        env.FORCE_TOON_SLOT = String(clash.toonSlot);
      }
      return env;
    }
  }
};

export function gamePathFor(server: ServerId, config: AppConfig): string {
  return server === 'clash' ? config.clashPath : config.rewrittenPath;
}
