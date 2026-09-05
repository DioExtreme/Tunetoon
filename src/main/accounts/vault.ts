import { createCipheriv, createDecipheriv, randomBytes, scrypt } from 'crypto';

export const VAULT_VERSION = 3;

export type WrapHeader =
  | { wrap: 'device'; wrappedKey: string }
  | { wrap: 'password'; wrappedKey: string; salt: string; wrapNonce: string; wrapTag: string };

export type VaultEnvelope = WrapHeader & { version: typeof VAULT_VERSION; nonce: string; tag: string; data: string };

const SCRYPT_N = 1 << 15;
const SCRYPT_R = 8;
const SCRYPT_P = 1;
const SCRYPT_MAXMEM = 256 * 1024 * 1024;

function deriveKey(password: string, salt: Buffer): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    scrypt(password, salt, 32, { N: SCRYPT_N, r: SCRYPT_R, p: SCRYPT_P, maxmem: SCRYPT_MAXMEM }, (err, key) =>
      err ? reject(err) : resolve(key)
    );
  });
}

function aadOf(header: WrapHeader): Buffer {
  const passwordParts = header.wrap === 'password' ? `${header.salt}|${header.wrapNonce}|${header.wrapTag}` : '||';
  return Buffer.from(`${VAULT_VERSION}|${header.wrap}|${header.wrappedKey}|${passwordParts}`, 'utf8');
}

export function generateDataKey(): Buffer {
  return randomBytes(32);
}

export async function wrapKeyPassword(password: string, dataKey: Buffer): Promise<WrapHeader> {
  const salt = randomBytes(16);
  const nonce = randomBytes(12);
  const cipher = createCipheriv('aes-256-gcm', await deriveKey(password, salt), nonce);
  const wrapped = Buffer.concat([cipher.update(dataKey), cipher.final()]);
  return {
    wrap: 'password',
    wrappedKey: wrapped.toString('base64'),
    salt: salt.toString('base64'),
    wrapNonce: nonce.toString('base64'),
    wrapTag: cipher.getAuthTag().toString('base64')
  };
}

export async function unwrapKeyPassword(password: string, header: WrapHeader): Promise<Buffer> {
  if (header.wrap !== 'password' || !header.salt || !header.wrapNonce || !header.wrapTag) {
    throw new Error('Not a password-wrapped vault.');
  }
  const key = await deriveKey(password, Buffer.from(header.salt, 'base64'));
  const decipher = createDecipheriv('aes-256-gcm', key, Buffer.from(header.wrapNonce, 'base64'));
  decipher.setAuthTag(Buffer.from(header.wrapTag, 'base64'));
  return Buffer.concat([decipher.update(Buffer.from(header.wrappedKey, 'base64')), decipher.final()]);
}

export function sealVault(header: WrapHeader, dataKey: Buffer, plaintext: string): VaultEnvelope {
  const nonce = randomBytes(12);
  const cipher = createCipheriv('aes-256-gcm', dataKey, nonce);
  cipher.setAAD(aadOf(header));
  const data = Buffer.concat([cipher.update(plaintext, 'utf8'), cipher.final()]);
  return {
    version: VAULT_VERSION,
    ...header,
    nonce: nonce.toString('base64'),
    tag: cipher.getAuthTag().toString('base64'),
    data: data.toString('base64')
  };
}

export function openVault(envelope: VaultEnvelope, dataKey: Buffer): string {
  const decipher = createDecipheriv('aes-256-gcm', dataKey, Buffer.from(envelope.nonce, 'base64'));
  decipher.setAAD(aadOf(envelope));
  decipher.setAuthTag(Buffer.from(envelope.tag, 'base64'));
  return Buffer.concat([decipher.update(Buffer.from(envelope.data, 'base64')), decipher.final()]).toString('utf8');
}
