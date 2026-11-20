using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using Kairisei.Recovery;

public static class CnProtocolTests
{
    private static int checks;
    private const string Session = "recovery-session-0123456789";

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
    }

    private static void Reject(Action action, string name)
    {
        try { action(); }
        catch (FormatException) { checks++; return; }
        catch (ArgumentException) { checks++; return; }
        throw new Exception("FAIL: accepted " + name);
    }

    private static JObject LoginBody(string origin)
    {
        return new JObject { { "res_code", 0 }, { "sess_key", Session }, { "userid", 1337 },
            { "api_url", origin + "/" }, { "version_url", origin + "/local/content/iOS/6000.3.26f1/current/" } };
    }

    private static void UnitChecks()
    {
        Check(ServerEndpoint.Parse("game.example.com").BaseUri.Port == 26040, "default game port");
        Check(ServerEndpoint.Parse("game.example.com:80").BaseUri.Port == 80, "explicit port 80 preserved");
        Check(ServerEndpoint.Parse("http://game.example.com").BaseUri.Port == 80, "explicit HTTP default");
        Check(ServerEndpoint.Parse("https://game.example.com").BaseUri.Port == 443, "HTTPS default");
        Check(ServerEndpoint.Parse("[::1]").BaseUri.Port == 26040, "IPv6 game port");
        Check(ServerEndpoint.Parse("[::1]:26042").BaseUri.Port == 26042, "IPv6 explicit port");
        Check(ServerEndpoint.Parse("  game.example.com:26040/  ").BaseUri.Port == 26040, "trim surrounding space");
        foreach (string bad in new[] { "", "::1", "file:///tmp/game", "http://user:password@game.example.com/", "game.example.com/path", "game.example.com?x=1", "game.example.com#x", "game.example.com:0", "game.example.com:65536", "game .example.com", "game\\example.com" })
            Reject(delegate { ServerEndpoint.Parse(bad); }, "invalid endpoint");
        ServerEndpoint server = ServerEndpoint.Parse("http://127.0.0.1:26040");
        Check(server.Route("/local/client/login").AbsolutePath == "/local/client/login", "login path");
        Reject(delegate { server.Route("/../other"); }, "route traversal");
        Reject(delegate { server.Route("https://other.example/"); }, "route origin override");
        Reject(delegate { server.Route("/Connect?token=x"); }, "route query injection");
        Reject(delegate { server.Route("/Connect\n"); }, "route newline injection");

        JObject request = JObject.Parse(CnProtocol.CreateLogin("recovery-fixture-uuid", CnProtocol.UnityVersion));
        Check((string)request["resource_platform"] == "iOS", "never masquerade as Android or Windows");
        Check((string)request["clver"] == "2.0.4", "current server version gate");
        Check((string)request["unity_version"] == "6000.3.26f1", "exact Unity resource version");
        Check(request.Count == 4, "expected login fields only");
        Check(CnProtocol.CreateSessionRequest(Session, new JObject()) == Session + "{}", "session framing without separator");
        Check(CnProtocol.CreateSessionRequest(Session, null) == Session, "session-only framing");
        Check(CnProtocol.CreateUserRequest(Session, "测试骑士", 3) == Session + "{\"name\":\"测试骑士\",\"arthur_type\":3}", "character creation payload");
        Reject(delegate { CnProtocol.CreateUserRequest(Session, "bad name", 1); }, "name with space");
        Reject(delegate { CnProtocol.CreateUserRequest(Session, " ", 1); }, "empty character name");
        Reject(delegate { CnProtocol.CreateUserRequest(Session, "name", 5); }, "invalid starting role");
        string createdResponse = "{\"res_code\":0}\n{\"user\":{\"userid\":1337,\"name\":\"测试骑士\",\"active_arthur_type\":3},\"push_option\":{}}\n{}";
        CnCreatedUser createdUser = CnProtocol.ParseCreatedUser(createdResponse, 1337);
        Check(createdUser.Name == "测试骑士" && createdUser.ArthurType == 3, "created character fields");
        Reject(delegate { CnProtocol.ParseCreatedUser(createdResponse, 1338); }, "wrong created user");
        Reject(delegate { CnProtocol.ParseCreatedUser("{\"res_code\":0}\n{\"user\":{}}", 1337); }, "missing created user fields");
        string homeResponse = "{\"res_code\":0}\n{\"user\":{\"userid\":1337,\"name\":\"测试骑士\",\"lv\":4,\"active_arthur_type\":3,\"ap\":15,\"ap_max\":20,\"bp\":2,\"bp_max\":3,\"gold\":123456},\"server_time\":100}\n{}";
        CnHomeState home = CnProtocol.ParseHome(homeResponse, 1337);
        Check(home.Name == "测试骑士" && home.Level == 4 && home.Ap == 15 && home.ApMax == 20 && home.Bp == 2 && home.BpMax == 3 && home.Gold == 123456 && home.ServerTime == 100, "home user fields");
        Check(home.BackgroundId == null && home.NaviType == null, "absent home visual selection remains unknown");
        string visualResponse = homeResponse.Replace("\"active_arthur_type\":3", "\"active_arthur_type\":3,\"navi_type\":7").Replace("\"server_time\":100", "\"background\":123,\"server_time\":100");
        CnHomeState visualHome = CnProtocol.ParseHome(visualResponse, 1337);
        Check(visualHome.BackgroundId == 123 && visualHome.NaviType == 7 && (long)visualHome.Wire["background"] == 123, "home visual fields retained");
        Reject(delegate { CnProtocol.ParseHome(visualResponse.Replace("\"background\":123", "\"background\":-1"), 1337); }, "negative background");
        Reject(delegate { CnProtocol.ParseHome(visualResponse.Replace("\"navi_type\":7", "\"navi_type\":\"7\""), 1337); }, "invalid navigator");
        string bonusResponse = homeResponse.Replace("\"server_time\":100", "\"server_time\":100,\"login_bonus_daily\":[{\"day_num\":2,\"reward\":{\"reward\":{\"type\":10,\"reward_typeid\":0,\"num\":20}}}],\"login_bonus_beginner\":[{\"day_num\":2,\"reward\":[{\"reward\":{\"type\":1,\"reward_typeid\":111,\"num\":1}},{\"reward\":{\"type\":2,\"reward_typeid\":222,\"num\":3}}]}],\"login_bonus_total\":[{\"day_num\":7,\"reward\":{\"reward\":{\"type\":3,\"reward_typeid\":333,\"num\":4}}},{\"day_num\":14,\"reward\":{\"reward\":{\"type\":9,\"reward_typeid\":999,\"num\":1}}}]");
        string notices = CnProtocol.ParseHome(bonusResponse, 1337).BonusNotice;
        Check(notices.Contains("编号 222") && notices.Contains("数量 20") && notices.Contains("编号 333") && !notices.Contains("编号 111") && !notices.Contains("编号 999"), "only earned login rewards, not previews");
        Reject(delegate { CnProtocol.ParseHome(bonusResponse.Replace("\"day_num\":2,\"reward\":[", "\"day_num\":9,\"reward\":["), 1337); }, "invalid beginner reward day");
        Check(home.BonusNotice == "", "no login reward notice");
        string cardsResponse = "{\"res_code\":0}\n{\"1\":[{\"0\":9223372036,\"1\":123,\"2\":4,\"6\":[1,2]}],\"3\":[{\"0\":1,\"1\":0,\"4\":[9223372036,0],\"5\":[0],\"6\":[0],\"7\":[0],\"8\":\"test deck\",\"9\":1}]}";
        CnCardCollection collection = CnProtocol.ParseCardShow(cardsResponse);
        Check(collection.Cards[9223372036].CardId == 123 && collection.Decks[0].Cards[0] == 9223372036 && (int)collection.Cards[9223372036].Wire["6"][1] == 2 && (int)collection.Decks[0].Wire["9"] == 1, "card/deck identifiers and full wire fields retained");
        Reject(delegate { CnProtocol.ParseCardShow("{\"res_code\":0}\n{\"1\":[],\"3\":[{\"0\":1,\"1\":0,\"4\":[\"1\"],\"5\":[],\"6\":[],\"7\":[],\"8\":\"a\"}]}"); }, "non-integer deck slot");
        Reject(delegate { CnProtocol.ParseCardShow(cardsResponse.Replace("\"3\":[", "\"3\":null,\"unused\":[")); }, "missing deck array");
        string questResponse = "{\"res_code\":0}\n{\"9\":[{\"0\":23,\"4\":\"Boss\",\"10\":[{\"0\":99,\"4\":\"Hard\",\"5\":5,\"10\":1,\"26\":0}]}],\"10\":[],\"11\":[],\"12\":[]}";
        var quest = CnProtocol.ParseSoloQuests(questResponse);
        Check(quest.Count == 1 && quest[0].BossId == 99 && quest[0].BpCost == 5 && quest[0].Category == "普通", "quest identity, category and cost");
        Reject(delegate { CnProtocol.ParseSoloQuests(questResponse.Replace("\"12\":[]", "\"12\":null")); }, "missing quest category");
        string partnerResponse = "{\"res_code\":0}\n{\"arthurs\":[{\"arthur_type\":1,\"partners\":[{\"userid\":12,\"arthur_type\":1,\"name\":\"test\",\"lv\":10,\"rental_idx\":2}]}]}";
        Check(CnProtocol.ParseSoloPartners(partnerResponse)[0].UserId == 12, "partner identity");
        var draft = new CnSoloPartyDraft();
        draft.Add(CnProtocol.ParseSoloPartners(partnerResponse)[0]);
        Check(draft.Count == 1 && (long)draft.RequestPartners()[0]["userid"] == 12 && (int)draft.RequestPartners()[0]["deck_idx"] == 2, "partner selection request retains rental index");
        try { draft.Add(CnProtocol.ParseSoloPartners(partnerResponse)[0]); throw new Exception("duplicate partner accepted"); } catch (InvalidOperationException) { checks++; }
        draft.RemoveLast(); Check(draft.Count == 0, "remove last partner");
        Reject(delegate { CnProtocol.ParseSoloPartners(partnerResponse.Replace("\"rental_idx\":2", "\"rental_idx\":-1")); }, "negative partner deck index");
        Reject(delegate { CnProtocol.ParseSoloPartners(partnerResponse.Replace("\"arthur_type\":1,\"name\"", "\"arthur_type\":2,\"name\"")); }, "partner profession mismatch");
        var partners = new JArray(new JObject { { "userid", 1000001 }, { "arthur_type", 1 }, { "deck_idx", 0 } });
        string soloRequest = CnProtocol.CreateSoloBattleStartRequest(Session, 30010102, 3, 0, partners);
        Check((long)JObject.Parse(soloRequest.Substring(Session.Length))["partner_deck_selects"][0]["userid"] == 1000001, "solo partner selection");
        Reject(delegate { CnProtocol.CreateSoloBattleStartRequest(Session, 0, 3, 0, partners); }, "invalid boss");
        Reject(delegate { CnProtocol.CreateSoloBattleStartRequest(Session, 1, 3, 0, new JArray(new JObject { { "userid", 1 } })); }, "incomplete partner");
        string startResponse = "{\"res_code\":0}\n{\"seed\":-1,\"start_time\":2,\"battles\":[{\"enemy_partyid\":1,\"enemy_type\":0}],\"partner_deck\":[{\"deck\":[42]}]}";
        var started = CnProtocol.ParseSoloBattleStart(startResponse);
        Check(started.Seed == -1 && started.Battles.Count == 1 && (int)started.Wire["partner_deck"][0]["deck"][0] == 42, "start state retains full loadout");
        Reject(delegate { CnProtocol.ParseSoloBattleStart(startResponse.Replace("\"enemy_partyid\":1", "\"enemy_partyid\":0")); }, "invalid enemy party");
        Reject(delegate { CnProtocol.ParseSoloBattleStart(startResponse.Replace("\"battles\":[{\"enemy_partyid\":1,\"enemy_type\":0}]", "\"battles\":[]")); }, "empty battles");
        Reject(delegate { CnProtocol.ParseHome(homeResponse, 1338); }, "home user mismatch");
        Reject(delegate { CnProtocol.ParseHome("{\"res_code\":0}\n{\"user\":{\"userid\":1337,\"name\":\"name\"}}", 1337); }, "incomplete home response");
        Reject(delegate { CnProtocol.CreateSessionRequest("short", new JObject()); }, "short session");
        Reject(delegate { CnProtocol.CreateSessionRequest(Session + "\n", new JObject()); }, "session newline injection");
        Check(CnProtocol.ParseHealth("{\"state\":\"PASS\",\"battle_port\":26041}") == 26041, "health response");
        Reject(delegate { CnProtocol.ParseHealth("{\"state\":\"FAIL\",\"battle_port\":26041}"); }, "unready health");
        Reject(delegate { CnProtocol.ParseHealth("{\"state\":\"PASS\",\"battle_port\":0}"); }, "invalid battle port");

        CnEnvelope connect = CnProtocol.ParseEnvelope("{\"res_code\":0}\n{\"user\":{\"id\":1}}\n");
        Check((int)connect.Payload["user"]["id"] == 1 && connect.Popup == null, "two-segment Connect");
        CnConnectState existing = CnProtocol.ParseConnect("{\"res_code\":0}\n{\"is_user\":1,\"tutorial_flag\":134217727}");
        Check(existing.UserCreated && existing.TutorialFlag == 134217727, "existing character and tutorial state");
        CnConnectState fresh = CnProtocol.ParseConnect("{\"res_code\":0}\n{\"is_user\":0,\"tutorial_flag\":0}");
        Check(!fresh.UserCreated && fresh.TutorialFlag == 0, "new character state");
        Reject(delegate { CnProtocol.ParseConnect("{\"res_code\":0}\n{}"); }, "missing Connect state");
        Reject(delegate { CnProtocol.ParseConnect("{\"res_code\":0}\n{\"is_user\":2,\"tutorial_flag\":0}"); }, "invalid character state");
        Reject(delegate { CnProtocol.ParseConnect("{\"res_code\":0}\n{\"is_user\":1,\"tutorial_flag\":\"0\"}"); }, "invalid tutorial state");
        Reject(delegate { CnProtocol.ParseConnect("{\"res_code\":0}\n[]"); }, "Connect array payload");
        CnEnvelope three = CnProtocol.ParseEnvelope("\uFEFF{\"res_code\":0}\r\n[]\r\n{\"popup\":[]}\r\n");
        Check(three.Popup != null && three.Payload.Type == JTokenType.Array, "three-segment business envelope and CRLF/BOM");
        Reject(delegate { CnProtocol.ParseEnvelope("{\"res_code\":0}"); }, "missing payload");
        Reject(delegate { CnProtocol.ParseEnvelope("{}\n{}"); }, "missing result code");
        Reject(delegate { CnProtocol.ParseEnvelope("{\"res_code\":0}\nnull"); }, "null payload");
        Reject(delegate { CnProtocol.ParseEnvelope("{\"res_code\":0}\n{}\n{}\n{}"); }, "extra protocol segment");
        try { CnProtocol.ParseEnvelope("{\"res_code\":-3208,\"res_str\":\"session expired\"}\n{}\n{\"popup\":[]}"); throw new Exception("accepted expired session"); }
        catch (CnProtocolException error) { Check(error.Code == -3208 && error.Message == "session expired", "session expiry propagated"); }

        CnLogin login = CnProtocol.ParseLogin(LoginBody("http://127.0.0.1:26040").ToString(), server);
        Check(login.UserId == 1337 && login.SessionKey == Session, "login identity");
        JObject badLogin = LoginBody("http://127.0.0.1:26040");
        badLogin["api_url"] = "http://other.example:26040/";
        Reject(delegate { CnProtocol.ParseLogin(badLogin.ToString(), server); }, "session forwarding to another origin");
        badLogin = LoginBody("http://127.0.0.1:26040"); badLogin["version_url"] = "file:///tmp/catalog";
        Reject(delegate { CnProtocol.ParseLogin(badLogin.ToString(), server); }, "non-HTTP content URL");
        badLogin = LoginBody("http://127.0.0.1:26040"); badLogin["sess_key"] = "short";
        Reject(delegate { CnProtocol.ParseLogin(badLogin.ToString(), server); }, "bad returned session");
    }

    private static void NetworkChecks(string origin)
    {
        ServerEndpoint endpoint = ServerEndpoint.Parse(origin);
        using (HttpClientHandler handler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
        using (HttpClient client = new HttpClient(handler))
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            string health = client.GetStringAsync(endpoint.Route("/healthz")).GetAwaiter().GetResult();
            Check(CnProtocol.ParseHealth(health) == 26041, "loopback health transport");
            string request = CnProtocol.CreateLogin("recovery-contract-fixture", CnProtocol.UnityVersion);
            using (HttpResponseMessage response = client.PostAsync(endpoint.Route("/local/client/login"), new StringContent(request, Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
            {
                response.EnsureSuccessStatusCode();
                CnLogin login = CnProtocol.ParseLogin(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), endpoint);
                Check(login.UserId == 1337, "loopback login transport");
                string body = CnProtocol.CreateSessionRequest(login.SessionKey, new JObject());
                using (HttpResponseMessage connect = client.PostAsync(login.Api.Route("/Connect"), new StringContent(body, Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
                {
                    connect.EnsureSuccessStatusCode();
                    CnConnectState state = CnProtocol.ParseConnect(connect.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    Check(!state.UserCreated && state.TutorialFlag == 0, "loopback new character state");
                }
                string create = CnProtocol.CreateUserRequest(login.SessionKey, "测试骑士", 3);
                using (HttpResponseMessage responseCreate = client.PostAsync(login.Api.Route("/UserCreate"), new StringContent(create, Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
                {
                    responseCreate.EnsureSuccessStatusCode();
                    CnCreatedUser user = CnProtocol.ParseCreatedUser(responseCreate.Content.ReadAsStringAsync().GetAwaiter().GetResult(), login.UserId);
                    Check(user.Name == "测试骑士" && user.ArthurType == 3, "loopback character creation");
                }
                using (HttpResponseMessage reconnect = client.PostAsync(login.Api.Route("/Connect"), new StringContent(body, Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
                {
                    reconnect.EnsureSuccessStatusCode();
                    Check(CnProtocol.ParseConnect(reconnect.Content.ReadAsStringAsync().GetAwaiter().GetResult()).UserCreated, "loopback created state after reconnect");
                }
                using (HttpResponseMessage home = client.PostAsync(login.Api.Route("/HomeShow"), new StringContent(CnProtocol.CreateSessionRequest(login.SessionKey, null), Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
                {
                    home.EnsureSuccessStatusCode();
                    CnHomeState state = CnProtocol.ParseHome(home.Content.ReadAsStringAsync().GetAwaiter().GetResult(), login.UserId);
                    Check(state.Name == "测试骑士" && state.Ap == 15 && state.Gold == 123456, "loopback home user state");
                }
                using (HttpResponseMessage cards = client.PostAsync(login.Api.Route("/CardShow2"), new StringContent(CnProtocol.CreateSessionRequest(login.SessionKey, new JObject { { "0", 1 } }), Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
                {
                    cards.EnsureSuccessStatusCode();
                    CnCardCollection state = CnProtocol.ParseCardShow(cards.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    Check(state.Decks.Count > 0 && state.Cards.Count > 0, "loopback cards and decks");
                }
                using (HttpResponseMessage quests = client.PostAsync(login.Api.Route("/TeamBattleSoloShow"), new StringContent(CnProtocol.CreateSessionRequest(login.SessionKey, new JObject { { "0", 3 } }), Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
                {
                    quests.EnsureSuccessStatusCode();
                    Check(CnProtocol.ParseSoloQuests(quests.Content.ReadAsStringAsync().GetAwaiter().GetResult()).Count > 0, "loopback solo quests");
                }
                using (HttpResponseMessage partners = client.PostAsync(login.Api.Route("/TeamBattleSoloPartnerShow"), new StringContent(CnProtocol.CreateSessionRequest(login.SessionKey, new JObject { { "bossid", 30010102 } }), Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
                {
                    partners.EnsureSuccessStatusCode();
                    Check(CnProtocol.ParseSoloPartners(partners.Content.ReadAsStringAsync().GetAwaiter().GetResult()).Count > 0, "loopback solo partners");
                }
            }
        }
    }

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            UnitChecks();
            if (args.Length == 1) NetworkChecks(args[0]);
            if (args.Length == 2)
            {
                NetworkChecks(args[0]);
                string actual = File.ReadAllText(args[1]);
                JObject actualUser = (JObject)CnProtocol.ParseEnvelope(actual).Payload["user"];
                CnHomeState actualHome = CnProtocol.ParseHome(actual, (long)actualUser["userid"]);
                Check(actualHome.Name == "测试骑士" && actualHome.ServerTime > 0, "actual isolated service home response");
                Check(actualHome.BackgroundId == 1 && actualHome.NaviType == 0 && (long)actualHome.Wire["background"] == 1, "isolated service home visual selection");
            }
            string projected = Environment.GetEnvironmentVariable("IOS_BONUS_PROJECTION_FIXTURE");
            if (!String.IsNullOrEmpty(projected))
            {
                string bonus = CnProtocol.ParseHome(File.ReadAllText(projected), 1337).BonusNotice;
                Check(bonus.Contains("编号 20") && bonus.Contains("编号 70") && !bonus.Contains("编号 10") && !bonus.Contains("编号 140"), "isolated actual service reward projections exclude previews");
            }
            string actualCards = Environment.GetEnvironmentVariable("IOS_CARD_TEST_FIXTURE");
            if (!String.IsNullOrEmpty(actualCards))
            {
                CnCardCollection actualCollection = CnProtocol.ParseCardShow(File.ReadAllText(actualCards));
                Check(actualCollection.Cards.Count == 10 && actualCollection.Decks.Count == 4 && actualCollection.Cards.ContainsKey(actualCollection.Decks[0].Cards[0]), "isolated service starter cards and decks");
                string expectedPath = Environment.GetEnvironmentVariable("IOS_DECK_REQUEST_FIXTURE");
                if (!String.IsNullOrEmpty(expectedPath))
                {
                    CnDeck deck = actualCollection.Decks[0];
                    long[] draft = (long[])deck.Cards.Clone();
                    long temp = draft[1]; draft[1] = draft[2]; draft[2] = temp;
                    string body = CnProtocol.CreateDeckSetRequest(Session, deck, draft);
                    Check(JToken.DeepEquals(JObject.Parse(body.Substring(Session.Length)), JObject.Parse(File.ReadAllText(expectedPath))), "deck set request matches actual isolated service reorder");
                    Check(deck.Cards[1] != draft[1], "editing draft leaves server snapshot unchanged");
                    Reject(delegate { CnProtocol.CreateDeckSetRequest(Session, deck, new long[0]); }, "invalid draft slot count");
                }
            }
            string actualQuests = Environment.GetEnvironmentVariable("IOS_QUEST_TEST_FIXTURE");
            if (!String.IsNullOrEmpty(actualQuests))
            {
                var stages = CnProtocol.ParseSoloQuests(File.ReadAllText(actualQuests));
                Check(stages.Count > 0 && stages[0].BossId == 30020102 && stages[0].BpCost == 1, "isolated service tutorial quest");
            }
            string actualPartners = Environment.GetEnvironmentVariable("IOS_PARTNER_TEST_FIXTURE");
            if (!String.IsNullOrEmpty(actualPartners))
            {
                var candidates = CnProtocol.ParseSoloPartners(File.ReadAllText(actualPartners));
                Check(candidates.Count >= 4, "isolated service partner candidates");
                var actualDraft = new CnSoloPartyDraft();
                for (int i = 0; i < 3; i++) actualDraft.Add(candidates[i]);
                string body = CnProtocol.CreateSoloBattleStartRequest(Session, 30010102, 3, 0, actualDraft.RequestPartners());
                JArray selections = (JArray)JObject.Parse(body.Substring(Session.Length))["partner_deck_selects"];
                Check(selections.Count == 3 && (long)selections[0]["userid"] == candidates[0].UserId && (long)selections[2]["deck_idx"] == candidates[2].RentalIndex, "isolated candidates form start request");
                try { actualDraft.Add(candidates[3]); throw new Exception("fourth partner accepted"); } catch (InvalidOperationException) { checks++; }
                actualDraft.Clear(); Check(actualDraft.Count == 0, "clear party for new quest");
            }
            string actualStart = Environment.GetEnvironmentVariable("IOS_SOLO_START_FIXTURE");
            string actualStartRequest = Environment.GetEnvironmentVariable("IOS_SOLO_START_REQUEST_FIXTURE");
            if (!String.IsNullOrEmpty(actualStart) && !String.IsNullOrEmpty(actualStartRequest))
            {
                JObject expected = JObject.Parse(File.ReadAllText(actualStartRequest));
                string requestBody = CnProtocol.CreateSoloBattleStartRequest(Session, (long)expected["bossid"], (int)expected["deck_arthur_type"], (int)expected["deck_arthur_type_idx"], (JArray)expected["partner_deck_selects"]);
                Check(JToken.DeepEquals(expected, JObject.Parse(requestBody.Substring(Session.Length))), "solo request matches isolated service");
                var result = CnProtocol.ParseSoloBattleStart(File.ReadAllText(actualStart));
                Check(result.Battles.Count == 1 && (int)result.Battles[0]["enemy_partyid"] == 1 && ((JArray)result.Wire["partner_deck"]).Count == 3 && (int)result.Wire["hold_max"] == 6, "isolated service battle data retained");
            }
            Console.WriteLine("PASS " + checks + " protocol checks");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.ToString()); return 1; }
    }
}
