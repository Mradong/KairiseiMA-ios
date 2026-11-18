package main

import (
	"encoding/binary"
	"testing"
)

func TestDecodeRawSkill(t *testing.T) {
	data := make([]byte, skillWireSize)
	put32 := func(pos int, v int32) { binary.LittleEndian.PutUint32(data[pos:], uint32(v)) }
	put64 := func(pos int, v int64) { binary.LittleEndian.PutUint64(data[pos:], uint64(v)) }
	put32(0, 123)
	put32(56, -91)
	put64(60, -9223372036854775807)
	put32(128, 991)
	put32(2432, 42)
	for i := 0; i < 5; i++ {
		base := 132 + i*460
		put32(base+44, int32(100+i))
		put64(base+48+4*8, int64(-500-i))
		put32(base+88, int32(200+i))
		put32(base+96, int32(300+i))
		for j := 0; j < 5; j++ {
			pos := base + 100 + j*72
			put32(pos, int32(1000+i*5+j))
			data[pos+8] = byte(i + j + 1)
			put32(pos+12, int32(-100-i-j))
			put32(pos+52, int32(700+i*5+j))
			put32(pos+68, int32(800+i*5+j))
		}
	}
	got, err := decodeRawSkill(data)
	if err != nil || got.Header[0] != 123 || got.Header[14] != -91 || got.HoldParameters[0] != -9223372036854775807 || got.HandFields[7] != 991 || got.Level != 42 {
		t.Fatalf("header: %+v, %v", got, err)
	}
	for i, ext := range got.Extends {
		if ext.Conditions[1].Order != int32(100+i) || ext.Conditions[1].Parameters[4] != int64(-500-i) || ext.Priority != int32(200+i) || ext.RoleID != int32(300+i) {
			t.Fatalf("extend %d: %+v", i, ext)
		}
		for j, role := range ext.Roles {
			if role.Role != int32(1000+i*5+j) || role.ExcludeSelf != byte(i+j+1) || role.Attribute != int32(-100-i-j) || role.Parameters[9] != int32(700+i*5+j) || role.ValueBoost != int32(800+i*5+j) {
				t.Fatalf("role %d/%d: %+v", i, j, role)
			}
		}
	}
	for _, size := range []int{0, skillWireSize - 1, skillWireSize + 1} {
		if _, err := decodeRawSkill(make([]byte, size)); err == nil {
			t.Fatalf("accepted length %d", size)
		}
	}
}
