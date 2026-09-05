import { parentPort } from 'worker_threads';
import { readFileSync, writeFileSync } from 'fs';
import { createHash } from 'crypto';
import { bunzipDecode } from './bunzip';
import { applyBsPatch } from './bspatch';
import { PATCH_TMP_SUFFIX } from '../types';

export interface WorkerJob {
  op: 'bunzip' | 'bspatch' | 'sha1';
  input: string;
  output?: string;
}

export interface WorkerReply {
  ok: boolean;
  sha1?: string;
  error?: string;
}

const sha1Of = (data: Buffer): string => createHash('sha1').update(data).digest('hex');

parentPort!.on('message', (job: WorkerJob) => {
  const reply: WorkerReply = { ok: true };
  try {
    if (job.op === 'sha1') {
      reply.sha1 = sha1Of(readFileSync(job.input));
    } else {
      const bunzip = job.op === 'bunzip';
      const outputPath = bunzip ? job.output! : job.input + PATCH_TMP_SUFFIX;
      const out = bunzip
        ? bunzipDecode(readFileSync(job.input))
        : applyBsPatch(readFileSync(job.input), readFileSync(job.output!));
      writeFileSync(outputPath, out);
      reply.sha1 = sha1Of(out);
    }
  } catch (err) {
    reply.ok = false;
    reply.error = (err as Error).message;
  }
  parentPort!.postMessage(reply);
});
