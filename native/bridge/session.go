package main

/*
#include <stdint.h>
*/
import "C"

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"strings"
	"sync"
	"unsafe"

	"kairisei.local/server/battlecodec"
	"kairisei.local/server/battlecontent"
	"kairisei.local/server/battlecore"
)

const maxJSONInput = 65536
const maxSessionOutput = 1048576

type nativeEnumValue struct {
	Name  string `json:"name"`
	Value int32  `json:"value"`
}
type nativeEnum struct {
	Name   string            `json:"name"`
	Values []nativeEnumValue `json:"values"`
}
type libraryCatalog struct {
	value      *battlecore.CombatCatalog
	schema     map[string]map[int32]string
	references int
}
type libraryBattle struct {
	value     *battlecore.StandaloneBattle
	catalogID uint64
	mode      int
	enemyType int
	pending   []byte
	faulted   bool
	lastError []byte
}

var sessions = struct {
	sync.Mutex
	next     uint64
	catalogs map[uint64]*libraryCatalog
	battles  map[uint64]*libraryBattle
}{catalogs: make(map[uint64]*libraryCatalog), battles: make(map[uint64]*libraryBattle)}

func decodeJSON(raw []byte, target any, allowEmpty bool) error {
	if len(raw) == 0 && allowEmpty {
		return nil
	}
	decoder := json.NewDecoder(bytes.NewReader(raw))
	decoder.DisallowUnknownFields()
	if err := decoder.Decode(target); err != nil {
		return err
	}
	var extra any
	if err := decoder.Decode(&extra); err != io.EOF {
		return errors.New("trailing JSON input")
	}
	return nil
}

func decodeInput(input *C.uchar, count C.int, target any, allowEmpty bool, maximum int) error {
	if count == 0 && allowEmpty {
		return nil
	}
	if input == nil || count < 1 || int(count) > maximum {
		return errors.New("invalid input buffer length")
	}
	return decodeJSON(append([]byte(nil), unsafe.Slice((*byte)(unsafe.Pointer(input)), int(count))...), target, false)
}

func validateNativeEnums(input []nativeEnum) (map[string]map[int32]string, error) {
	schema := make(map[string]map[int32]string, len(input))
	for _, family := range input {
		if family.Name == "" || len(family.Values) == 0 || len(family.Values) > 1024 {
			return nil, errors.New("invalid native enum family")
		}
		if _, exists := schema[family.Name]; exists {
			return nil, errors.New("duplicate native enum family")
		}
		values := make(map[int32]string, len(family.Values))
		for _, value := range family.Values {
			if value.Name == "" || strings.HasPrefix(value.Name, "sizeof_") {
				return nil, errors.New("invalid native enum name")
			}
			if old, exists := values[value.Value]; exists && old != value.Name {
				return nil, errors.New("ambiguous native enum")
			}
			values[value.Value] = value.Name
		}
		schema[family.Name] = values
	}
	return schema, nil
}

func nextSessionHandle() uint64 {
	sessions.next++
	if sessions.next == 0 {
		panic("library handle counter exhausted")
	}
	return sessions.next
}

func insertCatalog(handle *C.ulonglong, load func() (*libraryCatalog, error)) C.int {
	sessions.Lock()
	defer sessions.Unlock()
	if len(sessions.catalogs) >= 2 {
		return 5
	}
	catalog, err := load()
	if err != nil {
		return statusInvalid
	}
	key := nextSessionHandle()
	sessions.catalogs[key] = catalog
	*handle = C.ulonglong(key)
	return statusOK
}

//export battlecore_catalog_open
func battlecore_catalog_open(input *C.uchar, count C.int, handle *C.ulonglong) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if handle == nil {
		return statusBuffer
	}
	var request struct {
		CardMaster string `json:"card_master"`
		BattleRoot string `json:"battle_root"`
	}
	if decodeInput(input, count, &request, false, maxJSONInput) != nil || len(request.CardMaster) == 0 || len(request.CardMaster) > 4096 || len(request.BattleRoot) == 0 || len(request.BattleRoot) > 4096 || strings.ContainsRune(request.CardMaster+request.BattleRoot, 0) {
		return statusInvalid
	}
	return insertCatalog(handle, func() (*libraryCatalog, error) {
		defs, err := battlecontent.LoadDefinitions(request.CardMaster, request.BattleRoot)
		if err != nil {
			return nil, err
		}
		catalog, err := battlecore.NewCatalog(defs)
		return &libraryCatalog{value: catalog}, err
	})
}

