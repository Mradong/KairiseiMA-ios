// Isolated failure fixture only. Never use as a gameplay service.
import http from 'node:http';
import crypto from 'node:crypto';
import {pathToFileURL} from 'node:url';

const sha=data=>crypto.createHash('sha256').update(data).digest('hex');
function crc32(data) {
 let crc=0xffffffff;
 for(const byte of data) { crc^=byte; for(let bit=0;bit<8;bit++) crc=(crc>>>1)^((crc&1)?0xedb88320:0); }
 return ((crc^0xffffffff)>>>0).toString(16).toUpperCase().padStart(8,'0');
}
export function makeFixture() {
 const bytes=Buffer.from('Kairisei isolated invalid Unity bundle fixture; not game content.\n');
 const bundle={name:'recovery/ui.dat',file:sha(bytes)+'.bundle',bytes:bytes.length,sha256:sha(bytes),crc32:crc32(bytes),dependencies:[]};
 const contentId=sha(Buffer.from(`iOS\n6000.3.26f1\n${bundle.name}\n${bundle.sha256}\n${bundle.bytes}\n${bundle.crc32}\n\n`));
 return {bytes,manifest:{format:1,platform:'iOS',unityVersion:'6000.3.26f1',contentId,bundles:[bundle]}};
}
export function createFixtureServer(origin) {
 const parsed=new URL(origin);
 if(parsed.protocol!=='http:' || parsed.username || parsed.password || parsed.pathname!=='/' || parsed.search || parsed.hash) throw new Error('Use an explicit isolated http origin without a path.');
 const {bytes,manifest}=makeFixture();
 const root=`/fixture/${manifest.contentId}/`;
 return http.createServer((req,res)=>{
  const send=(status,body,type='application/json')=>{res.writeHead(status,{'Content-Type':type,'Cache-Control':'no-store','Connection':'close'});res.end(body);};
  if(req.method==='GET' && req.url==='/healthz') return send(200,JSON.stringify({state:'PASS',battle_port:1}));
  if(req.method==='POST' && req.url==='/local/client/login') {
   // Discard the synthetic endpoint-specific UUID; never retain credentials or sessions.
   req.resume();
   return send(200,JSON.stringify({res_code:0,userid:1,sess_key:'fixture_session_0001',api_url:parsed.origin,version_url:parsed.origin+root}));
  }
  if(req.method==='GET' && req.url===root+'client-manifest.json') return send(200,JSON.stringify(manifest));
  if(req.method==='GET' && req.url===root+'patch/'+manifest.bundles[0].name+'.v'+manifest.bundles[0].crc32) return send(200,bytes,'application/octet-stream');
  send(404,JSON.stringify({error:'isolation fixture: route not available'}));
 });
}
if(process.argv[1] && import.meta.url===pathToFileURL(process.argv[1]).href) {
 const [host,portText]=process.argv.slice(2);
 if(!host || !/^\d{1,5}$/.test(portText||'') || Number(portText)<1024 || Number(portText)>65535 || !/^(?:\d{1,3}\.){3}\d{1,3}$/.test(host) || host.split('.').some(x=>Number(x)>255)) throw new Error('Usage: node resource-failure-fixture.mjs <explicit IPv4 address> <port 1024..65535>');
 const origin=`http://${host}:${portText}`;
 createFixtureServer(origin).listen(Number(portText),host,()=>console.log(`Isolated failure fixture listening on ${origin}; no real accounts or game state.`));
}
