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
        object loginInfo=null,contentInfo=null,connectInfo=null;
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
        Console.WriteLine(JsonConvert.SerializeObject(new {checkedAt=DateTime.UtcNow.ToString("o"),success=success,stage=stage,errorType=errorType,protocolCode=protocolCode,login=loginInfo,content=contentInfo,connect=connectInfo,requests=Steps,
            scope="Actual runtime CnProtocol and RecoveryContent C# against the public isolated iOS service via Windows HttpClient. Public bundle bytes are size/SHA-256/CRC checked and cache reopened. UI AssetBundle loading/rendering, UnityWebRequest, iPhone execution and full gameplay are unverified. No character creation, home rewards, deck edits or battle entry performed."},Formatting.Indented));
        if(!success)Environment.ExitCode=1;
    }
}
