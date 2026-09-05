const { rm } = require('fs/promises');
const path = require('path');

exports.default = async function afterPack(context) {
  for (const file of ['dxcompiler.dll', 'dxil.dll']) {
    await rm(path.join(context.appOutDir, file), { force: true });
  }
};
