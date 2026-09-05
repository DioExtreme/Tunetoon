/// <reference types="svelte" />
/// <reference types="vite/client" />

import type { RendererApi } from '@shared/types';

declare global {
  interface Window {
    api: RendererApi;
  }
}

export {};
