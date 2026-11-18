package main

import (
	"bytes"
	"testing"
)

func TestEnemyReplacementAndClear(t *testing.T) {
	p := newPreparation(1)
	raw := make([]byte, 276)
	putWire(raw, 0, 7)
	raw[275] = 0x6b
	if p.setEnemy(5, raw) != 0 {
		t.Fatal("valid enemy rejected")
	}
	raw[275] = 0
	if p.enemies[0].data[275] != 0x6b {
		t.Fatal("enemy aliases caller data")
	}
	skill := make([]byte, 2656)
	skill[2655] = 0x7c
	if p.setEnemySkill(5, 0, 20, skill) != 0 {
		t.Fatal("valid enemy skill rejected")
	}
	skill[2655] = 0
	if p.enemies[0].normal[20][2655] != 0x7c {
		t.Fatal("enemy skill loses tail or aliases caller")
	}
	putWire(skill, 2432, -1)
	if p.setEnemySkill(5, 0, 20, skill) != 2 || p.enemies[0].normal[20][2655] != 0x7c {
		t.Fatal("invalid skill replaced previous data")
	}
	old := p.enemies[0]
	putWire(raw, 176, 9)
	if p.setEnemy(5, raw) != 2 || p.enemies[0] != old {
		t.Fatal("invalid enemy replaced previous data")
	}
	putWire(raw, 176, 8)
	if p.setEnemy(5, raw) != 0 || p.enemies[0] == old || p.enemies[0].normal[20] != nil {
		t.Fatal("base replacement did not reset enemy state")
	}
	if p.clearEnemies() != 0 || !p.enemiesRegistered || p.enemies[0] != nil || p.setEnemySkill(5, 0, 0, make([]byte, 2656)) != 2 {
		t.Fatal("enemy clear left stale registration")
	}
}

func TestInitializationRecordsPreserveWireSemantics(t *testing.T) {
	p := newPreparation(2)
	awake := []int32{1, 0, 1, 0, 1, 0}
	if p.initialize(1, awake) != 0 {
		t.Fatal("awake rejected")
	}
	awake[0] = 0
	if p.settings.awake[0] != 1 {
		t.Fatal("awake aliases input")
	}
	awake[5] = 2
	if p.initialize(1, awake) != 2 || p.settings.awake[0] != 1 {
		t.Fatal("invalid awake changed configuration")
	}
	trance := make([]int32, 44)
	for row, kind := range []int32{3, 1, 0, 2} {
		trance[row*11] = kind
		for col := 1; col < 11; col++ {
			trance[row*11+col] = kind*100 + int32(col) - 400
		}
	}
	if p.initialize(2, trance) != 0 {
		t.Fatal("permuted trance rejected")
	}
	for kind := 0; kind < 4; kind++ {
		for state := 0; state < 2; state++ {
			for cost := 0; cost < 5; cost++ {
				if p.settings.trance[kind][state][cost] != kind*100+state*5+cost+1-400 {
					t.Fatal("trance row mapping changed")
				}
			}
		}
	}
	previous := *p.settings.trance
	trance[33] = trance[0]
	if p.initialize(2, trance) != 2 || *p.settings.trance != previous {
		t.Fatal("duplicate trance changed previous table")
	}
	burst := []int32{10, 20, 0, -5, -1, 0, 3, 4, 5}
	if p.initialize(3, burst) != 0 || p.settings.burst.DamageReduction != -5 || p.settings.burst.ReactionRates[0] != -1 {
		t.Fatal("burst fields incorrectly clamped")
	}
	burst[0] = 0
	if p.initialize(3, burst) != 2 || p.settings.burst.NormalThreshold != 10 {
		t.Fatal("invalid burst changed prior table")
	}
	if p.initialize(5, []int32{65537, 32768, -1}) != 0 || p.settings.storyEnd.Turn != 1 || p.settings.storyEnd.EnemyHPPercent != -32768 || !p.settings.storyEnd.UserHPZero {
		t.Fatal("story end int16/bool wire conversion changed")
	}
}

func TestEnemyGaugeCapacityAndReplacement(t *testing.T) {
	p := newPreparation(1)
	raw := make([]byte, 276)
	putWire(raw, 0, 1)
	if p.setEnemy(8, raw) != 0 {
		t.Fatal("setup")
	}
	reaction := make([]int32, 18)
	reaction[0], reaction[2] = 3, 0x123456
	for i := 0; i < 50; i++ {
		if p.initialize(4, reaction) != 0 {
			t.Fatal("reaction rejected below capacity")
		}
	}
	reaction[2] = -1
	if p.initialize(4, reaction) != 2 || len(p.enemies[3].gaugeReactions) != 50 || p.enemies[3].gaugeReactions[0][1] != 0x123456 {
		t.Fatal("capacity rejection or ownership failed")
	}
	if p.setEnemy(8, raw) != 0 || len(p.enemies[3].gaugeReactions) != 0 || p.initialize(4, reaction) != 0 {
		t.Fatal("replacement did not reset gauge capacity")
	}
	reaction[17] = 8
	if p.initialize(4, reaction) != 2 || len(p.enemies[3].gaugeReactions) != 1 {
		t.Fatal("invalid late target partially appended")
	}
}

func TestStoryHelpCopiesWithoutExtraValidation(t *testing.T) {
	p := newPreparation(2)
	raw := bytes.Repeat([]byte{0xff}, 4980)
	if p.setStoryHelp(raw, -123) != 0 {
		t.Fatal("original helper path accepts all fixed-layout records")
	}
	raw[0] = 0
	if p.storyHelp.data[0] != 0xff || p.storyHelp.target != -123 {
		t.Fatal("helper ownership/target mismatch")
	}
	previous := p.storyHelp
	if p.setStoryHelp(raw[:4979], 0) != 2 || p.storyHelp != previous {
		t.Fatal("invalid helper changed previous registration")
	}
	if newPreparation(1).setStoryHelp(raw, 0) != 2 {
		t.Fatal("helper accepted outside story mode")
	}
}
