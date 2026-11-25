export async function playRounds(step, result, metadata, strategy, stopOnKO=false) {
  const retired=new Set();
  let actions=0;
  for(let turn=0;turn<50&&!result.state.Ended;turn++){
    result=await step({operation:2});
    if(result.state.Ended)break;
    result=await step({operation:3});
    const phase=result;
    for(let member=1;member<=4;member++){
      const actor=result.state.Members.find(value=>value.MemberType===member);
      if(!actor||actor.HP<=0)continue;
      const choice={member_type:member};
      if(strategy==='attack'){
        const states=new Map(phase.rows.filter(row=>row.Command===29&&row.Args[0]===member).map(row=>[row.Args[1],row.Args[2]]));
        const candidate=phase.rows.find(row=>row.Command===27&&row.Args[0]===member&&row.Args[2]<=actor.Cost&&(states.get(row.Args[1])??0)===0);
        if(candidate){
          const cardType=candidate.Args[1];
          const card=metadata[member-1].find(card=>card.card_type===cardType);
          const enemy=result.state.Members.find(value=>value.MemberType>=5&&value.HP>0);
          const target=card?.target?.startsWith('ENEMY')?enemy?.MemberType:member;
          if(target){choice.card_types=[cardType,0,0,0,0];choice.targets=[target,0,0,0,0];actions++;}
        }
      }
      result=await step({operation:4,selection:choice});
    }
    for(const operation of [5,8,6,9]){
      result=await step({operation});
      if(result.state.Ended)break;
    }
    for(const actor of result.state.Members.filter(actor=>actor.MemberType<=4&&actor.HP<=0&&!retired.has(actor.MemberType))){
      if(stopOnKO)return {result,actions,continueMember:actor.MemberType};
      result=await step({operation:11,member:actor.MemberType});
      retired.add(actor.MemberType);
      if(result.state.Ended)break;
    }
  }
  if(!result.state.Ended)throw new Error('battle did not finish within probe bound');
  return {result,actions};
}
