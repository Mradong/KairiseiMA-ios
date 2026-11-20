package main

import (
	"fmt"

	"kairisei.local/server/battlemodel"
)

const sphereWireSize = 7328

// SphrData: id/count/type, three SkillData records, then play condition/param.
func decodePreparedSphere(schema map[string]map[int32]string, rules map[string]battlemodel.CombatRoleParameterRule, raw []byte, slot int, masters map[int]battlemodel.CombatSphereDefinition, install func(registeredSkill) error) (battlemodel.CombatSphereDefinition, battlemodel.Sphere, error) {
	var instance battlemodel.Sphere
	if len(raw) != sphereWireSize || slot < 1 || slot > 3 {
		return battlemodel.CombatSphereDefinition{}, instance, fmt.Errorf("invalid registered sphere record")
	}
	id := int(wireInt(raw, 0))
	definition, exists := masters[id]
	if !exists {
		return definition, instance, fmt.Errorf("registered sphere has no master metadata")
	}
	kind, err := lookupNativeEnum(schema, "SPHR_TYPE", wireInt(raw, 8))
	if err != nil || kind != "NORMAL" && kind != "CHALICE" {
		return definition, instance, fmt.Errorf("unsupported registered sphere type")
	}
	condition, err := lookupNativeEnum(schema, "SPHR_PLAY_CONDITION", wireInt(raw, 7320))
	if err != nil || condition != "NONE" && condition != "TURN_AFTER" {
		return definition, instance, fmt.Errorf("unsupported registered sphere play condition")
	}
	count, parameter := wireInt(raw, 4), wireInt(raw, 7324)
	if count < 0 || parameter < 0 {
		return definition, instance, fmt.Errorf("negative registered sphere count/condition")
	}
	instance = battlemodel.Sphere{SphereType: slot, SphereID: id}
	for index := range instance.SkillLevels {
		offset := 12 + index*skillWireSize
		skill, err := decodeRegisteredSkill(schema, rules, raw[offset:offset+skillWireSize])
		if err != nil {
			return definition, instance, err
		}
		if err = install(skill); err != nil {
			return definition, instance, err
		}
		instance.SkillLevels[index] = skill.level
		for _, previous := range instance.SkillLevels[:index] {
			if skill.level.ID != 0 && previous.ID == skill.level.ID && previous.Value != skill.level.Value {
				return definition, instance, fmt.Errorf("conflicting registered sphere skill levels")
			}
		}
	}
	if instance.SkillLevels[0].ID == 0 || instance.SkillLevels[0].Value < 1 {
		return definition, instance, fmt.Errorf("registered sphere requires an executable primary skill")
	}
	instance.Level = instance.SkillLevels[0].Value
	definition.Type, definition.Count = kind, int(count)
	definition.PlayCondition, definition.PlayParam = condition, int(parameter)
	definition.SkillID, definition.CallSkillID, definition.PassiveSkillID = instance.SkillLevels[0].ID, instance.SkillLevels[1].ID, instance.SkillLevels[2].ID
	return definition, instance, nil
}
