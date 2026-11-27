package main

/*
#include <stdint.h>
*/
import "C"

import (
	"errors"
	"fmt"

	"kairisei.local/server/battlecore"
	"kairisei.local/server/battlemodel"
)

// Solo opening data contains the authoritative combat loadouts and planned enemy
// parties. Catalog definitions, effects and AI remain owned by the combat core.
type soloOpening struct {
	Seed              uint32       `json:"seed"`
	CostInitial       int          `json:"cost_initial"`
	HoldMax           int          `json:"hold_max"`
	BurstGaugeInitial int          `json:"burst_gauge_initial"`
	BurstForPrimary   int          `json:"is_active_only_burst_gauge_initial"`
	EndTurn           int          `json:"end_turn"`
	ContinueAllowed   bool         `json:"continue_allowed"`
	Party             []soloMember `json:"party"`
	Waves             []soloWave   `json:"waves"`
}

type soloWave struct {
	PartyID   int                     `json:"enemy_partyid"`
	EnemyType int                     `json:"enemy_type"`
	Drops     []battlecore.BattleDrop `json:"drops"`
}

type soloMember struct {
	ArthurType      int        `json:"arthur_type"`
	JobType         int        `json:"job_type"`
	HP              int        `json:"hp"`
	Attack          int        `json:"atkp"`
	Magic           int        `json:"intp"`
	Mind            int        `json:"mndp"`
	SupportUnlocked int        `json:"support_card_unlock_slot_num"`
	Cards           []soloCard `json:"deck"`
	Support         []soloCard `json:"support_deck"`
	Spheres         []struct {
		ID    int `json:"sphrid"`
		Level int `json:"lv"`
	} `json:"sphrs"`
	Buddies []struct {
		ID    int `json:"buddyid"`
		Level int `json:"lv"`
	} `json:"buddys"`
}

type soloCard struct {
	ID          int   `json:"cardid"`
	Level       int   `json:"lv"`
	Fame        int   `json:"fame"`
	Love        int   `json:"love"`
	SkillLevels []int `json:"skill_lv"`
}

// Map service execution levels to their resolved skill identities rather than
// substituting the displayed card level for an explicitly supplied value.
func projectSoloCard(catalog *battlecore.CombatCatalog, input soloCard, slot int) (battlecore.BattleCard, error) {
	def, found := catalog.Cards[input.ID]
	if !found || input.Level < 1 || input.Level > def.MaxLevel || input.Love < 0 || input.Love > def.LoveMax ||
		input.Fame < 0 || input.Fame > 100 || len(input.SkillLevels) > 6 {
		return battlecore.BattleCard{}, fmt.Errorf("solo card %d in slot %d is invalid or absent from the catalog", input.ID, slot)
	}
	card := battlecore.BattleCard{CardType: slot, CardID: input.ID, Level: input.Level, Fame: input.Fame, Love: input.Love}
	ids := [...]int{def.NormalSkillID, def.ArthurSkillID, def.CallSkillID, def.PassiveSkillID, def.ArthurPassiveSkillID}
	for index, level := range input.SkillLevels {
		if level < 0 || index >= len(ids) && level != 0 {
			return card, fmt.Errorf("solo card %d has an unsupported skill level in position %d", input.ID, index)
		}
		if index < len(ids) {
			card.SkillLevels[index] = battlemodel.SkillLevel{ID: ids[index], Value: level}
		}
	}
	return card, nil
}

