package httpapi

import (
	"encoding/json"
	"os"
	"testing"

	"kairisei.local/server/internal/game"
	"kairisei.local/server/internal/gamestate"
)

func TestIOSLoginBonusWireProjection(t *testing.T) {
	reward := func(n int) gamestate.Reward {
		return gamestate.Reward{Type: 10, Num: n, RewardTypeID: n, CardSkillLevels: []int16{}}
	}
	day := func(n int) gamestate.LoginBonusDay {
		return gamestate.LoginBonusDay{Day: n, Comment: "test", Reward: reward(n * 10)}
	}
	claim := func(n int) *game.LoginBonusClaim {
		d := day(n)
		return &game.LoginBonusClaim{Day: d, Result: game.PresentReceiveResult{Rewards: []game.ReceivedReward{{Reward: d.Reward}}}}
	}
	daily, err := projectDailyLoginBonus(claim(2))
	if err != nil {
		t.Fatal(err)
	}
	beginner, err := projectBeginnerLoginBonus(claim(2), []gamestate.LoginBonusDay{day(1), day(2), day(3)})
	if err != nil {
		t.Fatal(err)
	}
	total, err := projectTotalLoginBonus(claim(7), []gamestate.LoginBonusDay{day(7), day(14)})
	if err != nil {
		t.Fatal(err)
	}
	payload := map[string]any{"user": map[string]any{"userid": 1337, "name": "test", "lv": 1, "ap": 1, "ap_max": 2, "bp": 1, "bp_max": 2, "gold": 0}, "server_time": 1, "login_bonus_daily": daily, "login_bonus_beginner": beginner, "login_bonus_total": total}
	raw, err := json.Marshal(payload)
	if err != nil {
		t.Fatal(err)
	}
	if target := os.Getenv("IOS_BONUS_PROJECTION_FIXTURE"); target != "" {
		if err := os.WriteFile(target, append([]byte("{\"res_code\":0}\n"), raw...), 0600); err != nil {
			t.Fatal(err)
		}
	}
}
