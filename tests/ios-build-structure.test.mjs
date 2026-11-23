import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { inspectMachO, verifyApp, verifyHttpTransport, version } from '../scripts/verify-ios-build.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
let checks = 0;
const check = (name, action) => { action(); checks++; console.log('PASS ' + name); };
// Synthetic headers test the verifier only. They are not a compiled application or an IPA.
function thin({ cpu = 0x0100000c, platform = 2, minimum = 0x000f0000, fileType = 2, encrypted = false } = {}) {
  const b = Buffer.alloc(256);
  b.writeUInt32LE(0xfeedfacf, 0); b.writeUInt32LE(cpu, 4); b.writeUInt32LE(fileType, 12);
  b.writeUInt32LE(fileType === 2 ? 4 : 3, 16); b.writeUInt32LE(fileType === 2 ? 144 : 120, 20);
  let at = 32;
  b.writeUInt32LE(0x32, at); b.writeUInt32LE(24, at + 4); b.writeUInt32LE(platform, at + 8); b.writeUInt32LE(minimum, at + 12); b.writeUInt32LE(0x001a0000, at + 16); at += 24;
  b.writeUInt32LE(0x19, at); b.writeUInt32LE(72, at + 4); b.write('__TEXT', at + 8, 'ascii'); b.writeBigUInt64LE(256n, at + 32); b.writeBigUInt64LE(256n, at + 48); at += 72;
  b.writeUInt32LE(0x2c, at); b.writeUInt32LE(24, at + 4); b.writeUInt32LE(encrypted ? 1 : 0, at + 16); at += 24;
  if (fileType === 2) { b.writeUInt32LE(0x80000028, at); b.writeUInt32LE(24, at + 4); b.writeBigUInt64LE(192n, at + 8); }
  return b;
}
function fat() {
  const b = Buffer.alloc(4096 + 256);
  b.writeUInt32BE(0xcafebabe, 0); b.writeUInt32BE(1, 4); b.writeUInt32BE(0x0100000c, 8); b.writeUInt32BE(4096, 16); b.writeUInt32BE(256, 20);
  thin().copy(b, 4096);
  return b;
}
check('supported iOS version parsing', () => assert.deepEqual(version('17.4.1'), [17, 4, 1]));
check('invalid version rejected', () => assert.throws(() => version('17.x')));
check('ARM64 device header admitted', () => assert.equal(inspectMachO(thin())[0].platform, 'iOS'));
check('ARM64 universal slice admitted', () => assert.equal(inspectMachO(fat())[0].offset, 4096));
check('x86 simulator architecture rejected', () => assert.throws(() => inspectMachO(thin({ cpu: 0x01000007 }))));
check('ARM64 simulator platform rejected', () => assert.throws(() => inspectMachO(thin({ platform: 7 }))));
check('newer iOS deployment target rejected', () => assert.throws(() => inspectMachO(thin({ minimum: 0x00120000 }))));
check('non-executable main image rejected', () => assert.throws(() => inspectMachO(thin({ fileType: 6 }))));
check('framework dylib admitted separately', () => assert.equal(inspectMachO(thin({ fileType: 6 }), { executable: false })[0].fileType, 6));
check('truncated image rejected', () => assert.throws(() => inspectMachO(thin().subarray(0, 64))));
check('encrypted image rejected', () => assert.throws(() => inspectMachO(thin({ encrypted: true }))));
check('missing iOS command rejected', () => { const b = thin(); b.writeUInt32LE(0x31, 32); assert.throws(() => inspectMachO(b)); });
check('load command size mismatch rejected', () => { const b = thin(); b.writeUInt32LE(136, 20); assert.throws(() => inspectMachO(b)); });
check('load command escaping image rejected', () => { const b = thin(); b.writeUInt32LE(1024, 36); assert.throws(() => inspectMachO(b)); });
check('entry pointing into header rejected', () => { const b = thin(); b.writeBigUInt64LE(4n, 160); assert.throws(() => inspectMachO(b)); });
check('universal table slice escaping file rejected', () => { const b = fat(); b.writeUInt32BE(100000, 16); assert.throws(() => inspectMachO(b)); });
check('universal header/image CPU mismatch rejected', () => { const b = fat(); b.writeUInt32BE(0x01000007, 8); assert.throws(() => inspectMachO(b)); });

