import { bunzipDecode } from './bunzip';

const MAGIC = Buffer.from('BSDIFF40', 'ascii');
const HEADER_SIZE = 32;
const CONTROL_ENTRY_SIZE = 24;

function readSignMagnitude(buffer: Buffer, position: number): number {
  const value = buffer.readUInt32LE(position) & 0x7fffffff;
  return (buffer[position + 7]! & 0x80) !== 0 ? -value : value;
}

export function applyBsPatch(oldData: Buffer, patch: Buffer): Buffer {
  if (patch.length < HEADER_SIZE || !patch.subarray(0, 8).equals(MAGIC)) {
    throw new Error('Not a BSDIFF40 patch.');
  }

  const controlLength = Number(patch.readBigInt64LE(8));
  const diffLength = Number(patch.readBigInt64LE(16));
  const newSize = Number(patch.readBigInt64LE(24));
  if (controlLength < 0 || diffLength < 0 || newSize < 0) {
    throw new Error('Corrupt patch header.');
  }

  const diffStart = HEADER_SIZE + controlLength;
  const extraStart = diffStart + diffLength;
  const control = bunzipDecode(patch.subarray(HEADER_SIZE, diffStart));
  const diff = bunzipDecode(patch.subarray(diffStart, extraStart));
  const extra = bunzipDecode(patch.subarray(extraStart));

  const output = Buffer.alloc(newSize);
  let controlPos = 0;
  let diffPos = 0;
  let extraPos = 0;
  let oldPos = 0;
  let newPos = 0;

  while (newPos < newSize) {
    if (controlPos + CONTROL_ENTRY_SIZE > control.length) {
      throw new Error('Corrupt patch data.');
    }
    const addLength = readSignMagnitude(control, controlPos);
    const copyLength = readSignMagnitude(control, controlPos + 8);
    const seekLength = readSignMagnitude(control, controlPos + 16);
    controlPos += CONTROL_ENTRY_SIZE;

    if (
      addLength < 0 ||
      copyLength < 0 ||
      newPos + addLength + copyLength > newSize ||
      diffPos + addLength > diff.length ||
      extraPos + copyLength > extra.length
    ) {
      throw new Error('Corrupt patch data.');
    }

    diff.copy(output, newPos, diffPos, diffPos + addLength);
    const overlapStart = Math.max(0, -oldPos);
    const overlapEnd = Math.min(addLength, oldData.length - oldPos);
    for (let i = overlapStart; i < overlapEnd; i++) {
      output[newPos + i] = (output[newPos + i]! + oldData[oldPos + i]!) & 0xff;
    }
    diffPos += addLength;
    newPos += addLength;
    oldPos += addLength;

    extra.copy(output, newPos, extraPos, extraPos + copyLength);
    extraPos += copyLength;
    newPos += copyLength;
    oldPos += seekLength;
  }

  return output;
}
