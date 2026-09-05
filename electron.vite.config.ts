import { defineConfig, externalizeDepsPlugin } from 'electron-vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import tailwindcss from '@tailwindcss/vite';
import { resolve } from 'path';

const alias = { '@shared': resolve(__dirname, 'src/shared') };

const devCsp = {
  name: 'dev-csp',
  apply: 'serve' as const,
  transformIndexHtml: (html: string) => html.replace("connect-src 'self'", "connect-src 'self' ws: http://localhost:*")
};

export default defineConfig({
  main: { plugins: [externalizeDepsPlugin()], resolve: { alias }, build: { minify: 'esbuild' } },
  preload: { plugins: [externalizeDepsPlugin()], resolve: { alias }, build: { minify: 'esbuild' } },
  renderer: {
    plugins: [svelte(), tailwindcss(), devCsp],
    resolve: { alias },
    build: {
      minify: 'esbuild',
      rollupOptions: {
        input: {
          index: resolve(__dirname, 'src/renderer/index.html'),
          splash: resolve(__dirname, 'src/renderer/splash.html')
        }
      }
    }
  }
});
