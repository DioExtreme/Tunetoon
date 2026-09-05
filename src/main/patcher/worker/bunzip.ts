/*
 * Port of seek-bzip 2.0.0 (https://github.com/cscott/seek-bzip),
 * stripped to the bare minimum for Tunetoon with some performance
 * enhancements.
 *
 * Original licenses (MIT):
 *
 * Copyright (C) 2013 C. Scott Ananian
 * Copyright (C) 2012 Eli Skeggs
 * Copyright (C) 2011 Kevin Kwok
 * CRC32 ported from jbzip2, Copyright (C) 2011 Matthew Francis
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 * Based on micro-bunzip by Rob Landley, in turn based on bzip2 decompression
 * code by Julian R Seward.
 */

const MAX_HUFCODE_BITS = 20;
const MAX_SYMBOLS = 258;
const SYMBOL_RUNA = 0;
const SYMBOL_RUNB = 1;
const MIN_GROUPS = 2;
const MAX_GROUPS = 6;
const GROUP_SIZE = 50;

const MASKS = new Int32Array(25);
for (let i = 1; i <= 24; i++) MASKS[i] = (1 << i) - 1;

class BitReader {
  private pos: number;
  private bitBuf = 0;
  private bitCount = 0;

  constructor(
    private readonly buf: Buffer,
    pos: number
  ) {
    this.pos = pos;
  }

  read(bits: number): number {
    while (this.bitCount < bits) {
      if (this.pos >= this.buf.length) throw new Error('Unexpected input EOF');
      this.bitBuf = (this.bitBuf << 8) | this.buf[this.pos++]!;
      this.bitCount += 8;
    }
    this.bitCount -= bits;
    return (this.bitBuf >>> this.bitCount) & MASKS[bits]!;
  }

  peek(bits: number): number {
    while (this.bitCount < bits) {
      if (this.pos >= this.buf.length) throw new Error('Unexpected input EOF');
      this.bitBuf = (this.bitBuf << 8) | this.buf[this.pos++]!;
      this.bitCount += 8;
    }
    return (this.bitBuf >>> (this.bitCount - bits)) & MASKS[bits]!;
  }

  consume(bits: number): void {
    this.bitCount -= bits;
  }
}

class OutputBuffer {
  private buf: Buffer;
  private pos = 0;

  constructor(initialSize: number) {
    this.buf = Buffer.allocUnsafe(initialSize);
  }

  writeRun(byte: number, count: number): void {
    const need = this.pos + count;
    if (need > this.buf.length) {
      let size = this.buf.length * 2;
      while (size < need) size *= 2;
      const next = Buffer.allocUnsafe(size);
      this.buf.copy(next, 0, 0, this.pos);
      this.buf = next;
    }
    if (count === 1) {
      this.buf[this.pos++] = byte;
    } else {
      this.buf.fill(byte, this.pos, need);
      this.pos = need;
    }
  }

  take(): Buffer {
    return this.buf.subarray(0, this.pos);
  }
}

interface HufGroup {
  permute: Uint16Array;
  limit: Uint32Array;
  base: Uint32Array;
  minLen: number;
  maxLen: number;
}

function mtf(array: Uint8Array, index: number): number {
  const src = array[index]!;
  if (index > 16) {
    array.copyWithin(1, 0, index);
  } else {
    for (let i = index; i > 0; i--) array[i] = array[i - 1]!;
  }
  array[0] = src;
  return src;
}

function corrupt(detail?: string): never {
  throw new Error(detail ? `Corrupt bzip data: ${detail}` : 'Corrupt bzip data');
}

