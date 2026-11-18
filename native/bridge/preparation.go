package main

/*
#include <stdint.h>
*/
import "C"

import (
	"sync"
	"unsafe"

	"kairisei.local/server/battlecodec"
	"kairisei.local/server/battlecore"
)

type preparedUser struct {
	data        [18]int32
	seed        int32
	cards       [20][]byte
	extraSkills [20][4][]byte
	spheres     []byte
	buddies     [5][]byte
}

type preparation struct {
	mode              int
	users             [8]*preparedUser
	settings          preparationSettings
	summons           [10]*preparedSummon
	npcs              [][80]byte
	enemies           [4]*preparedEnemy
	enemiesRegistered bool
	storyHelp         *preparedStoryHelp
}

// This subset supports preparation lifetimes, player/summon/NPC registration,
// settings and skill queries. Full battle preparation is not advertised.
var preparations = struct {
	sync.Mutex
	next   uint64
	values map[uint64]*preparation
}{values: make(map[uint64]*preparation)}

//export battlecore_preparation_open
func battlecore_preparation_open(mode C.int, handle *C.ulonglong) C.int {
	if handle == nil {
		return statusBuffer
	}
	if mode < 0 || mode > 7 {
		return statusInvalid
	}
	preparations.Lock()
	defer preparations.Unlock()
	if len(preparations.values) >= 64 {
		return 5
	}
	preparations.next++
	if preparations.next == 0 {
		return statusPanic
	}
	preparations.values[preparations.next] = newPreparation(int(mode))
	*handle = C.ulonglong(preparations.next)
	return statusOK
}

//export battlecore_preparation_close
func battlecore_preparation_close(handle C.ulonglong) C.int {
	preparations.Lock()
	defer preparations.Unlock()
	if _, exists := preparations.values[uint64(handle)]; !exists {
		return 4
	}
	delete(preparations.values, uint64(handle))
	return statusOK
}

//export battlecore_preparation_skill_info
func battlecore_preparation_skill_info(handle C.ulonglong, input *C.uchar, count C.int, output *C.longlong, outputCount C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if input == nil || output == nil || count != 2436 || outputCount != 250 {
		return statusBuffer
	}
	wire := append([]byte(nil), unsafe.Slice((*byte)(unsafe.Pointer(input)), 2436)...)
	preparations.Lock()
	value, exists := preparations.values[uint64(handle)]
	preparations.Unlock()
	if !exists {
		return 4
	}
	values, err := battlecore.NativeClientSkillInfoForMode(wire, value.mode)
	if err != nil {
		return statusInvalid
	}
	result := unsafe.Slice(output, 250)
	for i, value := range values {
		result[i] = C.longlong(value)
	}
	return statusOK
}

//export battlecore_preparation_user
func battlecore_preparation_user(handle C.ulonglong, member C.int, data *C.int, count C.int, seed C.int, output *C.uchar, capacity C.int, written *C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if data == nil || count != 18 || output == nil || capacity < 1 || capacity > 1048576 || written == nil {
		return statusBuffer
	}
	var user [18]int32
	for i, v := range unsafe.Slice(data, 18) {
		user[i] = int32(v)
	}
	preparations.Lock()
	defer preparations.Unlock()
	value, exists := preparations.values[uint64(handle)]
	if !exists {
		return 4
	}
	memberLimit := C.int(4)
	if value.mode == 3 || value.mode == 4 {
		memberLimit = 8
	}
	if member < 1 || member > memberLimit {
		return statusInvalid
	}
	result, err := battlecore.NativeClientUserParameters(int(member), user, value.mode == 6 || value.mode == 7)
	if err != nil {
		return statusInvalid
	}
	csv, err := battlecodec.Encode([]battlecore.BattleResult{result})
	if err != nil {
		return statusInvalid
	}
	csv += "\n"
	*written = C.int(len(csv))
	if int(capacity) < len(csv) {
		return statusBuffer
	}
	copy(unsafe.Slice((*byte)(unsafe.Pointer(output)), int(capacity)), csv)
	// Match the original wrapper's transactional update: insufficient output
	// capacity reports the required byte count without committing user data.
	// Re-registering base parameters preserves this member's existing cards,
	// extra skills and companions, matching the original candidate-user copy.
	updated := preparedUser{data: user, seed: int32(seed)}
	if previous := value.users[int(member)-1]; previous != nil {
		updated = *previous
		updated.data, updated.seed = user, int32(seed)
	}
	value.users[int(member)-1] = &updated
	return statusOK
}
