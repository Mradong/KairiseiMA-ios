import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

export function version(value) {
  if (typeof value !== 'string' || !/^\d+(?:\.\d+){0,2}$/.test(value)) throw new Error('Invalid iOS version');
  return value.split('.').map(Number).concat([0, 0]).slice(0, 3);
}
function newer(a, b) {
  for (let i = 0; i < 3; i++) if (a[i] !== b[i]) return a[i] > b[i];
  return false;
}
function packedVersion(value) { return [value >>> 16, (value >>> 8) & 255, value & 255]; }
export function inspectMachO(buffer, { executable = true, deviceOS = '17.4.1' } = {}) {
  const target = version(deviceOS);
  if (buffer.length < 32) throw new Error('Mach-O image is truncated');
  const magic = buffer.readUInt32BE(0);
  let slices;
  if (magic === 0xcafebabe || magic === 0xcafebabf) {
    const wide = magic === 0xcafebabf;
    const count = buffer.readUInt32BE(4);
    const stride = wide ? 32 : 20;
    if (count < 1 || count > 16 || 8 + count * stride > buffer.length) throw new Error('Invalid universal image table');
    slices = Array.from({ length: count }, (_, i) => {
      const at = 8 + i * stride;
      const cpu = buffer.readUInt32BE(at);
      const offset = wide ? Number(buffer.readBigUInt64BE(at + 8)) : buffer.readUInt32BE(at + 8);
      const size = wide ? Number(buffer.readBigUInt64BE(at + 16)) : buffer.readUInt32BE(at + 12);
      if (!Number.isSafeInteger(offset) || !Number.isSafeInteger(size) || offset < 8 + count * stride || size < 32 || offset + size > buffer.length) throw new Error('Universal image slice is outside the file');
      return { cpu, offset, size };
    });
    const ordered = [...slices].sort((a, b) => a.offset - b.offset);
    for (let i = 1; i < ordered.length; i++) if (ordered[i].offset < ordered[i - 1].offset + ordered[i - 1].size) throw new Error('Universal image slices overlap');
  } else { slices = [{ cpu: null, offset: 0, size: buffer.length }]; }
  return slices.map(slice => {
    const b = buffer.subarray(slice.offset, slice.offset + slice.size);
    if (b.readUInt32LE(0) !== 0xfeedfacf) throw new Error('Expected little-endian 64-bit Mach-O');
    const cpu = b.readUInt32LE(4);
    if (cpu !== 0x0100000c || (slice.cpu !== null && slice.cpu !== cpu)) throw new Error('Expected ARM64 device architecture');
    const fileType = b.readUInt32LE(12);
    if (fileType !== (executable ? 2 : 6)) throw new Error('Unexpected Mach-O file type');
    const count = b.readUInt32LE(16);
    const commandBytes = b.readUInt32LE(20);
    if (count < 1 || count > 10000 || 32 + commandBytes > b.length) throw new Error('Invalid Mach-O load command table');
    let cursor = 32;
    let build = null;
    let text = null;
    let entry = null;
    let encrypted = false;
    for (let i = 0; i < count; i++) {
      if (cursor + 8 > 32 + commandBytes) throw new Error('Truncated load command');
      const rawCommand = b.readUInt32LE(cursor);
      const command = rawCommand & 0x7fffffff;
      const size = b.readUInt32LE(cursor + 4);
      if (size < 8 || (size & 7) !== 0 || cursor + size > 32 + commandBytes) throw new Error('Invalid load command size');
      if (command === 0x32) {
        if (build || size < 24 || b.readUInt32LE(cursor + 8) !== 2) throw new Error('Expected exactly one iOS device build-version command; simulator images are rejected');
        const minimum = packedVersion(b.readUInt32LE(cursor + 12));
        if (newer(minimum, target)) throw new Error('Binary requires a newer iOS than the target phone');
        build = { platform: 'iOS', minimumOS: minimum.join('.'), sdk: packedVersion(b.readUInt32LE(cursor + 16)).join('.') };
      }
      if (command === 0x19) {
        if (size < 72) throw new Error('Truncated segment command');
        const name = b.toString('ascii', cursor + 8, cursor + 24).replace(/\0.*$/, '');
        const start = Number(b.readBigUInt64LE(cursor + 40));
        const length = Number(b.readBigUInt64LE(cursor + 48));
        if (!Number.isSafeInteger(start) || !Number.isSafeInteger(length) || start + length > b.length) throw new Error('Segment is outside the binary');
        if (name === '__TEXT') text = { start, length };
      }
      if (command === 0x28) {
        if (size < 24) throw new Error('Truncated entry-point command');
        entry = Number(b.readBigUInt64LE(cursor + 8));
      }
      if (command === 0x2c) {
        if (size < 24) throw new Error('Truncated encryption command');
        encrypted ||= b.readUInt32LE(cursor + 16) !== 0;
      }
      cursor += size;
    }
    if (cursor !== 32 + commandBytes) throw new Error('Load command length mismatch');
    if (!build) throw new Error('iOS build platform could not be established');
    if (!text || text.length === 0) throw new Error('Executable text segment is missing');
    if (executable && (!Number.isSafeInteger(entry) || entry < text.start || entry >= text.start + text.length || entry < 32 + commandBytes)) throw new Error('Application entry point is outside executable text');
    if (encrypted) throw new Error('Unexpected encrypted executable in a source-built IPA');
    return { architecture: 'arm64', fileType, ...build, offset: slice.offset, bytes: slice.size };
  });
}

