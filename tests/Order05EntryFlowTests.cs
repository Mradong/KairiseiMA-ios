using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Kairisei.Recovery;

public static class Order05EntryFlowTests
{
    private static int checks;
    private const string Session = "order05-fixture-session-0123456789";
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (InvalidOperationException) { checks++; return; } catch (FormatException) { checks++; return; } catch (ArgumentException) { checks++; return; }
        throw new Exception("accepted " + name);
    }
    private static string Envelope(JObject payload) { return "{\"res_code\":0}\n" + payload.ToString(Newtonsoft.Json.Formatting.None) + "\n{}"; }
    private static string Connect(bool created) { return Envelope(new JObject { { "is_user", created ? 1 : 0 }, { "tutorial_flag", 0 } }); }
    private static string Home(long id)
    {
        return Envelope(new JObject { { "server_time", 100 }, { "user", new JObject { { "userid", id }, { "name", "fixture" }, { "lv", 2 }, { "active_arthur_type", 1 }, { "ap", 10 }, { "ap_max", 20 }, { "bp", 5 }, { "bp_max", 20 }, { "gold", 500 } } } });
    }
    private static void Login(CnEntryFlow flow, ServerEndpoint endpoint)
    {
        flow.BeginLogin();
        flow.ReceiveLogin(new JObject { { "res_code", 0 }, { "userid", 7 }, { "sess_key", Session }, { "api_url", endpoint.ToString() }, { "version_url", endpoint.Route("/local/content/iOS/6000.3.26f1/fixture/").ToString() } }.ToString(), endpoint);
    }
    public static int Main(string[] args)
    {
        try
        {
            var endpoint = ServerEndpoint.Parse("http://127.0.0.1:27040");
            var flow = new CnEntryFlow();
            flow.BeginHealth(); Login(flow, endpoint);
            Check(flow.Step == CnEntryStep.Content, "login requires resources before Connect");
            var sameLogin = flow.Login;
            flow.Fail(new IOException());
            Check(flow.PrepareRetry() == CnEntryStep.Content && flow.Login == sameLogin, "resource retry retains same login");
            flow.ResourcesReady(); flow.ReceiveConnect(Connect(false));
            Check(flow.Step == CnEntryStep.Character, "fresh account chooses character creation");
            Reject(delegate { flow.BeginCreate("", 1); }, "invalid name does not leave character form");
            Check(flow.Step == CnEntryStep.Character && !flow.CreationMayHaveCommitted, "no creation uncertainty for validation rejection");
            flow.BeginCreate("fixture", 1);
            Reject(delegate { flow.BeginCreate("fixture", 1); }, "duplicate create cannot be issued while pending");
            flow.Fail(new CnTransportException(0, "truncated"));
            Check(flow.CreationMayHaveCommitted && flow.RetryStep == CnEntryStep.Connect, "ambiguous create queries Connect instead of recreating");
            flow.PrepareRetry(); flow.ReceiveConnect(Connect(true));
            Check(flow.Step == CnEntryStep.HomePermission && !flow.CreationMayHaveCommitted, "committed create recovered by authoritative Connect");
            Reject(delegate { flow.BeginCreate("again", 1); }, "existing character cannot be recreated");
            Reject(delegate { flow.BeginHome(true); }, "pending battle blocks home");
            Check(flow.Step == CnEntryStep.PendingBattle, "pending battle remains explicitly blocked");
            flow.BeginHome(false); flow.ReceiveHome(Home(7));
            var previous = flow.Home;
            flow.BeginHome(false);
            Reject(delegate { flow.BeginHome(false); }, "duplicate home blocked while pending");
            Reject(delegate { flow.ReceiveHome(Home(8)); }, "wrong home identity");
            flow.Fail(new FormatException());
            Check(flow.Home == previous && flow.HomeMayHaveCommitted && flow.RetryStep == CnEntryStep.HomePermission, "malformed refresh retains last valid home and uncertainty");
            flow.PrepareRetry(); flow.BeginHome(false); flow.ReceiveHome(Home(7));
            Check(flow.Step == CnEntryStep.HomeReady && !flow.HomeMayHaveCommitted, "home retry accepts fresh authoritative state");
            flow.BeginConnect(); flow.Fail(new CnProtocolException(-3208, "expired"));
            Check(flow.Login == null && flow.PrepareRetry() == CnEntryStep.Login, "HTTP-200 expired session requires login");
            Login(flow, endpoint); flow.ResourcesReady(); flow.ReceiveConnect(Connect(false)); flow.BeginCreate("fixture", 1);
            flow.Fail(new CnTransportException(401, "expired"));
            Check(flow.CreationMayHaveCommitted && flow.RetryStep == CnEntryStep.Login, "expired create retains uncertainty while renewing session");
            flow.PrepareRetry(); Login(flow, endpoint); flow.ResourcesReady(); flow.ReceiveConnect(Connect(true));
            Check(flow.Step == CnEntryStep.HomePermission && flow.Login.UserId == 7, "renewed identity rechecks existing character");

            string directory = Path.GetFullPath(args[0]);
            var journal = new CnEntryJournal(directory, endpoint, 7);
            Check(!journal.BeginHome(), "first home marker records before sending");
            var reopened = new CnEntryJournal(directory, endpoint, 7);
            Check(reopened.HomeUnconfirmed && reopened.BeginHome(), "home uncertainty survives a new journal instance");
            journal.BeginCreate();
            Check(new CnEntryJournal(directory, endpoint, 7).CreationUnconfirmed, "creation uncertainty persisted");
            foreach (string file in Directory.GetFiles(Path.Combine(directory, "RecoveryEntry")))
                Check(File.ReadAllText(file) == "pending\n", "journal contains no identity, name, session or response");
            Check(!new CnEntryJournal(directory, endpoint, 8).HomeUnconfirmed, "journal cannot cross identities");
            Check(!new CnEntryJournal(directory, ServerEndpoint.Parse("http://127.0.0.1:27041"), 7).HomeUnconfirmed, "journal cannot cross services");
            File.WriteAllText(journal.PendingBattlePath, "fixture battle owner\n");
            Reject(delegate { journal.BeginHome(); }, "persistent pending battle prevents home marking/request");
            Check(File.ReadAllText(journal.PendingBattlePath) == "fixture battle owner\n", "home guard never deletes pending battle");
            File.Delete(journal.PendingBattlePath);
            journal.ConfirmHome(); journal.ConfirmCharacterState();
            Check(!reopened.HomeUnconfirmed && !reopened.CreationUnconfirmed, "only authoritative confirmations clear markers");
            Console.WriteLine("PASS " + checks + " entry-flow and journal checks; synthetic identity only");
            return 0;
        }
        catch (Exception error) { Console.WriteLine("FAIL " + error.GetType().Name + ": " + error.Message); return 1; }
    }
}
