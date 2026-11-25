package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"os"

	"kairisei.local/server/battlecodec"
	"kairisei.local/server/battlecontent"
	"kairisei.local/server/battlecore"
	"kairisei.local/server/battlemodel"
)

const maxSessionOutput = 1048576

type partyCard struct {
	ID          int   `json:"cardid"`
	Level       int   `json:"lv"`
	Love        int   `json:"love"`
	Fame        int   `json:"fame"`
	SkillLevels []int `json:"skill_lv"`
}
type partyMember struct {
	ArthurType      int         `json:"arthur_type"`
	JobType         int         `json:"job_type"`
	HP              int         `json:"hp"`
	Attack          int         `json:"atkp"`
	Magic           int         `json:"intp"`
	Mind            int         `json:"mndp"`
	Cards           []partyCard `json:"deck"`
	Support         []partyCard `json:"support_deck"`
	SupportUnlocked int         `json:"support_card_unlock_slot_num"`
	Spheres         []struct {
		ID    int `json:"sphrid"`
		Level int `json:"lv"`
	} `json:"sphrs"`
	Buddies []struct {
		ID    int `json:"buddyid"`
		Level int `json:"lv"`
	} `json:"buddys"`
}
type command struct {
	Operation int                               `json:"operation"`
	Config    battlecore.StandaloneBattleConfig `json:"config"`
	EnemyType int                               `json:"enemy_type"`
	Party     []partyMember                     `json:"party"`
	Selection battlecore.StandaloneCardPlay     `json:"selection"`
	Member    int                               `json:"member"`
}

