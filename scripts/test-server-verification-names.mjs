import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {labelVerificationFiles,lydFilename} from './server-verification-names.mjs';

const directory=fs.mkdtempSync('test-output/verification-names-');
fs.writeFileSync(path.join(directory,'Run.cmd'),'powershell -File Check.ps1');
fs.writeFileSync(path.join(directory,'Check.ps1'),'Write-Host ok');
const binary=Buffer.from([0,255,128,1]);
fs.writeFileSync(path.join(directory,'asset.bin'),binary);
labelVerificationFiles(directory);
assert.equal(fs.readFileSync(path.join(directory,'Run-lyd.cmd'),'utf8'),'powershell -File Check-lyd.ps1');
assert.deepEqual(fs.readFileSync(path.join(directory,'asset-lyd.bin')),binary);
assert.equal(lydFilename('Check-lyd.ps1'),'Check-lyd.ps1');
labelVerificationFiles(directory);
assert.deepEqual(fs.readdirSync(directory).sort(),['Check-lyd.ps1','Run-lyd.cmd','asset-lyd.bin']);
console.log('Verification names, references, binary bytes and idempotence PASS');
