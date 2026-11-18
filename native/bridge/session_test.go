package main

import (
	"encoding/json"
	"os"
	"testing"

	"kairisei.local/server/battlecontent"
	"kairisei.local/server/battlecore"
)

func TestStandaloneSyntheticCatalogAndBattle(t *testing.T) {
	raw, err := os.ReadFile("../../tests/fixtures/native-session/tables.json")
	if err != nil {
		t.Fatal(err)
	}
	var input struct{ Tables []struct{ Name, Text string } }
	if err = json.Unmarshal(raw, &input); err != nil {
		t.Fatal(err)
	}
	tables := make(map[string]string)
	for _, table := range input.Tables {
		tables[table.Name] = table.Text
	}
	defs, err := battlecontent.LoadDefinitionsFromTables(tables)
	if err != nil {
		t.Fatal(err)
	}
	catalog, err := battlecore.NewCatalog(defs)
	if err != nil {
		t.Fatal(err)
	}
	var request struct {
		Config  battlecore.StandaloneBattleConfig
		Members []battlecore.StandaloneBattleMember
	}
	raw, err = os.ReadFile("../../tests/fixtures/native-session/battle.json")
	if err != nil {
		t.Fatal(err)
	}
	if err = json.Unmarshal(raw, &request); err != nil {
		t.Fatal(err)
	}
	battle, err := battlecore.NewStandaloneBattle(catalog, request.Config, request.Members)
	if err != nil {
		t.Fatal(err)
	}
	rows, err := battle.Start()
	if err != nil || len(rows) == 0 {
		t.Fatalf("start produced no rows: %v", err)
	}
	if _, err = battle.TurnPhase(); err != nil {
		t.Fatal(err)
	}
	if _, err = battle.UserPhase(); err != nil {
		t.Fatal(err)
	}
}
