import http from 'node:http';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const origin='http://47.116.51.61:27040';
const pkg=path.join(root,'inputs/server-order03/Order03-Combat-Verified-20261123');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function request(route,body){return new Promise((resolve,reject)=>{
 const req=http.request(origin+route,{method:'POST',timeout:15000,headers:{'Content-Type':'application/json'}},res=>{
  let chunks=[],size=0;res.on('data',b=>{size+=b.length;if(size>4*1024*1024){req.destroy(Error('response limit'));return;}chunks.push(b)});
  res.on('end',()=>resolve({status:res.statusCode,text:Buffer.concat(chunks).toString('utf8')}));
 });req.on('timeout',()=>req.destroy(Error('timeout')));req.on('error',reject);req.end(body);
});}
function envelope(response){if(response.status!==200)throw Error('HTTP error');const lines=response.text.trim().split(/\r?\n/);if(lines.length<2||lines.length>3)throw Error('invalid segments');const common=JSON.parse(lines[0]),payload=JSON.parse(lines[1]);if(common.res_code!==0||!payload||Array.isArray(payload)||typeof payload!=='object')throw Error('business error');return {lines,common,payload};}
function fields(x,keys){const out={};for(const k of keys)if(Object.hasOwn(x,k)){const v=x[k];out[k]=Array.isArray(v)?'array':v===null?'null':typeof v;}return out;}
const result={checkedAt:new Date().toISOString(),target:origin,success:false,identity:'fresh isolated guest and fresh test character; identifying values and response contents omitted',steps:[],scope:'Isolated guest and character creation, own CardShow2 and one invalid CardShow2 request. This creates a persistent test identity and character. No HomeShow, deck write, quest or battle request. Counts and shapes are redacted; successful card ID membership checks do not prove matching version or correct semantics.'};
try {
 const sums=fs.readFileSync(path.join(pkg,'SHA256SUMS-tables.txt'),'utf8').replaceAll('\r\n','\n');const masters=new Set(), hashes={};
 for(const name of ['card.csv','card_alchemist.csv']){const data=fs.readFileSync(path.join(pkg,'tables',name));const m=sums.match(new RegExp('^([0-9a-f]{64})  tables/'+name.replace('.','\\.')+'$','m'));if(!m||hash(data)!==m[1])throw Error('master hash mismatch');hashes[name]=m[1];for(const line of data.toString('utf8').split(/\r?\n/)){const id=line.split(',',1)[0];if(/^\d+$/.test(id))masters.add(id);}}
 const uuid=crypto.randomUUID(), name='ios'+crypto.randomBytes(6).toString('hex');
 let r=await request('/local/client/login',JSON.stringify({uuid,clver:'2.0.4',resource_platform:'iOS',unity_version:'6000.3.26f1'}));if(r.status!==200)throw Error('login HTTP error');const login=JSON.parse(r.text);if(typeof login.sess_key!=='string'||typeof login.userid!=='number'||new URL(login.api_url).origin!==origin||new URL(login.version_url).origin!==origin)throw Error('login contract mismatch');const sess=login.sess_key;
 result.steps.push({path:'/local/client/login',httpStatus:200,sameOrigin:true,resourceRevisionMatchesKnownUiOnly:new URL(login.version_url).pathname.split('/').filter(Boolean).at(-1)==='55c68a8971aafc9a137f79f16d5c2795bfb37c89b7f64bdca1542fc0eb1fd2bb'});
 r=await request('/Connect',sess+'{}');const connect=envelope(r);if(connect.payload.is_user!==0)throw Error('fresh identity already has character');result.steps.push({path:'/Connect',httpStatus:r.status,segments:connect.lines.length,userCreated:false});
 r=await request('/UserCreate',sess+JSON.stringify({name,arthur_type:1}));let create=envelope(r);if(!create.payload.user||create.payload.user.userid!==login.userid||create.payload.user.name!==name)throw Error('created identity mismatch');result.steps.push({path:'/UserCreate',httpStatus:r.status,segments:create.lines.length,requestFields:['name','arthur_type'],responseFields:fields(create.payload,['user']),identityMatched:true});
 r=await request('/CardShow2',sess+JSON.stringify({'0':1}));let card=envelope(r);const cards=card.payload['1'],decks=card.payload['3'];if(!Array.isArray(cards)||!Array.isArray(decks)||!cards.length||!decks.length)throw Error('missing card/deck data');const owned=new Set();let missing=0;for(const c of cards){if(!Number.isSafeInteger(c['0'])||!Number.isSafeInteger(c['1'])||c['0']<=0||c['1']<=0||owned.has(c['0']))throw Error('invalid card identity');owned.add(c['0']);if(!masters.has(String(c['1'])))missing++;}let refs=0,unowned=0;for(const d of decks){if(!Array.isArray(d['4']))throw Error('invalid deck card slots');for(const id of d['4'])if(id!==0){refs++;if(!owned.has(id))unowned++;}}
 result.steps.push({path:'/CardShow2',httpStatus:r.status,segments:card.lines.length,requestFields:['0'],payloadFields:fields(card.payload,['1','3']),cardFields:fields(cards[0],['0','1','2']),deckFields:fields(decks[0],['0','1','2','3','4','5','6','7','8','9','10','11']),cardCount:cards.length,deckCount:decks.length,deckNonzeroCardRefs:refs,deckRefsNotInReturnedCards:unowned,cardDefinitionsMissingFromDeliveredTables:missing,comparedTableSha256:hashes});
 r=await request('/CardShow2',sess+JSON.stringify({unexpected:1}));result.steps.push({path:'/CardShow2',case:'unexpected field',httpStatus:r.status,responseBody:'omitted'});if(r.status!==400)throw Error('unexpected field response differs');
 result.success=true;
}catch(e){result.failed=true;result.errorClass=e.constructor.name;}
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-isolated-card-contract.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));if(!result.success)process.exitCode=1;
