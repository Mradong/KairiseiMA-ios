import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const out = path.join(root, 'test-output');
fs.mkdirSync(out, { recursive: true });
const csc = path.join(process.env.WINDIR || 'C:/Windows', 'Microsoft.NET/Framework64/v4.0.30319/csc.exe');
const jsonDll = path.join(root, 'tools/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll');
const exe = path.join(out, 'CnProtocolTests.exe');
assert.ok(fs.existsSync(csc), 'The Windows .NET Framework C# compiler is required.');
assert.ok(fs.existsSync(jsonDll), 'The pinned Newtonsoft 13.0.2 NuGet package is required.');
fs.copyFileSync(jsonDll, path.join(out, 'Newtonsoft.Json.dll'));
const compile = spawnSync(csc, ['/nologo', '/codepage:65001', '/target:exe', '/out:' + exe,
  '/reference:' + jsonDll, '/reference:System.Net.Http.dll',
  path.join(root, 'ios-client/Assets/Recovery/Runtime/CnProtocol.cs'), path.join(root, 'tests/CnProtocolTests.cs')], { encoding: 'utf8' });
if (compile.status !== 0) throw new Error('Protocol compilation failed:\n' + compile.stdout + compile.stderr);

const calls = [];
const failures = [];
const session = 'recovery-session-0123456789';
let origin;
const server = http.createServer(async (request, response) => {
  const chunks = [];
  for await (const chunk of request) chunks.push(chunk);
  const body = Buffer.concat(chunks).toString('utf8');
  calls.push(request.method + ' ' + request.url);
  response.setHeader('Content-Type', 'application/json; charset=utf-8');
  try {
    if (request.method === 'GET' && request.url === '/healthz') {
      response.end(JSON.stringify({ state: 'PASS', battle_port: 26041 }));
    } else if (request.method === 'POST' && request.url === '/local/client/login') {
      assert.match(request.headers['content-type'], /^application\/json/);
      assert.deepEqual(JSON.parse(body), { uuid: 'recovery-contract-fixture', clver: '2.0.4', resource_platform: 'iOS', unity_version: '6000.3.26f1' });
      response.end(JSON.stringify({ res_code: 0, res_str: '', userid: 1337, sess_key: session, api_url: origin + '/', version_url: origin + '/local/content/iOS/6000.3.26f1/current/' }));
    } else if (request.method === 'POST' && request.url === '/Connect') {
      assert.equal(body, session + '{}');
      response.end(JSON.stringify({ res_code: 0, res_str: '' }) + '\n' + JSON.stringify({ is_user: 1, tutorial_flag: 7 }) + '\n');
    } else { throw new Error('Unexpected fixture route'); }
  } catch (error) {
    failures.push(error.message);
    response.writeHead(400);
    response.end(JSON.stringify({ error: 'Fixture contract mismatch' }));
  }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
origin = 'http://127.0.0.1:' + server.address().port;
let stdout = '';
let stderr = '';
let status;
try {
  const child = spawn(exe, [origin], { windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
  child.stdout.on('data', chunk => { stdout += chunk; });
  child.stderr.on('data', chunk => { stderr += chunk; });
  status = await new Promise((resolve, reject) => { child.on('error', reject); child.on('exit', resolve); });
} finally { await new Promise(resolve => server.close(resolve)); }
const expectedCalls = ['GET /healthz', 'POST /local/client/login', 'POST /Connect'];
const result = {
  generatedAt: new Date().toISOString(), success: status === 0 && failures.length === 0 && JSON.stringify(calls) === JSON.stringify(expectedCalls),
  compiler: 'Windows .NET Framework C# compiler', jsonLibrary: 'Newtonsoft.Json 13.0.2 net45',
  scope: 'CnProtocol.cs compilation, validation cases, and loopback HTTP contract fixture only; no Unity/iOS runtime or live game server was tested.',
  stdout: stdout.trim(), stderr: stderr.trim(), calls, failures,
};
fs.writeFileSync(path.join(root, 'evidence/protocol-tests.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.success) process.exitCode = 1;
