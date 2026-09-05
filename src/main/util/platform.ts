export const IS_MAC = process.platform === 'darwin';
export const TTR_PLATFORM: 'darwin' | 'win64' = IS_MAC ? 'darwin' : 'win64';
export const CLASH_PLATFORM: 'macos' | 'windows' = IS_MAC ? 'macos' : 'windows';
