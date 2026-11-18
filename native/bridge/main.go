// Package main reconstructs only the read-only parameter C ABI recovered from
// the supplied Windows library. It does not implement battle sessions yet.
package main

/*
#include <stdint.h>
*/
import "C"

import (
	"math"
	"unsafe"

	"kairisei.local/server/battlecore"
	"kairisei.local/server/internal/clientbridge"
	"kairisei.local/server/internal/gamestate"
)

const (
	statusOK      = 0
	statusBuffer  = 1
	statusInvalid = 2
	statusPanic   = 3
)

func recoverStatus(status *C.int) {
	if recover() != nil {
		*status = statusPanic
	}
}

//export battlecore_abi_version
func battlecore_abi_version() C.int { return 1 }

//export battlecore_capabilities
func battlecore_capabilities() C.uint {
	// CardParameters | DeckParameters | SummonParameters. No battle capability
	// is advertised by this partial reconstruction.
	return 0x02000041
}

//export battlecore_card_parameters
func battlecore_card_parameters(cardID, level, love, fame C.int, base *C.int, baseCount C.int, bonus *C.int, bonusCount C.int, output *C.int, outputCount C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if base == nil || bonus == nil || output == nil || baseCount != 14 || bonusCount != 4 || outputCount != 4 {
		return statusBuffer
	}
	b, v := unsafe.Slice(base, 14), unsafe.Slice(bonus, 4)
	result, err := clientbridge.CalculateCardParameters(clientbridge.CardParameterRequest{
		CardID: int(cardID), Level: int(level), Love: int(love), Fame: int(fame),
		Base: clientbridge.CardBase{
			HP: int(b[0]), HPLimit: int(b[1]), HPLove: int(b[2]),
			Attack: int(b[3]), AtkLimit: int(b[4]), AtkLove: int(b[5]),
			Magic: int(b[6]), MagLimit: int(b[7]), MagLove: int(b[8]),
			Mind: int(b[9]), MndLimit: int(b[10]), MndLove: int(b[11]),
			LevelMax: int(b[12]), LoveMax: int(b[13]),
		},
		Bonus: clientbridge.CardBonus{HP: int(v[0]), Attack: int(v[1]), Magic: int(v[2]), Mind: int(v[3])},
	})
	if err != nil {
		return statusInvalid
	}
	values := [4]int{result.HP, result.Attack, result.Magic, result.Mind}
	for _, value := range values {
		if value < 0 || value > math.MaxInt32 {
			return statusInvalid
		}
	}
	for index, value := range values {
		unsafe.Slice(output, 4)[index] = C.int(value)
	}
	return statusOK
}

//export battlecore_leader_parameter
func battlecore_leader_parameter(value C.int, output *C.int) C.int {
	if output == nil {
		return statusBuffer
	}
	if value < 0 {
		return statusInvalid
	}
	result := gamestate.DeckLeaderParameter(int(value))
	if result < 0 || result > math.MaxInt32 {
		return statusInvalid
	}
	*output = C.int(result)
	return statusOK
}

//export battlecore_support_parameters
func battlecore_support_parameters(base *C.int, baseCount C.int, bonus *C.int, bonusCount C.int, love C.int, output *C.int, outputCount C.int) C.int {
	if base == nil || bonus == nil || output == nil || baseCount != 4 || bonusCount != 4 || outputCount != 4 {
		return statusBuffer
	}
	if love < 0 || love > 100 {
		return statusInvalid
	}
	b, v := unsafe.Slice(base, 4), unsafe.Slice(bonus, 4)
	var result [4]C.int
	for index := range result {
		if b[index] < 0 || v[index] < 0 {
			return statusInvalid
		}
		value := gamestate.DeckSupportParameter(int(b[index]), int(v[index]), int(love))
		if value < 0 || value > math.MaxInt32 {
			return statusInvalid
		}
		result[index] = C.int(value)
	}
	copy(unsafe.Slice(output, 4), result[:])
	return statusOK
}

//export battlecore_support_skill_index
func battlecore_support_skill_index(partitions *C.int, count C.int, love C.int, output *C.int) C.int {
	if partitions == nil || output == nil || count != 3 {
		return statusBuffer
	}
	p := unsafe.Slice(partitions, 3)
	if love < 0 || love > 100 || p[0] < 1 || p[1] <= p[0] || p[2] <= p[1] || p[2] > 100 {
		return statusInvalid
	}
	result := battlecore.NativeSupportSkillIndex([3]int{int(p[0]), int(p[1]), int(p[2])}, int(love))
	*output = C.int(result)
	return statusOK
}

//export battlecore_summons_parameters
func battlecore_summons_parameters(level C.int, base *C.int, baseCount C.int, output *C.int, outputCount C.int) (status C.int) {
	status = statusPanic
	defer recoverStatus(&status)
	if base == nil || output == nil || baseCount != 13 || outputCount != 6 {
		return statusBuffer
	}
	b := unsafe.Slice(base, 13)
	var ranges [6]battlecore.SummonsParameterRange
	for index := range ranges {
		ranges[index] = battlecore.SummonsParameterRange{Initial: int32(b[index*2]), Maximum: int32(b[index*2+1])}
	}
	result := battlecore.SummonsParameters(int32(level), int32(b[12]), ranges)
	for index, value := range result {
		unsafe.Slice(output, 6)[index] = C.int(value)
	}
	return statusOK
}

func main() {}
