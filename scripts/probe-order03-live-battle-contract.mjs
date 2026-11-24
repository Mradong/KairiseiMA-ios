import http from 'node:http';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const origin='http://47.116.51.61:27040';
const bossId=30010102;
const arthurType=1;

function request(route,body,maximum=4*1024*1024){return new Promise((resolve,reject)=>{
 const req=http.request(origin+route,{method:'POST',timeout:20000,headers:{'Content-Type':'application/json'}},res=>{
  const chunks=[];let size=0;
  res.on('data',chunk=>{size+=chunk.length;if(size>maximum){req.destroy(Error('response exceeds limit'));return;}chunks.push(chunk)});
  res.on('end',()=>resolve({status:res.statusCode,text:Buffer.concat(chunks).toString('utf8')}));
 });
 req.on('timeout',()=>req.destroy(Error('request timeout')));req.on('error',reject);req.end(body);
});}

function envelope(response,success=true){
 const lines=response.text.trim().split(/\r?\n/);if(lines.length<1||lines.length>3)throw Error('invalid response segment count');
 const common=JSON.parse(lines[0]);
 if(success&&(response.status!==200||common.res_code!==0||lines.length<2))throw Error('business request failed');
 if(!success&&response.status===200&&common.res_code===0)throw Error('request unexpectedly succeeded');
 return {common,payload:lines.length>1?JSON.parse(lines[1]):null,segments:lines.length};
}

function integer(value,label){if(!Number.isSafeInteger(value))throw Error('invalid integer '+label);return value;}
function sha(value){return crypto.createHash('sha256').update(JSON.stringify(value)).digest('hex');}
function sessionBody(session,payload){return session+JSON.stringify(payload);}
function startRequest(session,partners){return sessionBody(session,{bossid:bossId,deck_arthur_type:arthurType,deck_arthur_type_idx:0,partner_deck_selects:partners});}

function choosePartners(payload){
 const groups=payload&&payload.arthurs;if(!Array.isArray(groups))throw Error('partner groups missing');
 const selected=[],used=new Set();
 for(const role of [2,3,4]){
  const group=groups.find(item=>item&&item.arthur_type===role);if(!group||!Array.isArray(group.partners))throw Error('partner role missing');
  let candidate=group.partners.find(item=>item&&Number.isSafeInteger(item.userid)&&item.userid>0&&Number.isSafeInteger(item.rental_idx)&&item.rental_idx>=0&&!used.has(item.userid));
  if(!candidate)candidate=group.partners.find(item=>item&&Number.isSafeInteger(item.userid)&&item.userid>0&&Number.isSafeInteger(item.rental_idx)&&item.rental_idx>=0);
  if(!candidate)throw Error('no usable partner for role '+role);
  used.add(candidate.userid);selected.push({userid:candidate.userid,arthur_type:role,deck_idx:candidate.rental_idx});
 }
 return selected;
}

function startSummary(payload){
 if(!payload||!Array.isArray(payload.battles)||payload.battles.length===0)throw Error('start battle segments missing');
 for(const battle of payload.battles){integer(battle.enemy_partyid,'enemy_partyid');integer(battle.enemy_type,'enemy_type');}
 integer(payload.seed,'seed');integer(payload.start_time,'start_time');integer(payload.cost_initial,'cost_initial');integer(payload.burst_gauge_initial,'burst_gauge_initial');integer(payload.hold_max,'hold_max');integer(payload.end_turn,'end_turn');
 return {segments:payload.battles.length,enemyPartyIds:payload.battles.map(x=>x.enemy_partyid),enemyTypes:payload.battles.map(x=>x.enemy_type),partnerDecks:Array.isArray(payload.partner_deck)?payload.partner_deck.length:null,costInitial:payload.cost_initial,burstGaugeInitial:payload.burst_gauge_initial,holdMax:payload.hold_max,endTurn:payload.end_turn};
}

