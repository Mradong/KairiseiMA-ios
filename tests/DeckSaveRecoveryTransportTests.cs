using System;
using System.Net.Http;
using System.Text;
using Kairisei.Recovery;
using Newtonsoft.Json.Linq;

class DeckSaveRecoveryTransportTests
{
    const string Session = "recovery-session-0123456789";
    static string Post(HttpClient client, string origin, string route, string body)
    {
        using (HttpResponseMessage response = client.PostAsync(origin + route, new StringContent(body, Encoding.UTF8, "application/json")).GetAwaiter().GetResult())
        { response.EnsureSuccessStatusCode(); return response.Content.ReadAsStringAsync().GetAwaiter().GetResult(); }
    }
    static void Main(string[] args)
    {
        try
        {
            using (var handler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                foreach (string scenario in new[] { "committed", "not_committed", "conflict" })
                {
                    client.GetStringAsync(args[0] + "/reset/" + scenario).GetAwaiter().GetResult();
                    string read = CnProtocol.CreateSessionRequest(Session, new JObject { { "0", 1 } });
                    CnCardCollection before = CnProtocol.ParseCardShow(Post(client, args[0], "/CardShow2", read));
                    long[] proposed = { 2, 1 }; var recovery = new CnDeckSaveRecovery(before.Decks[0], proposed);
                    string body = CnProtocol.CreateDeckSetRequest(Session, before.Decks[0], proposed);
                    bool failed = false;
                    try { CnProtocol.ParseEnvelope(Post(client, args[0], "/CardDeckSet", body)); }
                    catch (Exception) { failed = true; }
                    if (!failed) throw new Exception("expected interrupted response: " + scenario);
                    CnCardCollection after = CnProtocol.ParseCardShow(Post(client, args[0], "/CardShow2", read));
                    int index; long[] draft; CnDeckRecoveryResult actual = recovery.Reconcile(after, out index, out draft);
                    CnDeckRecoveryResult expected = scenario == "committed" ? CnDeckRecoveryResult.Saved : scenario == "not_committed" ? CnDeckRecoveryResult.RestoreDraft : CnDeckRecoveryResult.Conflict;
                    if (actual != expected || (actual == CnDeckRecoveryResult.RestoreDraft && (index != 0 || draft == null || draft[0] != 2 || draft[1] != 1)) || (actual != CnDeckRecoveryResult.RestoreDraft && draft != null))
                        throw new Exception("incorrect reconciliation: " + scenario);
                    Console.WriteLine(scenario + ": " + actual);
                }
            }
        }
        catch (Exception e) { Console.Error.WriteLine(e.GetType().Name + ": " + e.Message); Environment.ExitCode = 1; }
    }
}
