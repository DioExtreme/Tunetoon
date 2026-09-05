import type { Worker } from 'worker_threads';
import createPatchWorker from './patchWorker?nodeWorker';
import type { WorkerJob, WorkerReply } from './patchWorker';

type ReplyResolver = (reply: WorkerReply) => void;

const SHUTDOWN_REPLY: WorkerReply = { ok: false, error: 'The launcher is shutting down.' };

export class PatchWorkerPool {
  private workers: Worker[] = [];
  private idle: Worker[] = [];
  private queue: Array<{ job: WorkerJob; pending: ReplyResolver }> = [];
  private pendingByWorker = new Map<Worker, ReplyResolver>();
  private destroyed = false;

  constructor(readonly size: number) {}

  private onReply(worker: Worker, reply: WorkerReply): void {
    const pending = this.pendingByWorker.get(worker);
    this.pendingByWorker.delete(worker);
    const next = this.queue.shift();
    if (next) {
      this.pendingByWorker.set(worker, next.pending);
      worker.postMessage(next.job);
    } else {
      this.idle.push(worker);
    }
    pending?.(reply);
  }

  private takeWorker(): Worker | undefined {
    const idleWorker = this.idle.pop();
    if (idleWorker) {
      return idleWorker;
    }
    if (this.workers.length >= this.size) {
      return undefined;
    }
    const worker = createPatchWorker({});
    worker.on('message', (reply: WorkerReply) => this.onReply(worker, reply));
    this.workers.push(worker);
    return worker;
  }

  submit(job: WorkerJob): Promise<WorkerReply> {
    return new Promise<WorkerReply>((resolve) => {
      if (this.destroyed) {
        resolve(SHUTDOWN_REPLY);
        return;
      }
      const worker = this.takeWorker();
      if (worker) {
        this.pendingByWorker.set(worker, resolve);
        worker.postMessage(job);
      } else {
        this.queue.push({ job, pending: resolve });
      }
    });
  }

  async destroy(): Promise<void> {
    this.destroyed = true;
    const orphaned = [...this.pendingByWorker.values(), ...this.queue.map((q) => q.pending)];
    this.pendingByWorker.clear();
    this.queue = [];
    for (const pending of orphaned) {
      pending(SHUTDOWN_REPLY);
    }

    await Promise.all(this.workers.map((w) => w.terminate()));
    this.workers = [];
    this.idle = [];
  }
}