const report={checkedAt:new Date().toISOString(),target:origin,success:false,stage:'setup',authorization:'User explicitly allowed creating a real dedicated account when it accelerates order 03 on 2026-11-24.',identity:'Fresh dedicated isolated-service account and character. UUID, user ID, session, name, partner IDs, card instance IDs, seed and start time are never recorded.',sideEffects:{accountMayPersist:true,characterMayPersist:true,bossId,expectedEntryBPCost:15,clearRewardRequested:false},steps:[],scope:'Live isolated service account-level contract probe: create, Home/Card/Quest/Partner read, one solo Start, identical retry, relog retry, rejected no-charge Continue, non-clear End/abandon, identical End retry and final Home/Quest read. No existing account, clear claim, reward, fabricated victory, server restart or configuration change.'};

try{
 const uuid=crypto.randomUUID(),name='ios'+crypto.randomBytes(6).toString('hex');
 const loginBody=JSON.stringify({uuid,clver:'2.0.4',resource_platform:'iOS',unity_version:'6000.3.26f1'});
 report.stage='login';let response=await request('/local/client/login',loginBody);let login=JSON.parse(response.text);
 if(response.status!==200||typeof login.sess_key!=='string'||!Number.isSafeInteger(login.userid)||new URL(login.api_url).origin!==origin)throw Error('login contract mismatch');
 let session=login.sess_key,userId=login.userid;
 report.stage='connect';let parsed=envelope(await request('/Connect',sessionBody(session,{})));if(parsed.payload.is_user!==0)throw Error('new identity is not fresh');
 report.stage='create';parsed=envelope(await request('/UserCreate',sessionBody(session,{name,arthur_type:arthurType})));if(!parsed.payload.user||parsed.payload.user.userid!==userId||parsed.payload.user.name!==name)throw Error('created identity mismatch');
 report.steps.push({phase:'identity',paths:['/local/client/login','/Connect','/UserCreate'],freshDedicatedAccount:true,characterCreated:true,identifiersRedacted:true});

 report.stage='home_before';parsed=envelope(await request('/HomeShow',session));const initialBP=integer(parsed.payload.user&&parsed.payload.user.bp,'initial bp');
 report.stage='cards';const cards=envelope(await request('/CardShow2',sessionBody(session,{'0':1}))).payload;
 const ownDeck=Array.isArray(cards['3'])&&cards['3'].find(deck=>deck&&deck['0']===arthurType&&deck['1']===0);if(!ownDeck)throw Error('own deck missing');
 report.stage='quests';const quests=envelope(await request('/TeamBattleSoloShow',sessionBody(session,{'0':arthurType}))).payload;
 let candidate=null;for(const key of ['9','10','11','12'])for(const group of quests[key]||[])for(const boss of group['10']||[])if(boss['0']===bossId)candidate=boss;
 if(!candidate||candidate['10']!==0||candidate['26']!==0||!Number.isSafeInteger(candidate['5'])||candidate['5']<=0)throw Error('candidate is not admitted');
 report.stage='partners';const partnerPayload=envelope(await request('/TeamBattleSoloPartnerShow',sessionBody(session,{bossid:bossId}))).payload;const partners=choosePartners(partnerPayload);
 report.steps.push({phase:'admission',paths:['/HomeShow','/CardShow2','/TeamBattleSoloShow','/TeamBattleSoloPartnerShow'],initialBP,candidate:{bossId,state:candidate['10'],locked:candidate['26'],bpCost:candidate['5']},ownDeck:{arthurType,index:0},partnerRoles:partners.map(x=>x.arthur_type),partnerIdentifiersRedacted:true});

 report.stage='start';const startBody=startRequest(session,partners);let start=envelope(await request('/TeamBattleSoloStart',startBody));const first=startSummary(start.payload),firstSeed=start.payload.seed,firstTime=start.payload.start_time;
 report.stage='start_retry';let retry=envelope(await request('/TeamBattleSoloStart',startBody));if(retry.payload.seed!==firstSeed||retry.payload.start_time!==firstTime||sha(retry.payload)!==sha(start.payload))throw Error('start retry changed accepted snapshot');
 report.stage='relogin';response=await request('/local/client/login',loginBody);login=JSON.parse(response.text);if(response.status!==200||login.userid!==userId||typeof login.sess_key!=='string')throw Error('relogin changed identity');session=login.sess_key;
 report.stage='start_after_relogin';const relogBody=startRequest(session,partners);retry=envelope(await request('/TeamBattleSoloStart',relogBody));if(retry.payload.seed!==firstSeed||retry.payload.start_time!==firstTime||sha(retry.payload)!==sha(start.payload))throw Error('relogin did not recover accepted start');
 report.steps.push({phase:'start_and_recovery',path:'/TeamBattleSoloStart',requestFields:['bossid','deck_arthur_type','deck_arthur_type_idx','partner_deck_selects'],first,identicalRetrySameSnapshot:true,reloginSameIdentity:true,reloginRetrySameSnapshot:true,entryChargedAtMostOnce:true});

 const inputCommands=start.payload.battles.map((_,index)=>index===0?'0,22'+',0'.repeat(20):'');const deadBits=start.payload.battles.map(()=>0);
 report.stage='continue_rejection';const rejected=envelope(await request('/TeamBattleSoloContinue',sessionBody(session,{pay_type:0,progress:1,input_cmd:inputCommands,enemy_dead_bit:deadBits})),false);
 report.steps.push({phase:'continue_rejection',path:'/TeamBattleSoloContinue',requestFields:['pay_type','progress','input_cmd','enemy_dead_bit'],payType:0,accepted:false,httpStatus:rejected.common.res_code===0?200:'non-success',resCode:rejected.common.res_code,responseBody:'omitted',expectedSideEffect:'none'});
 report.stage='start_after_continue_rejection';retry=envelope(await request('/TeamBattleSoloStart',relogBody));if(retry.payload.seed!==firstSeed||retry.payload.start_time!==firstTime)throw Error('rejected continue destroyed active run');

 report.stage='abandon';const endPayload={progress:0,is_clear:0,input_cmd:start.payload.battles.map(()=>''),enemy_dead_bit:deadBits,bossid:bossId};const endBody=sessionBody(session,endPayload);const ended=envelope(await request('/TeamBattleSoloEnd',endBody));if(ended.payload.is_clear!==0)throw Error('non-clear end differs');
 report.stage='abandon_retry';const endedAgain=envelope(await request('/TeamBattleSoloEnd',endBody));if(sha(ended.payload)!==sha(endedAgain.payload))throw Error('end retry changed receipt snapshot');
 report.stage='home_after';const homeAfter=envelope(await request('/HomeShow',session));const finalBP=integer(homeAfter.payload.user&&homeAfter.payload.user.bp,'final bp');
 report.stage='quest_after';const afterQuests=envelope(await request('/TeamBattleSoloShow',sessionBody(session,{'0':arthurType}))).payload;let afterCandidate=null;for(const key of ['9','10','11','12'])for(const group of afterQuests[key]||[])for(const boss of group['10']||[])if(boss['0']===bossId)afterCandidate=boss;
 if(!afterCandidate)throw Error('candidate disappeared after abandon');
 report.steps.push({phase:'abandon_and_receipt',path:'/TeamBattleSoloEnd',requestFields:Object.keys(endPayload),isClear:0,clearRewardRequested:false,identicalRetrySamePayload:true,finalHomeBP:finalBP,bpDelta:initialBP-finalBP,expectedBPCost:candidate['5'],chargedExactlyOnce:initialBP-finalBP===candidate['5'],candidateStateAfter:afterCandidate['10'],candidateLockedAfter:afterCandidate['26']});
 if(initialBP-finalBP!==candidate['5'])throw Error('entry BP debit differs or repeated');
 report.stage='complete';report.success=true;
}catch(error){report.errorClass=error.constructor.name;report.errorStage=report.stage;}

const target=path.join(root,'evidence/ios-package-search/order03-live-battle-contract.json');
fs.writeFileSync(target,JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;
