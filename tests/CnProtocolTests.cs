using System;
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
            Console.WriteLine("PASS " + checks + " protocol checks");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.ToString()); return 1; }
    }
}
