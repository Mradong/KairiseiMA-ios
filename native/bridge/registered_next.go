package main

/*
#include <stdint.h>
*/
import "C"

import (
	"fmt"
	"kairisei.local/server/battlecodec"
	"kairisei.local/server/battlecore"
)

//export battlecore_context_next_registered
func battlecore_context_next_registered(battleID, preparationID C.ulonglong, enemyType C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if enemyType < 0 || enemyType > 4 {
		return statusInvalid
	}
	sessions.Lock()
	defer sessions.Unlock()
	battle := sessions.battles[uint64(battleID)]
	if battle == nil {
		return 4
	}
	defer func() {
		if cause := recover(); cause != nil {
			battle.setFault(0, "next-panic", cause)
			status = statusPanic
		}
	}()
	if battle.faulted {
		return 6
	}
	if !battle.registered || battle.mode != 1 {
		return statusInvalid
	}
	if (enemyType == 4) != (battle.value.EndType() == 4) {
		return statusInvalid
	}
	if len(battle.pending) != 0 {
		return 5
	}
	preparations.Lock()
	defer preparations.Unlock()
	p := preparations.values[uint64(preparationID)]
	if p == nil {
		return 4
	}
	catalog := sessions.catalogs[battle.catalogID]
	if p.mode != battle.mode || !p.enemiesRegistered || !validateRegisteredParty(p, catalog) {
		return statusInvalid
	}
	projection, err := projectPreparedState(p, catalog, false)
	if err != nil || projection.encounter == nil || len(projection.members) != 4 {
		return statusInvalid
	}
	if p.settings.trance != nil {
		projection.catalog.TranceRates = *p.settings.trance
	}
	if p.settings.burst != nil {
		projection.catalog.BurstGauge = *p.settings.burst
	}
	if battle.journal == nil {
		return statusInvalid
	}
	if err = battle.journal.begin(battle.value.StateView(), 1, 0, int(enemyType), 0, battlecore.StandaloneCardPlay{}, battlecore.BurstSelection{}); err != nil {
		return statusInvalid
	}
	battle.value.SetDeckOrderObserver(battle.journal.deckChanged)
	defer battle.value.SetDeckOrderObserver(nil)
	next, err := battle.value.NextEncounter(projection.catalog, *projection.encounter, battlecore.WaveTakeover{})
	if err != nil {
		return statusInvalid
	}
	next.SetDeckOrderObserver(battle.journal.deckChanged)
	defer next.SetDeckOrderObserver(nil)
	rows, err := next.Start()
	if err != nil {
		battle.setFault(0, "next-start", err)
		return 6
	}
	text, err := battlecodec.EncodeStart(rows, battle.identities)
	if err != nil || len(text) > maxSessionOutput {
		if err == nil {
			err = fmt.Errorf("encoded battle result exceeds 1 MiB")
		}
		battle.setFault(0, "next-result", err)
		return 6
	}
	if err = battle.journal.commit(); err != nil {
		battle.setFault(0, "next-journal", err)
		return 6
	}
	battle.value = next
	battle.enemyType = int(enemyType)
	battle.pending = []byte(text)
	return statusOK
}
