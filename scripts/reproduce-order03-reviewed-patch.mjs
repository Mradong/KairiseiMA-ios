import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const reference = path.join(root, 'source-review/kairisei-ma-ch');
const patchRelative = 'inputs/server-order03/source-patch-20261124/ios-server-reviewed.patch';
const patchFile = path.join(root, patchRelative);
const sha = filename => crypto.createHash('sha256').update(fs.readFileSync(filename)).digest('hex');
const blobSha = data => crypto.createHash('sha1').update(Buffer.from('blob ' + data.length + '\0')).update(data).digest('hex');
assert.equal(sha(patchFile), 'b868e5db9c6fd673c0b7d90621780aed8d6d0b38389c4de30721047964aa360c');
const allowedPaths = [
  'server/internal/cnbootstrap/cdn_export.go',
  'server/internal/cnbootstrap/platform_apple_test.go',
  'server/internal/cnbootstrap/platform_content_http.go',
  'server/internal/cnbootstrap/platform_delivery.go',
  'server/internal/cnbootstrap/platform_manifest.go',
  'server/internal/cnbootstrap/platform_manifest_contract_test.go'
];
const patchText = fs.readFileSync(patchFile, 'utf8');
// New-file patches insert their mode before the index line.
const metadata = patchText.split(/(?=^diff --git )/m).filter(text => text.startsWith('diff --git ')).map(section => {
  const names = /^diff --git a\/(\S+) b\/(\S+)/.exec(section);
  const index = /^index ([a-f0-9]{40})\.\.([a-f0-9]{40})/m.exec(section);
  assert(names && index && names[1] === names[2] && allowedPaths.includes(names[1]), 'Unreviewed patch path');
  return { path: names[1], beforeBlob: index[1], afterBlob: index[2] };
});
assert.equal(metadata.length, allowedPaths.length);
assert.equal(new Set(metadata.map(entry => entry.path)).size, allowedPaths.length);
const git = (...args) => {
  const result = spawnSync('git', ['-c', 'core.autocrlf=false', ...args], { encoding: 'utf8', windowsHide: true });
  if (result.status !== 0) throw new Error(result.stdout + result.stderr);
  return result.stdout.trim();
};
const revision = git('-c', 'safe.directory=' + reference.replaceAll('\\', '/'), '-C', reference, 'rev-parse', 'HEAD');
assert.equal(revision, 'd1a202eb539b99511e8a33f3975fac409ab545bf');
const output = fs.mkdtempSync(path.join(root, 'test-output/order03-reviewed-source-'));
const applicationPatch = path.join(output, 'reviewed-lf.patch');
fs.writeFileSync(applicationPatch, patchText.replaceAll('\r\n', '\n'));
fs.cpSync(path.join(reference, 'server'), path.join(output, 'server'), {
  recursive: true, force: false, errorOnExist: true,
  filter: filename => { if (fs.lstatSync(filename).isSymbolicLink()) throw new Error('Reference source contains a symlink'); return true; }
});
for (const entry of metadata) {
  const filename = path.join(output, entry.path);
  if (/^0+$/.test(entry.beforeBlob)) assert.equal(fs.existsSync(filename), false);
  else {
    const data = fs.readFileSync(filename);
    assert.equal(blobSha(data), entry.beforeBlob, 'Patch base differs from received index ' + entry.path);
  }
}
git('-C', output, 'apply', '--check', applicationPatch);
git('-C', output, 'apply', applicationPatch);
const sourceIdentities = metadata.map(entry => {
  const filename = path.join(output, entry.path);
  assert.equal(blobSha(fs.readFileSync(filename)), entry.afterBlob, 'Applied bytes differ from received index ' + entry.path);
  return { ...entry, sha256: sha(filename) };
});
const module = path.join(output, 'server');
const temp = path.join(output, 'temp');
fs.mkdirSync(temp);
const environment = {
  ...process.env,
  GOROOT: path.join(root, 'tools/go'), GOPATH: path.join(root, 'tools/go-work'),
  GOMODCACHE: path.join(root, 'tools/go-module-cache'), GOCACHE: path.join(root, 'tools/go-build-cache'),
  GOTOOLCHAIN: 'local', GOOS: 'windows', GOARCH: 'amd64', GOAMD64: 'v1', CGO_ENABLED: '0',
  TMP: temp, TEMP: temp, IOS_COMPILED_PROFILE: ''
};
const go = (args, logName) => {
  const result = spawnSync(path.join(root, 'tools/go/bin/go.exe'), args, {
    cwd: module, env: environment, encoding: 'utf8', windowsHide: true, timeout: 180000
  });
  fs.writeFileSync(path.join(output, logName), result.stdout + result.stderr);
  if (result.status !== 0) throw new Error(result.stdout + result.stderr);
  return result.stdout;
};
const executable = path.join(output, 'kairi-server-stripped.exe');
const flags = ['-mod=mod','-buildvcs=false','-trimpath','-ldflags=-s -w'];
go(['build', ...flags, '-o', executable, './cmd/kairi-server'], 'build.log');
const observedHash = '7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918';
const reproducedHash = sha(executable);
const tests = go(['test','./internal/cnbootstrap','-run','^TestIOS','-count=1','-v'], 'ios-tests.log');
const report = {
  checkedAt: new Date().toISOString(), analysisCompleted: true,
  inputPatch: { file: patchRelative, bytes: fs.statSync(patchFile).size, sha256: sha(patchFile) },
  applicationPatch: { lineEndings: 'LF', sha256: sha(applicationPatch), scope: 'Normalized patch-text line endings in the isolated copy; resulting source bytes are checked against every received Git blob identity.' },
  baseCommit: revision, allBaseAndPatchedGitBlobIdentitiesMatched: true,
  appliedSourceFiles: sourceIdentities,
  build: {
    executable: path.relative(root, executable).replaceAll('\\', '/'),
    toolchain: 'go1.25.1', target: 'windows/amd64', cgoEnabled: false, flags,
    bytes: fs.statSync(executable).size, sha256: reproducedHash,
    observedServerSha256: observedHash, exactObservedBinaryReproduction: reproducedHash === observedHash
  },
  targetedIOSTests: { passed: true, output: tests, unityProfileGateRequested: false },
  logs: { directory: path.relative(root, output).replaceAll('\\', '/') },
  scriptSha256: sha(fileURLToPath(import.meta.url)),
  scope: 'Received reviewed patch applied to an isolated copy of the pinned full server module. Binary comparison and synthetic iOS protocol/manifest tests. Actual loaded configuration/table bytes, original CSV extraction, real battle contracts and device acceptance remain separate.'
};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-reviewed-patch-reproduction.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ ...report, targetedIOSTests: { ...report.targetedIOSTests, output: tests.slice(-3500) } }, null, 2));