// Resolve a complete member, keeping main, unlocked support, sphere and buddy slots distinct.
func projectSoloMember(catalog *battlecore.CombatCatalog, source soloMember, slot int, burst int) (battlecore.StandaloneBattleMember, error) {
	if source.ArthurType < 1 || source.ArthurType > 4 || source.JobType < 1 || source.JobType > 4 ||
		source.HP <= 0 || len(source.Cards) != 10 || len(source.Support) > 10 ||
		source.SupportUnlocked < 0 || source.SupportUnlocked > len(source.Support) || len(source.Spheres) > 3 || len(source.Buddies) > 5 {
		return battlecore.StandaloneBattleMember{}, errors.New("invalid solo member or deck shape")
	}
	job := source.JobType
	member := battlecore.StandaloneBattleMember{MemberType: slot, ArthurType: source.ArthurType, JobType: &job,
		HP: source.HP, Attack: source.Attack, Magic: source.Magic, Mind: source.Mind, InitialBurst: &burst}
	for index, sourceCard := range source.Cards {
		card, err := projectSoloCard(catalog, sourceCard, index+1)
		if err != nil {
			return member, err
		}
		member.DeckCards = append(member.DeckCards, card)
	}
	for index, sourceCard := range source.Support[:source.SupportUnlocked] {
		if sourceCard.ID == 0 {
			continue
		}
		card, err := projectSoloCard(catalog, sourceCard, index+11)
		if err != nil {
			return member, err
		}
		member.SupportCards = append(member.SupportCards, card)
	}
	for index, sphere := range source.Spheres {
		if sphere.ID == 0 {
			continue
		}
		def, found := catalog.Spheres[sphere.ID]
		if !found || sphere.Level < 1 || sphere.Level > def.MaxLevel {
			return member, fmt.Errorf("solo sphere %d is invalid or unavailable", sphere.ID)
		}
		member.DeckSpheres = append(member.DeckSpheres, battlecore.BattleSphere{SphereType: index + 1, SphereID: sphere.ID, Level: sphere.Level})
	}
	for index, buddy := range source.Buddies {
		if buddy.ID == 0 {
			continue
		}
		def, found := catalog.Buddies[buddy.ID]
		if !found || buddy.Level < 1 || buddy.Level > def.MaxLevel {
			return member, fmt.Errorf("solo buddy %d is invalid or unavailable", buddy.ID)
		}
		member.DeckBuddies = append(member.DeckBuddies, battlecore.BattleBuddy{BuddyType: index + 1, BuddyID: buddy.ID, Level: buddy.Level})
	}
	return member, nil
}

// Resolve every published wave before allocating the live battle handle.
func makeSoloOpening(catalog *battlecore.CombatCatalog, opening soloOpening) (*battlecore.StandaloneBattle, []battlecore.BattleResult, error) {
	if len(opening.Party) != 4 || len(opening.Waves) == 0 || len(opening.Waves) > 32 || opening.Waves[0].EnemyType == 4 ||
		opening.EndTurn < 0 || opening.CostInitial < 0 || opening.HoldMax < 0 || opening.BurstGaugeInitial < 0 ||
		opening.BurstForPrimary < 0 || opening.BurstForPrimary > 1 {
		return nil, nil, errors.New("unsupported or invalid solo opening")
	}
	for _, wave := range opening.Waves {
		party, found := catalog.EnemyParties[wave.PartyID]
		if !found || wave.EnemyType < 0 || wave.EnemyType > 4 {
			return nil, nil, fmt.Errorf("solo enemy party %d is unavailable", wave.PartyID)
		}
		// The registry supports consecutive ordinary waves. Awakening needs the quest's takeover flags.
		if wave.EnemyType == 4 {
			return nil, nil, errors.New("solo awakening takeover is unavailable")
		}
		for _, drop := range wave.Drops {
			if drop.EnemyIndex < 0 || drop.EnemyIndex >= len(party.Slots) || party.Slots[drop.EnemyIndex].EnemyID == 0 ||
				drop.RewardType < 0 || drop.Num <= 0 || drop.RewardTypeID < 0 {
				return nil, nil, errors.New("invalid planned enemy drop")
			}
		}
	}
	members := make([]battlecore.StandaloneBattleMember, 0, len(opening.Party))
	identities := make([]battlecore.BattleResult, 0, len(opening.Party)*10)
	for index, source := range opening.Party {
		burst := opening.BurstGaugeInitial
		if opening.BurstForPrimary == 1 && index != 0 {
			burst = 0
		}
		member, err := projectSoloMember(catalog, source, index+1, burst)
		if err != nil {
			return nil, nil, fmt.Errorf("solo member %d: %w", index+1, err)
		}
		members = append(members, member)
		for _, cards := range [][]battlecore.BattleCard{member.DeckCards, member.SupportCards} {
			for _, card := range cards {
				identities = append(identities, battlecore.BattleResult{Command: 23, Args: []int64{
					int64(member.MemberType), int64(card.CardType), int64(card.CardID), int64(card.Level),
				}})
			}
		}
	}
	config := battlecore.StandaloneBattleConfig{EnemyPartyID: opening.Waves[0].PartyID, EnemyType: opening.Waves[0].EnemyType,
		Drops: opening.Waves[0].Drops, CostInitial: opening.CostInitial, HoldMax: opening.HoldMax, Seed: opening.Seed,
		EndTurn: opening.EndTurn, ContinueAllowed: opening.ContinueAllowed, GameOverByCaller: true}
	battle, err := battlecore.NewStandaloneBattle(catalog, config, members)
	return battle, identities, err
}

