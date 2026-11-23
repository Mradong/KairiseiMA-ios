package main

import (
	"math"
	"testing"
)

func TestQueryBranchesNumericAndUnsupported(t *testing.T) {
	schema := map[string]map[int32]string{"SKILL_EXTEND_EXEC_ORDER": {0: "NONE", 8: "SELF_HP_PER", 2: "TARGET_ATTR"}, "ATTR": {0: "FIRE"}}
	var raw rawSkill
	raw.Extends[3].RoleID = 42
	raw.Extends[3].Priority = -5
	raw.Extends[3].Conditions[0].Order = 8
	raw.Extends[3].Conditions[0].Parameters = [5]int64{math.MinInt64, math.MaxInt64, -1, 0, 1}
	got, err := queryBranches(schema, raw)
	if err != nil {
		t.Fatal(err)
	}
	if got[3].FunctionID != 42 || got[3].BranchPriority != -5 || got[3].BranchCondition != "SELF_HP_PER" || got[3].BranchParameters[0] != "-9223372036854775808" || got[3].BranchParameters[1] != "9223372036854775807" || got[3].BranchCondition2 != "" {
		t.Fatalf("unexpected conversion: %+v", got[3])
	}
	raw.Extends[3].Conditions[0].Order = 2
	raw.Extends[3].Conditions[0].Parameters[0] = 0
	got, err = queryBranches(schema, raw)
	if err != nil || got[3].BranchParameters[0] != "FIRE" {
		t.Fatalf("attribute conversion: %+v %v", got[3], err)
	}
	raw.Extends[3].Conditions[0].Parameters[0] = math.MaxInt64
	if _, err = queryBranches(schema, raw); err == nil {
		t.Fatal("out of range enum accepted")
	}
	raw.Extends[3].RoleID = 0
	if _, err = queryBranches(nil, raw); err != nil {
		t.Fatal("empty slot should not decode conditions", err)
	}
}

func TestEnemyVariableConditionUsesSchemaAndPreservesPackedBounds(t *testing.T) {
	for _, ordinal := range []int32{110, 111, 900} {
		for slot := 0; slot < 2; slot++ {
			schema := map[string]map[int32]string{"SKILL_EXTEND_EXEC_ORDER": {0: "NONE", ordinal: "SELF_ENEMY_AI_VAR"}}
			var raw rawSkill
			raw.Extends[1].RoleID = 889
			raw.Extends[1].Conditions[slot] = rawSkillCondition{Order: ordinal, Parameters: [5]int64{4, math.MinInt64, math.MaxInt64, 0, 0}}
			got, err := queryBranches(schema, raw)
			if err != nil {
				t.Fatal(err)
			}
			name, params := got[1].BranchCondition, got[1].BranchParameters
			if slot == 1 {
				name, params = got[1].BranchCondition2, got[1].BranchParameters2
			}
			if name != "SELF_ENEMY_AI_VAR" || params != [5]string{"4", "-9223372036854775808", "9223372036854775807", "0", "0"} {
				t.Fatalf("ordinal %d slot %d: %s %v", ordinal, slot, name, params)
			}
			// Same ordinal with a different label must retain its own support boundary.
			schema["SKILL_EXTEND_EXEC_ORDER"][ordinal] = "SELF_STYLE"
			if _, err = queryBranches(schema, raw); err == nil {
				t.Fatal("unsupported STYLE accepted as an AI variable")
			}
			delete(schema["SKILL_EXTEND_EXEC_ORDER"], ordinal)
			if _, err = queryBranches(schema, raw); err == nil {
				t.Fatal("missing mapping accepted")
			}
		}
	}
	_, params, err := decodeSkillCondition(map[string]map[int32]string{"SKILL_EXTEND_EXEC_ORDER": {1: "SELF_ENEMY_AI_VAR"}}, rawSkillCondition{Order: 1})
	if err != nil || params != [5]string{"0", "0", "0", "0", "0"} {
		t.Fatalf("packed explicit zeros changed: %v %v", params, err)
	}
}
