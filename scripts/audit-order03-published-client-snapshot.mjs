import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const read=file=>fs.readFileSync(path.join(root,file));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const sha=file=>hash(read(file));
const load=file=>JSON.parse(read(file).toString('utf8').replace(/^\uFEFF/,''));
const p='evidence/ios-package-search/',base='inputs/source-provenance/2.0.4/';
const release=load('test-output/order03-release-204-metadata-lyd.json');
const asset=release.assets.find(asset=>asset.id===746980950);
assert.equal(asset.name,'KairiseiMA-Windows.zip');assert.equal(asset.size,148021626);
assert.equal(asset.digest,'sha256:c5b0cc41e540bef59ac5d8ee5c522bd1fee35b88d17f329b0c2ec9fbb20d8e5c');
const tail=read(base+'windows-zip-tail-lyd.bin');assert.equal(tail.length,65536);
const end=tail.length-22;assert.equal(tail.readUInt32LE(end),0x06054b50);
assert.equal(tail.readUInt16LE(end+20),0);assert.equal(tail.readUInt16LE(end+10),30);
const directoryOffset=tail.readUInt32LE(end+16);assert.equal(directoryOffset,148018910);
assert.equal(directoryOffset+tail.readUInt32LE(end+12),asset.size-22);
let cursor=directoryOffset-(asset.size-tail.length);let member;
for(let index=0;index<30;index++) {
  assert.equal(tail.readUInt32LE(cursor),0x02014b50);
  const n=tail.readUInt16LE(cursor+28),e=tail.readUInt16LE(cursor+30),c=tail.readUInt16LE(cursor+32);
  const name=tail.subarray(cursor+46,cursor+46+n).toString('utf8');
  if(name==='ClientRebuild6_Data/il2cpp_data/Metadata/global-metadata.dat') {
    assert(!member);member={name,flags:tail.readUInt16LE(cursor+8),method:tail.readUInt16LE(cursor+10),crc:tail.readUInt32LE(cursor+16),compressed:tail.readUInt32LE(cursor+20),bytes:tail.readUInt32LE(cursor+24),offset:tail.readUInt32LE(cursor+42)};
  }
  cursor+=46+n+e+c;
}
assert.equal(cursor,end);assert(member);assert.equal(member.method,8);assert.equal(member.flags&1,0);
assert.equal(member.offset,93009975);assert.equal(member.compressed,3388595);assert.equal(member.bytes,11223492);
const range=read(base+'windows-metadata-range-lyd.bin');assert.equal(range.length,3388705);
assert.equal(range.readUInt32LE(0),0x04034b50);
const n=range.readUInt16LE(26),payloadOffset=30+n+range.readUInt16LE(28);
assert.equal(range.subarray(30,30+n).toString('utf8'),member.name);
assert.equal(range.length,payloadOffset+member.compressed);
const metadata=zlib.inflateRawSync(range.subarray(payloadOffset),{maxOutputLength:member.bytes});
assert.equal(metadata.length,member.bytes);
let crc=0xffffffff;
for(const byte of metadata){crc^=byte;for(let bit=0;bit<8;bit++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}
assert.equal((crc^0xffffffff)>>>0,member.crc);
const metadataFile=base+'public-windows-metadata-lyd.dat';fs.writeFileSync(path.join(root,metadataFile),metadata);
const enums=load(p+'order03-modern-metadata-enums.json');
assert(enums.success&&enums.independentCsharpAgreement&&enums.candidateEnumAgreement);
assert.equal(hash(metadata),enums.source.metadataSha256);
assert.equal(sha('tests/Order03MetadataEnumsProbe.cs'),enums.independentReaderSha256);
const snapshotReceipt=load(p+'order03-candidate-snapshot.json');
assert.equal(sha(snapshotReceipt.snapshot),snapshotReceipt.snapshotSha256);
assert.equal(enums.candidateSha256,snapshotReceipt.snapshotSha256);
const snapshot=load(snapshotReceipt.snapshot),publication=load(p+'order03-publication-provenance-lyd.json');
assert.equal(snapshot.tables.length,29);assert.equal(snapshot.enums.length,23);
const required=read('ios-client/Assets/Recovery/Runtime/CombatTableSet.cs').toString('utf8').match(/RequiredTables = \{([\s\S]*?)\};/)[1].match(/[a-z_]+\.csv/g);
assert.equal(required.length,20);
assert.deepEqual(required.filter(name=>!snapshot.tables.some(table=>table.name===name)),[]);
const tables=snapshot.tables.map(table=>{
  const expected=publication.tables.find(t=>t.name===table.name);assert(expected);
  const bytes=Buffer.from(table.text,'utf8');assert.equal(bytes.length,expected.bytes);assert.equal(hash(bytes),expected.sha256);
  return {name:table.name,bytes:bytes.length,sha256:hash(bytes),publishedPath:expected.publishedPath};
});
for(const family of snapshot.enums) {
  const source=enums.families.find(f=>f.name===family.name);assert(source);
  assert.deepEqual(family.values,source.values.map(value=>({name:value.name,value:value.value})));
}
const constants=snapshot.enums.reduce((count,family)=>count+family.values.length,0);assert.equal(constants,822);
const validator=load(p+'order03-candidate-validation.json');
assert.equal(validator.candidateSha256,snapshotReceipt.snapshotSha256);assert(validator.managedValidatorPassed);
for(const source of validator.sourceHashes)assert.equal(sha(source.file),source.sha256);
const report={checkedAt:new Date().toISOString(),success:true,planItem:'03',scriptSha256:sha('scripts/audit-order03-published-client-snapshot.mjs'),
  publication:{releaseURL:release.html_url,projectVersion:'2.0.4',assetID:asset.id,assetURL:asset.browser_download_url,archiveBytes:asset.size,githubDeclaredArchiveDigest:asset.digest,wholeArchiveDigestVerified:false,archiveMember:member.name,memberCRC32Verified:true,downloadedRange:{start:member.offset,end:member.offset+range.length-1}},
  metadata:{file:metadataFile,bytes:metadata.length,sha256:hash(metadata),matchesPinnedWindowsInput:true,matchesIndependentlyDecodedEnums:true},
  snapshot:{file:snapshotReceipt.snapshot,bytes:read(snapshotReceipt.snapshot).length,sha256:snapshotReceipt.snapshotSha256,requiredTables:20,requiredTablesMissing:[],totalTables:29,allCSVBytesMatchPublishedServerManifest:true,enums:23,constants:822,allEnumValuesMatchPublishedClientMetadata:true,currentManagedTransportSourceMatchesValidatedSource:true},
  tables,inputFiles:['test-output/order03-release-204-metadata-lyd.json',base+'windows-zip-tail-lyd.bin',base+'windows-metadata-range-lyd.bin',metadataFile,p+'order03-modern-metadata-enums.json',p+'order03-candidate-snapshot.json',p+'order03-candidate-validation.json',p+'order03-publication-provenance-lyd.json'].map(file=>({file,sha256:sha(file)})),
  versionConclusion:'CSV identities and exact candidate enum source are published together under the recovered-project 2.0.4 release; the server and Windows client ZIP members have distinct roles.',
  scope:'Published reconstruction version and exact snapshot byte identities, not proof of original official-client extraction version, complete native skill execution, Unity/ARM64/device gameplay or production deployment. Only the public metadata member was decompressed; full ZIP digest not checked.'};
fs.writeFileSync(path.join(root,p+'order03-published-client-snapshot-lyd.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({success:true,requiredTables:20,missing:[],tables:29,enums:23,constants:822,publishedVersion:'2.0.4',metadataSha256:hash(metadata)},null,2));