// battlecore_context_create_solo_opening registers verified master data and service loadouts in one session.
//
//export battlecore_context_create_solo_opening
func battlecore_context_create_solo_opening(catalogID C.ulonglong, input *C.uchar, count C.int, handle *C.ulonglong) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if handle == nil {
		return statusBuffer
	}
	var opening soloOpening
	if decodeInput(input, count, &opening, false, maxJSONInput) != nil {
		return statusInvalid
	}
	sessions.Lock()
	defer sessions.Unlock()
	catalog := sessions.catalogs[uint64(catalogID)]
	if catalog == nil {
		return 4
	}
	if len(sessions.battles) >= 64 {
		return 5
	}
	battle, identities, err := makeSoloOpening(catalog.value, opening)
	if err != nil {
		return statusInvalid
	}
	key := nextSessionHandle()
	sessions.battles[key] = &libraryBattle{value: battle, registered: true, identities: identities,
		catalogID: uint64(catalogID), mode: 1, enemyType: opening.Waves[0].EnemyType, journal: &inputJournal{}, soloWaves: opening.Waves}
	catalog.references++
	*handle = C.ulonglong(key)
	return statusOK
}

// battlecore_context_next_solo_opening advances only to the next verified and published enemy party.
//
//export battlecore_context_next_solo_opening
func battlecore_context_next_solo_opening(battleID C.ulonglong) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	sessions.Lock()
	defer sessions.Unlock()
	battle := sessions.battles[uint64(battleID)]
	if battle == nil {
		return 4
	}
	defer func() {
		if cause := recover(); cause != nil {
			battle.setFault(0, "solo-next-panic", cause)
			status = statusPanic
		}
	}()
	if battle.faulted {
		return 6
	}
	if len(battle.pending) != 0 {
		return 5
	}
	next := battle.waveIndex + 1
	if next >= len(battle.soloWaves) || battle.value.EndType() != 1 {
		return statusInvalid
	}
	catalog := sessions.catalogs[battle.catalogID].value
	wave := battle.soloWaves[next]
	encounter := projectSoloEncounter(catalog, wave)
	if result := advanceRegisteredWave(battle, catalog, encounter, wave.EnemyType); result != statusOK {
		return result
	}
	battle.waveIndex = next
	return statusOK
}

// Resolve the same enemy, level, parent, HP multiplier and drop slots used by the first catalog wave.
func projectSoloEncounter(catalog *battlecore.CombatCatalog, wave soloWave) battlecore.Encounter {
	var encounter battlecore.Encounter
	for slot, source := range catalog.EnemyParties[wave.PartyID].Slots {
		if source.EnemyID == 0 {
			continue
		}
		enemy := catalog.Enemies[source.EnemyID]
		hp := enemy.HP * source.HPRate
		encounter.Enemies[slot] = battlecore.EnemyInput{Definition: enemy, Level: catalog.EnemyLevels[source.EnemyID],
			HP: hp, MaxHP: hp, Parent: source.ParentIndex}
	}
	for _, drop := range wave.Drops {
		enemy := &encounter.Enemies[drop.EnemyIndex]
		enemy.Drops = append(enemy.Drops, drop)
	}
	return encounter
}
