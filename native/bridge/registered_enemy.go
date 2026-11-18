package main

import (
	"encoding/binary"
	"fmt"

	"kairisei.local/server/battlemodel"
)

// Enemy programs and reactive gauges need separate decoding and are rejected.
func decodePreparedEnemy(schema map[string]map[int32]string, master map[int]battlemodel.CombatEnemyDefinition, enemy *preparedEnemy, index int) (battlemodel.EnemyInput, error) {
	var value battlemodel.EnemyInput
	if enemy == nil || len(enemy.data) != 276 || index < 0 || index > 3 {
		return value, fmt.Errorf("invalid enemy record")
	}
	for _, skill := range enemy.normal {
		if skill != nil {
			return value, fmt.Errorf("enemy program projection unavailable")
		}
	}
	for _, skill := range enemy.passive {
		if skill != nil {
			return value, fmt.Errorf("enemy passive projection unavailable")
		}
	}
	for _, skill := range enemy.booster {
		if skill != nil {
			return value, fmt.Errorf("enemy gauge skill projection unavailable")
		}
	}
	if enemy.boost != nil || len(enemy.gaugeReactions) != 0 {
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