const fixture = path.join(root, 'test-output/ios-structure-synthetic', Date.now().toString(), 'Fixture.app');
fs.mkdirSync(path.join(fixture, 'Frameworks/UnityFramework.framework'), { recursive: true });
fs.mkdirSync(path.join(fixture, 'Data/Managed/Metadata'), { recursive: true });
fs.writeFileSync(path.join(fixture, 'Fixture'), thin());
fs.writeFileSync(path.join(fixture, 'Info.plist'), 'Synthetic fixture only; not a valid application.');
fs.writeFileSync(path.join(fixture, 'Frameworks/UnityFramework.framework/UnityFramework'), thin({ fileType: 6 }));
const metadata = Buffer.alloc(32); metadata.writeUInt32LE(0xfab11baf, 0); metadata.writeUInt32LE(39, 4);
const metadataPath = path.join(fixture, 'Data/Managed/Metadata/global-metadata.dat');
fs.writeFileSync(metadataPath, metadata);
const info = { CFBundlePackageType: 'APPL', CFBundleSupportedPlatforms: ['iPhoneOS'], CFBundleIdentifier: 'local.recovery.fixture', CFBundleExecutable: 'Fixture', MinimumOSVersion: '15.0', NSAppTransportSecurity: { NSAllowsArbitraryLoads: true } };
check('HTTP exception accepted without overriding keys', () => verifyHttpTransport(info));
check('missing HTTP exception rejected', () => assert.throws(() => verifyHttpTransport({})));
for (const key of ['NSAllowsLocalNetworking', 'NSAllowsArbitraryLoadsForMedia', 'NSAllowsArbitraryLoadsInWebContent']) {
  for (const value of [true, false])
    check('ATS override rejected: ' + key + '=' + value, () => assert.throws(() => verifyHttpTransport({ NSAppTransportSecurity: { NSAllowsArbitraryLoads: true, [key]: value } })));
}
check('bundle structure admitted with explicit runtime limitations', () => { const x = verifyApp(fixture, info); assert.equal(x.structuralValidation, 'passed'); assert.match(x.limitations.join(' '), /do not establish.*installation/); });
check('simulator Info.plist rejected', () => assert.throws(() => verifyApp(fixture, { ...info, CFBundleSupportedPlatforms: ['iPhoneSimulator'] })));
check('unsafe executable path rejected', () => assert.throws(() => verifyApp(fixture, { ...info, CFBundleExecutable: '../Fixture' })));
check('newer Info.plist minimum system rejected', () => assert.throws(() => verifyApp(fixture, { ...info, MinimumOSVersion: '18.0' })));
check('missing Unity IL2CPP metadata rejected', () => { fs.renameSync(metadataPath, metadataPath + '.saved'); assert.throws(() => verifyApp(fixture, info)); fs.renameSync(metadataPath + '.saved', metadataPath); });
check('incorrect Unity metadata version rejected', () => { const bad = Buffer.from(metadata); bad.writeUInt32LE(31, 4); fs.writeFileSync(metadataPath, bad); assert.throws(() => verifyApp(fixture, info)); fs.writeFileSync(metadataPath, metadata); });

const result = { generatedAt: new Date().toISOString(), success: true, checks, scope: 'Synthetic Mach-O and bundle structural verifier tests only. No IPA, Xcode compilation, signature, device installation or gameplay was produced or verified.' };
fs.writeFileSync(path.join(root, 'evidence/ios-structure-tests.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
