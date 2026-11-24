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

func TestArthurTypeConditionUsesSchemaAndRejectsInvalidValues(t *testing.T) {
	schema := map[string]map[int32]string{"SKILL_EXTEND_EXEC_ORDER": {107: "SELF_ARTHUR_TYPE"}, "ARTHUR_TYPE": {0: "NULL", 1: "MERCENARY", 5: "ALCHEMIST"}}
	for _, value := range []int64{0, 1, 5} {
		_, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: 107, Parameters: [5]int64{value, -9, 0, 0, 0}})
		if err != nil || params[0] != schema["ARTHUR_TYPE"][int32(value)] || params[1] != "-9" {
			t.Fatalf("value %d: %v %v", value, params, err)
		}
	}
	for _, value := range []int64{6, math.MaxInt64} {
		if _, _, err := decodeSkillCondition(schema, rawSkillCondition{Order: 107, Parameters: [5]int64{value}}); err == nil {
			t.Fatalf("accepted invalid type %d", value)
		}
	}
}

func TestOtherSelectedCardConditionsUseSchemaAndRejectInvalidValues(t *testing.T) {
	schema := map[string]map[int32]string{
		"SKILL_EXTEND_EXEC_ORDER": {5: "SELF_OTHER_PLAY_ATTR", 6: "SELF_OTHER_PLAY_SKILL_KIND", 7: "SELF_OTHER_PLAY_RARITY"},
		"ATTR":                    {1: "FIRE"},
		"SKILL_KIND":              {1: "ATTACK"},
		"RARITY":                  {1: "NORMAL"},
	}
	for _, test := range []struct {
		ordinal int32
		want    string
	}{
		{ordinal: 5, want: "FIRE"},
		{ordinal: 6, want: "ATTACK"},
		{ordinal: 7, want: "NORMAL"},
	} {
		_, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: test.ordinal, Parameters: [5]int64{1, 2, -3, 0, 4}})
		if err != nil || params != [5]string{test.want, "2", "-3", "0", "4"} {
			t.Fatalf("ordinal %d: %v %v", test.ordinal, params, err)
		}
		for _, invalid := range []int64{2, math.MaxInt64} {
			if _, _, err := decodeSkillCondition(schema, rawSkillCondition{Order: test.ordinal, Parameters: [5]int64{invalid}}); err == nil {
				t.Fatalf("ordinal %d accepted invalid selector %d", test.ordinal, invalid)
			}
		}
	}
}

func TestEnemyFlagConditionsPreserveNumericParameters(t *testing.T) {
	for _, name := range []string{"SELF_ENEMY_AI_FLAG", "SELF_ENEMY_AI_FLAG_AND"} {
		for _, ordinal := range []int32{27, 69, 900} {
			schema := map[string]map[int32]string{"SKILL_EXTEND_EXEC_ORDER": {ordinal: name}}
			got, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: ordinal, Parameters: [5]int64{0, 31, -1, math.MinInt64, math.MaxInt64}})
			if err != nil || got != name || params != [5]string{"0", "31", "-1", "-9223372036854775808", "9223372036854775807"} {
				t.Fatalf("%s ordinal %d: %s %v %v", name, ordinal, got, params, err)
			}
		}
	}
}

func TestSelectedCountConditionsPreserveNumericParameters(t *testing.T) {
	for ordinal, name := range map[int32]string{14: "FRIEND_PLAY_NUM", 66: "SELF_PLAY_COST_NUM", 67: "SELF_NOT_PLAY_HAND_NUM"} {
		schema := map[string]map[int32]string{"SKILL_EXTEND_EXEC_ORDER": {ordinal: name}}
		_, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: ordinal, Parameters: [5]int64{math.MinInt64, math.MaxInt64, -1, 0, 1}})
		if err != nil || params != [5]string{"-9223372036854775808", "9223372036854775807", "-1", "0", "1"} {
			t.Fatalf("%s: %v %v", name, params, err)
		}
	}
}

func TestSelectedTagConditionsPreserveNumericParameters(t *testing.T) {
	for ordinal, name := range map[int32]string{28: "SELF_OTHER_PLAY_TAG", 29: "FRIEND_PLAY_TAG"} {
		schema := map[string]map[int32]string{"SKILL_EXTEND_EXEC_ORDER": {ordinal: name}}
		_, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: ordinal, Parameters: [5]int64{30020003, 0, math.MaxInt32, -1, math.MaxInt64}})
		if err != nil || params != [5]string{"30020003", "0", "2147483647", "-1", "9223372036854775807"} {
			t.Fatalf("%s: %v %v", name, params, err)
		}
	}
}

