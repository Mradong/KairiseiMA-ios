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
	"reflect"
	"strconv"
	"unsafe"
)

// Unknown conditions are rejected rather than interpreted as numeric.
func decodeSkillCondition(schema map[string]map[int32]string, c rawSkillCondition) (string, [5]string, error) {
	name, ok := schema["SKILL_EXTEND_EXEC_ORDER"][c.Order]
	var params [5]string
	if !ok {
		return "", params, fmt.Errorf("unavailable condition enum %d", c.Order)
	}
	switch name {
	case "NONE", "NULL":
		name = ""
	case "TARGET_ATTR", "SELF_MAIN_DECK_ATTR", "SELF_MAIN_DECK_SKILL_KIND":
		family, slots := "ATTR", 1
		if name == "SELF_MAIN_DECK_ATTR" {
			slots = 2
		}
		if name == "SELF_MAIN_DECK_SKILL_KIND" {
			family, slots = "SKILL_KIND", 2
		}
		for k, value := range c.Parameters {
			if k >= slots {
				params[k] = strconv.FormatInt(value, 10)
				continue
			}
			if value < math.MinInt32 || value > math.MaxInt32 {
				return "", params, fmt.Errorf("condition enum exceeds int32")
			}
			var ok bool
			params[k], ok = schema[family][int32(value)]
			if !ok {
				return "", params, fmt.Errorf("unavailable %s enum %d", family, value)
			}
		}
	case "RANDOM", "SELF_HP_PER", "SELF_OTHER_PLAY_NUM", "SELF_PLAY_MOST_LOW_COST", "SELF_PLAY_COST_TOTAL", "ENEMY_TRIBAL",
		"SELF_MAIN_DECK_TAG", "SELF_SUPPORT_DECK_TAG", "SELF_ALL_DECK_TAG":
		for k, value := range c.Parameters {
			params[k] = strconv.FormatInt(value, 10)
		}
	default:
		return "", params, fmt.Errorf("unsupported condition %s", name)
	}
	return name, params, nil
}

func queryBranches(schema map[string]map[int32]string, raw rawSkill) ([]battlecore.CombatSkillDefinition, error) {
	result := make([]battlecore.CombatSkillDefinition, 5)
	for i, ext := range raw.Extends {
		result[i].FunctionID = int(ext.RoleID)
		result[i].BranchPriority = int(ext.Priority)
		if ext.RoleID == 0 {
			continue
		}
		for j, c := range ext.Conditions {
			name, params, err := decodeSkillCondition(schema, c)
			if err != nil {
				return nil, err
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

type preparedProjection struct {
	catalog   *battlecore.CombatCatalog
	members   []battlemodel.Participant
	encounter *battlemodel.Encounter
}

// projectPreparedState copies registered content into an isolated catalog.
func projectPreparedState(p *preparation, catalog *libraryCatalog) (*preparedProjection, error) {
	if catalog.schema == nil {
		return nil, fmt.Errorf("skill query requires content schema")
	}
	var encounter *battlemodel.Encounter
	for index, enemy := range p.enemies {
		if enemy == nil {
			continue
		}
		if encounter == nil {
			encounter = &battlemodel.Encounter{}
		}
		projected, err := decodePreparedEnemy(catalog.schema, catalog.value.Enemies, enemy, index)
		if err != nil {
			return nil, err
		}
		encounter.Enemies[index] = projected
	}
	value := *catalog.value
	value.Cards = make(map[int]battlemodel.CombatCardDefinition, len(catalog.value.Cards))
	for id, card := range catalog.value.Cards {
		value.Cards[id] = card
	}
	value.Skills = make(map[int][]battlemodel.CombatSkillDefinition, len(catalog.value.Skills))
	for id, skill := range catalog.value.Skills {
		value.Skills[id] = skill
	}
	value.SkillRoles = make(map[int][]battlemodel.CombatSkillRole, len(catalog.value.SkillRoles))
	for id, roles := range catalog.value.SkillRoles {
		value.SkillRoles[id] = roles
	}
	registeredSkills := make(map[int]registeredSkill)
	registeredRoles := make(map[int][]battlemodel.CombatSkillRole)
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
			wires := [6][]byte{raw[28 : 28+skillWireSize], raw[28+skillWireSize : 28+2*skillWireSize], user.extraSkills[slot][0], user.extraSkills[slot][1], user.extraSkills[slot][2], user.extraSkills[slot][3]}
			var levels [6]battlemodel.SkillLevel
			for n, wire := range wires {
				decoded, e := decodeRegisteredSkill(catalog.schema, catalog.value.RoleParamRules, wire)
				if e != nil {
					return nil, e
				}
				levels[n] = decoded.level
				if n < 2 {
					levels[n].Value = int(wireInt(raw, 8))
				}
				for j := 0; n < 4 && j < n; j++ {
					if levels[n].ID != 0 && levels[j].ID == levels[n].ID && levels[j].Value != levels[n].Value {
						return nil, fmt.Errorf("conflicting instance skill levels")
					}
				}
				if decoded.level.ID == 0 {
					continue
				}
				if old, exists := registeredSkills[decoded.level.ID]; exists {
					if !reflect.DeepEqual(old.variants, decoded.variants) || !reflect.DeepEqual(old.roles, decoded.roles) {
						return nil, fmt.Errorf("conflicting registered skill")
					}
				} else {
					registeredSkills[decoded.level.ID] = decoded
					value.Skills[decoded.level.ID] = decoded.variants
					for key, roles := range decoded.roles {
						if old, exists := registeredRoles[key]; exists && !reflect.DeepEqual(old, roles) {
							return nil, fmt.Errorf("conflicting registered role set")
						}
						registeredRoles[key] = roles
						value.SkillRoles[key] = roles
					}
				}
			}
			rarity, ok := catalog.schema["RARITY"][wireInt(raw, 20)]
			if !ok {
				return nil, fmt.Errorf("registered card rarity unavailable")
			}
			definition.Rarity = rarity
			definition.RarityPlus = int(wireInt(raw, 24))
			definition.NormalSkillID, definition.ArthurSkillID = levels[0].ID, levels[1].ID
			definition.CallSkillID, definition.PassiveSkillID, definition.ArthurPassiveSkillID = levels[3].ID, levels[4].ID, levels[5].ID
			partition := 0
			for partition < 3 && catalog.value.SupportLoveRates[partition] <= int(wireInt(raw, 16)) {
				partition++
			}
			definition.SupportSkillIDs[partition] = levels[2].ID
			for n := range definition.ProfileTags {
				definition.ProfileTags[n] = int(wireInt(raw, 4928+n*4))
			}
			if previous, exists := registeredCards[id]; exists && previous != definition {
				return nil, fmt.Errorf("conflicting registered cards")
			}
			registeredCards[id], value.Cards[id] = definition, definition
			card := battlemodel.Card{CardType: slot + 1, CardID: id, Level: int(wireInt(raw, 8)), Fame: int(wireInt(raw, 12)), LoveRate: int(wireInt(raw, 16)), HasLoveRate: true, SkillLevels: levels}
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
	return &preparedProjection{catalog: &value, members: members, encounter: encounter}, nil
}

func preparedQuery(p *preparation, catalog *libraryCatalog) (*battlecore.PreparedSkillQuery, error) {
	projection, err := projectPreparedState(p, catalog)
	if err != nil {
		return nil, err
	}
	return battlecore.NewPreparedSkillQuery(projection.catalog, projection.members, projection.encounter, p.mode == 3 || p.mode == 4)
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
