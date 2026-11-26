using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kairisei.Recovery;

public static class Order05LiveEntryProbe
{
    static readonly List<object> steps=new List<object>();
    static string stage="health";
    static string Post(HttpClient client,Uri uri,string body)
    {
        using(var response=client.PostAsync(uri,new StringContent(body,Encoding.UTF8,"application/json")).GetAwaiter().GetResult())
        {
            steps.Add(new{stage=stage,path=uri.AbsolutePath,status=(int)response.StatusCode});
            if(!response.IsSuccessStatusCode)throw new CnTransportException((int)response.StatusCode,"Request rejected");
            string value=response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if(value.Length>4*1024*1024)throw new InvalidDataException("Response limit");return value;
        }
    }
    static Dictionary<string,long> Balances(CnHomeState home)
    {
        var result=new Dictionary<string,long>();
        foreach(string key in new[]{"gold","coin","coin_free","friend_point"})
        {
            JToken token=home.Wire["user"][key];if(token!=null&&token.Type==JTokenType.Integer)result[key]=(long)token;
        }
        return result;
    }
    public static int Main(string[] args)
    {
        Console.OutputEncoding=Encoding.UTF8;
        bool success=false,sameIdentity=false,characterRetained=false,repeatedBalances=false;
        int createdIdentitiesMaximum=0,createdCharactersMaximum=0,homeRequests=0;string errorType=null;
        try
        {
            ServerEndpoint endpoint=ServerEndpoint.Parse(args[0]);
            string uuid=Guid.NewGuid().ToString("D"),name="ios"+Guid.NewGuid().ToString("N").Substring(0,7);
            string loginBody=CnProtocol.CreateLogin(uuid,CnProtocol.UnityVersion);
            using(var handler=new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false})
            using(var client=new HttpClient(handler))
            {
                client.Timeout=TimeSpan.FromSeconds(30);
                string health=client.GetStringAsync(endpoint.Route("/healthz")).GetAwaiter().GetResult();CnProtocol.ParseHealth(health);
                var flow=new CnEntryFlow();flow.BeginLogin();stage="login";createdIdentitiesMaximum=1;
                flow.ReceiveLogin(Post(client,endpoint.Route("/local/client/login"),loginBody),endpoint);long identity=flow.Login.UserId;
                flow.ResourcesReady();stage="connect_fresh";
                flow.ReceiveConnect(Post(client,flow.Login.Api.Route("/Connect"),CnProtocol.CreateSessionRequest(flow.Login.SessionKey,new JObject())));
                if(flow.CharacterState.UserCreated)throw new InvalidDataException("Not a fresh dedicated identity");
                string create=flow.BeginCreate(name,1);stage="create";createdCharactersMaximum=1;
                flow.ReceiveCreated(Post(client,flow.Login.Api.Route("/UserCreate"),create));
                stage="connect_created";flow.ReceiveConnect(Post(client,flow.Login.Api.Route("/Connect"),CnProtocol.CreateSessionRequest(flow.Login.SessionKey,new JObject())));
                if(!flow.CharacterState.UserCreated)throw new InvalidDataException("Created state missing");
                flow.BeginHome(false);stage="first_home";homeRequests++;
                flow.ReceiveHome(Post(client,flow.Login.Api.Route("/HomeShow"),CnProtocol.CreateSessionRequest(flow.Login.SessionKey,null)));
                var before=Balances(flow.Home);int firstNoticeLength=flow.Home.BonusNotice.Length;
                flow.BeginHome(false);stage="repeat_home";homeRequests++;
                flow.ReceiveHome(Post(client,flow.Login.Api.Route("/HomeShow"),CnProtocol.CreateSessionRequest(flow.Login.SessionKey,null)));
                repeatedBalances=JObject.FromObject(before).ToString(Formatting.None)==JObject.FromObject(Balances(flow.Home)).ToString(Formatting.None);
                if(!repeatedBalances)throw new InvalidDataException("Repeated home direct balances changed");
                stage="repeat_home_observation";steps.Add(new{stage=stage,observedBalanceFields=new List<string>(before.Keys),sameDirectBalances=true,firstNoticePresent=firstNoticeLength>0,repeatNoticePresent=flow.Home.BonusNotice.Length>0});
                flow.BeginLogin();stage="relogin";
                flow.ReceiveLogin(Post(client,endpoint.Route("/local/client/login"),loginBody),endpoint);sameIdentity=flow.Login.UserId==identity;
                if(!sameIdentity)throw new InvalidDataException("Relogin changed identity");
                flow.ResourcesReady();stage="connect_relogin";
                flow.ReceiveConnect(Post(client,flow.Login.Api.Route("/Connect"),CnProtocol.CreateSessionRequest(flow.Login.SessionKey,new JObject())));
                characterRetained=flow.CharacterState.UserCreated;
                if(!characterRetained)throw new InvalidDataException("Relogin lost character");
                flow.BeginHome(false);stage="relogin_home";homeRequests++;
                flow.ReceiveHome(Post(client,flow.Login.Api.Route("/HomeShow"),CnProtocol.CreateSessionRequest(flow.Login.SessionKey,null)));
                if(JObject.FromObject(before).ToString(Formatting.None)!=JObject.FromObject(Balances(flow.Home)).ToString(Formatting.None))throw new InvalidDataException("Relogin home balances changed");
                if(flow.Home.Name!=name)throw new InvalidDataException("Home name mismatch");
                success=true;stage="complete";
            }
        }
        catch(Exception error){errorType=error.GetType().Name;}
        Console.WriteLine(JsonConvert.SerializeObject(new{checkedAt=DateTime.UtcNow.ToString("o"),success=success,stage=stage,errorType=errorType,sameIdentity=sameIdentity,characterRetained=characterRetained,repeatedDirectBalancesMatched=repeatedBalances,
          dedicatedIdentitiesMaximum=createdIdentitiesMaximum,dedicatedCharactersMaximum=createdCharactersMaximum,homeRequests=homeRequests,steps=steps,
          battleAdmissions=0,settlements=0,privateIdentitiesUsed=false,credentialsRecorded=false,
          scope="Actual CnEntryFlow/CnProtocol via Windows HttpClient against the authorized isolated service. One fresh guest/character maximum, two logins using the same in-memory UUID, at most three HomeShow calls with configured reward/persistence side effects. No existing account, battle entry, currency spending or artificial progress/reward. Content-ready is supplied for protocol-only execution: no Unity scene, content loader, resource delivery, mask/GPU, iOS/ARM64 or device acceptance."},Formatting.Indented));
        return success?0:1;
    }
}
