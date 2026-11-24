package main

import (
	"encoding/json"
	"fmt"
	"os"

	"kairisei.local/server/internal/gamestate"
	"kairisei.local/server/internal/masterdata"
)

func main() {
	if len(os.Args) != 3 {
		fmt.Fprintln(os.Stderr, "expected battle and normal-quest JSON paths")
		os.Exit(1)
	}
	battle, err := masterdata.LoadBattleRuntimeMaster(os.Args[1])
	if err != nil {
		fmt.Fprintln(os.Stderr, "battle master validation failed:", err)
		os.Exit(1)
	}
	normal, err := masterdata.LoadNormalQuestRuntimeMaster(os.Args[2])
	if err != nil {
		fmt.Fprintln(os.Stderr, "normal-quest master validation failed:", err)
		os.Exit(1)
	}
	state := gamestate.State{TeamBattleSolo: json.RawMessage(`{"9":[]}`)}
	if err := masterdata.ApplyBattleRuntimeMaster(&state, battle); err != nil {
		fmt.Fprintln(os.Stderr, "battle catalog projection failed:", err)
		os.Exit(1)
	}
	result := map[string]any{
		"success": true,
		"battle": map[string]int{
			"groups":                  len(battle.Groups),
			"replays":                 len(battle.Replays),
			"rewards":                 len(battle.Rewards),
			"recommendations":         len(battle.Recommendations),
			"pastBossGroups":          len(battle.PastBossGroups),
			"scheduleGroupIDs":        len(battle.ScheduleGroupIDs),
			"towerProfiles":           len(battle.TowerProfiles),
			"ownDeckVariants":         len(battle.OwnDeckVariants),
			"optionalOwnDeckVariants": len(battle.OptionalOwnDeckBossIDs),
			"deduplicatedFamilies":    len(battle.DeduplicatedFamilies),
		},
		"normalQuest": map[string]int{
			"entryGroups":     len(normal.EntryGroups),
			"areas":           len(normal.Areas),
			"replays":         len(normal.Replays),
			"rewards":         len(normal.Rewards),
			"readingSections": len(normal.MainQuestReadingSections),
		},
		"scope": "Received raw static JSON validated by pinned local production loaders, with battle catalog projection into a fresh in-memory state. Deployment/runtime, real-account and gameplay verification remains pending.",
	}
	result["ownDeckMappings"] = battle.OwnDeckVariants
	result["optionalOwnDeckBossIDs"] = battle.OptionalOwnDeckBossIDs
	result["disabledBossIDs"] = state.DisabledTeamBattleBossIDs
	result["projectedReplayCount"] = len(state.TeamBattleReplays)
	result["projectedRewardCount"] = len(state.TeamBattleRewards)
	var catalog map[string]json.RawMessage
	if err := json.Unmarshal(state.TeamBattleSolo, &catalog); err != nil {
		fmt.Fprintln(os.Stderr, "battle catalog decoding failed:", err)
		os.Exit(1)
	}
	var groups []struct {
		Bosses []json.RawMessage `json:"10"`
	}
	if err := json.Unmarshal(catalog["9"], &groups); err != nil {
		fmt.Fprintln(os.Stderr, "battle entry decoding failed:", err)
		os.Exit(1)
	}
	entryCount := 0
	for _, group := range groups {
		entryCount += len(group.Bosses)
	}
	result["projectedEntryBossCount"] = entryCount
	if err := json.NewEncoder(os.Stdout).Encode(result); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}
