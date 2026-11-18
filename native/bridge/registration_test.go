package main

import (
	"bytes"
	"encoding/binary"
	"testing"
)

func putWire(raw []byte, offset int, value int32) {
	binary.LittleEndian.PutUint32(raw[offset:], uint32(value))
}

func TestRegistrationOwnsCallerBuffers(t *testing.T) {
	p := newPreparation(6)
	p.users[0] = &preparedUser{}
	tests := []struct {
		name     string
		size     int
		register func([]byte) int
		stored   func() []byte
	}{
		{"card", cardWireSize, func(b []byte) int { return p.setCard(1, 1, b) }, func() []byte { return p.users[0].cards[0] }},
		{"extra", skillWireSize, func(b []byte) int { return p.setExtraSkill(1, 1, 0, b) }, func() []byte { return p.users[0].extraSkills[0][0] }},
		{"sphere", spheresWireSize, func(b []byte) int { return p.setSpheres(1, b) }, func() []byte { return p.users[0].spheres }},
		{"buddy", buddyWireSize, func(b []byte) int { return p.setBuddy(1, 1, b) }, func() []byte { return p.users[0].buddies[0] }},
		{"summon-card", cardWireSize, func(b []byte) int { return p.setSummon(1, b, true) }, func() []byte { return p.summons[0].card }},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			raw := make([]byte, test.size)
			// Non-header data must be retained verbatim, not silently zeroed.
			raw[len(raw)-5] = 0x6b
			expected := append([]byte(nil), raw...)
			if test.register(raw) != statusOK {
				t.Fatal("valid record rejected")
			}
			for i := range raw {
				raw[i] = 0xff
			}
			if !bytes.Equal(test.stored(), expected) {
				t.Fatal("registration aliases caller memory or loses bytes")
			}
		})
	}
}

func TestRejectedReplacementPreservesRegisteredData(t *testing.T) {
	p := newPreparation(1)
	p.users[0] = &preparedUser{}
	card, skill := make([]byte, cardWireSize), make([]byte, skillWireSize)
	sphere, buddy := make([]byte, spheresWireSize), make([]byte, buddyWireSize)
	if p.setCard(1, 1, card) != 0 || p.setExtraSkill(1, 1, 0, skill) != 0 || p.setSpheres(1, sphere) != 0 || p.setBuddy(1, 1, buddy) != 0 {
		t.Fatal("setup rejected")
	}
	putWire(card, 0, -1)
	putWire(skill, 2432, -1)
	putWire(sphere, 2*7328+4, -1)
	putWire(buddy, buddyWireSize-4, -1)
	if p.setCard(1, 1, card) != 2 || p.setExtraSkill(1, 1, 0, skill) != 2 || p.setSpheres(1, sphere) != 2 || p.setBuddy(1, 1, buddy) != 2 {
		t.Fatal("malformed replacement accepted")
	}
	u := p.users[0]
	for _, stored := range [][]byte{u.cards[0], u.extraSkills[0][0], u.spheres, u.buddies[0]} {
		if !bytes.Equal(stored, make([]byte, len(stored))) {
			t.Fatal("rejected replacement corrupted previous record")
		}
	}
}

func TestSummonRegistrationPreservesOtherRecord(t *testing.T) {
	for _, cardFirst := range []bool{false, true} {
		p := newPreparation(7)
		card, data := make([]byte, cardWireSize), make([]byte, 36)
		putWire(card, 0, -123) // Original summon-card path accepts this identity.
		for i := 0; i < 9; i++ {
			putWire(data, i*4, int32(i-4))
		}
		first, second := data, card
		if cardFirst {
			first, second = card, data
		}
		if p.setSummon(10, first, cardFirst) != 0 || p.setSummon(10, second, !cardFirst) != 0 {
			t.Fatal("valid paired summon registration rejected")
		}
		for i := range data {
			data[i] = 0xff
		}
		card[0] = 0
		s := p.summons[9]
		for i, value := range s.data {
			if value != int32(i-4) {
				t.Fatal("summon data lost or aliases caller memory")
			}
		}
		if wireInt(s.card, 0) != -123 {
			t.Fatal("summon card lost or aliases caller memory")
		}
	}
}

func TestNPCBoundedTransactionalAppend(t *testing.T) {
	p := newPreparation(1)
	raw := make([]byte, 800)
	for i := 0; i < 10; i++ {
		putWire(raw, i*80, int32(i+1))
		raw[i*80+79] = byte(i + 11)
	}
	for i := 0; i < 102; i++ {
		if p.addNPC(raw) != 0 {
			t.Fatal("NPC rejected below capacity")
		}
	}
	if p.addNPC(raw) != 2 || len(p.npcs) != 1020 {
		t.Fatal("overflow append was not atomic")
	}
	putWire(raw, 4*80, 0)
	if p.addNPC(raw) != 0 || len(p.npcs) != 1024 {
		t.Fatal("exact capacity failed after rejected append")
	}
	for i := range raw {
		raw[i] = 0
	}
	if p.addNPC(raw) != 0 || len(p.npcs) != 1024 {
		t.Fatal("empty batch at capacity should succeed")
	}
	if wireInt(p.npcs[0][:], 0) != 1 || p.npcs[1023][79] != 14 {
		t.Fatal("NPC records lost or alias caller memory")
	}
}

func TestSettingsDefaultsAndIndependentMembers(t *testing.T) {
	for mode := 0; mode < 8; mode++ {
		p := newPreparation(mode)
		if mode == 3 || mode == 4 {
			if p.settings.endTurn == nil || *p.settings.endTurn != 10 {
				t.Fatal("PvP turn default missing")
			}
		} else if p.settings.endTurn != nil {
			t.Fatal("turn limit should remain unspecified")
		}
		if p.configure(1, 0, 0) != 0 || p.settings.continueAllowed == nil || *p.settings.continueAllowed {
			t.Fatal("explicit false must differ from unspecified")
		}
		if p.configure(2, 0, 0) != 0 || *p.settings.endTurn != 0 {
			t.Fatal("explicit zero turn limit lost")
		}
		if p.configure(3, 1, 12) != 0 || p.configure(3, p.memberLimit(), 34) != 0 {
			t.Fatal("valid member setting rejected")
		}
		if *p.settings.initialBurst[0] != 12 || *p.settings.initialBurst[p.memberLimit()-1] != 34 {
			t.Fatal("member settings not independent")
		}
		if p.configure(3, 1, -1) != 2 || *p.settings.initialBurst[0] != 12 {
			t.Fatal("invalid setting changed previous value")
		}
		if p.configure(4, 0, 1) != map[bool]int{true: 0, false: 2}[mode == 2] || p.settings.deckShuffleOff != (mode == 2) {
			t.Fatal("story shuffle mode mismatch")
		}
	}
}
