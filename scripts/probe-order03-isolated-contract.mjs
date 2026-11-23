import http from 'node:http';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
// Fixed user-authorized isolated test endpoint. All credentials stay in memory.
const origin='http://47.116.51.61:27040';
function request(method,route,body){return new Promise((resolve,reject)=>{
 const req=http.request(origin+route,{method,timeout:12000,headers:body===undefined?{}:{'Content-Type':'application/json'}},res=>{
  let data=[];let size=0;res.on('data',chunk=>{size+=chunk.length;if(size>262144){req.destroy(Error('response exceeds limit'));return;}data.push(chunk)});
  res.on('end',()=>resolve({status:res.statusCode,text:Buffer.concat(data).toString('utf8')}));
 });req.on('timeout',()=>req.destroy(Error('request timeout')));req.on('error',reject);req.end(body);
});}
function object(text){const x=JSON.parse(text);if(!x||Array.isArray(x)||typeof x!=='object')throw Error('expected JSON object');return x;}
function shape(x,keys){let out={};for(const key of keys)if(Object.hasOwn(x,key)){const v=x[key];out[key]=Array.isArray(v)?'array':v===null?'null':typeof v;}return out;}
const report={checkedAt:new Date().toISOString(),target:origin,success:false,identity:'fresh generated isolated guest; UUID, user ID and session never recorded',steps:[],scope:'Actual isolated service health, new guest login and Connect plus invalid UUID and invalid Connect payload. Guest login may persist a fresh isolated identity; no character creation, HomeShow, quest, battle or existing player account access. Shapes omit all values except safe protocol status and test state. Not proof of deck or combat contracts.'};
try {
 let r=await request('GET','/healthz');let health=object(r.text);report.steps.push({path:'/healthz',method:'GET',httpStatus:r.status,fields:shape(health,['state','battle_port','battle_rooms']),state:health.state==='PASS'?'PASS':'other'});if(r.status!==200||health.state!=='PASS')throw Error('health failed');
 r=await request('POST','/local/client/login',JSON.stringify({uuid:'invalid',clver:'2.0.4',resource_platform:'iOS',unity_version:'6000.3.26f1'}));report.steps.push({path:'/local/client/login',method:'POST',case:'invalid UUID',requestFields:['uuid','clver','resource_platform','unity_version'],httpStatus:r.status,responseBody:'omitted'});if(r.status!==400)throw Error('invalid UUID response differs');
 const uuid=crypto.randomUUID();const loginRequest=JSON.stringify({uuid,clver:'2.0.4',resource_platform:'iOS',unity_version:'6000.3.26f1'});
 r=await request('POST','/local/client/login',loginRequest);if(r.status!==200)throw Error('guest login HTTP failure');const login=object(r.text);
 if(typeof login.sess_key!=='string'||typeof login.userid!=='number'||new URL(login.api_url).origin!==origin||new URL(login.version_url).origin!==origin)throw Error('login contract mismatch');
 report.steps.push({path:'/local/client/login',method:'POST',case:'fresh isolated guest',requestFields:['uuid','clver','resource_platform','unity_version'],httpStatus:r.status,fields:shape(login,['res_code','userid','sess_key','api_url','version_url']),sameOriginApi:true,sameOriginVersion:true});
 r=await request('POST','/Connect',login.sess_key+'{}');if(r.status!==200)throw Error('Connect HTTP failure');const lines=r.text.trim().split(/\r?\n/);if(lines.length!==2&&lines.length!==3)throw Error('Connect segments mismatch');const common=object(lines[0]),payload=object(lines[1]);if(common.res_code!==0||payload.is_user!==0||!Number.isInteger(payload.tutorial_flag))throw Error('Connect payload mismatch');
 report.steps.push({path:'/Connect',method:'POST',case:'fresh guest',requestFormat:'<redacted session> followed by {}',httpStatus:r.status,segments:lines.length,commonFields:shape(common,['res_code','res_str','notification','revision','is_appupdate','res_err_action','res_is_del_savedata']),payloadFields:shape(payload,['is_user','tutorial_flag']),resCode:0,userCreated:false});
 r=await request('POST','/Connect',login.sess_key+'{"unexpected":1}');report.steps.push({path:'/Connect',method:'POST',case:'unexpected field',requestFormat:'<redacted session> followed by {unexpected:1}',httpStatus:r.status,responseBody:'omitted'});if(r.status!==400)throw Error('unexpected Connect field response differs');
 report.success=true;
}catch(e){report.errorClass=e.constructor.name;report.failed=true;}
const target=path.join(root,'evidence/ios-package-search/order03-isolated-contract-probe.json');fs.writeFileSync(target,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;
