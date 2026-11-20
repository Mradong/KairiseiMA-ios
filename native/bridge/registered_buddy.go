package main

import (
	"fmt"
	"kairisei.local/server/battlemodel"
)

// BuddyData is two int32s, three 4884-byte BurstSkillData records,
// and one SkillData. BurstSkillData holds two skills then gauge,
// recharge_turn and required_lv (modern recovered C# layout).
func decodePreparedBuddy(schema map[string]map[int32]string, rules map[string]battlemodel.CombatRoleParameterRule, raw []byte, slot int, masters map[int]battlemodel.CombatBuddyDefinition, install func(registeredSkill) error) (battlemodel.CombatBuddyDefinition, battlemodel.Buddy, error) {
	var instance battlemodel.Buddy
	if len(raw) != buddyWireSize || slot < 1 || slot > 5 {
		return battlemodel.CombatBuddyDefinition{}, instance, fmt.Errorf("invalid registered buddy record")
	}
	id := int(wireInt(raw, 0))
	definition, exists := masters[id]
	if !exists {
		return definition, instance, fmt.Errorf("registered buddy has no master metadata")
	}
	instance = battlemodel.Buddy{BuddyType: slot, BuddyID: id, Level: int(wireInt(raw, 4))}
	for index := range definition.Skills {
		start := 8 + index*4884
		for kind := 0; kind < 2; kind++ {
			offset := start + kind*skillWireSize
			skill, err := decodeRegisteredSkill(schema, rules, raw[offset:offset+skillWireSize])
			if err != nil {
				return definition, instance, err
			}
			if err = install(skill); err != nil {
				return definition, instance, err
			}
			instance.SkillLevels[index*2+kind] = skill.level
			if kind == 0 {
				definition.Skills[index].SkillID = skill.level.ID
			} else {
				definition.Skills[index].CallSkillID = skill.level.ID
			}
		}
		gauge, recharge, required := wireInt(raw, start+4872), wireInt(raw, start+4876), wireInt(raw, start+4880)
		if gauge < 0 || recharge < 0 || required < 0 {
			return definition, instance, fmt.Errorf("negative registered buddy burst configuration")
		}
		definition.Skills[index].GaugeCost = int(gauge)
		definition.Skills[index].RechargeTurn = int(recharge)
		definition.Skills[index].RequiredLevel = int(required)
	}
	passive, err := decodeRegisteredSkill(schema, rules, raw[14660:])
	if err != nil {
		return definition, instance, err
	}
	if err = install(passive); err != nil {
		return definition, instance, err
	}
	definition.PassiveSkillID = passive.level.ID
	instance.SkillLevels[6] = passive.level
	for i, skill := range instance.SkillLevels {
		for _, previous := range instance.SkillLevels[:i] {
			if skill.ID != 0 && previous.ID == skill.ID && previous.Value != skill.Value {
				return definition, instance, fmt.Errorf("conflicting registered buddy skill levels")
			}
		}
	}
	return definition, instance, nil
}
