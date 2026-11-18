package main

import (
	"encoding/binary"
	"kairisei.local/server/battlemodel"
)

// These wire records are copied rather than decoded into battle content here.
// Content decoding and battle creation remain separate, unfinished work.
const (
	cardWireSize    = 4960
	skillWireSize   = 2436
	spheresWireSize = 21984
	buddyWireSize   = 17096
)

type preparationSettings struct {
	continueAllowed *bool
	endTurn         *int32
	initialBurst    [8]*int32
	deckShuffleOff  bool
	awake           *[6]int32
	trance          *[4][2][5]int
	burst           *battlemodel.CombatBurstGaugeConfig
	storyEnd        battlemodel.StoryEndOrder
}

type preparedSummon struct {
	data [9]int32
	card []byte
}

func newPreparation(mode int) *preparation {
	p := &preparation{mode: mode}
	if mode == 3 || mode == 4 {
		limit := int32(10)
		p.settings.endTurn = &limit
	}
	return p
}

func (p *preparation) memberLimit() int {
	if p.mode == 3 || p.mode == 4 {
		return 8
	}
	return 4
}

func (p *preparation) registeredUser(member int) *preparedUser {
	if member < 1 || member > p.memberLimit() {
		return nil
	}
	return p.users[member-1]
}

func wireInt(raw []byte, offset int) int32 {
	return int32(binary.LittleEndian.Uint32(raw[offset : offset+4]))
}

func validRegisteredSkill(raw []byte) bool {
	return len(raw) == skillWireSize && wireInt(raw, 0) >= 0 && wireInt(raw, 2432) >= 0
}

func (p *preparation) configure(operation, member int, value int32) int {
	switch operation {
	case 1:
		if member != 0 || value < 0 || value > 1 {
			return statusInvalid
		}
		enabled := value != 0
		p.settings.continueAllowed = &enabled
	case 2:
		if member != 0 || value < 0 {
			return statusInvalid
		}
		p.settings.endTurn = &value
	case 3:
		if member < 1 || member > p.memberLimit() || value < 0 {
			return statusInvalid
		}
		p.settings.initialBurst[member-1] = &value
	case 4:
		if p.mode != 2 || member != 0 || value != 1 {
			return statusInvalid
		}
		p.settings.deckShuffleOff = true
	default:
		return statusInvalid
	}
	return statusOK
}

func (p *preparation) setCard(member, slot int, raw []byte) int {
	u := p.registeredUser(member)
	if u == nil || slot < 1 || slot > 20 || len(raw) != cardWireSize || wireInt(raw, 0) < 0 {
		return statusInvalid
	}
	u.cards[slot-1] = append([]byte(nil), raw...)
	return statusOK
}

func (p *preparation) setExtraSkill(member, slot, kind int, raw []byte) int {
	u := p.registeredUser(member)
	if u == nil || slot < 1 || slot > 20 || kind < 0 || kind > 3 || u.cards[slot-1] == nil || !validRegisteredSkill(raw) {
		return statusInvalid
	}
	u.extraSkills[slot-1][kind] = append([]byte(nil), raw...)
	return statusOK
}

func (p *preparation) setSpheres(member int, raw []byte) int {
	u := p.registeredUser(member)
	if u == nil || len(raw) != spheresWireSize {
		return statusInvalid
	}
	for slot := 0; slot < 3; slot++ {
		base := slot * 7328
		id, count, kind := wireInt(raw, base), wireInt(raw, base+4), wireInt(raw, base+8)
		if id < 0 || count < 0 || kind < 0 || kind > 3 || (id != 0 && kind == 0) {
			return statusInvalid
		}
		for skill := 0; skill < 3; skill++ {
			start := base + 12 + skill*skillWireSize
			if !validRegisteredSkill(raw[start : start+skillWireSize]) {
				return statusInvalid
			}
		}
	}
	u.spheres = append([]byte(nil), raw...)
	return statusOK
}

func (p *preparation) setBuddy(member, slot int, raw []byte) int {
	u := p.registeredUser(member)
	if u == nil || slot < 1 || slot > 5 || len(raw) != buddyWireSize || wireInt(raw, 0) < 0 || wireInt(raw, 4) < 0 {
		return statusInvalid
	}
	for burst := 0; burst < 3; burst++ {
		for skill := 0; skill < 2; skill++ {
			start := 8 + burst*4884 + skill*skillWireSize
			if !validRegisteredSkill(raw[start : start+skillWireSize]) {
				return statusInvalid
			}
		}
	}
	if !validRegisteredSkill(raw[14660:]) {
		return statusInvalid
	}
	u.buddies[slot-1] = append([]byte(nil), raw...)
	return statusOK
}

func (p *preparation) setSummon(slot int, raw []byte, card bool) int {
	size := 36
	if card {
		size = cardWireSize
	}
	if (p.mode != 6 && p.mode != 7) || slot < 1 || slot > 10 || len(raw) != size {
		return statusInvalid
	}
	summon := p.summons[slot-1]
	if summon == nil {
		summon = &preparedSummon{}
	}
	if card {
		// Unlike ordinary player cards, this path does not reject negative IDs.
		summon.card = append([]byte(nil), raw...)
	} else {
		for i := range summon.data {
			summon.data[i] = wireInt(raw, i*4)
		}
	}
	p.summons[slot-1] = summon
	return statusOK
}

func (p *preparation) addNPC(raw []byte) int {
	if len(raw) != 800 {
		return statusInvalid
	}
	var added [][80]byte
	for offset := 0; offset < len(raw) && wireInt(raw, offset) > 0; offset += 80 {
		var record [80]byte
		copy(record[:], raw[offset:offset+80])
		added = append(added, record)
	}
	if len(p.npcs)+len(added) > 1024 {
		return statusInvalid
	}
	p.npcs = append(p.npcs, added...)
	return statusOK
}
