const statusEl = document.getElementById('status')!;
const fillEl = document.getElementById('fill')!;

window.api.onUpdateProgress((percent) => {
  statusEl.textContent = 'Downloading update…';
  fillEl.style.width = `${percent}%`;
});

export {};
