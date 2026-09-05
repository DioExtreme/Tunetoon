import { randomBytes } from 'crypto';
import type { ClashAccount } from '@shared/types';
import { postForm } from '../util/http';
import { errorMessage } from '../util/errors';
import { CLASH_URLS } from '../clash/urls';

const LAUNCHER_API = `${CLASH_URLS.api}/api/launcher/v1`;
const TOKEN_ATTEMPTS = 2;

interface ClashRegisterResponse {
  status: boolean;
  message?: string;
  token?: string;
}

interface ClashLoginResponse {
  status: boolean;
  toonstep: boolean;
  message?: string;
  token?: string;
  bad_token?: boolean;
}

export type ClashOutcome = { kind: 'ok'; cookie: string } | { kind: 'fail'; reason: string } | { kind: 'toonstep' };

type TokenResult = { ok: true; token: string } | { ok: false; error: string };

async function requestLauncherToken(username: string, password: string, signal?: AbortSignal): Promise<TokenResult> {
  const response = await postForm<ClashRegisterResponse>(
    `${LAUNCHER_API}/register`,
    { username, password, friendly: 'Tunetoon-' + randomBytes(12).toString('base64url') },
    { signal }
  );
  if (response.status && response.token) {
    return { ok: true, token: response.token };
  }
  return { ok: false, error: response.message || 'Corporate Clash rejected the account credentials.' };
}

type ClashValidation = { ok: true; token: string | null; warning?: string } | { ok: false; error: string };

export async function validateClashCredentials(username: string, password: string): Promise<ClashValidation> {
  try {
    const registration = await requestLauncherToken(username, password);
    return registration.ok ? { ok: true, token: registration.token } : registration;
  } catch {
    return { ok: true, token: null, warning: 'Could not reach Corporate Clash to verify the account. Saved anyway.' };
  }
}

export function revokeLauncherToken(loginToken: string): void {
  void postForm(`${LAUNCHER_API}/revoke_self`, {}, { headers: { Authorization: `Bearer ${loginToken}` } }).catch(
    () => {}
  );
}

type EnsureTokenResult = { ok: true; token: string } | { ok: false; reason: string };

async function ensureToken(account: ClashAccount, signal?: AbortSignal): Promise<EnsureTokenResult> {
  if (account.loginToken) {
    return { ok: true, token: account.loginToken };
  }
  const registration = await requestLauncherToken(account.username, account.password, signal);
  if (!registration.ok) {
    account.loginToken = null;
    return { ok: false, reason: registration.error };
  }
  account.loginToken = registration.token;
  return { ok: true, token: registration.token };
}

export async function loginClash(account: ClashAccount, signal?: AbortSignal): Promise<ClashOutcome> {
  try {
    for (let attempt = 0; attempt < TOKEN_ATTEMPTS; attempt++) {
      const tokenResult = await ensureToken(account, signal);
      if (!tokenResult.ok) {
        return { kind: 'fail', reason: tokenResult.reason };
      }

      const response = await postForm<ClashLoginResponse>(
        `${LAUNCHER_API}/login`,
        {},
        { headers: { Authorization: `Bearer ${tokenResult.token}`, 'x-realm': 'production' }, signal }
      );
      if (response.status && response.token) {
        return { kind: 'ok', cookie: response.token };
      }
      if (response.toonstep) {
        return { kind: 'toonstep' };
      }
      if (response.bad_token) {
        account.loginToken = null;
        continue;
      }
      return { kind: 'fail', reason: response.message || 'Corporate Clash login failed.' };
    }
    return { kind: 'fail', reason: 'Corporate Clash rejected the launcher token.' };
  } catch (err) {
    return { kind: 'fail', reason: `Could not reach Corporate Clash (${errorMessage(err)}).` };
  }
}