//export battlecore_catalog_open_tables
func battlecore_catalog_open_tables(input *C.uchar, count C.int, handle *C.ulonglong) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if handle == nil {
		return statusBuffer
	}
	var request struct {
		Tables []struct {
			Name string `json:"name"`
			Text string `json:"text"`
		} `json:"tables"`
		Enums []nativeEnum `json:"enums"`
	}
	if decodeInput(input, count, &request, false, 67108864) != nil || len(request.Tables) == 0 || len(request.Tables) > 64 {
		return statusInvalid
	}
	tables := make(map[string]string, len(request.Tables))
	for _, table := range request.Tables {
		if table.Name == "" || len(table.Name) > 128 || strings.ContainsAny(table.Name, "/\\\x00") || table.Text == "" || len(table.Text) > battlecontent.ModernTableByteLimit(table.Name) {
			return statusInvalid
		}
		if _, exists := tables[table.Name]; exists {
			return statusInvalid
		}
		tables[table.Name] = table.Text
	}
	return insertCatalog(handle, func() (*libraryCatalog, error) {
		defs, err := battlecontent.LoadDefinitionsFromTables(tables)
		if err != nil {
			return nil, err
		}
		catalog, err := battlecore.NewCatalog(defs)
		if err != nil {
			return nil, err
		}
		var schema map[string]map[int32]string
		if len(request.Enums) != 0 {
			schema, err = validateNativeEnums(request.Enums)
			if err != nil {
				return nil, err
			}
		}
		return &libraryCatalog{value: catalog, schema: schema}, nil
	})
}

//export battlecore_catalog_close
func battlecore_catalog_close(handle C.ulonglong) C.int {
	sessions.Lock()
	defer sessions.Unlock()
	catalog := sessions.catalogs[uint64(handle)]
	if catalog == nil {
		return 4
	}
	if catalog.references != 0 {
		return 5
	}
	delete(sessions.catalogs, uint64(handle))
	return statusOK
}

func createContext(catalogHandle, preparationHandle C.ulonglong, input *C.uchar, count C.int, handle *C.ulonglong) (status C.int) {
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
	if decodeInput(input, count, &request, false, maxJSONInput) != nil || request.Mode != 1 || request.Config.Alchemist {
		return statusInvalid
	}
	if request.EnemyType != nil && (*request.EnemyType < 0 || *request.EnemyType > 4) {
		return statusInvalid
	}
	sessions.Lock()
	defer sessions.Unlock()
	catalog := sessions.catalogs[uint64(catalogHandle)]
	if catalog == nil {
		return 4
	}
	if len(sessions.battles) >= 64 {
		return 5
	}
	if request.EnemyType != nil {
		request.Config.EnemyType = *request.EnemyType
	}
	if preparationHandle != 0 {
		preparations.Lock()
		defer preparations.Unlock()
		p := preparations.values[uint64(preparationHandle)]
		if p == nil {
			return 4
		}
		if p.mode != request.Mode {
			return statusInvalid
		}
		if p.settings.continueAllowed != nil {
			request.Config.ContinueAllowed = *p.settings.continueAllowed
		}
		if p.settings.endTurn != nil {
			request.Config.EndTurn = int(*p.settings.endTurn)
		}
		request.Config.DeckShuffleOff = p.settings.deckShuffleOff
		request.Config.StoryEnd = p.settings.storyEnd
		for i := range request.Members {
			member := &request.Members[i]
			if member.MemberType < 1 || member.MemberType > p.memberLimit() {
				return statusInvalid
			}
			if value := p.settings.initialBurst[member.MemberType-1]; value != nil {
				burst := int(*value)
				member.InitialBurst = &burst
			}
		}
	}
	battle, err := battlecore.NewStandaloneBattle(catalog.value, request.Config, request.Members)
	if err != nil {
		return statusInvalid
	}
	key := nextSessionHandle()
	sessions.battles[key] = &libraryBattle{value: battle, catalogID: uint64(catalogHandle), mode: request.Mode, enemyType: request.Config.EnemyType}
	catalog.references++
	*handle = C.ulonglong(key)
	return statusOK
}

//export battlecore_context_create
func battlecore_context_create(catalog C.ulonglong, input *C.uchar, count C.int, handle *C.ulonglong) C.int {
	return createContext(catalog, 0, input, count, handle)
}

//export battlecore_context_create_with_settings
func battlecore_context_create_with_settings(catalog, preparation C.ulonglong, input *C.uchar, count C.int, handle *C.ulonglong) C.int {
	return createContext(catalog, preparation, input, count, handle)
}

