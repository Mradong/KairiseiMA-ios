package httpapi

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"kairisei.local/server/internal/accountstore"
	"kairisei.local/server/internal/game"
	"kairisei.local/server/internal/gamestate"
	"kairisei.local/server/internal/testfixture"
)

func TestOrder03DurableSoloOpening(t *testing.T) {
	for _, kind := range []string{"primary", "named"} {
		t.Run(kind, func(t *testing.T) {
			accounts := testfixture.NewFriendCapacityTestAccounts(t)
			userID := accountstore.PrimaryUserID
			if kind == "named" {
				_ = testfixture.CreateNamedFriendCapacityAccount(t, accounts, 0x03cf)
				userID = testfixture.CreateNamedFriendCapacityAccount(t, accounts, 0x03d0)
				if userID <= accountstore.PrimaryUserID {
					t.Fatal("named fixture did not allocate a separate account")
				}
			}
			snapshotTable, snapshotKey, snapshotID := "cn_save_snapshot", "singleton", 1
			if kind == "named" {
				snapshotTable, snapshotKey, snapshotID = "cn_account_snapshot", "user_id", userID
			}
			var initial gamestate.State
			account := testAccount(t, func(state *gamestate.State) {
				state.User.UserID = userID
				state.User.Name = "本地持久开场夹具"
				state.User.BP = 20
				helper := &state.PlayerProgressionPolicy.Friends.HelperReward
				helper.OtherPerPartner, helper.FriendPerPartner = 5, 10
				state.TeamBattleSolo = json.RawMessage(`{"9":[],"10":[{"0":300101,"9":0,"10":[{"0":30010102,"5":15,"10":0,"16":0}]}],"11":[],"12":[]}`)
				initial = *state
			})
			state := account.Snapshot(initial)
			state.TeamBattleReplays = []gamestate.TeamBattleReplay{{BossID: 30010102, EnemyPartyID: 1, CostInitial: 3, HoldMax: 6}}
			state.TeamBattleRewards = []gamestate.TeamBattleRewardProfile{{BossID: 30010102, ResultRewards: []gamestate.Reward{{Type: 4, Num: 1, CardSkillLevels: []int16{}}}}}
			accounts.Database().SetCatalog(state)
			api := &API{initialState: state, account: account}
			payload := fmt.Sprintf(`{"bossid":30010102,"deck_arthur_type":3,"deck_arthur_type_idx":0,"partner_deck_selects":[{"userid":%d,"arthur_type":1,"deck_idx":0},{"userid":%d,"arthur_type":2,"deck_idx":0},{"userid":%d,"arthur_type":4,"deck_idx":0}]}`, userID, userID, userID)
			start := func(api *API) *httptest.ResponseRecorder {
				response := httptest.NewRecorder()
				api.teamBattleSoloStart(response, httptest.NewRequest("POST", "/TeamBattleSoloStart", strings.NewReader(payload)))
				return response
			}
			first := start(api)
			if first.Code != 200 {
				t.Fatalf("first entry HTTP %d", first.Code)
			}
			saved := api.account.Snapshot(state)
			if saved.User.BP != 5 || saved.ActiveTeamBattle == nil {
				t.Fatal("first entry must consume exactly 15 BP and retain the opening")
			}
			if err := accounts.PersistState(userID, saved); err != nil {
				t.Fatal(err)
			}
			fresh := testfixture.TestAccountRepository(t, accounts.Database())
			restored, err := fresh.LoadState(userID)
			if err != nil || restored.ActiveTeamBattle == nil || restored.User.BP != 5 {
				t.Fatal("fresh SQLite repository did not restore opening and paid balance")
			}
			before, _ := json.Marshal(saved.ActiveTeamBattle)
			after, _ := json.Marshal(restored.ActiveTeamBattle)
			if string(before) != string(after) {
				t.Fatal("durable opening changed")
			}
			configuration, err := game.NewContentConfiguration(2, state)
			if err != nil {
				t.Fatal(err)
			}
			restored = game.ApplyContentState(restored, configuration)
			api.account, err = game.New(restored)
			if err != nil {
				t.Fatal(err)
			}
			for retry := 0; retry < 2; retry++ {
				response := start(api)
				if response.Code != 200 || response.Body.String() != first.Body.String() || api.account.Snapshot(state).User.BP != 5 {
					t.Fatal("restored HTTP Start changed the frozen payload or debited again")
				}
			}
			// A failed atomic metadata write must retain both the paid balance and
			// the previous durable/cache opening; no live account is used.
			database, err := accounts.Database().Open()
			if err != nil {
				t.Fatal(err)
			}
			if _, err = database.Exec(fmt.Sprintf(`CREATE TRIGGER order03_fail_metadata BEFORE UPDATE ON %s BEGIN SELECT RAISE(ABORT,'synthetic failure'); END`, snapshotTable)); err != nil {
				t.Fatal(err)
			}
			if _, err = database.Exec(fmt.Sprintf(`CREATE TRIGGER order03_fail_insert BEFORE INSERT ON %s BEGIN SELECT RAISE(ABORT,'synthetic failure'); END`, snapshotTable)); err != nil {
				t.Fatal(err)
			}
			changed := saved
			changed.User.BP = 4
			var changedOpening gamestate.TeamBattleActiveState
			if err := json.Unmarshal(before, &changedOpening); err != nil {
				t.Fatal(err)
			}
			changedOpening.Seed++
			changed.ActiveTeamBattle = &changedOpening
			if err := fresh.PersistState(userID, changed); err == nil {
				t.Fatal("synthetic metadata failure was not returned")
			}
			if _, err := fresh.DiscardUnsettledBattlesDurably(); err == nil {
				t.Fatal("durable interruption swallowed a failed snapshot write")
			}
			still, err := fresh.LoadState(userID)
			if err != nil || still.User.BP != 5 || still.ActiveTeamBattle == nil {
				t.Fatal("failed write partially changed the durable run or balance")
			}
			stillOpening, _ := json.Marshal(still.ActiveTeamBattle)
			if string(stillOpening) != string(before) {
				t.Fatal("failed write replaced the prior frozen opening")
			}
			if _, err = database.Exec(`DROP TRIGGER order03_fail_metadata`); err != nil {
				t.Fatal(err)
			}
			if _, err = database.Exec(`DROP TRIGGER order03_fail_insert`); err != nil {
				t.Fatal(err)
			}
			api.account.AbandonSoloBattle()
			if err := fresh.PersistState(userID, api.account.Snapshot(state)); err != nil {
				t.Fatal(err)
			}
			abandoned, err := testfixture.TestAccountRepository(t, accounts.Database()).LoadState(userID)
			if err != nil || abandoned.ActiveTeamBattle != nil || abandoned.User.BP != 5 {
				t.Fatal("abandoned opening was recovered or its cost refunded")
			}
			// Restore the paid test run and expire only its metadata retention.
			if err := fresh.PersistState(userID, saved); err != nil {
				t.Fatal(err)
			}
			metadata, err := accountstore.EncodeAccountMetadata(saved)
			if err != nil {
				t.Fatal(err)
			}
			var fields map[string]json.RawMessage
			if err := json.Unmarshal(metadata, &fields); err != nil {
				t.Fatal(err)
			}
			fields["active_solo_battle_expires_unix"] = json.RawMessage(fmt.Sprint(time.Now().Unix() - 1))
			metadata, _ = json.Marshal(fields)
			digest := sha256.Sum256(metadata)
			if _, err := database.Exec(fmt.Sprintf(`UPDATE %s SET payload_json=?, payload_sha256=? WHERE %s=?`, snapshotTable, snapshotKey), metadata, hex.EncodeToString(digest[:]), snapshotID); err != nil {
				t.Fatal(err)
			}
			expired, err := testfixture.TestAccountRepository(t, accounts.Database()).LoadState(userID)
			if err != nil || expired.ActiveTeamBattle != nil || expired.User.BP != 5 {
				t.Fatal("expired opening was retained or affected its paid balance")
			}
			// Removing the two new optional fields represents the previous schema.
			delete(fields, "active_solo_battle")
			delete(fields, "active_solo_battle_expires_unix")
			metadata, _ = json.Marshal(fields)
			digest = sha256.Sum256(metadata)
			if _, err := database.Exec(fmt.Sprintf(`UPDATE %s SET payload_json=?, payload_sha256=? WHERE %s=?`, snapshotTable, snapshotKey), metadata, hex.EncodeToString(digest[:]), snapshotID); err != nil {
				t.Fatal(err)
			}
			legacy, err := testfixture.TestAccountRepository(t, accounts.Database()).LoadState(userID)
			if err != nil || legacy.ActiveTeamBattle != nil || legacy.User.BP != 5 {
				t.Fatal("previous metadata contract was not readable")
			}
			if err := fresh.PersistState(userID, saved); err != nil {
				t.Fatal(err)
			}
			interrupter := testfixture.TestAccountRepository(t, accounts.Database())
			count, err := interrupter.DiscardUnsettledBattlesDurably()
			if err != nil || count != 1 {
				t.Fatal("durable-only opening was not counted and interrupted", err, count)
			}
			interrupted, err := testfixture.TestAccountRepository(t, accounts.Database()).LoadState(userID)
			if err != nil || interrupted.ActiveTeamBattle != nil || interrupted.User.BP != 5 || interrupted.User.Gold != saved.User.Gold {
				t.Fatal("interrupted opening came back or changed balance")
			}
			t.Log("fresh repository: identical opening and HTTP retry; BP=5; failure rollback; abandon; expiry; previous schema; durable interruption")
		})
	}
}
