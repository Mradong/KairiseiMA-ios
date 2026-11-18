package main

import (
	"encoding/binary"
	"errors"
)

// rawSkill preserves native values before schema-dependent enum conversion.
// In particular, condition parameters are signed 64-bit and need not fit an
// int32 unless their condition identifies that position as an enum.
type rawSkill struct {
	Header         [15]int32
	HoldParameters [5]int64
	HandFields     [8]int32
	Extends        [5]rawSkillExtend
	Level          int32
}

type rawSkillCondition struct {
	Order      int32
	Parameters [5]int64
}

type rawSkillExtend struct {
	Conditions                  [2]rawSkillCondition
	Priority, ValueRole, RoleID int32
	Roles                       [5]rawSkillRole
}

type rawSkillRole struct {
	Role, Target                 int32
	ExcludeSelf                  byte
	Attribute                    int32
	Parameters                   [10]int32
	Chain, Hate, PVP, ValueBoost int32
}

func decodeRawSkill(wire []byte) (rawSkill, error) {
	var result rawSkill
	if len(wire) != skillWireSize {
		return result, errors.New("invalid packed skill size")
	}
	i32 := func(offset int) int32 { return int32(binary.LittleEndian.Uint32(wire[offset:])) }
	i64 := func(offset int) int64 { return int64(binary.LittleEndian.Uint64(wire[offset:])) }
	for i := range result.Header {
		result.Header[i] = i32(i * 4)
	}
	for i := range result.HoldParameters {
		result.HoldParameters[i] = i64(60 + i*8)
	}
	for i := range result.HandFields {
		result.HandFields[i] = i32(100 + i*4)
	}
	for i := range result.Extends {
		base := 132 + i*460
		ext := &result.Extends[i]
		for j := range ext.Conditions {
			offset := base + j*44
			ext.Conditions[j].Order = i32(offset)
			for k := range ext.Conditions[j].Parameters {
				ext.Conditions[j].Parameters[k] = i64(offset + 4 + k*8)
			}
		}
		ext.Priority, ext.ValueRole, ext.RoleID = i32(base+88), i32(base+92), i32(base+96)
		for j := range ext.Roles {
			offset := base + 100 + j*72
			role := &ext.Roles[j]
			role.Role, role.Target, role.ExcludeSelf, role.Attribute = i32(offset), i32(offset+4), wire[offset+8], i32(offset+12)
			for k := range role.Parameters {
				role.Parameters[k] = i32(offset + 16 + k*4)
			}
			role.Chain, role.Hate, role.PVP, role.ValueBoost = i32(offset+56), i32(offset+60), i32(offset+64), i32(offset+68)
		}
	}
	result.Level = i32(2432)
	return result, nil
}
