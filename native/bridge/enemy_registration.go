package main

import "kairisei.local/server/battlemodel"

type preparedEnemy struct {
	data           []byte
	normal         [21][]byte
	passive        [10][]byte
	boost          []byte
	booster        [3][]byte
	gaugeReactions [][17]int32
}

type preparedStoryHelp struct {
	data   []byte
	target int32
}

func (p *preparation) clearEnemies() int {
	p.enemies = [4]*preparedEnemy{}
	p.enemiesRegistered = true
	return statusOK
}

func (p *preparation) setEnemy(member int, raw []byte) int {
	if member < 5 || member > 8 || len(raw) != 276 || wireInt(raw, 0) <= 0 || wireInt(raw, 176) < 0 || wireInt(raw, 176) > 8 {
		return statusInvalid
	}
	// A new base enemy replaces its prior skills and gauge reactions.
	p.enemies[member-5] = &preparedEnemy{data: append([]byte(nil), raw...)}
	p.enemiesRegistered = true
	return statusOK
}

func (p *preparation) setEnemySkill(member, kind, slot int, raw []byte) int {
	if member < 5 || member > 8 || p.enemies[member-5] == nil {
		return statusInvalid
	}
	var target *[]byte
	enemy := p.enemies[member-5]
	switch {
	case kind == 0 && slot >= 0 && slot <= 20 && len(raw) == 2656:
		target = &enemy.normal[slot]
	case kind == 1 && slot >= 0 && slot < 10 && len(raw) == skillWireSize:
		target = &enemy.passive[slot]
	case kind == 2 && slot == 0 && len(raw) == skillWireSize:
		target = &enemy.boost
	case kind == 3 && slot >= 0 && slot < 3 && len(raw) == 2656:
		target = &enemy.booster[slot]
	default:
		return statusInvalid
	}
	if !validRegisteredSkill(raw[:skillWireSize]) {
		return statusInvalid
	}
	*target = append([]byte(nil), raw...)
	return statusOK
}

func (p *preparation) setStoryHelp(raw []byte, target int32) int {
	if p.mode != 2 || len(raw) != 4980 {
		return statusInvalid
	}
	p.storyHelp = &preparedStoryHelp{data: append([]byte(nil), raw...), target: target}
	return statusOK
}

func (p *preparation) initialize(kind int, values []int32) int {
	switch kind {
	case 1:
		if len(values) != 6 {
			return statusInvalid
		}
		var flags [6]int32
		for i, value := range values {
			if value != 0 && value != 1 {
				return statusInvalid
			}
			flags[i] = value
		}
		p.settings.awake = &flags
	case 2:
		if len(values) != 44 {
			return statusInvalid
		}
		var rates [4][2][5]int
		var seen [4]bool
		for row := 0; row < 4; row++ {
			kind := values[row*11]
			if kind < 0 || kind > 3 || seen[kind] {
				return statusInvalid
			}
			seen[kind] = true
			for state := 0; state < 2; state++ {
				for cost := 0; cost < 5; cost++ {
					rates[kind][state][cost] = int(values[row*11+1+state*5+cost])
				}
			}
		}
		p.settings.trance = &rates
	case 3:
		if len(values) != 9 || values[0] < 1 || values[1] < values[0] || values[2] < 0 {
			return statusInvalid
		}
		burst := &battlemodel.CombatBurstGaugeConfig{NormalThreshold: int(values[0]), Maximum: int(values[1]), BreakTurns: int(values[2]), DamageReduction: int(values[3])}
		for i := range burst.ReactionRates {
			burst.ReactionRates[i] = int(values[i+4])
		}
		p.settings.burst = burst
	case 4:
		if len(values) != 18 || values[0] < 0 || values[0] > 3 || values[1] < 0 || values[1] > 7 || values[12] < 0 || values[12] > 8 {
			return statusInvalid
		}
		enemy := p.enemies[values[0]]
		if enemy == nil || len(enemy.gaugeReactions) >= 50 {
			return statusInvalid
		}
		for _, target := range values[13:] {
			if target < 0 || target > 7 {
				return statusInvalid
			}
		}
		var record [17]int32
		copy(record[:], values[1:])
		enemy.gaugeReactions = append(enemy.gaugeReactions, record)
	case 5:
		if p.mode != 2 || len(values) != 3 {
			return statusInvalid
		}
		p.settings.storyEnd = battlemodel.StoryEndOrder{Turn: int(int16(values[0])), EnemyHPPercent: int(int16(values[1])), UserHPZero: values[2] != 0}
	default:
		return statusInvalid
	}
	return statusOK
}
