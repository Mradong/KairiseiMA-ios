import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const base='inputs/source-provenance/2.0.4/';
const read=file=>fs.readFileSync(path.join(root,file));
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const sha=file=>hash(read(file));
const load=file=>JSON.parse(read(file).toString('utf8').replace(/^\uFEFF/,''));
const metadataFile='test-output/order03-release-204-metadata-lyd.json',tagFile='test-output/order03-release-204-tag-lyd.json';
const metadata=load(metadataFile),tag=load(tagFile),asset=metadata.assets.find(a=>a.id===746981591);
assert.equal(metadata.tag_name,'2.0.4');assert.equal(tag.object.type,'commit');
assert.equal(tag.object.sha,'d1a202eb539b99511e8a33f3975fac409ab545bf');
assert.equal(asset.size,408049800);assert.equal(asset.digest,'sha256:be81b6cf2d6ddb9c24664f14ce331b5457e27674e80dbfa6173c8290dce6f662');
assert.equal(asset.browser_download_url,'https://github.com/kuuhaku1314/kairisei-ma-ch/releases/download/2.0.4/KairiseiMA-Server-Update.zip');
const tail=read(base+'release-zip-tail-lyd.bin');assert.equal(tail.length,65536);
const end=tail.length-22;assert.equal(tail.readUInt32LE(end),0x06054b50);assert.equal(tail.readUInt16LE(end+20),0);
assert.equal(tail.readUInt16LE(end+10),273);
const directoryOffset=tail.readUInt32LE(end+16),directoryBytes=tail.readUInt32LE(end+12);
assert.equal(directoryOffset,408022184);assert.equal(directoryOffset+directoryBytes,asset.size-22);
const entries=[];let cursor=directoryOffset-(asset.size-tail.length);
for(let index=0;index<273;index++) {
  assert.equal(tail.readUInt32LE(cursor),0x02014b50);
  const nameBytes=tail.readUInt16LE(cursor+28),extraBytes=tail.readUInt16LE(cursor+30),commentBytes=tail.readUInt16LE(cursor+32);
  entries.push({name:tail.subarray(cursor+46,cursor+46+nameBytes).toString('utf8'),flags:tail.readUInt16LE(cursor+8),method:tail.readUInt16LE(cursor+10),crc:tail.readUInt32LE(cursor+16),compressed:tail.readUInt32LE(cursor+20),bytes:tail.readUInt32LE(cursor+24),offset:tail.readUInt32LE(cursor+42)});
  cursor+=46+nameBytes+extraBytes+commentBytes;
}
assert.equal(cursor,end);
function crc32(bytes) {
  let crc=0xffffffff;
  for(const byte of bytes){crc^=byte;for(let bit=0;bit<8;bit++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}
  return (crc^0xffffffff)>>>0;
}
const manifests=[];
for(const [kind,name] of [['release','release-manifest.json'],['resource','resource-set/resource-set.json']]) {
  const matches=entries.filter(e=>e.name===name);assert.equal(matches.length,1);
  const entry=matches[0],rangeFile=base+kind+'-manifest-range-lyd.bin',bytes=read(rangeFile);
  assert.equal(entry.method,8);assert.equal(entry.flags&1,0);assert.equal(bytes.readUInt32LE(0),0x04034b50);
  const filenameBytes=bytes.readUInt16LE(26),payloadOffset=30+filenameBytes+bytes.readUInt16LE(28);
  assert.equal(bytes.subarray(30,30+filenameBytes).toString('utf8'),name);
  assert.equal(bytes.length,payloadOffset+entry.compressed);
  const raw=zlib.inflateRawSync(bytes.subarray(payloadOffset),{maxOutputLength:entry.bytes});
  assert.equal(raw.length,entry.bytes);assert.equal(crc32(raw),entry.crc);
  const file=base+kind+'-manifest-lyd.json';fs.writeFileSync(path.join(root,file),raw);
  manifests.push({kind,file,bytes:raw.length,sha256:hash(raw),archiveEntry:name,zipCRC32Verified:true,rangeFile,rangeSha256:sha(rangeFile),rangeStart:entry.offset,rangeEnd:entry.offset+bytes.length-1});
}
const release=load(manifests[0].file),resource=load(manifests[1].file);
const expected=load('evidence/ios-package-search/order03-expected-tables.json');
const selection=load('evidence/ios-package-search/order03-runtime-selection-validation.json');
assert.equal(release.server_version,'2.0.4');assert.equal(release.client_version,'2.0.4');
assert.equal(manifests[1].sha256,release.resource_set_sha256);
assert.equal(manifests[1].sha256,selection.selectedManifest.sha256);
const tables=expected.tables.map(table=>{
  const matches=release.files.filter(f=>f.path.startsWith('resource-set/_local/control/server/cn602-')&&path.posix.basename(f.path)===table.name);
  assert.equal(matches.length,1);const published=matches[0];
  assert.equal(published.bytes,table.bytes);assert.equal(published.sha256,table.sha256);
  const receivedFile='inputs/server-order03/Order03-Combat-Verified-20261123/tables/'+table.name;
  assert.equal(read(receivedFile).length,published.bytes);assert.equal(sha(receivedFile),published.sha256);
  return {...table,publishedPath:published.path,receivedFile,publishedIdentityMatchesReceivedBytes:true};
});
assert.equal(tables.length,29);
assert.equal(resource.entrypoints['cn-combat-card-master'],'_local/control/server/cn602-card-master/card.csv');
assert.equal(resource.entrypoints['cn-combat-master-root'],'_local/control/server/cn602-battle-master');
const report={checkedAt:new Date().toISOString(),success:true,planItem:'03',scriptSha256:sha('scripts/audit-order03-publication-provenance.mjs'),
  publication:{releaseURL:metadata.html_url,tag:'2.0.4',baseCommit:tag.object.sha,releaseAssetURL:asset.browser_download_url,assetID:asset.id,assetBytes:asset.size,githubDeclaredArchiveDigest:asset.digest,wholeArchiveDigestVerified:false,method:'HTTPS byte ranges for ZIP directory and only the two public manifests; decompression size and ZIP CRC32 verified. Received CSV bytes independently match published SHA-256 identities. No archive contents executed.'},
  inputFiles:[metadataFile,tagFile,base+'release-zip-tail-lyd.bin',...manifests.flatMap(m=>[m.file,m.rangeFile])].map(file=>({file,bytes:read(file).length,sha256:sha(file)})),
  manifests,tableCount:tables.length,tableMismatches:0,tables,publishedResourceManifestMatchesSelectedRuntime:true,resourceManifestSha256:manifests[1].sha256,
  publishedSourceVersionEstablished:'Recovered project 2.0.4, pinned commit d1a202eb; not the official original-client extraction version.',
  originalOfficialClientExtractionProvenance:'UNKNOWN: published identities do not document original extraction input, tool, date or transformations.',
  limitations:['The update archive declares the 29 CSV hashes in its manifest, but contains no CSV members. CSV bytes were supplied separately by the user/server.','Complete archive SHA-256 was not verified; only downloaded manifest/member integrity and declared identities were checked.','Original official client version/extraction lineage, complete gameplay semantics and device acceptance remain separate.'],
  scope:'Traceable project publication identity and runtime-resource cohort. Does not assert official original-client extraction or an unobserved gameplay/ABI contract.'};
const output='evidence/ios-package-search/order03-publication-provenance-lyd.json';
fs.writeFileSync(path.join(root,output),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({evidence:output,success:true,tableCount:tables.length,tableMismatches:0,publishedResourceManifestMatchesSelectedRuntime:true,originalOfficialClientExtractionProvenance:'UNKNOWN',wholeArchiveDigestVerified:false},null,2));
