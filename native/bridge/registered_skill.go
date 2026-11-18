package main

import (
	"fmt"
	"reflect"
	"strconv"
	"strings"

	"kairisei.local/server/battlemodel"
)

type registeredSkill struct {
	level    battlemodel.SkillLevel
	variants []battlemodel.CombatSkillDefinition
	roles    map[int][]battlemodel.CombatSkillRole
}

func lookupNativeEnum(schema map[string]map[int32]string, family string, number int32) (string, error) {
	value, ok := schema[family][number]
	if !ok {
		return "", fmt.Errorf("native enum %s:%d is unavailable", family, number)
	}
	return value, nil
}

func nativeRoleParameter(schema map[string]map[int32]string, family string, number int32) (string, error) {
	switch family {
	case "", "NULL":
		if number != 0 {
			return "", fmt.Errorf("unused role parameter is nonzero")
		}
		return "", nil
	case "VALUE":
		return strconv.FormatInt(int64(number), 10), nil
	case "ATTR", "SKILL_KIND":
		if number == -1 {
			return "", nil
		}
	case "ATTR_BIT", "SKILL_KIND_BIT":
		if number == -1 {
			return "ALL", nil
		}
		bits := uint32(number)
		if bits == 0 || bits&(bits-1) != 0 {
			return "", fmt.Errorf("role mask has no canonical selector")
		}
		index := int32(0)
		for bits > 1 {
			bits >>= 1
			index++
		}
		return lookupNativeEnum(schema, strings.TrimSuffix(family, "_BIT"), index)
	}
	return lookupNativeEnum(schema, family, number)
}

func decodeRegisteredSkill(schema map[string]map[int32]string, rules map[string]battlemodel.CombatRoleParameterRule, wire []byte) (registeredSkill, error) {
	var result registeredSkill
	if wire == nil {
		return result, nil
	}
	raw, err := decodeRawSkill(wire)
	if err != nil || raw.Header[0] < 0 || raw.Level < 0 {
		return result, fmt.Errorf("invalid registered skill")
	}
	if raw.Header[0] == 0 {
		return result, nil
	}
	header := battlemodel.CombatSkillDefinition{ID: int(raw.Header[0]), Cost: int(raw.Header[7]), PriorityPVE: int(raw.Header[8]), PriorityPVP: int(raw.Header[9]), HateRatio: int(raw.Header[10]), AppendDuration: int(raw.Header[13]), HandSelectCount: int(raw.HandFields[0]), HandMinCost: int(raw.HandFields[3]), HandMaxCost: int(raw.HandFields[4])}
	for i := range header.Groups {
		header.Groups[i] = int(raw.HandFields[5+i])
	}
	fields := []struct {
		family string
		value  int32
		target *string
	}{
		{"SKILL_TYPE", raw.Header[1], &header.Type}, {"SKILL_KIND", raw.Header[2], &header.Kind},
		{"ATTR", raw.Header[3], &header.Attribute}, {"JOB_TYPE", raw.Header[4], &header.Job},
		{"ARTHUR_TYPE", raw.Header[5], &header.ArthurJob}, {"SKILL_PHYSICS_TYPE", raw.Header[6], &header.DamageKind},
		{"SKILL_TARGET", raw.Header[11], &header.Target}, {"SKILL_EXEC_TIMING", raw.Header[12], &header.AppendTrigger},
		{"ATTR", raw.HandFields[1], &header.HandAttribute}, {"SKILL_KIND", raw.HandFields[2], &header.HandKind},
	}
	for _, field := range fields {
		*field.target, err = lookupNativeEnum(schema, field.family, field.value)
		if err != nil {
			return result, err
		}
	}
	if header.AppendTrigger == "NORMAL" {
		header.AppendTrigger = ""
	}
	header.AppendCondition, header.AppendParameters, err = decodeSkillCondition(schema, rawSkillCondition{Order: raw.Header[14], Parameters: raw.HoldParameters})
	if err != nil {
		return result, err
	}
	branches, err := queryBranches(schema, raw)
	if err != nil {
		return result, err
	}
	result.roles = make(map[int][]battlemodel.CombatSkillRole)
	for i, branch := range branches {
		if branch.FunctionID == 0 {
			continue
		}
		ext := raw.Extends[i]
		variant := header
		variant.FunctionID, variant.BranchPriority, variant.DisplayRole = branch.FunctionID, branch.BranchPriority, int(ext.ValueRole)
		variant.BranchCondition, variant.BranchParameters = branch.BranchCondition, branch.BranchParameters
		variant.BranchCondition2, variant.BranchParameters2 = branch.BranchCondition2, branch.BranchParameters2
		roles := make([]battlemodel.CombatSkillRole, 0, 5)
		for slot, rawRole := range ext.Roles {
			if rawRole.Role == 0 {
				if slot == 0 {
					roles = append(roles, battlemodel.CombatSkillRole{SkillID: branch.FunctionID, Function: "NONE", Target: "SELECT", HasTargetAttributes: true})
				}
				continue
			}
			function, e := lookupNativeEnum(schema, "SKILL_ROLE", rawRole.Role)
			if e != nil {
				return result, e
			}
			target, e := lookupNativeEnum(schema, "SKILL_ROLE_TARGET", rawRole.Target)
			if e != nil {
				return result, e
			}
			mask := uint32(rawRole.Attribute)
			if mask&0xfffffc01 != 0 {
				return result, fmt.Errorf("invalid target attribute mask")
			}
			rule, ok := rules[function]
			if !ok {
				return result, fmt.Errorf("missing role parameter rule %s", function)
			}
			role := battlemodel.CombatSkillRole{SkillID: branch.FunctionID, RoleIndex: slot + 1, Function: function, Target: target, ExcludeSelf: rawRole.ExcludeSelf != 0, HasTargetAttributes: true, ChainRate: int(rawRole.Chain), HateLimit: int(rawRole.Hate), PVPRate: int(rawRole.PVP), ValueBoostUse: int(rawRole.ValueBoost)}
			for n := range role.Attributes {
				role.Attributes[n] = mask&(1<<(n+1)) != 0
			}
			for n := range role.Parameters {
				role.Parameters[n], e = nativeRoleParameter(schema, rule.Parameters[n], rawRole.Parameters[n])
				if e != nil {
					return result, e
				}
			}
			roles = append(roles, role)
		}
		if prior, ok := result.roles[branch.FunctionID]; ok && !reflect.DeepEqual(prior, roles) {
			return result, fmt.Errorf("conflicting role set")
		}
		result.roles[branch.FunctionID] = roles
		result.variants = append(result.variants, variant)
	}
	if len(result.variants) == 0 {
		return result, fmt.Errorf("registered skill has no executable definition")
	}
	result.level = battlemodel.SkillLevel{ID: int(raw.Header[0]), Value: int(raw.Level)}
	return result, nil
}
