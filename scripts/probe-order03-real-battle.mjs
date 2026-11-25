import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import crypto from 'node:crypto';
import { isDeepStrictEqual } from 'node:util';
import { fileURLToPath } from 'node:url';
import { Order03Engine } from './order03-engine-client.mjs';
import { playRounds } from './order03-play-rounds.mjs';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const origin='http://47.116.51.61:27040';
const bossId=30010102;
const verifyContinue=process.argv.includes('--continue');
const defeatOnly=process.argv.includes('--defeat-only');
const baseName=defeatOnly?'order03-real-defeat-retry':verifyContinue?'order03-real-battle-continue':'order03-real-battle';
const file=path.join(root,'evidence/ios-package-search/'+baseName+'.json');
const report={checkedAt:new Date().toISOString(),success:false,stage:'before_identity_creation',authorization:'Existing user authorization for dedicated real test identities on the isolated iOS service.',sideEffects:{maximumNewAccounts:2,maximumNewCharacters:2,maximumEntries:1,expectedEntryBP:15,clearRewardMayBeGrantedOnlyAfterComputedVictory:true},steps:[],scope:'Dedicated identities and actual service party data, delivered real tables and deterministic engine actions. Identity/session/name/instance identifiers and seed remain in memory.'};
const save=()=>fs.writeFileSync(file,JSON.stringify(report,null,2)+'\n');
report.sideEffects.maximumEntries=verifyContinue?2:1;
report.sideEffects.clearRewardMayBeGrantedOnlyAfterComputedVictory=!defeatOnly;
report.sideEffects.maximumContinueDebits=verifyContinue?1:0;
report.sideEffects.expectedContinueCrystalCost=50;
const runStamp=new Date().toISOString().replace(/[-:.]/g,'');
const logs=path.join(root,'evidence/ios-package-search/'+baseName+'-logs-'+runStamp);
fs.mkdirSync(logs,{recursive:true});
save();
function request(route,body){return new Promise((resolve,reject)=>{
  const req=http.request(origin+route,{method:'POST',timeout:20000,headers:{'Content-Type':'application/json'}},res=>{
    const chunks=[];let bytes=0;
    res.on('data',chunk=>{bytes+=chunk.length;if(bytes>8*1024*1024)req.destroy(new Error('response limit'));else chunks.push(chunk);});
    res.on('end',()=>resolve({status:res.statusCode,text:Buffer.concat(chunks).toString('utf8')}));
  });
  req.on('error',reject);req.on('timeout',()=>req.destroy(new Error('request timeout')));req.end(body);
});}
function envelope(response){
  const parts=response.text.trim().split(/\r?\n/);const common=JSON.parse(parts[0]);
  if(response.status!==200||common.res_code!==0||parts.length<2)throw new Error('service request rejected');
  return JSON.parse(parts[1]);
}
const body=(identity,value)=>identity.session+JSON.stringify(value);
async function identity(label){
  report.stage='create_'+label;save();
  const uuid=crypto.randomUUID();const name='ios'+crypto.randomBytes(6).toString('hex');
  const login=await request('/local/client/login',JSON.stringify({uuid,clver:'2.0.4',resource_platform:'iOS',unity_version:'6000.3.26f1'}));
  const account=JSON.parse(login.text);
  if(login.status!==200||typeof account.sess_key!=='string'||!Number.isSafeInteger(account.userid))throw new Error('login schema');
  const value={session:account.sess_key,userId:account.userid};
  const connected=envelope(await request('/Connect',body(value,{})));
  if(connected.is_user!==0)throw new Error('identity is not fresh');
  const created=envelope(await request('/UserCreate',body(value,{name,arthur_type:1})));
  if(created.user?.userid!==value.userId)throw new Error('character mismatch');
  report.steps.push({phase:'identity_'+label,fresh:true,characterCreated:true});save();return value;
}
function project(wire){
  const keys=['arthur_type','job_type','hp','atkp','intp','mndp','support_card_unlock_slot_num'];
  const result=Object.fromEntries(keys.map(key=>[key,wire[key]]));
  for(const key of ['deck','support_deck'])result[key]=(wire[key]||[]).map(card=>({cardid:card.cardid,lv:card.lv,love:card.love,fame:card.fame,skill_lv:card.skill_lv}));
  result.sphrs=(wire.sphrs||[]).map(sphere=>({sphrid:sphere.sphrid,lv:sphere.lv}));
  result.buddys=(wire.buddys||[]).map(buddy=>({buddyid:buddy.buddyid,lv:buddy.lv}));
  return result;
}
const hash=value=>crypto.createHash('sha256').update(JSON.stringify(value)).digest('hex');
function frozenResult(value){const {user,...result}=value;return result;}
function compareResult(first,second){
  return {
    frozenResultIdentical:isDeepStrictEqual(frozenResult(first),frozenResult(second)),
    fullPayloadIdentical:isDeepStrictEqual(first,second),
    changedTopLevelFields:Object.keys({...first,...second}).filter(key=>!isDeepStrictEqual(first[key],second[key])),
    changedUserFields:Object.keys({...first.user,...second.user}).filter(key=>!isDeepStrictEqual(first.user?.[key],second.user?.[key]))
  };
}
let main,engine,startPayload,active=false;
let logName=defeatOnly?'defeat':'victory';
async function step(command){
  const result=await engine.request(command);
  fs.appendFileSync(path.join(logs,logName+'-steps.jsonl'),JSON.stringify({operation:command.operation,selection:command.selection,member:command.member,state:result.state,rows:result.rows})+'\n');
  fs.writeFileSync(path.join(logs,logName+'-input.txt'),result.input_cmd);
  return result;
}
try {
  main=await identity('battle');const viewer=await identity('viewer');
  report.stage='home_and_actual_party';save();
  const before=envelope(await request('/HomeShow',main.session));
  const ownResponse=envelope(await request('/TeamBattleSoloPartnerRentalDeck',body(viewer,{userid:main.userId})));
  const own=ownResponse.partner_deck?.find(deck=>deck.arthur_type===1&&deck.rental_idx===0);
  if(!own)throw new Error('own rental projection unavailable');
  const choices=envelope(await request('/TeamBattleSoloPartnerShow',body(main,{bossid:bossId})));
  const selected=[];const preview=[project(own)];
  for(const arthur of [2,3,4]){
    const group=choices.arthurs?.find(group=>group.arthur_type===arthur);
    const candidate=group?.partners?.find(candidate=>Number.isSafeInteger(candidate.userid)&&candidate.userid>0);
    if(!candidate)throw new Error('partner unavailable');
    selected.push({userid:candidate.userid,arthur_type:arthur,deck_idx:candidate.rental_idx});
    const rental=envelope(await request('/TeamBattleSoloPartnerRentalDeck',body(main,{userid:candidate.userid})));
    const deck=rental.partner_deck?.find(deck=>deck.arthur_type===arthur&&deck.rental_idx===candidate.rental_idx);
    if(!deck)throw new Error('partner rental mismatch');preview.push(project(deck));
  }
  engine=new Order03Engine();
  await engine.request({operation:0,config:{enemy_party_id:bossId,cost_initial:3,hold_max:6,burst_gauge_initial:0,seed:1,continue_allowed:true},enemy_type:1,party:preview});
  report.steps.push({phase:'actual_party_prepared',members:4,cards:preview.map(member=>member.deck.length),allMemberParametersFromService:true});save();
  const startBody=body(main,{bossid:bossId,deck_arthur_type:1,deck_arthur_type_idx:0,partner_deck_selects:selected});
  report.stage='entry';save();
  startPayload=envelope(await request('/TeamBattleSoloStart',startBody));active=true;
  if(startPayload.battles?.length!==1)throw new Error('one-wave probe required');
  const party=[project(own),...startPayload.partner_deck.map(project)];
  const initialized=await engine.request({operation:0,config:{enemy_party_id:startPayload.battles[0].enemy_partyid,cost_initial:startPayload.cost_initial,hold_max:startPayload.hold_max,burst_gauge_initial:startPayload.burst_gauge_initial,seed:startPayload.seed,continue_allowed:true,end_turn:startPayload.end_turn},enemy_type:startPayload.battles[0].enemy_type,party});
  const played=await playRounds(step,await step({operation:1}),initialized.card_metadata,defeatOnly?'pass':'attack');
  let result=played.result;const actions=played.actions;
  const victory=result.state.EndType===1;
  let deadBits=0;for(const enemy of result.state.Members.filter(member=>member.MemberType>=5&&member.HP<=0))deadBits|=1<<(enemy.MemberType-5);
  report.steps.push({phase:'engine_finished',victory,endType:result.state.EndType,turns:result.state.Turn,selectedCardActions:actions,journalBytes:result.input_cmd.length,computedDamage:result.score_damage,enemyDeadBits:deadBits});save();
  report.stage='settlement';save();
  const payload={bossid:bossId,progress:1,is_clear:victory?1:0,input_cmd:[result.input_cmd],enemy_dead_bit:[deadBits],score_damage:result.score_damage};
  const endBody=body(main,payload);const ended=envelope(await request('/TeamBattleSoloEnd',endBody));active=false;
  const retry=envelope(await request('/TeamBattleSoloEnd',endBody));
  const after=envelope(await request('/HomeShow',main.session));
  const compared=compareResult(ended,retry);
  report.steps.push({phase:'settlement',isClear:ended.is_clear,identicalRetry:compared.fullPayloadIdentical,...compared,bpBefore:before.user.bp,bpAfter:after.user.bp,coinBefore:before.user.coin,coinAfter:after.user.coin,coinFreeBefore:before.user.coin_free,coinFreeAfter:after.user.coin_free});
  if(!compared.frozenResultIdentical)throw new Error('frozen settlement receipt changed');
  if(verifyContinue){
    if(!victory||after.user.bp<15||after.user.coin+after.user.coin_free<50)throw new Error('earned continue budget unavailable');
    report.stage='second_entry';save();
    startPayload=envelope(await request('/TeamBattleSoloStart',startBody));active=true;
    logName='defeat';
    const secondParty=[project(own),...startPayload.partner_deck.map(project)];
    const second=await engine.request({operation:0,config:{enemy_party_id:startPayload.battles[0].enemy_partyid,cost_initial:startPayload.cost_initial,hold_max:startPayload.hold_max,burst_gauge_initial:startPayload.burst_gauge_initial,seed:startPayload.seed,continue_allowed:true,end_turn:startPayload.end_turn},enemy_type:startPayload.battles[0].enemy_type,party:secondParty});
    const loss=await playRounds(step,await step({operation:1}),second.card_metadata,'pass',true);
    if(loss.result.state.EndType===1)throw new Error('pass-only run unexpectedly cleared');
    report.stage='paid_continue';save();
    const continueBody=body(main,{pay_type:3,progress:1,input_cmd:[loss.result.input_cmd],enemy_dead_bit:[0]});
    const continued=envelope(await request('/TeamBattleSoloContinue',continueBody));
    const continuedRetry=envelope(await request('/TeamBattleSoloContinue',continueBody));
    const spent=after.user.coin+after.user.coin_free-continued.coin-continued.coin_free;
    if(spent!==50||hash(continued)!==hash(continuedRetry))throw new Error('continue debit or retry mismatch');
    report.steps.push({phase:'paid_continue',realDefeatTurn:loss.result.state.Turn,debitedCrystals:spent,identicalRetry:true});save();
    const revived=await step({operation:13,member:loss.continueMember});
    const lostAgain=await playRounds(step,revived,second.card_metadata,'pass');
    report.stage='defeat_settlement';save();
    const defeatBody=body(main,{bossid:bossId,progress:1,is_clear:0,input_cmd:[lostAgain.result.input_cmd],enemy_dead_bit:[0],score_damage:lostAgain.result.score_damage});
    const defeat=envelope(await request('/TeamBattleSoloEnd',defeatBody));active=false;
    await new Promise(resolve=>setTimeout(resolve,1500));
    const defeatRetry=envelope(await request('/TeamBattleSoloEnd',defeatBody));
    const finalHome=envelope(await request('/HomeShow',main.session));
    const defeatComparison=compareResult(defeat,defeatRetry);
    report.steps.push({phase:'defeat_settlement',isClear:defeat.is_clear,...defeatComparison,coinFreeAfter:finalHome.user.coin_free,bpAfter:finalHome.user.bp});
    if(!defeatComparison.frozenResultIdentical)throw new Error('continued defeat receipt changed');
  }
  report.success=true;report.stage='complete';
}catch(error){
  report.failureClass=error.constructor.name;report.failureStage=report.stage;
  if(active&&main&&startPayload){
    try{envelope(await request('/TeamBattleSoloEnd',body(main,{bossid:bossId,progress:0,is_clear:0,input_cmd:startPayload.battles.map(()=>''),enemy_dead_bit:startPayload.battles.map(()=>0)})));report.steps.push({phase:'failure_abandon',completed:true});active=false;}
    catch{report.steps.push({phase:'failure_abandon',completed:false});}
  }
}finally{
  if(engine)engine.close();
  report.logs=fs.readdirSync(logs).map(name=>({file:path.relative(root,path.join(logs,name)).replaceAll('\\','/'),sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(logs,name))).digest('hex')}));
  save();
}
console.log(JSON.stringify(report,null,2));
if(!report.success)process.exitCode=1;
