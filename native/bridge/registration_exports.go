package main

/*
#include <stdint.h>
*/
import "C"

import "unsafe"

func registerWire(handle C.ulonglong, data *C.uchar, count C.int, size int, apply func(*preparation, []byte) int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if data == nil || int(count) != size {
		return statusBuffer
	}
	preparations.Lock()
	defer preparations.Unlock()
	p := preparations.values[uint64(handle)]
	if p == nil {
		return 4
	}
	return C.int(apply(p, unsafe.Slice((*byte)(unsafe.Pointer(data)), size)))
}

//export battlecore_preparation_configure
func battlecore_preparation_configure(handle C.ulonglong, operation, member, value C.int) C.int {
	preparations.Lock()
	defer preparations.Unlock()
	p := preparations.values[uint64(handle)]
	if p == nil {
		return 4
	}
	return C.int(p.configure(int(operation), int(member), int32(value)))
}

//export battlecore_preparation_card
func battlecore_preparation_card(handle C.ulonglong, member, slot C.int, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, cardWireSize, func(p *preparation, raw []byte) int {
		return p.setCard(int(member), int(slot), raw)
	})
}

//export battlecore_preparation_extra_skill
func battlecore_preparation_extra_skill(handle C.ulonglong, member, slot, kind C.int, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, skillWireSize, func(p *preparation, raw []byte) int {
		return p.setExtraSkill(int(member), int(slot), int(kind), raw)
	})
}

//export battlecore_preparation_spheres
func battlecore_preparation_spheres(handle C.ulonglong, member C.int, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, spheresWireSize, func(p *preparation, raw []byte) int {
		return p.setSpheres(int(member), raw)
	})
}

//export battlecore_preparation_buddy
func battlecore_preparation_buddy(handle C.ulonglong, member, slot C.int, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, buddyWireSize, func(p *preparation, raw []byte) int {
		return p.setBuddy(int(member), int(slot), raw)
	})
}

//export battlecore_preparation_summons
func battlecore_preparation_summons(handle C.ulonglong, slot C.int, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, 36, func(p *preparation, raw []byte) int {
		return p.setSummon(int(slot), raw, false)
	})
}

//export battlecore_preparation_summons_card
func battlecore_preparation_summons_card(handle C.ulonglong, slot C.int, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, cardWireSize, func(p *preparation, raw []byte) int {
		return p.setSummon(int(slot), raw, true)
	})
}

//export battlecore_preparation_npc
func battlecore_preparation_npc(handle C.ulonglong, data *C.uchar, count C.int) C.int {
	return registerWire(handle, data, count, 800, (*preparation).addNPC)
}
