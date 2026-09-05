import { setTimeout as delay } from 'timers/promises';
import type { RewrittenAccount } from '@shared/types';
import { postForm } from '../util/http';
import { errorMessage } from '../util/errors';

const LOGIN_URL = 'https://www.toontownrewritten.com/api/login?format=json';

interface RewrittenLoginResponse {
  success: 'true' | 'false' | 'partial' | 'delayed';
  gameserver?: string;
  cookie?: string;
  banner?: string;
  responseToken?: string;
  queueToken?: string;
  eta?: string;
}

export type RewrittenOutcome =
  | { kind: 'ok'; gameserver: string; cookie: string }
  | { kind: 'fail'; reason: string }
  | { kind: 'two-step'; responseToken: string; toonguard: boolean };

const MAX_QUEUE_POLLS = 300;
const MAX_QUEUE_WAIT_SEC = 10;
const DEFAULT_QUEUE_WAIT_MS = 1500;

function queueWaitMs(eta: string | undefined): number {
  const etaSeconds = Number(eta);
  return Number.isFinite(etaSeconds) && etaSeconds > 0
    ? Math.min(etaSeconds, MAX_QUEUE_WAIT_SEC) * 1000
    : DEFAULT_QUEUE_WAIT_MS;
}

async function pollUntilResolved(first: RewrittenLoginResponse, signal?: AbortSignal): Promise<RewrittenOutcome> {
  let response = first;
  for (let polls = 0; response.success === 'delayed'; polls++) {
    if (polls >= MAX_QUEUE_POLLS) {
      return { kind: 'fail', reason: 'Gave up waiting in the login queue.' };
    }
    await delay(queueWaitMs(response.eta), undefined, { signal });
    response = await postForm<RewrittenLoginResponse>(LOGIN_URL, { queueToken: response.queueToken ?? '' }, { signal });
  }
  switch (response.success) {
    case 'true':
      return { kind: 'ok', gameserver: response.gameserver ?? '', cookie: response.cookie ?? '' };
    case 'false':
      return { kind: 'fail', reason: response.banner || 'Login rejected.' };
    case 'partial':
      return {
        kind: 'two-step',
        responseToken: response.responseToken ?? '',
        toonguard: (response.banner ?? '').includes('ToonGuard')
      };
    default:
      return { kind: 'fail', reason: 'Unexpected response from the login API.' };
  }
}

async function attempt(fields: Record<string, string>, signal?: AbortSignal): Promise<RewrittenOutcome> {
  try {
    return await pollUntilResolved(await postForm<RewrittenLoginResponse>(LOGIN_URL, fields, { signal }), signal);
  } catch (err) {
    return { kind: 'fail', reason: `Could not reach the TTR login server (${errorMessage(err)}).` };
  }
}

export function loginRewritten(account: RewrittenAccount, signal?: AbortSignal): Promise<RewrittenOutcome> {
  return attempt({ username: account.username, password: account.password }, signal);
}

export function continueRewrittenTwoStep(
  responseToken: string,
  code: string,
  signal?: AbortSignal
): Promise<RewrittenOutcome> {
  return attempt({ appToken: code, authToken: responseToken }, signal);
}
