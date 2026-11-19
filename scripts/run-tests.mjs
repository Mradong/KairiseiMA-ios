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
let userCreated = false;
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
      response.end(JSON.stringify({ res_code: 0, res_str: '' }) + '\n' + JSON.stringify({ is_user: userCreated ? 1 : 0, tutorial_flag: 0 }) + '\n');
    } else if (request.method === 'POST' && request.url === '/UserCreate') {
      assert.equal(userCreated, false);
      assert.equal(body, session + JSON.stringify({ name: '测试骑士', arthur_type: 3 }));
      userCreated = true;
      response.end(JSON.stringify({res_code:0}) + '\n' + JSON.stringify({user:{userid:1337,name:'测试骑士',active_arthur_type:3},push_option:{enable_flag:0}}) + '\n' + JSON.stringify({popup:[]}));
    } else if (request.method === 'POST' && request.url === '/HomeShow') {
      assert.equal(body, session);
      assert.equal(userCreated, true);
      response.end(JSON.stringify({res_code:0}) + '\n' + JSON.stringify({user:{userid:1337,name:'测试骑士',lv:4,ap:15,ap_max:20,bp:2,bp_max:3,gold:123456},server_time:100}) + '\n' + JSON.stringify({popup:[]}));
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
  const args = [origin];
  if (process.env.IOS_HOME_TEST_FIXTURE) args.push(process.env.IOS_HOME_TEST_FIXTURE);
  const child = spawn(exe, args, { windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
  child.stdout.on('data', chunk => { stdout += chunk; });
  child.stderr.on('data', chunk => { stderr += chunk; });
  status = await new Promise((resolve, reject) => { child.on('error', reject); child.on('exit', resolve); });
} finally { await new Promise(resolve => server.close(resolve)); }
const expectedCalls = ['GET /healthz', 'POST /local/client/login', 'POST /Connect', 'POST /UserCreate', 'POST /Connect', 'POST /HomeShow'];
const result = {
  generatedAt: new Date().toISOString(), success: status === 0 && failures.length === 0 && JSON.stringify(calls) === JSON.stringify(expectedCalls),
  compiler: 'Windows .NET Framework C# compiler', jsonLibrary: 'Newtonsoft.Json 13.0.2 net45',
  scope: 'CnProtocol.cs compilation, validation cases, and loopback HTTP contract fixture' + (process.env.IOS_HOME_TEST_FIXTURE ? '; additionally parses an isolated service test HomeShow response' : '') + '; no Unity/iOS runtime or live game server was tested.',
  stdout: stdout.trim(), stderr: stderr.trim(), calls, failures,
};
fs.writeFileSync(path.join(root, 'evidence/protocol-tests.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.success) process.exitCode = 1;