//export battlecore_context_close
func battlecore_context_close(handle C.ulonglong) C.int {
	sessions.Lock()
	defer sessions.Unlock()
	battle := sessions.battles[uint64(handle)]
	if battle == nil {
		return 4
	}
	sessions.catalogs[battle.catalogID].references--
	delete(sessions.battles, uint64(handle))
	return statusOK
}

func (b *libraryBattle) setFault(operation int, stage string, cause any) {
	b.faulted = true
	if b.lastError == nil {
		b.lastError, _ = json.Marshal(struct {
			API       string `json:"api"`
			Operation int    `json:"operation"`
			Stage     string `json:"stage"`
			Message   string `json:"message"`
		}{"step", operation, stage, fmt.Sprint(cause)})
	}
}

//export battlecore_step
func battlecore_step(handle C.ulonglong, operation C.int, input *C.uchar, count C.int) (status C.int) {
	status = statusPanic
	var battle *libraryBattle
	defer func() {
		if cause := recover(); cause != nil {
			if battle != nil {
				battle.setFault(int(operation), "panic", cause)
			}
			status = statusPanic
		}
	}()
	var selection battlecore.StandaloneCardPlay
	var member struct {
		MemberType int `json:"member_type"`
	}
	var reservation struct {
		MemberType int  `json:"member_type"`
		Slot       *int `json:"slot"`
	}
	var burstInput struct {
		MemberType int   `json:"member_type"`
		Target     *int  `json:"target"`
		CardTypes  []int `json:"card_types"`
		SkillSlot  *int  `json:"skill_slot"`
	}
	var burst battlecore.BurstSelection
	switch operation {
	case 4:
		if decodeInput(input, count, &selection, false, maxJSONInput) != nil {
			return statusInvalid
		}
	case 10, 11, 13:
		if decodeInput(input, count, &member, false, maxJSONInput) != nil || member.MemberType < 1 || member.MemberType > 8 {
			return statusInvalid
		}
	case 12:
		if decodeInput(input, count, &reservation, false, maxJSONInput) != nil || reservation.MemberType < 1 || reservation.MemberType > 4 || reservation.Slot == nil || *reservation.Slot < 0 || *reservation.Slot > 3 {
			return statusInvalid
		}
		member.MemberType = reservation.MemberType
	case 15:
		if decodeInput(input, count, &burstInput, false, maxJSONInput) != nil || burstInput.MemberType < 1 || burstInput.MemberType > 4 || burstInput.Target == nil || *burstInput.Target < 0 || *burstInput.Target > 11 || burstInput.SkillSlot == nil || *burstInput.SkillSlot < 1 || *burstInput.SkillSlot > 3 || len(burstInput.CardTypes) != 5 {
			return statusInvalid
		}
		for i, card := range burstInput.CardTypes {
			if (card < 0 || card > 10) && (card < 23 || card > 32) {
				return statusInvalid
			}
			burst.CardTypes[i] = card
		}
		burst.Target, burst.SkillSlot = *burstInput.Target, *burstInput.SkillSlot
		member.MemberType = burstInput.MemberType
	default:
		if operation < 1 || (operation > 9 && operation != 14) {
			return statusInvalid
		}
		if decodeInput(input, count, &struct{}{}, true, maxJSONInput) != nil {
			return statusInvalid
		}
	}
	sessions.Lock()
	defer sessions.Unlock()
	battle = sessions.battles[uint64(handle)]
	if battle == nil {
		return 4
	}
	// Record engine panics while session state remains locked. The outer
	// recovery also covers input decoding before a battle has been selected.
	defer func() {
		if cause := recover(); cause != nil {
			battle.setFault(int(operation), "panic", cause)
			status = statusPanic
		}
	}()
	if battle.faulted {
		return 6
	}
	if (operation == 10 || operation == 11 || operation == 13) && member.MemberType > 4 {
		return statusInvalid
	}
	if operation == 10 && len(sessions.catalogs[battle.catalogID].value.NPC.Rules) == 0 {
		// The original rejects an unavailable NPC program before calling the
		// engine, without faulting an otherwise usable battle context.
		return statusInvalid
	}
	if operation == 4 {
		err := battlecore.CheckSummonSelectionInput(selection.SummonsCardMask, false)
		if errors.Is(err, battlecore.ErrSummonSelectionUnsupported) {
			return 7
		}
		if err != nil {
			return statusInvalid
		}
	}
	var rows []battlecore.BattleResult
	var err error
	switch operation {
	case 1:
		rows, err = battle.value.Start()
	case 2:
		rows, err = battle.value.TurnPhase()
	case 3:
		rows, err = battle.value.UserPhase()
	case 4:
		rows, err = battle.value.Submit(selection)
	case 5:
		rows, err = battle.value.UserAttack()
	case 6:
		rows, err = battle.value.EnemyPhase()
	case 7:
		rows, err = battle.value.ResumeResults()
	case 8:
		rows, err = battle.value.ChaliceUserPhase()
	case 9:
		rows, err = battle.value.ChaliceEnemyPhase()
	case 10:
		rows, err = battle.value.SubmitNPC(member.MemberType, battle.enemyType, sessions.catalogs[battle.catalogID].value.NPC)
	case 11:
		rows, err = battle.value.GameOver(member.MemberType)
	case 12:
		rows, err = battle.value.ReserveChaliceSphere(member.MemberType, *reservation.Slot)
	case 13:
		rows, err = battle.value.Continue(member.MemberType)
	case 14:
		rows, err = battle.value.ContinueDataGet()
	case 15:
		rows, err = battle.value.ExecuteBurst(member.MemberType, burst)
	}
	if err != nil {
		if operation == 4 && errors.Is(err, battlecore.ErrSummonSlotsFull) {
			return 8
		}
		battle.setFault(int(operation), "core", err)
		return 6
	}
	text, err := battlecodec.EncodeOptional(rows)
	if err != nil || len(text) > maxSessionOutput {
		if err == nil {
			err = errors.New("encoded battle result exceeds 1 MiB")
		}
		battle.setFault(int(operation), "result-encode", err)
		return 6
	}
	battle.pending = []byte(text)
	return statusOK
}