function decodeBlock(reader: BitReader, dbuf: Uint32Array, dbufSize: number, out: OutputBuffer): void {
  if (reader.read(1)) throw new Error('Obsolete (pre 0.9.5) bzip format not supported');
  const origPointer = reader.read(24);
  if (origPointer > dbufSize) corrupt('initial position out of bounds');

  const symToByte = new Uint8Array(256);
  let symTotal = 0;
  const present = reader.read(16);
  for (let i = 0; i < 16; i++) {
    if (present & (1 << (0xf - i))) {
      const o = i * 16;
      const bits = reader.read(16);
      for (let j = 0; j < 16; j++) {
        if (bits & (1 << (0xf - j))) symToByte[symTotal++] = o + j;
      }
    }
  }

  const groupCount = reader.read(3);
  if (groupCount < MIN_GROUPS || groupCount > MAX_GROUPS) corrupt();
  const nSelectors = reader.read(15);
  if (nSelectors === 0) corrupt();

  const mtfSymbol = new Uint8Array(256);
  for (let i = 0; i < groupCount; i++) mtfSymbol[i] = i;
  const selectors = new Uint8Array(nSelectors);
  for (let i = 0; i < nSelectors; i++) {
    let j = 0;
    while (reader.read(1)) {
      if (j >= groupCount) corrupt();
      j++;
    }
    selectors[i] = mtf(mtfSymbol, j);
  }

  const symCount = symTotal + 2;
  const groups: HufGroup[] = [];
  for (let g = 0; g < groupCount; g++) {
    const length = new Uint8Array(symCount);
    const temp = new Uint16Array(MAX_HUFCODE_BITS + 1);
    let len = reader.read(5);
    for (let i = 0; i < symCount; i++) {
      for (;;) {
        if (len < 1 || len > MAX_HUFCODE_BITS) corrupt();
        if (!reader.read(1)) break;
        if (!reader.read(1)) len++;
        else len--;
      }
      length[i] = len;
    }

    let minLen = length[0]!;
    let maxLen = length[0]!;
    for (let i = 1; i < symCount; i++) {
      const l = length[i]!;
      if (l > maxLen) maxLen = l;
      else if (l < minLen) minLen = l;
    }

    const hufGroup: HufGroup = {
      permute: new Uint16Array(MAX_SYMBOLS),
      limit: new Uint32Array(MAX_HUFCODE_BITS + 2),
      base: new Uint32Array(MAX_HUFCODE_BITS + 1),
      minLen,
      maxLen
    };
    groups.push(hufGroup);

    let pp = 0;
    for (let i = minLen; i <= maxLen; i++) {
      temp[i] = 0;
      for (let s = 0; s < symCount; s++) {
        if (length[s] === i) hufGroup.permute[pp++] = s;
      }
    }
    for (let i = 0; i < symCount; i++) {
      temp[length[i]!] = temp[length[i]!]! + 1;
    }
    pp = 0;
    let cumulative = 0;
    for (let i = minLen; i < maxLen; i++) {
      pp += temp[i]!;
      hufGroup.limit[i] = pp - 1;
      pp <<= 1;
      cumulative += temp[i]!;
      hufGroup.base[i + 1] = pp - cumulative;
    }
    hufGroup.limit[maxLen] = pp + temp[maxLen]! - 1;
    hufGroup.base[minLen] = 0;
  }

  const byteCount = new Uint32Array(256);
  for (let i = 0; i < 256; i++) mtfSymbol[i] = i;
  let runPos = 0;
  let runLen = 0;
  let dbufCount = 0;
  let selector = 0;
  let symbolsInGroup = 0;
  let limit!: Uint32Array;
  let base!: Uint32Array;
  let permute!: Uint16Array;
  let minLen = 0;
  let maxLen = 0;
  for (;;) {
    if (symbolsInGroup === 0) {
      if (selector >= nSelectors) corrupt();
      symbolsInGroup = GROUP_SIZE;
      const g = groups[selectors[selector++]!]!;
      limit = g.limit;
      base = g.base;
      permute = g.permute;
      minLen = g.minLen;
      maxLen = g.maxLen;
    }
    symbolsInGroup--;

    const w = reader.peek(maxLen);
    let codeLen = minLen;
    for (; ; codeLen++) {
      if (codeLen > maxLen) corrupt();
      if (w >>> (maxLen - codeLen) <= limit[codeLen]!) break;
    }
    reader.consume(codeLen);
    const j = (w >>> (maxLen - codeLen)) - base[codeLen]!;
    if (j < 0 || j >= MAX_SYMBOLS) corrupt();
    const nextSym = permute[j]!;

    if (nextSym === SYMBOL_RUNA || nextSym === SYMBOL_RUNB) {
      if (!runPos) {
        runPos = 1;
        runLen = 0;
      }
      runLen += nextSym === SYMBOL_RUNA ? runPos : 2 * runPos;
      runPos <<= 1;
      continue;
    }
    if (runPos) {
      runPos = 0;
      if (dbufCount + runLen > dbufSize) corrupt();
      const uc = symToByte[mtfSymbol[0]!]!;
      byteCount[uc] = byteCount[uc]! + runLen;
      while (runLen--) dbuf[dbufCount++] = uc;
    }
    if (nextSym > symTotal) break;
    if (dbufCount >= dbufSize) corrupt();
    const uc = symToByte[mtf(mtfSymbol, nextSym - 1)]!;
    byteCount[uc] = byteCount[uc]! + 1;
    dbuf[dbufCount++] = uc;
  }
  if (origPointer >= dbufCount) corrupt();

  let total = 0;
  for (let i = 0; i < 256; i++) {
    const next = total + byteCount[i]!;
    byteCount[i] = total;
    total = next;
  }
  for (let i = 0; i < dbufCount; i++) {
    const uc = dbuf[i]! & 0xff;
    const idx = byteCount[uc]!;
    dbuf[idx] = dbuf[idx]! | (i << 8);
    byteCount[uc] = idx + 1;
  }

  let pos = dbuf[origPointer]!;
  let current = pos & 0xff;
  pos >>= 8;
  let run = -1;
  let remaining = dbufCount;
  while (remaining) {
    remaining--;
    const previous = current;
    pos = dbuf[pos]!;
    current = pos & 0xff;
    pos >>= 8;
    let copies: number;
    let outbyte: number;
    if (run++ === 3) {
      copies = current;
      outbyte = previous;
      current = -1;
    } else {
      copies = 1;
      outbyte = current;
    }
    out.writeRun(outbyte, copies);
    if (current !== previous) run = 0;
  }
}

let scratch = new Uint32Array(0);

export function bunzipDecode(input: Buffer): Buffer {
  if (input.length < 4 || input[0] !== 0x42 || input[1] !== 0x5a || input[2] !== 0x68) {
    throw new Error('Not bzip data: bad magic');
  }
  const level = input[3]! - 0x30;
  if (level < 1 || level > 9) throw new Error('Not bzip data: level out of range');

  const reader = new BitReader(input, 4);
  const dbufSize = 100000 * level;
  if (scratch.length < dbufSize) {
    scratch = new Uint32Array(dbufSize);
  }
  const dbuf = scratch;
  const out = new OutputBuffer(Math.max(1 << 16, input.length));
  for (;;) {
    const h1 = reader.read(24);
    const h2 = reader.read(24);
    if (h1 === 0x177245 && h2 === 0x385090) return out.take();
    if (h1 !== 0x314159 || h2 !== 0x265359) corrupt('bad block header');
    reader.read(16);
    reader.read(16);
    decodeBlock(reader, dbuf, dbufSize, out);
  }
}
