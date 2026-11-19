package main

import (
	"encoding/binary"
	"fmt"
	"strconv"

	"kairisei.local/server/battlemodel"
)

// Enemy programs and reactive gauges need separate decoding and are rejected.
func decodePreparedEnemy(schema map[string]map[int32]string, master map[int]battlemodel.CombatEnemyDefinition, enemy *preparedEnemy, index int) (battlemodel.EnemyInput, error) {
	var value battlemodel.EnemyInput
	if enemy == nil || len(enemy.data) != 276 || index < 0 || index > 3 {
		return value, fmt.Errorf("invalid enemy record")
	}
	for slot, skill := range enemy.normal {
		if skill != nil && wireInt(skill, 0) != 0 {
			action, err := decodeSimpleEnemyAction(schema, skill, slot)
			if err != nil {
				return value, err
			}
			value.Level.Actions = append(value.Level.Actions, action)
		}
	}
	for _, skill := range enemy.passive {
		if skill != nil && wireInt(skill, 0) != 0 {
			return value, fmt.Errorf("enemy passive projection unavailable")
		}
	}
	for _, skill := range enemy.booster {
		if skill != nil && wireInt(skill, 0) != 0 {
			return value, fmt.Errorf("enemy gauge skill projection unavailable")
		}
	}
	if (enemy.boost != nil && wireInt(enemy.boost, 0) != 0) || len(enemy.gaugeReactions) != 0 {
		return value, fmt.Errorf("enemy gauge projection unavailable")
	}
	raw := enemy.data
	for _, offset := range []int{160, 164, 168, 172} {
		if wireInt(raw, offset) != 0 {
			return value, fmt.Errorf("enemy reactive gauge projection unavailable")
		}
	}
	i64 := func(offset int) int64 { return int64(binary.LittleEndian.Uint64(raw[offset:])) }
	i16 := func(offset int) int { return int(int16(binary.LittleEndian.Uint16(raw[offset:]))) }
	id, tribe := int(wireInt(raw, 0)), int(wireInt(raw, 4))
	attribute, err := lookupNativeEnum(schema, "ATTR", wireInt(raw, 112))
	if err != nil {
		return value, err
	}
	hp, maxHP := i64(8), i64(16)
	if hp == 0 {
		hp = maxHP
	}
	if rate := int64(wireInt(raw, 24)); rate != 0 {
		hp, maxHP = hp*rate/100, maxHP*rate/100
	}
	def := battlemodel.CombatEnemyDefinition{ID: id, TribeIDs: []int{tribe}, Attribute: attribute, HP: int(maxHP), Attack: int(i64(28)), Magic: int(i64(36)), Recovery: int(i64(44)), Defense: int(i64(52)), MagicDefense: int(i64(60)), DamageReduction: int(wireInt(raw, 68))}
	for i := range def.AttributeFixed {
		def.AttributeFixed[i] = int(wireInt(raw, 92+i*4))
	}
	if existing, ok := master[id]; ok {
		if existing.ReactiveGauge != nil || existing.GaugeReactionID != 0 {
			return value, fmt.Errorf("enemy master gauge projection unavailable")
		}
		if len(existing.TribeIDs) >= 2 {
			if len(existing.TribeIDs) != 3 || existing.TribeIDs[0] != tribe {
				return value, fmt.Errorf("enemy tribe extension differs from master")
			}
			def.TribeIDs = append([]int(nil), existing.TribeIDs...)
		}
	}
	level := battlemodel.CombatEnemyLevel{ID: id, InitialAwake: int(wireInt(raw, 156)), ActionsPerTurn: int(wireInt(raw, 116))}
	for i := range level.AttributeRates {
		level.AttributeRates[i] = i16(72 + i*2)
	}
	for i := 1; i <= 11; i++ {
		level.StatusResistances[i] = i16(118 + i*2)
	}
	for i := range level.DOTReductions {
		level.DOTReductions[i] = i16(142 + i*2)
	}
	level.Actions = value.Level.Actions
	value = battlemodel.EnemyInput{Definition: def, Level: level, HP: int(hp), MaxHP: int(maxHP), Parent: int(wireInt(raw, 152))}
	count := int(wireInt(raw, 176))
	if count < 0 || count > 8 {
		return value, fmt.Errorf("invalid enemy drops")
	}
	for i := 0; i < count; i++ {
		offset := 180 + i*12
		value.Drops = append(value.Drops, battlemodel.Drop{EnemyIndex: index, RewardType: int(wireInt(raw, offset)), Num: int(wireInt(raw, offset+4)), RewardTypeID: int(wireInt(raw, offset+8))})
	}
	return value, nil
}

