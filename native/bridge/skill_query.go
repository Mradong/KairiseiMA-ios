package main

/*
#include <stdint.h>
*/
import "C"

import (
	"fmt"
	"kairisei.local/server/battlecore"
	"kairisei.local/server/battlemodel"
	"math"
	"strconv"
	"unsafe"
)

// queryBranches supports a deliberately limited set of condition families.
// Unknown families are rejected rather than interpreted as numeric.
func queryBranches(schema map[string]map[int32]string, raw rawSkill) ([]battlecore.CombatSkillDefinition, error) {
	result := make([]battlecore.CombatSkillDefinition, 5)
	for i, ext := range raw.Extends {
		result[i].FunctionID = int(ext.RoleID)
		result[i].BranchPriority = int(ext.Priority)
		if ext.RoleID == 0 {
			continue
		}
		for j, c := range ext.Conditions {
			name, ok := schema["SKILL_EXTEND_EXEC_ORDER"][c.Order]
			if !ok {
				return nil, fmt.Errorf("unavailable condition enum %d", c.Order)
			}
			var params [5]string
			switch name {
			case "NONE", "NULL":
				name = ""
			case "TARGET_ATTR":
				value := c.Parameters[0]
				if value < math.MinInt32 || value > math.MaxInt32 {
					return nil, fmt.Errorf("condition enum exceeds int32")
				}
				attribute, ok := schema["ATTR"][int32(value)]
				if !ok {
					return nil, fmt.Errorf("unavailable attribute enum %d", value)
				}
				params[0] = attribute
				for k := 1; k < len(params); k++ {
					params[k] = strconv.FormatInt(c.Parameters[k], 10)
				}
			case "RANDOM", "SELF_HP_PER", "SELF_OTHER_PLAY_NUM", "SELF_PLAY_MOST_LOW_COST", "SELF_PLAY_COST_TOTAL",
				"SELF_MAIN_DECK_TAG", "SELF_SUPPORT_DECK_TAG", "SELF_ALL_DECK_TAG":
				for k, value := range c.Parameters {
					params[k] = strconv.FormatInt(value, 10)
				}
			default:
				return nil, fmt.Errorf("unsupported condition %s", name)
			}
			if j == 0 {
				result[i].BranchCondition, result[i].BranchParameters = name, params
			} else {
				result[i].BranchCondition2, result[i].BranchParameters2 = name, params
			}
		}
	}
	return result, nil
}