export function verifyHttpTransport(info) {
  const ats = info.NSAppTransportSecurity;
  if (!ats || typeof ats !== 'object' || Array.isArray(ats) || ats.NSAllowsArbitraryLoads !== true)
    throw new Error('The test server requires an explicit HTTP transport exception');
  for (const key of ['NSAllowsLocalNetworking', 'NSAllowsArbitraryLoadsForMedia', 'NSAllowsArbitraryLoadsInWebContent'])
    if (Object.hasOwn(ats, key)) throw new Error('ATS key overrides the global HTTP exception: ' + key);
}

// Scan the archived app itself, including its framework and Unity Raw data.
export function verifyServerOnlyResources(appDirectory) {
  const pending = [appDirectory];
  const bundleHeaders = new Set(['UnityFS', 'UnityWeb', 'UnityRaw', 'UnityArchive']);
  let filesChecked = 0;
  while (pending.length) {
    const directory = pending.pop();
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      const location = path.join(directory, entry.name);
      const relative = path.relative(appDirectory, location);
      if (/^(?:RecoveryContent|IosContentManifest(?:\.json)?|client-manifest\.json|ios-delivery\.json|resource-build\.json)$/i.test(entry.name))
        throw new Error('Embedded server resource path: ' + relative);
      if (entry.isDirectory()) { pending.push(location); continue; }
      if (!entry.isFile()) continue;
      const prefix = Buffer.alloc(16);
      const fd = fs.openSync(location, 'r');
      try { fs.readSync(fd, prefix, 0, prefix.length, 0); }
      finally { fs.closeSync(fd); }
      if (bundleHeaders.has(prefix.toString('ascii').split('\0')[0]))
        throw new Error('Embedded Unity resource bundle: ' + relative);
      filesChecked++;
    }
  }
  return { policy: 'server-only', passed: true, filesChecked };
}

