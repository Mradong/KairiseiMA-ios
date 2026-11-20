package main

import (
	"encoding/binary"
	"testing"

	"kairisei.local/server/battlemodel"
)

func TestRegisteredBuddyGuardsAndMasterOwnership(t *testing.T) {
	master := battlemodel.CombatBuddyDefinition{ID: 1, MaxLevel: 10, PassiveSkillID: 99}
	masters := map[int]battlemodel.CombatBuddyDefinition{1: master}
	raw := make([]byte, buddyWireSize)
	put := func(offset int, value int32) { binary.LittleEndian.PutUint32(raw[offset:], uint32(value)) }
	put(0, 1)
	put(4, 1)
	install := func(registeredSkill) error { return nil }
	definition, _, err := decodePreparedBuddy(nil, nil, raw, 1, masters, install)
	if err != nil || definition.PassiveSkillID != 0 || masters[1] != master {
		t.Fatalf("registered overrides must not alter source metadata: %v", err)
	}
	for _, offset := range []int{4880, 4884, 4888} {
		put(offset, -1)
		if _, _, err := decodePreparedBuddy(nil, nil, raw, 1, masters, install); err == nil {
			t.Fatalf("negative burst field at %d was accepted", offset)
		}
		put(offset, 0)
	}
	if _, _, err := decodePreparedBuddy(nil, nil, raw[:len(raw)-1], 1, masters, install); err == nil {
		t.Fatal("short buddy accepted")
	}
	if _, _, err := decodePreparedBuddy(nil, nil, raw, 1, nil, install); err == nil {
		t.Fatal("buddy without master metadata accepted")
	}
	if _, _, err := decodePreparedBuddy(nil, nil, raw, 6, masters, install); err == nil {
		t.Fatal("out-of-range buddy slot accepted")
	}
	schema := map[string]map[int32]string{
		"SKILL_TYPE": {0: "ATTACK"}, "SKILL_KIND": {0: "ATTACK"}, "ATTR": {0: "FIRE"},
		"JOB_TYPE": {0: "NONE"}, "ARTHUR_TYPE": {0: "NULL"}, "SKILL_PHYSICS_TYPE": {0: "PHYSICS"},
		"SKILL_TARGET": {0: "ENEMY_ONE"}, "SKILL_EXEC_TIMING": {0: "NORMAL"}, "SKILL_EXTEND_EXEC_ORDER": {0: "NONE"},
	}
	for index := 0; index < 2; index++ {
		start := 8 + index*4884
		put(start, 7)
		put(start+228, 1)
		put(start+2432, int32(index+1))
	}
	if _, _, err := decodePreparedBuddy(schema, nil, raw, 1, masters, install); err == nil {
		t.Fatal("same skill ID with conflicting execution levels cannot be represented safely")
	}
	put(8+4884+2432, 1)
	if _, _, err := decodePreparedBuddy(schema, nil, raw, 1, masters, install); err != nil {
		t.Fatalf("same skill ID with equal execution levels rejected: %v", err)
	}
}
