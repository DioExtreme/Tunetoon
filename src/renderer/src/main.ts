import { mount } from 'svelte';
import './app.css';
import App from './App.svelte';
import { initAppState } from './lib/state.svelte';

void initAppState();

mount(App, { target: document.getElementById('app')! });
