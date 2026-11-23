package main

/*
#include <stdint.h>
*/
import "C"

import "kairisei.local/server/battlecore"

func validateRegisteredParty(p *preparation, catalog *libraryCatalog) bool {
	if p.mode != 1 || catalog.schema == nil || len(p.npcs) != 0 || p.storyHelp != nil {
		return false
	}
	for _, summon := range p.summons {
		if summon != nil {
			return false
		}
	}
	for i := 0; i < 4; i++ {
		u := p.users[i]
		if u == nil || len(u.spheres) != spheresWireSize {
			return false
		}
		if i > 0 && (u.seed != p.users[0].seed || u.data[8] != p.users[0].data[8] || u.data[17] != p.users[0].data[17]) {
			return false
		}
		for _, card := range u.cards {
			if len(card) != cardWireSize {
				return false
			}
		}
		for _, buddy := range u.buddies {
			if len(buddy) != buddyWireSize {
				return false
			}
		}
	}
	return true
}

//export battlecore_context_create_registered
func battlecore_context_create_registered(catalogID, preparationID C.ulonglong, input *C.uchar, count C.int, handle *C.ulonglong) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if handle == nil {
		return statusBuffer
	}
	var request struct {
		Mode      int                                 `json:"mode"`
		EnemyType *int                                `json:"enemy_type"`
		Config    battlecore.StandaloneBattleConfig   `json:"config"`
		Members   []battlecore.StandaloneBattleMember `json:"members"`
	}
	if decodeInput(input, count, &request, false, maxJSONInput) != nil || (request.Mode != 0 && request.Mode != 1 && request.Mode != 2 && request.Mode != 3 && request.Mode != 6 && request.Mode != 7) || request.EnemyType == nil || *request.EnemyType < 0 || *request.EnemyType > 4 || len(request.Members) != 0 {
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
	preparations.Lock()
	defer preparations.Unlock()
	p := preparations.values[uint64(preparationID)]
	if p == nil {
		return 4
	}
	if p.mode != request.Mode || !validateRegisteredParty(p, catalog) {
		return statusInvalid
	}
	if p.enemiesRegistered && request.Config.EnemyPartyID != 0 {
		return statusInvalid
	}
	projection, err := projectPreparedState(p, catalog, false)
	if err != nil || len(projection.members) != 4 || (p.enemiesRegistered && projection.encounter == nil) {
		return statusInvalid
	}
	if p.settings.trance != nil {
		projection.catalog.TranceRates = *p.settings.trance
	}
	if p.settings.burst != nil {
		projection.catalog.BurstGauge = *p.settings.burst
	}
	request.Config.EnemyType, request.Config.Seed = *request.EnemyType, uint32(p.users[0].seed)
	request.Config.Alchemist = false
	request.Config.CostInitial, request.Config.HoldMax = int(p.users[0].data[8]), int(p.users[0].data[17])
	request.Config.GameOverByCaller = true
	if p.enemiesRegistered {
		request.Config.Encounter = projection.encounter
	}
	if p.settings.continueAllowed != nil {
		request.Config.ContinueAllowed = *p.settings.continueAllowed
	}
	if p.settings.endTurn != nil {
		request.Config.EndTurn = int(*p.settings.endTurn)
	}
	members := make([]battlecore.StandaloneBattleMember, 0, 4)
	for _, m := range projection.members {
		members = append(members, battlecore.StandaloneBattleMember{MemberType: m.MemberType, ArthurType: m.ArthurType, JobType: m.JobType, HP: m.HP, MaxHP: m.MaxHP, Attack: m.Attack, Magic: m.Magic, Mind: m.Mind, InitialBurst: m.InitialBurst, DeckCards: m.DeckCards, SupportCards: m.SupportCards, DeckBuddies: m.DeckBuddies, DeckSpheres: m.DeckSpheres})
	}
	battle, err := battlecore.NewStandaloneBattle(projection.catalog, request.Config, members)
	if err != nil {
		return statusInvalid
	}
	var identities []battlecore.BattleResult
	for member, user := range p.users {
		if user == nil {
			continue
		}
		for slot, raw := range user.cards {
			if len(raw) == cardWireSize {
				identities = append(identities, battlecore.BattleResult{Command: 23, Args: []int64{int64(member + 1), int64(slot + 1), int64(wireInt(raw, 0)), int64(wireInt(raw, 8))}})
			}
		}
	}
	key := nextSessionHandle()
	sessions.battles[key] = &libraryBattle{value: battle, registered: true, identities: identities, journal: &inputJournal{}, catalogID: uint64(catalogID), mode: request.Mode, enemyType: *request.EnemyType}
	catalog.references++
	*handle = C.ulonglong(key)
	return statusOK
}
