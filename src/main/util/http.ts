const USER_AGENT = 'Tunetoon - A multi-toon launcher. (https://github.com/DioExtreme/Tunetoon)';

const TIMEOUT_MS = 5_000;

export function baseHeaders(extra?: Record<string, string>): Record<string, string> {
  return { 'User-Agent': USER_AGENT, ...extra };
}

function assertOk(res: Response, method: string, url: string): void {
  if (!res.ok) {
    throw new Error(`${method} ${url} failed: ${res.status}`);
  }
}

function withTimeout(signal?: AbortSignal): AbortSignal {
  const timeout = AbortSignal.timeout(TIMEOUT_MS);
  return signal ? AbortSignal.any([signal, timeout]) : timeout;
}

export async function getText(url: string): Promise<string> {
  const res = await fetch(url, { headers: baseHeaders(), signal: withTimeout() });
  assertOk(res, 'GET', url);
  return res.text();
}

export async function getJson<T>(url: string, signal: AbortSignal): Promise<T> {
  const res = await fetch(url, { headers: baseHeaders(), signal: withTimeout(signal) });
  assertOk(res, 'GET', url);
  return (await res.json()) as T;
}

export async function postForm<T>(
  url: string,
  fields: Record<string, string>,
  opts: { headers?: Record<string, string> | undefined; signal?: AbortSignal | undefined } = {}
): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    headers: baseHeaders(opts.headers),
    body: new URLSearchParams(fields),
    signal: withTimeout(opts.signal)
  });
  try {
    return (await res.json()) as T;
  } catch {
    assertOk(res, 'POST', url);
    throw new Error(`POST ${url} returned malformed JSON.`);
  }
}