export function verifyApp(appDirectory, info, deviceOS = '17.4.1', serverOnly = false) {
  if (!appDirectory.endsWith('.app') || !fs.statSync(appDirectory).isDirectory()) throw new Error('Expected an application bundle');
  if (info.CFBundlePackageType !== 'APPL' || !Array.isArray(info.CFBundleSupportedPlatforms) || !info.CFBundleSupportedPlatforms.includes('iPhoneOS')) throw new Error('Info.plist does not identify a device iOS application');
  if (typeof info.CFBundleIdentifier !== 'string' || !/^[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$/.test(info.CFBundleIdentifier)) throw new Error('Bundle identifier is invalid');
  if (newer(version(info.MinimumOSVersion), version(deviceOS))) throw new Error('Info.plist requires a newer system than the target phone');
  verifyHttpTransport(info);
  const executable = info.CFBundleExecutable;
  if (typeof executable !== 'string' || !executable || /[\\/\0]/.test(executable) || executable === '.' || executable === '..') throw new Error('Unsafe executable name');
  const binaryPath = path.join(appDirectory, executable);
  if (fs.lstatSync(binaryPath).isSymbolicLink() || !fs.statSync(binaryPath).isFile()) throw new Error('Application executable must be a regular file');
  const binary = fs.readFileSync(binaryPath);
  const slices = inspectMachO(binary, { deviceOS });
  const frameworks = [];
  const frameworkDirectory = path.join(appDirectory, 'Frameworks');
  if (fs.existsSync(frameworkDirectory)) {
    for (const entry of fs.readdirSync(frameworkDirectory, { withFileTypes: true })) {
      if (!entry.isDirectory() || !entry.name.endsWith('.framework')) continue;
      const framework = path.join(frameworkDirectory, entry.name, entry.name.slice(0, -'.framework'.length));
      if (fs.lstatSync(framework).isSymbolicLink()) throw new Error('Framework binary cannot be a symlink');
      frameworks.push({ name: entry.name, slices: inspectMachO(fs.readFileSync(framework), { executable: false, deviceOS }) });
    }
  }
  if (!frameworks.some(f => f.name === 'UnityFramework.framework')) throw new Error('UnityFramework is missing from the Unity application');
  const metadataCandidates = [
    'Data/Managed/Metadata/global-metadata.dat',
    'Frameworks/UnityFramework.framework/Data/Managed/Metadata/global-metadata.dat',
  ];
  const metadata = metadataCandidates.find(name => fs.existsSync(path.join(appDirectory, name)) && fs.statSync(path.join(appDirectory, name)).size > 8);
  if (!metadata) throw new Error('Unity IL2CPP metadata is missing');
  const metadataHeader = Buffer.alloc(8);
  const metadataFd = fs.openSync(path.join(appDirectory, metadata), 'r');
  try { fs.readSync(metadataFd, metadataHeader, 0, metadataHeader.length, 0); }
  finally { fs.closeSync(metadataFd); }
  if (metadataHeader.readUInt32LE(0) !== 0xfab11baf || metadataHeader.readUInt32LE(4) !== 39)
    throw new Error('Unexpected IL2CPP metadata format for the pinned Unity version');
  return {
    structuralValidation: 'passed', device: { model: 'iPhone 13', iOS: deviceOS },
    ...(serverOnly ? { resourceDelivery: verifyServerOnlyResources(appDirectory) } : {}),
    bundleIdentifier: info.CFBundleIdentifier, minimumOS: info.MinimumOSVersion, executable,
    executableSHA256: crypto.createHash('sha256').update(binary).digest('hex'), slices, frameworks, il2cppMetadata: metadata,
    il2cppMetadataVersion: metadataHeader.readUInt32LE(4),
    hasEmbeddedProvisioningProfile: fs.existsSync(path.join(appDirectory, 'embedded.mobileprovision')),
    limitations: ['Structural checks do not establish signing validity, installation success, rendering, networking, or complete gameplay.', 'A free Apple ID still has to sign the IPA during sideloading.'],
  };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const [app, infoFile, targetOS, policy] = process.argv.slice(2);
  if (!app || !infoFile || (policy && policy !== '--server-only'))
    throw new Error('Usage: node verify-ios-build.mjs <app-directory> <Info.plist-as-JSON> [target-iOS] [--server-only]');
  console.log(JSON.stringify(verifyApp(path.resolve(app), JSON.parse(fs.readFileSync(infoFile, 'utf8')), targetOS || '17.4.1', policy === '--server-only'), null, 2));
}