func projectCard(catalog *battlecore.CombatCatalog, wire partyCard, slot int) (battlecore.BattleCard, error) {
	def, exists := catalog.Cards[wire.ID]
	if !exists || wire.Level < 1 || wire.Level > def.MaxLevel || len(wire.SkillLevels) > 6 {
		return battlecore.BattleCard{}, errors.New("card definition or execution-level shape unavailable")
	}
	ids := []int{def.NormalSkillID, def.ArthurSkillID, def.CallSkillID, def.PassiveSkillID, def.ArthurPassiveSkillID}
	card := battlecore.BattleCard{CardType: slot, CardID: wire.ID, Level: wire.Level, Love: wire.Love, Fame: wire.Fame}
	for index, level := range wire.SkillLevels {
		if level < 0 {
			return card, errors.New("negative execution level")
		}
		if index >= len(ids) {
			if level != 0 {
				return card, errors.New("unmapped sixth execution level")
			}
			continue
		}
		card.SkillLevels[index] = battlemodel.SkillLevel{ID: ids[index], Value: level}
	}
	return card, nil
}
func createBattle(catalog *battlecore.CombatCatalog, cmd command) (*battlecore.StandaloneBattle, error) {
	if len(cmd.Party) != 4 {
		return nil, errors.New("four real party records required")
	}
	members := make([]battlecore.StandaloneBattleMember, 4)
	for index, wire := range cmd.Party {
		if len(wire.Cards) != 10 || wire.HP <= 0 || wire.SupportUnlocked < 0 || wire.SupportUnlocked > len(wire.Support) {
			return nil, errors.New("party parameters or deck shape unavailable")
		}
		job := wire.JobType
		member := battlecore.StandaloneBattleMember{MemberType: index + 1, ArthurType: wire.ArthurType, JobType: &job, HP: wire.HP, Attack: wire.Attack, Magic: wire.Magic, Mind: wire.Mind}
		for slot, card := range wire.Cards {
			projected, err := projectCard(catalog, card, slot+1)
			if err != nil {
				return nil, err
			}
			member.DeckCards = append(member.DeckCards, projected)
		}
		for slot, card := range wire.Support[:wire.SupportUnlocked] {
			if card.ID == 0 {
				continue
			}
			projected, err := projectCard(catalog, card, slot+11)
			if err != nil {
				return nil, err
			}
			member.SupportCards = append(member.SupportCards, projected)
		}
		for slot, sphere := range wire.Spheres {
			if sphere.ID == 0 {
				continue
			}
			if _, exists := catalog.Spheres[sphere.ID]; !exists || sphere.Level < 1 {
				return nil, errors.New("sphere definition unavailable")
			}
			member.DeckSpheres = append(member.DeckSpheres, battlecore.BattleSphere{SphereType: slot + 1, SphereID: sphere.ID, Level: sphere.Level})
		}
		for slot, buddy := range wire.Buddies {
			if buddy.ID == 0 {
				continue
			}
			if _, exists := catalog.Buddies[buddy.ID]; !exists || buddy.Level < 1 {
				return nil, errors.New("buddy definition unavailable")
			}
			member.DeckBuddies = append(member.DeckBuddies, battlecore.BattleBuddy{BuddyType: slot + 1, BuddyID: buddy.ID, Level: buddy.Level})
		}
		members[index] = member
	}
	cmd.Config.EnemyType = cmd.EnemyType
	cmd.Config.GameOverByCaller = true
	return battlecore.NewStandaloneBattle(catalog, cmd.Config, members)
}
func advance(battle *battlecore.StandaloneBattle, journal *inputJournal, cmd command, enemyType int) ([]battlecore.BattleResult, error) {
	if err := journal.begin(battle.StateView(), cmd.Operation, cmd.Member, enemyType, 0, cmd.Selection, battlecore.BurstSelection{}); err != nil {
		return nil, err
	}
	battle.SetDeckOrderObserver(journal.deckChanged)
	defer battle.SetDeckOrderObserver(nil)
	var rows []battlecore.BattleResult
	var err error
	switch cmd.Operation {
	case 1:
		rows, err = battle.Start()
	case 2:
		rows, err = battle.TurnPhase()
	case 3:
		rows, err = battle.UserPhase()
	case 4:
		rows, err = battle.Submit(cmd.Selection)
	case 5:
		rows, err = battle.UserAttack()
	case 6:
		rows, err = battle.EnemyPhase()
	case 8:
		rows, err = battle.ChaliceUserPhase()
	case 9:
		rows, err = battle.ChaliceEnemyPhase()
	case 11:
		rows, err = battle.GameOver(cmd.Member)
	case 13:
		rows, err = battle.Continue(cmd.Member)
	default:
		err = errors.New("unsupported engine operation")
	}
	if err != nil {
		return nil, err
	}
	if err := journal.commit(); err != nil {
		return nil, err
	}
	return rows, nil
}
func main() {
	if len(os.Args) != 3 {
		fmt.Fprintln(os.Stderr, "expected card and combat master paths")
		os.Exit(1)
	}
	defs, err := battlecontent.LoadDefinitions(os.Args[1], os.Args[2])
	if err != nil {
		fmt.Fprintln(os.Stderr, "master loading failed")
		os.Exit(1)
	}
	catalog, err := battlecore.NewCatalog(defs)
	if err != nil {
		fmt.Fprintln(os.Stderr, "catalog validation failed")
		os.Exit(1)
	}
	decoder := json.NewDecoder(os.Stdin)
	encoder := json.NewEncoder(os.Stdout)
	var battle *battlecore.StandaloneBattle
	journal := &inputJournal{}
	enemyType := 0
	for {
		var cmd command
		if err := decoder.Decode(&cmd); err != nil {
			return
		}
		if cmd.Operation == 0 {
			battle, err = createBattle(catalog, cmd)
			enemyType = cmd.EnemyType
			if err != nil {
				encoder.Encode(map[string]any{"success": false, "stage": "create", "error": err.Error()})
				continue
			}
			journal = &inputJournal{}
			metadata := make([][]map[string]any, len(cmd.Party))
			for member, wire := range cmd.Party {
				for slot, card := range wire.Cards {
					skill, _, skillErr := catalog.CardSkill(card.ID, wire.ArthurType)
					if skillErr != nil {
						continue
					}
					metadata[member] = append(metadata[member], map[string]any{"card_type": slot + 1, "target": skill.Target, "kind": skill.Kind, "cost": skill.Cost})
				}
			}
			encoder.Encode(map[string]any{"success": true, "state": battle.StateView(), "card_metadata": metadata})
			continue
		}
		if battle == nil {
			encoder.Encode(map[string]any{"success": false, "stage": "not_created"})
			continue
		}
		rows, err := advance(battle, journal, cmd, enemyType)
		if err != nil {
			encoder.Encode(map[string]any{"success": false, "stage": "advance", "error": err.Error()})
			continue
		}
		csv, err := battlecodec.EncodeOptional(rows)
		if err != nil {
			encoder.Encode(map[string]any{"success": false, "stage": "encode"})
			continue
		}
		encoder.Encode(map[string]any{"success": true, "state": battle.StateView(), "rows": rows, "csv": csv, "input_cmd": string(journal.data), "score_damage": battle.ScoreDamage()})
	}
}