func copySessionOutput(handle C.ulonglong, output *C.uchar, count C.int, written *C.int, kind int) C.int {
	if written == nil || count < 0 || count > maxSessionOutput {
		return statusBuffer
	}
	sessions.Lock()
	defer sessions.Unlock()
	battle := sessions.battles[uint64(handle)]
	if battle == nil {
		return 4
	}
	if kind == 2 {
		if battle.faulted {
			return 6
		}
		// Input journals only exist on registered contexts, which are not yet exposed.
		return statusInvalid
	}
	data := battle.pending
	if kind == 1 {
		data = battle.lastError
	}
	*written = C.int(len(data))
	if int(count) < len(data) || (len(data) != 0 && output == nil) {
		return statusBuffer
	}
	if len(data) != 0 {
		copy(unsafe.Slice((*byte)(unsafe.Pointer(output)), len(data)), data)
	}
	if kind == 0 {
		battle.pending = nil
	}
	return statusOK
}

//export battlecore_result_read
func battlecore_result_read(handle C.ulonglong, output *C.uchar, count C.int, written *C.int) C.int {
	return copySessionOutput(handle, output, count, written, 0)
}

//export battlecore_last_error
func battlecore_last_error(handle C.ulonglong, output *C.uchar, count C.int, written *C.int) C.int {
	return copySessionOutput(handle, output, count, written, 1)
}

//export battlecore_input_log
func battlecore_input_log(handle C.ulonglong, output *C.uchar, count C.int, written *C.int) C.int {
	return copySessionOutput(handle, output, count, written, 2)
}

//export battlecore_result_size
func battlecore_result_size(handle C.ulonglong, size *C.int) C.int {
	if size == nil {
		return statusBuffer
	}
	sessions.Lock()
	defer sessions.Unlock()
	battle := sessions.battles[uint64(handle)]
	if battle == nil {
		return 4
	}
	*size = C.int(len(battle.pending))
	return statusOK
}

//export battlecore_score_damage
func battlecore_score_damage(handle C.ulonglong, output *C.longlong) C.int {
	if output == nil {
		return statusBuffer
	}
	sessions.Lock()
	defer sessions.Unlock()
	battle := sessions.battles[uint64(handle)]
	if battle == nil {
		return 4
	}
	if battle.faulted {
		return 6
	}
	*output = C.longlong(battle.value.ScoreDamage())
	return statusOK
}

//export battlecore_summons_counts
func battlecore_summons_counts(handle C.ulonglong, output *C.short, count C.int) C.int {
	if output == nil || count != 10 {
		return statusBuffer
	}
	sessions.Lock()
	defer sessions.Unlock()
	battle := sessions.battles[uint64(handle)]
	if battle == nil {
		return 4
	}
	if battle.faulted {
		return 6
	}
	for i, value := range battle.value.SummonExecutionCounts() {
		unsafe.Slice(output, 10)[i] = C.short(value)
	}
	return statusOK
}