// preparedQueryProjects the registered state needed by the engine query.
// Nonempty skill and enemy records still require registered-content decoding.
// Do not silently discard them. Zero-ID cards are empty in the original.
func preparedQuery(p *preparation, catalog *libraryCatalog) (*battlecore.PreparedSkillQuery, error) {
	if catalog.schema == nil {
		return nil, fmt.Errorf("skill query requires content schema")
	}
	for _, enemy := range p.enemies {
		if enemy != nil {
			return nil, fmt.Errorf("registered enemy projection unavailable")
		}
	}
	value := *catalog.value
	value.Cards = make(map[int]battlemodel.CombatCardDefinition, len(catalog.value.Cards))
	for id, card := range catalog.value.Cards {
		value.Cards[id] = card
	}
	registeredCards := make(map[int]battlemodel.CombatCardDefinition)
	members := make([]battlemodel.Participant, 0, 8)
	for i, user := range p.users {
		if user == nil {
			continue
		}
		job, maxHP := int(user.data[6]), int(user.data[2])
		member := battlemodel.Participant{MemberType: i + 1, ArthurType: int(user.data[7]), JobType: &job,
			HP: int(user.data[1]), MaxHP: &maxHP, Attack: int(user.data[3]), Magic: int(user.data[4]), Mind: int(user.data[5])}
		for slot, raw := range user.cards {
			if raw == nil || wireInt(raw, 0) == 0 {
				continue
			}
			id := int(wireInt(raw, 0))
			definition, ok := catalog.value.Cards[id]
			if !ok {
				return nil, fmt.Errorf("registered card has no master metadata")
			}
			for _, offset := range []int{28, 28 + skillWireSize} {
				skill := raw[offset : offset+skillWireSize]
				if !validRegisteredSkill(skill) || wireInt(skill, 0) != 0 {
					return nil, fmt.Errorf("registered skill projection unavailable")
				}
			}
			for _, extra := range user.extraSkills[slot] {
				if extra != nil && (!validRegisteredSkill(extra) || wireInt(extra, 0) != 0) {
					return nil, fmt.Errorf("registered extra skill projection unavailable")
				}
			}
			rarity, ok := catalog.schema["RARITY"][wireInt(raw, 20)]
			if !ok {
				return nil, fmt.Errorf("registered card rarity unavailable")
			}
			definition.Rarity = rarity
			definition.RarityPlus = int(wireInt(raw, 24))
			definition.NormalSkillID, definition.ArthurSkillID = 0, 0
			definition.CallSkillID, definition.PassiveSkillID, definition.ArthurPassiveSkillID = 0, 0, 0
			partition := 0
			for partition < 3 && catalog.value.SupportLoveRates[partition] <= int(wireInt(raw, 16)) {
				partition++
			}
			definition.SupportSkillIDs[partition] = 0
			for n := range definition.ProfileTags {
				definition.ProfileTags[n] = int(wireInt(raw, 4928+n*4))
			}
			if previous, exists := registeredCards[id]; exists && previous != definition {
				return nil, fmt.Errorf("conflicting registered cards")
			}
			registeredCards[id], value.Cards[id] = definition, definition
			card := battlemodel.Card{CardType: slot + 1, CardID: id, Level: int(wireInt(raw, 8)), Fame: int(wireInt(raw, 12)), LoveRate: int(wireInt(raw, 16)), HasLoveRate: true}
			if slot < 10 {
				member.DeckCards = append(member.DeckCards, card)
			} else {
				member.SupportCards = append(member.SupportCards, card)
			}
		}
		if initial := p.settings.initialBurst[i]; initial != nil {
			value := int(*initial)
			member.InitialBurst = &value
		}
		if buddy := user.buddies[0]; buddy != nil && wireInt(buddy, 0) != 0 {
			member.DeckBuddies = []battlemodel.Buddy{{BuddyType: 1, BuddyID: int(wireInt(buddy, 0))}}
		}
		members = append(members, member)
	}
	return battlecore.NewPreparedSkillQuery(&value, members, nil, p.mode == 3 || p.mode == 4)
}

//export battlecore_skill_extend_index
func battlecore_skill_extend_index(catalogID, preparationID, battleID C.ulonglong, member C.int, input *C.uchar, count C.int, output *C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if input == nil || count != skillWireSize || output == nil {
		return statusBuffer
	}
	if member < 1 || member > 8 {
		return statusInvalid
	}
	raw, err := decodeRawSkill(unsafe.Slice((*byte)(unsafe.Pointer(input)), skillWireSize))
	if err != nil {
		return statusInvalid
	}
	sessions.Lock()
	defer sessions.Unlock()
	if battleID == 0 {
		catalog := sessions.catalogs[uint64(catalogID)]
		if catalog == nil {
			return 4
		}
		preparations.Lock()
		defer preparations.Unlock()
		p := preparations.values[uint64(preparationID)]
		if p == nil {
			return 4
		}
		if int(member) > p.memberLimit() {
			return statusInvalid
		}
		if p.users[int(member)-1] == nil {
			*output = -1
			return statusOK
		}
		variants, err := queryBranches(catalog.schema, raw)
		if err != nil {
			return statusInvalid
		}
		query, err := preparedQuery(p, catalog)
		if err != nil {
			return statusInvalid
		}
		index, err := query.SkillBranchIndex(int(member), variants)
		if err != nil {
			return statusInvalid
		}
		*output = C.int(index)
		return statusOK
	}
	if catalogID != 0 || preparationID != 0 {
		return statusInvalid
	}
	battle := sessions.battles[uint64(battleID)]
	if battle == nil {
		return 4
	}
	if battle.faulted {
		return 6
	}
	variants, err := queryBranches(sessions.catalogs[battle.catalogID].schema, raw)
	if err != nil {
		return statusInvalid
	}
	index, err := battle.value.SkillBranchIndex(int(member), variants)
	if err != nil {
		return statusInvalid
	}
	*output = C.int(index)
	return statusOK
}
