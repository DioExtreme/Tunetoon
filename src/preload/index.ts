import { contextBridge, ipcRenderer } from 'electron';
import type { Cmd, EvChannel, LivePatch, RendererApi, ToastMessage } from '@shared/types';

const invoke = (channel: Cmd, ...args: unknown[]): Promise<any> => ipcRenderer.invoke(channel, ...args);

function subscribe<T>(channel: EvChannel) {
  return (cb: (payload: T) => void): void => {
    ipcRenderer.on(channel, (_event, payload: T) => cb(payload));
  };
}

const api: RendererApi = {
  getState: () => invoke('getState'),
  getChangelog: () => invoke('getChangelog'),
  unlock: (password) => invoke('unlock', password),
  setupMasterPassword: (password) => invoke('setupMasterPassword', password),
  disableEncryption: () => invoke('disableEncryption'),

  setConfig: (patch) => invoke('setConfig', patch),
  pickFolder: (title) => invoke('pickFolder', title),

  addAccount: (server, draft) => invoke('addAccount', server, draft),
  updateAccount: (id, draft) => invoke('updateAccount', id, draft),
  deleteAccount: (id) => invoke('deleteAccount', id),
  reorderAccounts: (server, orderedIds) => invoke('reorderAccounts', server, orderedIds),
  setLoginWanted: (id, wanted) => invoke('setLoginWanted', id, wanted),
  setLoginWantedMany: (ids, wanted) => invoke('setLoginWantedMany', ids, wanted),
  setToonSlot: (id, slot) => invoke('setToonSlot', id, slot),

  getClashToons: (username) => invoke('getClashToons', username),

  getDisplays: () => invoke('getDisplays'),
  addGroup: (group) => invoke('addGroup', group),
  updateGroup: (group) => invoke('updateGroup', group),
  deleteGroup: (id) => invoke('deleteGroup', id),
  reorderGroups: (server, orderedIds) => invoke('reorderGroups', server, orderedIds),
  playGroup: (id) => invoke('playGroup', id),
  arrangeGroup: (id) => invoke('arrangeGroup', id),
  endGroup: (id) => invoke('endGroup', id),

  play: () => invoke('play'),
  cancelPatch: () => invoke('cancelPatch'),
  submitTwoStep: (codes) => invoke('submitTwoStep', codes),
  cancelTwoStep: (accountId) => invoke('cancelTwoStep', accountId),

  endGame: (id) => invoke('endGame', id),
  endAll: () => invoke('endAll'),
  confirmClose: () => invoke('confirmClose'),

  importLegacy: (folder, password) => invoke('importLegacy', folder, password),
  exportAccounts: () => invoke('exportAccounts'),
  importAccounts: () => invoke('importAccounts'),

  onStatePatch: subscribe<LivePatch>('onStatePatch'),
  onToast: subscribe<ToastMessage>('onToast'),
  onConfirmClose: subscribe<void>('onConfirmClose'),
  onToonsChanged: subscribe<void>('onToonsChanged'),
  onUpdateProgress: subscribe<number>('onUpdateProgress')
};

contextBridge.exposeInMainWorld('api', api);