func TestFriendSelectedEnumConditionsUseSchema(t *testing.T) {
	schema := map[string]map[int32]string{
		"SKILL_EXTEND_EXEC_ORDER": {108: "FRIEND_PLAY_ATTR_NUM", 109: "FRIEND_PLAY_SKILL_KIND_NUM"},
		"ATTR":                    {1: "FIRE"},
		"SKILL_KIND":              {1: "ATTACK"},
	}
	for ordinal, want := range map[int32]string{108: "FIRE", 109: "ATTACK"} {
		_, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: ordinal, Parameters: [5]int64{1, 2, -3, 0, 4}})
		if err != nil || params != [5]string{want, "2", "-3", "0", "4"} {
			t.Fatalf("ordinal %d: %v %v", ordinal, params, err)
		}
		if _, _, err := decodeSkillCondition(schema, rawSkillCondition{Order: ordinal, Parameters: [5]int64{math.MaxInt64}}); err == nil {
			t.Fatalf("ordinal %d accepted invalid selector", ordinal)
		}
	}
}

func TestTargetJobTypeConditionUsesSchemaForEverySelector(t *testing.T) {
	schema := map[string]map[int32]string{
		"SKILL_EXTEND_EXEC_ORDER": {15: "TARGET_JOB_TYPE"},
		"JOB_TYPE":                {0: "NULL", 1: "MERCENARY", 2: "MILLIONAIRE", 3: "THIEF", 4: "SINGER"},
	}
	_, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: 15, Parameters: [5]int64{0, 1, 2, 3, 4}})
	if err != nil || params != [5]string{"NULL", "MERCENARY", "MILLIONAIRE", "THIEF", "SINGER"} {
		t.Fatalf("target job selectors: %v %v", params, err)
	}
	for _, invalid := range []int64{5, math.MinInt64, math.MaxInt64} {
		if _, _, err := decodeSkillCondition(schema, rawSkillCondition{Order: 15, Parameters: [5]int64{invalid}}); err == nil {
			t.Fatalf("accepted invalid target job selector %d", invalid)
		}
	}
}

func TestStatusConditionsDecodeTwoKindsAndPreserveNumericTail(t *testing.T) {
	for _, name := range []string{"SELF_BUFF", "TARGET_BUFF", "USER_SIDE_BUFF", "ENEMY_SIDE_BUFF", "SELF_DEBUFF", "TARGET_DEBUFF", "USER_SIDE_DEBUFF", "ENEMY_SIDE_DEBUFF"} {
		family := "SKILL_ROLE_KIND_BUFF"
		if name == "SELF_DEBUFF" || name == "TARGET_DEBUFF" || name == "USER_SIDE_DEBUFF" || name == "ENEMY_SIDE_DEBUFF" {
			family = "SKILL_ROLE_KIND_DEBUFF"
		}
		schema := map[string]map[int32]string{
			"SKILL_EXTEND_EXEC_ORDER": {900: name},
			family:                    {0: "NULL", 1: "SELECTED_KIND"},
		}
		got, params, err := decodeSkillCondition(schema, rawSkillCondition{Order: 900, Parameters: [5]int64{1, 0, math.MinInt64, math.MaxInt64, -1}})
		if err != nil || got != name || params != [5]string{"SELECTED_KIND", "NULL", "-9223372036854775808", "9223372036854775807", "-1"} {
			t.Fatalf("%s: %s %v %v", name, got, params, err)
		}
		for _, invalid := range []int64{2, math.MinInt64, math.MaxInt64} {
			for slot := 0; slot < 2; slot++ {
				condition := rawSkillCondition{Order: 900}
				condition.Parameters[slot] = invalid
				if _, _, err = decodeSkillCondition(schema, condition); err == nil {
					t.Fatalf("%s slot %d accepted invalid selector %d", name, slot, invalid)
				}
			}
		}
		delete(schema, family)
		if _, _, err = decodeSkillCondition(schema, rawSkillCondition{Order: 900}); err == nil {
			t.Fatalf("%s accepted absent kind family", name)
		}
	}
}
