import fs from 'node:fs';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2);
const option = name => { const at = args.indexOf(name); return at < 0 ? undefined : args[at + 1]; };
const inputOption = option('--input');
const outputOption = option('--output');
if (!inputOption || !outputOption) throw new Error('Usage: node scripts/export-unity.mjs --input <file-or-directory> --output <new-directory-inside-KairiseiMA-ios>');
const input = path.resolve(inputOption);
const output = path.resolve(root, outputOption);
if (!fs.existsSync(input)) throw new Error('Input does not exist.');
const relativeOutput = path.relative(root, output);
if (!relativeOutput || relativeOutput.startsWith('..') || path.isAbsolute(relativeOutput)) throw new Error('Output must be a new child directory of KairiseiMA-ios.');
if (fs.existsSync(output)) throw new Error('Output already exists; existing recovery exports will not be overwritten.');
const executable = path.join(root, 'tools/AssetRipper/AssetRipper.GUI.Free.exe');
if (!fs.existsSync(executable)) throw new Error('Pinned AssetRipper tool is missing. See tools/downloads.json.');
const stamp = new Date().toISOString().replace(/[:.]/g, '-');
fs.mkdirSync(path.join(root, 'logs'), { recursive: true });
const logPath = path.join(root, 'logs', 'export-' + stamp + '.log');
const outputLog = fs.createWriteStream(path.join(root, 'logs', 'export-' + stamp + '-console.log'));
const child = spawn(executable, ['--headless', '--port', '0', '--log-path', logPath], { cwd: root, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
const address = await new Promise((resolve, reject) => {
  let text = '';
  const timeout = setTimeout(() => reject(new Error('AssetRipper startup timed out')), 60000);
  const consume = chunk => {
    outputLog.write(chunk);
    text += chunk.toString('utf8');
    const match = /Now listening on:\s*(http:\/\/127\.0\.0\.1:\d+)/.exec(text);
    if (match) { clearTimeout(timeout); resolve(match[1]); }
  };
  child.stdout.on('data', consume);
  child.stderr.on('data', consume);
  child.once('error', error => { clearTimeout(timeout); reject(error); });
  child.once('exit', code => { clearTimeout(timeout); reject(new Error('AssetRipper exited before startup: ' + code)); });
}).catch(error => { child.kill(); outputLog.end(); throw error; });
try {
  const route = fs.statSync(input).isDirectory() ? '/LoadFolder' : '/LoadFile';
  for (const [endpoint, target] of [[route, input], ['/Export/UnityProject', output]]) {
    const response = await fetch(address + endpoint, { method: 'POST', body: new URLSearchParams({ path: target }), redirect: 'manual' });
    const body = await response.text();
    if (response.status !== 302) throw new Error(endpoint + ' failed with HTTP ' + response.status + ': ' + body.slice(0, 200));
  }
  if (!fs.existsSync(path.join(output, 'ExportedProject/Assets'))) throw new Error('Export returned without creating a Unity Assets directory.');
  const log = fs.readFileSync(logPath, 'utf8');
  const manifest = { generatedAt: new Date().toISOString(), input, output, log: logPath, importErrors: (log.match(/Import \[Error\]/g) || []).length, exportErrors: (log.match(/Export \[Error\]/g) || []).length, note: 'Export success does not prove recovered method implementations, Unity compilation, rendering, or iOS compatibility.' };
  fs.writeFileSync(path.join(output, 'recovery-run.json'), JSON.stringify(manifest, null, 2) + '\n');
  console.log(JSON.stringify(manifest, null, 2));
} finally {
  child.kill();
  outputLog.end();
}
