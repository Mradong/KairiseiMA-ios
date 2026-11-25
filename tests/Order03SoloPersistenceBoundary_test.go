package httpapi

import (
	"encoding/json"
	"fmt"
	"net/http/httptest"
	"strings"
	"testing"

	"kairisei.local/server/internal/accountstore"
	"kairisei.local/server/internal/game"
	"kairisei.local/server/internal/gamestate"
	"kairisei.local/server/internal/testfixture"
)

// This describes the deployed source's boundary; it does not add persistence.
func TestOrder03SoloPersistenceBoundary(t *testing.T) {
	var initial gamestate.State
	account := testAccount(t, func(state *gamestate.State) {
		state.User.Name = "本地边界夹具"
		state.User.BP = 20
		helper := &state.PlayerProgressionPolicy.Friends.HelperReward
		helper.OtherPerPartner, helper.FriendPerPartner = 5, 10
		state.TeamBattleSolo = json.RawMessage(`{"9":[],"10":[{"0":300101,"9":0,"10":[{"0":30010102,"5":15,"10":0,"16":0}]}],"11":[],"12":[]}`)
		initial = *state
	})
	state := account.Snapshot(initial)
	state.TeamBattleReplays = []gamestate.TeamBattleReplay{{BossID: 30010102, EnemyPartyID: 1, CostInitial: 3, HoldMax: 6}}
	state.TeamBattleRewards = []gamestate.TeamBattleRewardProfile{{BossID: 30010102, ResultRewards: []gamestate.Reward{{Type: 4, Num: 1, CardSkillLevels: []int16{}}}}}
	api := &API{initialState: state, account: account}
	payload := fmt.Sprintf(`{"bossid":30010102,"deck_arthur_type":3,"deck_arthur_type_idx":0,"partner_deck_selects":[{"userid":%d,"arthur_type":1,"deck_idx":0},{"userid":%d,"arthur_type":2,"deck_idx":0},{"userid":%d,"arthur_type":4,"deck_idx":0}]}`, state.User.UserID, state.User.UserID, state.User.UserID)
	start := func(api *API) *httptest.ResponseRecorder {
		response := httptest.NewRecorder()
		api.teamBattleSoloStart(response, httptest.NewRequest("POST", "/TeamBattleSoloStart", strings.NewReader(payload)))
		return response
	}
	if response := start(api); response.Code != 200 {
		t.Fatalf("synthetic first entry: HTTP %d", response.Code)
	}
	saved := api.account.Snapshot(state)
	if saved.User.BP != 5 || saved.ActiveTeamBattle == nil {
		t.Fatal("synthetic first entry did not debit once or retain its opening")
	}
	accounts := testfixture.NewFriendCapacityTestAccounts(t)
	accounts.Database().SetCatalog(state)
	if saved.User.UserID != accountstore.PrimaryUserID {
		t.Fatal("unexpected synthetic primary fixture")
	}
	if err := accounts.PersistState(saved.User.UserID, saved); err != nil {
		t.Fatal(err)
	}
	inProcess, err := accounts.LoadState(saved.User.UserID)
	if err != nil || inProcess.ActiveTeamBattle == nil || inProcess.User.BP != 5 {
		t.Fatal("same repository lost its memory-cached run")
	}
	api.account, err = game.New(inProcess)
	if err != nil {
		t.Fatal(err)
	}
	if response := start(api); response.Code != 200 || api.account.Snapshot(state).User.BP != 5 {
		t.Fatal("same-repository retry was not recovered without a second charge")
	}
	fresh := testfixture.TestAccountRepository(t, accounts.Database())
	afterRestart, err := fresh.LoadState(saved.User.UserID)
	if err != nil || afterRestart.ActiveTeamBattle != nil || afterRestart.User.BP != 5 {
		t.Fatal("fresh repository did not preserve the paid balance and discard the opening")
	}
	api.account, err = game.New(afterRestart)
	if err != nil {
		t.Fatal(err)
	}
	if response := start(api); response.Code != 400 {
		t.Fatalf("fresh repository should reject a new 15-BP entry with only 5 BP: HTTP %d", response.Code)
	}
	if api.account.Snapshot(state).User.BP != 5 {
		t.Fatal("rejected fresh entry changed the paid balance")
	}
	t.Log("same-repository opening recovery=PASS; fresh-repository opening=ABSENT; saved BP=5; new Start HTTP=400; no additional debit")
}
