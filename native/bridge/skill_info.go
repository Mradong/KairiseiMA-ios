package main

/*
#include <stdint.h>
*/
import "C"

import (
	"unsafe"

	"kairisei.local/server/battlecore"
)

//export battlecore_skill_info
func battlecore_skill_info(input *C.uchar, count C.int, output *C.longlong, outputCount C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	// The original ABI consumes packed SkillData, not JSON. A successful
	// query returns five extends x five roles x ten signed 64-bit values.
	if input == nil || output == nil || count != 2436 || outputCount != 250 {
		return statusBuffer
	}
	wire := append([]byte(nil), unsafe.Slice((*byte)(unsafe.Pointer(input)), 2436)...)
	values, err := battlecore.NativeClientSkillInfoForMode(wire, 1)
	if err != nil {
		return statusInvalid
	}
	result := unsafe.Slice(output, 250)
	for i, value := range values {
		result[i] = C.longlong(value)
	}
	return statusOK
}
