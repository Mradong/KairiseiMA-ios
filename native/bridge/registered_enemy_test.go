package main

import (
	"encoding/binary"
	"testing"
)

func TestDecodePreparedEnemyFieldsAndGuards(t *testing.T) {
	data := make([]byte, 276)
	put32 := func(offset int, value uint32) { binary.LittleEndian.PutUint32(data[offset:], value) }
	put64 := func(offset int, value uint64) { binary.LittleEndian.PutUint64(data[offset:], value) }
	put32(0, 901)
	put32(4, 13)
	put64(8, 0)
	put64(16, 1000)
	put32(24, 25)
	put64(28, 345)
	put64(36, 456)
	put64(44, 567)
	put64(52, 678)
	put64(60, 789)
	put32(68, 42)
	binary.LittleEndian.PutUint16(data[72:], 65535)
	put32(92, 11)
	put32(112, 2)
	put32(116, 3)
	put32(152, 2)
	put32(156, 4)
	binary.LittleEndian.PutUint16(data[120:], 65534)
	binary.LittleEndian.PutUint16(data[142:], 65533)
	enemy := &preparedEnemy{data: data}
	got, err := decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0)
	if err != nil {
		t.Fatal(err)
	}
	if got.HP != 250 || got.MaxHP != 250 || got.Parent != 2 || got.Definition.Attack != 345 || got.Definition.MagicDefense != 789 || got.Definition.TribeIDs[0] != 13 || got.Definition.Attribute != "ICE" || got.Definition.AttributeFixed[0] != 11 || got.Level.AttributeRates[0] != -1 || got.Level.StatusResistances[1] != -2 || got.Level.DOTReductions[0] != -3 || got.Level.InitialAwake != 4 {
		t.Fatalf("unexpected enemy projection: %+v", got)
	}
	put32(160, 1)
	if _, err = decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0); err == nil {
		t.Fatal("unimplemented gauge must be rejected")
	}
	put32(160, 0)
	enemy.normal[0] = make([]byte, 2656)
	if _, err = decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0); err != nil {
		t.Fatal("empty enemy skill must be ignored", err)
	}
	binary.LittleEndian.PutUint32(enemy.normal[0], 1001)
	if _, err = decodePreparedEnemy(map[string]map[int32]string{"ATTR": {2: "ICE"}}, nil, enemy, 0); err == nil {
		t.Fatal("unimplemented enemy program must be rejected")
	}
}
