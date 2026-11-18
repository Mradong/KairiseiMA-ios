package main

/*
#include <stdint.h>
*/
import "C"

import "unsafe"

//export battlecore_preparation_enemy_clear
func battlecore_preparation_enemy_clear(handle C.ulonglong) C.int {
	preparations.Lock()
	defer preparations.Unlock()
	p := preparations.values[uint64(handle)]
	if p == nil {
		return 4
	}
	return C.int(p.clearEnemies())
}

//export battlecore_preparation_enemy
func battlecore_preparation_enemy(handle C.ulonglong, member C.int, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, 276, func(p *preparation, raw []byte) int { return p.setEnemy(int(member), raw) })
}

//export battlecore_preparation_enemy_skill
func battlecore_preparation_enemy_skill(handle C.ulonglong, member, kind, slot C.int, data *C.uchar, count C.int) C.int {
	if count != skillWireSize && count != 2656 {
		return statusBuffer
	}
	return registerWire(handle, data, count, int(count), func(p *preparation, raw []byte) int { return p.setEnemySkill(int(member), int(kind), int(slot), raw) })
}

//export battlecore_preparation_story_help
func battlecore_preparation_story_help(handle C.ulonglong, data *C.uchar, count, target C.int) C.int {
	return registerWire(handle, data, count, 4980, func(p *preparation, raw []byte) int { return p.setStoryHelp(raw, int32(target)) })
}

//export battlecore_preparation_initialization
func battlecore_preparation_initialization(handle C.ulonglong, kind C.int, data *C.int, count C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if data == nil || count < 1 || count > 44 {
		return statusBuffer
	}
	values := make([]int32, int(count))
	for i, value := range unsafe.Slice(data, int(count)) {
		values[i] = int32(value)
	}
	preparations.Lock()
	defer preparations.Unlock()
	p := preparations.values[uint64(handle)]
	if p == nil {
		return 4
	}
	return C.int(p.initialize(int(kind), values))
}
