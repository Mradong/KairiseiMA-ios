package main

import (
	"encoding/binary"
	"testing"

	"kairisei.local/server/battlemodel"
)

func TestDecodePreparedEnemyFieldsAndGuards(t *testing.T) {
	data := make([]byte, 276)
	put32 := func(offset int, value uint32) { binary.LittleEndian.PutUint32(data[offset:], value) }
	put64 := func(offset int, value uint64) { binary.LittleEndian.PutUint64(data[offset:], value) }
	put32(0, 901)
	put32(4, 13)
	put64(8, 0)
	put64(16, 1000)
	put32(24, 25)
	put64(28, 345)
	put64(36, 456)
	put64(44, 567)
	put64(52, 678)
	put64(60, 789)
	put32(68, 42)
	binary.LittleEndian.PutUint16(data[72:], 65535)
	put32(92, 11)
	put32(112, 2)
	put32(116, 3)
	put32(152, 2)
	put32(156, 4)
	binary.LittleEndian.PutUint16(data[120:], 65534)
	binary.LittleEndian.PutUint16(data[142:], 65533)
	enemy := &preparedEnemy{data: data}
	got, err := decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0, false)
	if err != nil {
		t.Fatal(err)
	}
	if got.HP != 250 || got.MaxHP != 250 || got.Parent != 2 || got.Definition.Attack != 345 || got.Definition.MagicDefense != 789 || got.Definition.TribeIDs[0] != 13 || got.Definition.Attribute != "ICE" || got.Definition.AttributeFixed[0] != 11 || got.Level.AttributeRates[0] != -1 || got.Level.StatusResistances[1] != -2 || got.Level.DOTReductions[0] != -3 || got.Level.InitialAwake != 4 {
		t.Fatalf("unexpected enemy projection: %+v", got)
	}
	put32(160, 1)
	if _, err = decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0, false); err == nil {
		t.Fatal("unimplemented gauge must be rejected")
	}
	put32(160, 0)
	enemy.normal[0] = make([]byte, 2656)
	if _, err = decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0, false); err != nil {
		t.Fatal("empty enemy skill must be ignored", err)
	}
	binary.LittleEndian.PutUint32(enemy.normal[0], 1001)
	if _, err = decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0, false); err == nil {
		t.Fatal("unimplemented enemy program must be rejected")
	}
}

func TestPreparedEnemyInertMasterGaugeIsQueryOnly(t *testing.T) {
	data := make([]byte, 276)
	binary.LittleEndian.PutUint32(data, 901)
	binary.LittleEndian.PutUint32(data[112:], 2)
	enemy := &preparedEnemy{data: data}
	schema := map[string]map[int32]string{"ATTR": {2: "ICE"}}
	master := map[int]battlemodel.CombatEnemyDefinition{901: {ID: 901, ReactiveGauge: &battlemodel.EnemyGaugeDefinition{}}}
	if _, err := decodePreparedEnemy(schema, master, enemy, 0, true); err != nil {
		t.Fatalf("inert gauge query rejected: %v", err)
	}
	if _, err := decodePreparedEnemy(schema, master, enemy, 0, false); err == nil {
		t.Fatal("unverified battle projection accepted")
	}
	for name, mutate := range map[string]func(*battlemodel.CombatEnemyDefinition){
		"reaction ID":         func(e *battlemodel.CombatEnemyDefinition) { e.GaugeReactionID = 1 },
		"master state":        func(e *battlemodel.CombatEnemyDefinition) { e.InitialGaugeState = 1 },
		"master rate":         func(e *battlemodel.CombatEnemyDefinition) { e.InitialGaugeRate = 1 },
		"trance limit":        func(e *battlemodel.CombatEnemyDefinition) { e.TranceLimit = 1 },
		"overheat limit":      func(e *battlemodel.CombatEnemyDefinition) { e.OverheatLimit = 1 },
		"overheat duration":   func(e *battlemodel.CombatEnemyDefinition) { e.OverheatTurns = 1 },
		"overheat resistance": func(e *battlemodel.CombatEnemyDefinition) { e.OverheatResist = 1 },
		"gauge state":         func(e *battlemodel.CombatEnemyDefinition) { e.ReactiveGauge.InitialState = 1 },
		"gauge rate":          func(e *battlemodel.CombatEnemyDefinition) { e.ReactiveGauge.InitialRate = 1 },
		"gauge reaction": func(e *battlemodel.CombatEnemyDefinition) {
			e.ReactiveGauge.Reactions = []battlemodel.EnemyGaugeReaction{{Kind: "DAMAGE"}}
		},
	} {
		active := battlemodel.CombatEnemyDefinition{ID: 901, ReactiveGauge: &battlemodel.EnemyGaugeDefinition{}}
		mutate(&active)
		master[901] = active
		if _, err := decodePreparedEnemy(schema, master, enemy, 0, true); err == nil {
			t.Fatalf("active gauge query accepted: %s", name)
		}
	}
}

func TestSimpleEnemyActionRejectsUnsupportedInput(t *testing.T) {
	schema := map[string]map[int32]string{
		"ENEMY_AI_TARGET":      {0: "NULL", 1: "RANDOM", 23: "ENEMY1", 47: "MERCENARY", 48: "MILLIONAIRE", 49: "THIEF", 50: "SINGER"},
		"ENEMY_AI_ORDER_PARTY": {0: "NULL"}, "ENEMY_AI_ORDER_TRANCE_GAUGE": {0: "NULL"}, "ENEMY_AI_ORDER_TRIGGER": {0: "NULL"},
	}
	raw := make([]byte, 2656)
	binary.LittleEndian.PutUint32(raw[0:], 2001)
	binary.LittleEndian.PutUint32(raw[2440:], 23)
	binary.LittleEndian.PutUint32(raw[2484:], 1)
	binary.LittleEndian.PutUint32(raw[2488:], 10)
	binary.LittleEndian.PutUint32(raw[2492:], 100)
	binary.LittleEndian.PutUint16(raw[2540:], 1)
	action, err := decodeSimpleEnemyAction(schema, raw, 0)
	if err != nil || action.SkillID != 2001 || action.Target != "ENEMY1" || action.Rate != 100 || action.Order.Fields[7] != "1" {
		t.Fatalf("unexpected action: %+v %v", action, err)
	}
	if _, err := decodeSimpleEnemyAction(schema, raw, 15); err == nil {
		t.Fatal("unsupported skill slot accepted")
	}
	if _, err := decodeSimpleEnemyAction(schema, raw, 1); err == nil {
		t.Fatal("unsupported skill-slot target accepted")
	}
	binary.LittleEndian.PutUint32(raw[2440:], 47)
	if skill, err := decodeSimpleEnemyAction(schema, raw, 1); err != nil || skill.Category != "skill" {
		t.Fatalf("supported skill slot rejected: %+v %v", skill, err)
	}
	for _, selector := range []uint32{47, 48, 49, 50} {
		binary.LittleEndian.PutUint32(raw[2440:], selector)
		if _, err := decodeSimpleEnemyAction(schema, raw, 0); err != nil {
			t.Fatalf("profession selector %d rejected: %v", selector, err)
		}
	}
	binary.LittleEndian.PutUint32(raw[2440:], 0)
	if _, err := decodeSimpleEnemyAction(schema, raw, 0); err == nil {
		t.Fatal("unsupported null selector accepted")
	}
	binary.LittleEndian.PutUint32(raw[2440:], 23)
	binary.LittleEndian.PutUint32(raw[2588:], 1)
	if _, err := decodeSimpleEnemyAction(schema, raw, 0); err == nil {
		t.Fatal("unsupported trigger accepted")
	}
}