// Simple registered actions currently allow only unconditional selectors. Other
// enum-dependent targets/triggers remain explicit errors until differential tests exist.
func decodeSimpleEnemyAction(schema map[string]map[int32]string, raw []byte, slot int) (battlemodel.CombatEnemyAction, error) {
	var action battlemodel.CombatEnemyAction
	if len(raw) != 2656 || slot < 0 || slot > 14 {
		return action, fmt.Errorf("unsupported enemy action slot")
	}
	target, err := lookupNativeEnum(schema, "ENEMY_AI_TARGET", wireInt(raw, 2440))
	if err != nil || (target != "ENEMY1" && target != "RANDOM" && target != "MERCENARY" && target != "MILLIONAIRE" && target != "THIEF" && target != "SINGER") {
		return action, fmt.Errorf("unsupported enemy target")
	}
	if slot > 0 && target != "MERCENARY" {
		return action, fmt.Errorf("unsupported target for enemy skill slot")
	}
	for _, off := range []int{2500, 2504, 2588} {
		if wireInt(raw, off) != 0 {
			return action, fmt.Errorf("unsupported enemy condition")
		}
	}
	for _, family := range []string{"ENEMY_AI_ORDER_PARTY", "ENEMY_AI_ORDER_TRANCE_GAUGE", "ENEMY_AI_ORDER_TRIGGER"} {
		name, e := lookupNativeEnum(schema, family, 0)
		if e != nil || name != "NULL" {
			return action, fmt.Errorf("missing empty enemy condition")
		}
	}
	fields := make([]string, 38)
	fields[1], fields[2], fields[29] = "NULL", "NULL", "NULL"
	for i := 0; i < 4; i++ {
		fields[3+i] = strconv.FormatInt(int64(binary.LittleEndian.Uint64(raw[2508+i*8:])), 10)
	}
	for i := 0; i < 11; i++ {
		fields[7+i] = strconv.Itoa(int(int16(binary.LittleEndian.Uint16(raw[2540+i*2:]))))
	}
	fields[18] = strconv.Itoa(int(wireInt(raw, 2584)))
	for i := 0; i < 10; i++ {
		fields[19+i] = strconv.Itoa(int(int16(binary.LittleEndian.Uint16(raw[2562+i*2:]))))
	}
	for i := 0; i < 8; i++ {
		fields[30+i] = strconv.FormatInt(int64(binary.LittleEndian.Uint64(raw[2592+i*8:])), 10)
	}
	action = battlemodel.CombatEnemyAction{Slot: slot, SkillID: int(wireInt(raw, 0)), Priority: int(wireInt(raw, 2436)), Target: target,
		ActionCost: int(wireInt(raw, 2484)), MaxUses: int(wireInt(raw, 2488)), Rate: int(wireInt(raw, 2492)), CountOnMiss: wireInt(raw, 2496) != 0,
		Order: &battlemodel.CombatEnemyAIOrder{Fields: fields}}
	for i := range action.TargetParams {
		action.TargetParams[i] = strconv.FormatInt(int64(binary.LittleEndian.Uint64(raw[2444+i*8:])), 10)
	}
	switch {
	case slot == 0:
		action.Category = "normal"
	case slot < 15:
		action.Category = "skill"
	case slot < 20:
		action.Category = "special"
	default:
		action.Category = "death"
	}
	return action, nil
}
