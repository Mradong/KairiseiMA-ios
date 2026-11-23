import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const origin = 'http://47.116.51.61:27040';
const report = {checkedAt: new Date().toISOString(), origin, method: 'GET', path: '/healthz', healthPass: false,
  runtimeVersionEstablished: false, effectiveTableSnapshotEstablished: false,
  scope: 'Only the documented public health route; no session, guest/character creation, account/business API, admission or configuration access. Health is not proof of build version or effective loaded tables.'};
try {
  const response = await new Promise((resolve, reject) => {
    const req = http.get(origin + '/healthz', {timeout: 12000}, res => {
      const chunks = []; let size = 0;
      res.on('data', chunk => { size += chunk.length; if (size > 16384) { res.destroy(Error('Health response exceeds limit')); return; } chunks.push(chunk); });
      res.on('error', reject); res.on('end', () => resolve({status: res.statusCode, text: Buffer.concat(chunks).toString('utf8')}));
    }); req.on('error', reject); req.on('timeout', () => req.destroy(Error('Health timeout')));
  });
  const value = JSON.parse(response.text);
  if (!value || Array.isArray(value) || typeof value !== 'object') throw Error('Unexpected health response');
  report.httpStatus = response.status;
  report.healthPass = response.status === 200 && value.state === 'PASS';
  report.fieldsPresent = ['state', 'battle_port', 'battle_rooms'].filter(name => Object.hasOwn(value, name));
  report.versionFieldPresent = ['version', 'build', 'commit', 'source_commit', 'resource_manifest_sha256'].some(name => Object.hasOwn(value, name));
  report.otherFieldCount = Object.keys(value).filter(name => !report.fieldsPresent.includes(name)).length;
  report.reason = report.versionFieldPresent ? 'Version-like fields would still need independent source and loaded-snapshot validation; values omitted pending review.' : 'Health response exposes no selected version/build/commit/resource-snapshot fields. Server-side evidence is required.';
} catch (e) { report.errorClass = e.constructor.name; }
report.scriptSha256 = crypto.createHash('sha256').update(fs.readFileSync(fileURLToPath(import.meta.url))).digest('hex');
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-public-version-evidence.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
if (!report.healthPass) process.exitCode = 1;
