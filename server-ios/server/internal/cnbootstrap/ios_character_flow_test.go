package cnbootstrap

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

// Exercises the actual isolated service router with a new disposable guest.
func TestIOSCharacterCreateConnectFlow(t *testing.T) {
	root := t.TempDir()
	if err := os.WriteFile(filepath.Join(root, "requests.jsonl"), nil, 0600); err != nil {
		t.Fatal(err)
	}
	handler := newTestHandler(t, root, filepath.Join(root, "requests.jsonl"))
	primary := handler.(testSessionHandler)
	bonusResponse := httptest.NewRecorder()
	handler.ServeHTTP(bonusResponse, httptest.NewRequest(http.MethodPost, "/HomeShow", strings.NewReader(primary.sessionKey)))
	if bonusResponse.Code != http.StatusOK {
		t.Fatalf("existing fixture home: %d", bonusResponse.Code)
	}
	if target := os.Getenv("IOS_BONUS_TEST_FIXTURE"); target != "" {
		if err := os.WriteFile(target, bonusResponse.Body.Bytes(), 0600); err != nil {
			t.Fatal(err)
		}
	}
	login := httptest.NewRecorder()
	handler.ServeHTTP(login, httptest.NewRequest(http.MethodPost, "/local/client/login", strings.NewReader(`{"uuid":"b2510000-0000-4000-8000-000000000001","clver":"2.0.4","resource_platform":"StandaloneWindows64","unity_version":"6000.3.26f1"}`)))
	var identity struct {
		UserID  int64  `json:"userid"`
		Session string `json:"sess_key"`
		Code    int    `json:"res_code"`
	}
	if login.Code != 200 || json.Unmarshal(login.Body.Bytes(), &identity) != nil || identity.Code != 0 || identity.UserID < 1 || identity.Session == "" {
		t.Fatalf("login: %d %s", login.Code, login.Body.String())
	}
	call := func(route, body string) map[string]json.RawMessage {
		t.Helper()
		response := httptest.NewRecorder()
		handler.ServeHTTP(response, httptest.NewRequest(http.MethodPost, route, strings.NewReader(identity.Session+body)))
		if response.Code != 200 {
			t.Fatalf("%s: %d %s", route, response.Code, response.Body.String())
		}
		lines := strings.Split(response.Body.String(), "\n")
		if len(lines) < 2 {
			t.Fatalf("%s: invalid envelope", route)
		}
		var common struct {
			Code int `json:"res_code"`
		}
		var payload map[string]json.RawMessage
		if json.Unmarshal([]byte(lines[0]), &common) != nil || common.Code != 0 || json.Unmarshal([]byte(lines[1]), &payload) != nil {
			t.Fatalf("%s: %s", route, response.Body.String())
		}
		return payload
	}
	before := call("/Connect", "{}")
	if string(before["is_user"]) != "0" || before["tutorial_flag"] == nil {
		t.Fatalf("new user state: %v", before)
	}
	created := call("/UserCreate", `{"name":"测试骑士","arthur_type":3}`)
	var user struct {
		UserID int64  `json:"userid"`
		Name   string `json:"name"`
		Role   int    `json:"active_arthur_type"`
	}
	if json.Unmarshal(created["user"], &user) != nil || user.UserID != identity.UserID || user.Name != "测试骑士" || user.Role != 3 {
		t.Fatalf("created user: %s", created["user"])
	}
	after := call("/Connect", "{}")
	if string(after["is_user"]) != "1" {
		t.Fatalf("created user state: %v", after)
	}
	again := httptest.NewRecorder()
	handler.ServeHTTP(again, httptest.NewRequest(http.MethodPost, "/UserCreate", strings.NewReader(identity.Session+`{"name":"other","arthur_type":1}`)))
	if again.Code == http.StatusOK {
		t.Fatal("duplicate character creation unexpectedly succeeded")
	}
	loginAgain := httptest.NewRecorder()
	handler.ServeHTTP(loginAgain, httptest.NewRequest(http.MethodPost, "/local/client/login", strings.NewReader(`{"uuid":"b2510000-0000-4000-8000-000000000001","clver":"2.0.4","resource_platform":"StandaloneWindows64","unity_version":"6000.3.26f1"}`)))
	var same struct {
		UserID  int64  `json:"userid"`
		Session string `json:"sess_key"`
	}
	if loginAgain.Code != 200 || json.Unmarshal(loginAgain.Body.Bytes(), &same) != nil || same.UserID != identity.UserID || same.Session == "" {
		t.Fatalf("repeat login: %d %s", loginAgain.Code, loginAgain.Body.String())
	}
	identity.Session = same.Session
	if string(call("/Connect", "{}")["is_user"]) != "1" {
		t.Fatal("created user missing on repeat login")
	}
	home := call("/HomeShow", "")
	cardShow := call("/CardShow2", `{"0":1}`)
	var cards, decks []json.RawMessage
	if json.Unmarshal(cardShow["1"], &cards) != nil || json.Unmarshal(cardShow["3"], &decks) != nil {
		t.Fatalf("card/deck response missing arrays")
	}
	if target := os.Getenv("IOS_CARD_TEST_FIXTURE"); target != "" {
		payload, err := json.Marshal(cardShow)
		if err != nil {
			t.Fatal(err)
		}
		if err := os.WriteFile(target, append([]byte("{\"res_code\":0}\n"), payload...), 0600); err != nil {
			t.Fatal(err)
		}
	}
	var homeUser struct {
		UserID int64  `json:"userid"`
		Name   string `json:"name"`
		Level  int64  `json:"lv"`
		AP     int64  `json:"ap"`
		APMax  int64  `json:"ap_max"`
		BP     int64  `json:"bp"`
		BPMax  int64  `json:"bp_max"`
		Gold   int64  `json:"gold"`
	}
	var timestamp int64
	if json.Unmarshal(home["user"], &homeUser) != nil || homeUser.UserID != identity.UserID || homeUser.Name != "测试骑士" ||
		json.Unmarshal(home["server_time"], &timestamp) != nil || timestamp <= 0 {
		t.Fatalf("home identity or timestamp: %s", home["user"])
	}
	var fields map[string]json.RawMessage
	if json.Unmarshal(home["user"], &fields) != nil {
		t.Fatal("home user format")
	}
	for _, key := range []string{"lv", "ap", "ap_max", "bp", "bp_max", "gold"} {
		var n int64
		if json.Unmarshal(fields[key], &n) != nil {
			t.Fatalf("home field %s is not an integer", key)
		}
	}
	if target := os.Getenv("IOS_HOME_TEST_FIXTURE"); target != "" {
		payload, err := json.Marshal(home)
		if err != nil {
			t.Fatal(err)
		}
		if err := os.WriteFile(target, append([]byte("{\"res_code\":0}\n"), payload...), 0600); err != nil {
			t.Fatal(err)
		}
	}
}
