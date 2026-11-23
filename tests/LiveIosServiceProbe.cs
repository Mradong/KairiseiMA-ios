using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Kairisei.Recovery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class LiveIosServiceProbe
{
    private static readonly List<object> Steps=new List<object>();
    private static readonly List<object> Bundles=new List<object>();
    private static string stage="setup";

    private static byte[] ReadBounded(Stream input,int maximum)
    {
        using(MemoryStream bytes=new MemoryStream())
        {
            byte[] buffer=new byte[65536];int count;
            while((count=input.Read(buffer,0,buffer.Length))!=0)
            {
                if(bytes.Length+count>maximum)throw new InvalidDataException("Response too large");
                bytes.Write(buffer,0,count);
            }
            return bytes.ToArray();
        }
    }

    private static byte[] Request(HttpClient client,Uri url,string body,int maximum)
    {
        using(HttpRequestMessage request=new HttpRequestMessage(body==null?HttpMethod.Get:HttpMethod.Post,url))
        {
            if(body!=null)request.Content=new StringContent(body,Encoding.UTF8,"application/json");
            using(HttpResponseMessage response=client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
                Steps.Add(new { stage=stage,method=body==null?"GET":"POST",path=url.AbsolutePath,status=(int)response.StatusCode });
                response.EnsureSuccessStatusCode();
                using(Stream input=response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())return ReadBounded(input,maximum);
            }
        }
    }

    private static void Download(HttpClient client,Uri url,ContentBundle item,ContentCache cache)
    {
        using(HttpRequestMessage head=new HttpRequestMessage(HttpMethod.Head,url))
        using(HttpResponseMessage response=client.SendAsync(head).GetAwaiter().GetResult())
        {
            Steps.Add(new {stage=stage,method="HEAD",path=url.AbsolutePath,status=(int)response.StatusCode});
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength!=item.bytes)throw new InvalidDataException("Bundle HEAD length differs");
        }
        string temporary=cache.CreateStagingPath();
        try
        {
            using(HttpRequestMessage request=new HttpRequestMessage(HttpMethod.Get,url))
            using(HttpResponseMessage response=client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
                Steps.Add(new {stage=stage,method="GET",path=url.AbsolutePath,status=(int)response.StatusCode});
                response.EnsureSuccessStatusCode();
                using(Stream input=response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                using(Stream output=File.Create(temporary))
                {
                    byte[] buffer=new byte[65536];long total=0;int count;
                    while((count=input.Read(buffer,0,buffer.Length))!=0)
                    {
                        total+=count;if(total>item.bytes)throw new InvalidDataException("Bundle exceeds declared size");
                        output.Write(buffer,0,count);
                    }
                }
            }
            cache.Commit(item,temporary);
        }
        finally { if(File.Exists(temporary))File.Delete(temporary); }
        using(Stream input=File.OpenRead(cache.PathFor(item)))
        {
            byte[] header=new byte[8];
            if(input.Read(header,0,header.Length)!=header.Length || Encoding.ASCII.GetString(header)!="UnityFS\0")throw new InvalidDataException("Not a UnityFS bundle");
        }
        Bundles.Add(new {name=item.name,bytes=item.bytes,sha256=item.sha256,crc32=item.crc32,verified=true});
    }

    public static void Main(string[] args)
    {
        bool success=false;string errorType=null;int? protocolCode=null;
        object loginInfo=null,contentInfo=null,connectInfo=null,characterInfo=null,questInfo=null;
        try
        {
            ServerEndpoint endpoint=ServerEndpoint.Parse(args[0]);
            // Explicitly limit this state-changing guest probe to the user-authorized
            // isolated environment. Credentials and UUID never leave process memory.
            if(endpoint.ToString()!="http://47.116.51.61:27040/")throw new InvalidOperationException("Probe target is not the authorized isolated service");
            string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
            string uuid=Guid.NewGuid().ToString("N");
            using(HttpClientHandler handler=new HttpClientHandler { UseProxy=false,AllowAutoRedirect=false,UseCookies=false })
            using(HttpClient client=new HttpClient(handler) { Timeout=TimeSpan.FromSeconds(20) })
            {
                stage="health";
                int battlePort=CnProtocol.ParseHealth(Encoding.UTF8.GetString(Request(client,endpoint.Route("/healthz"),null,32768)));
                stage="guest_login";
                CnLogin login=CnProtocol.ParseLogin(Encoding.UTF8.GetString(Request(client,endpoint.Route("/local/client/login"),CnProtocol.CreateLogin(uuid,CnProtocol.UnityVersion),262144)),endpoint);
                loginInfo=new {success=true,userIdRedacted=true,sessionRedacted=true,uuidRedacted=true,apiOrigin=login.Api.ToString(),versionUrl=login.VersionUrl.AbsoluteUri,battlePort=battlePort};
                stage="manifest";
                byte[] raw=Request(client,new Uri(login.VersionUrl,"client-manifest.json"),null,1024*1024);
                ContentManifest manifest=ContentManifest.Parse(Encoding.UTF8.GetString(raw),CnProtocol.UnityVersion);
                string revision=login.VersionUrl.Segments[login.VersionUrl.Segments.Length-1].Trim('/');
                if(revision!=manifest.contentId)throw new InvalidDataException("Login resource revision differs from manifest");
                File.WriteAllBytes(Path.Combine(output,"client-manifest.json"),raw);
                string cachePath=Path.Combine(output,"cache");ContentCache cache=new ContentCache(cachePath);
                int installed=0;
                foreach(ContentBundle item in manifest.DependencyOrder())
                {
                    stage="bundle_download";
                    Download(client,new Uri(login.VersionUrl,"patch/"+item.name+".v"+item.crc32),item,cache);installed++;
                }
                stage="cache_reopen";
                ContentCache reopened=new ContentCache(cachePath);int reused=0;
                foreach(ContentBundle item in manifest.bundles) { if(!reopened.Contains(item))throw new InvalidDataException("Verified bundle was not reusable");reused++; }
                contentInfo=new {contentId=manifest.contentId,platform=manifest.platform,unityVersion=manifest.unityVersion,installed=installed,reopenedCacheReused=reused,bundles=Bundles};
                stage="connect";
                CnConnectState first=CnProtocol.ParseConnect(Encoding.UTF8.GetString(Request(client,login.Api.Route("/Connect"),CnProtocol.CreateSessionRequest(login.SessionKey,new JObject()),1024*1024)));
                stage="repeat_guest_login";
                CnLogin repeat=CnProtocol.ParseLogin(Encoding.UTF8.GetString(Request(client,endpoint.Route("/local/client/login"),CnProtocol.CreateLogin(uuid,CnProtocol.UnityVersion),262144)),endpoint);
                if(repeat.UserId!=login.UserId || repeat.VersionUrl!=login.VersionUrl)throw new InvalidDataException("Repeat guest identity/resource revision changed");
                stage="repeat_connect";
                CnConnectState second=CnProtocol.ParseConnect(Encoding.UTF8.GetString(Request(client,repeat.Api.Route("/Connect"),CnProtocol.CreateSessionRequest(repeat.SessionKey,new JObject()),1024*1024)));
                if(first.UserCreated!=second.UserCreated || first.TutorialFlag!=second.TutorialFlag)throw new InvalidDataException("Read-only character state changed after login");
                connectInfo=new {success=true,userCreated=first.UserCreated,tutorialFlag=first.TutorialFlag,repeatLoginSameIdentity=true,repeatConnectSameState=true};
                if(args.Length>2 && args[2]=="--character")
                {
                    if(first.UserCreated)throw new InvalidDataException("New test guest already has a character");
                    string characterName="ios"+Guid.NewGuid().ToString("N").Substring(0,12);
                    stage="create_character";
                    CnCreatedUser created=CnProtocol.ParseCreatedUser(Encoding.UTF8.GetString(Request(client,repeat.Api.Route("/UserCreate"),CnProtocol.CreateUserRequest(repeat.SessionKey,characterName,1),1024*1024)),repeat.UserId);
                    if(created.Name!=characterName || created.ArthurType!=1)throw new InvalidDataException("Created identity differs from request");
                    stage="created_connect";
                    CnConnectState createdState=CnProtocol.ParseConnect(Encoding.UTF8.GetString(Request(client,repeat.Api.Route("/Connect"),CnProtocol.CreateSessionRequest(repeat.SessionKey,new JObject()),1024*1024)));
                    if(!createdState.UserCreated)throw new InvalidDataException("Created character is not visible to Connect");
                    stage="home";
                    CnHomeState home=CnProtocol.ParseHome(Encoding.UTF8.GetString(Request(client,repeat.Api.Route("/HomeShow"),CnProtocol.CreateSessionRequest(repeat.SessionKey,null),1024*1024)),repeat.UserId);
                    if(home.Name!=characterName || home.ArthurType!=1)throw new InvalidDataException("Home identity differs from created user");
                    stage="cards";
                    CnCardCollection cards=CnProtocol.ParseCardShow(Encoding.UTF8.GetString(Request(client,repeat.Api.Route("/CardShow2"),CnProtocol.CreateSessionRequest(repeat.SessionKey,new JObject{{"0",1}}),1024*1024)));
                    if(cards.Cards.Count==0 || cards.Decks.Count==0)throw new InvalidDataException("New character has no starting cards or decks");
                    stage="quest_list";
                    List<CnQuest> quests=CnProtocol.ParseSoloQuests(Encoding.UTF8.GetString(Request(client,repeat.Api.Route("/TeamBattleSoloShow"),CnProtocol.CreateSessionRequest(repeat.SessionKey,new JObject{{"0",home.ArthurType}}),4*1024*1024)));
                    int partnerCount=0;bool partnerQueried=false;
                    if(quests.Count>0)
                    {
                        stage="quest_partners";
                        List<CnSoloPartner> partners=CnProtocol.ParseSoloPartners(Encoding.UTF8.GetString(Request(client,repeat.Api.Route("/TeamBattleSoloPartnerShow"),CnProtocol.CreateSessionRequest(repeat.SessionKey,new JObject{{"bossid",quests[0].BossId}}),1024*1024)));
                        partnerQueried=true;partnerCount=partners.Count;
                    }
                    questInfo=new {listParsed=true,questCount=quests.Count,partnerQueried=partnerQueried,partnerCount=partnerCount,battleStarted=false};
                    stage="character_relogin";
                    CnLogin again=CnProtocol.ParseLogin(Encoding.UTF8.GetString(Request(client,endpoint.Route("/local/client/login"),CnProtocol.CreateLogin(uuid,CnProtocol.UnityVersion),262144)),endpoint);
                    if(again.UserId!=repeat.UserId)throw new InvalidDataException("Character identity changed after relogin");
                    stage="character_reconnect";
                    CnConnectState after=CnProtocol.ParseConnect(Encoding.UTF8.GetString(Request(client,again.Api.Route("/Connect"),CnProtocol.CreateSessionRequest(again.SessionKey,new JObject()),1024*1024)));
                    if(!after.UserCreated)throw new InvalidDataException("Character creation not retained after relogin");
                    characterInfo=new {created=true,identityRedacted=true,role=created.ArthurType,homeParsed=true,level=home.Level,cardCount=cards.Cards.Count,deckCount=cards.Decks.Count,reloginSameIdentity=true,characterRetained=true};
                }
                stage="complete";success=true;
            }
        }
        catch(Exception error)
        {
            // No response bodies, session strings, UUIDs or arbitrary server
            // messages are written to disk/stdout, even on a failed request.
            errorType=error.GetType().Name;
            CnProtocolException protocol=error as CnProtocolException;if(protocol!=null)protocolCode=protocol.Code;
        }
        Console.WriteLine(JsonConvert.SerializeObject(new {checkedAt=DateTime.UtcNow.ToString("o"),success=success,stage=stage,errorType=errorType,protocolCode=protocolCode,login=loginInfo,content=contentInfo,connect=connectInfo,character=characterInfo,quests=questInfo,requests=Steps,
            scope="Actual runtime CnProtocol and RecoveryContent C# against the public isolated iOS service via Windows HttpClient. Optional --character creates a new isolated test character, reads HomeShow (which may claim login rewards), cards, solo quest list (which may clear pending quest selection) and partner candidates, then relogs. No deck edits or battle entry. UI AssetBundle loading/rendering, UnityWebRequest, iPhone execution and full gameplay are unverified."},Formatting.Indented));
        if(!success)Environment.ExitCode=1;
    }
}
