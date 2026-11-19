using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kairisei.Recovery
{
    // The wire format comes from server/internal/cnbootstrap, not from dummy DLL bodies.
    public sealed class ServerEndpoint
    {
        public Uri BaseUri { get; private set; }

        private ServerEndpoint(Uri uri) { BaseUri = uri; }

        public static ServerEndpoint Parse(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) throw new FormatException("请输入服务器地址。");
            value = value.Trim();
            if (Regex.IsMatch(value, @"[\s\\]")) throw new FormatException("服务器地址不能包含空白或反斜线。");
            bool hasScheme = value.Contains("://");
            string authority = hasScheme ? value.Substring(value.IndexOf("://", StringComparison.Ordinal) + 3) : value;
            if (!hasScheme && authority.IndexOf(':') != authority.LastIndexOf(':') && !authority.StartsWith("[", StringComparison.Ordinal))
                throw new FormatException("IPv6 地址请使用 [地址]:端口 格式。");
            Uri uri;
            if (!Uri.TryCreate(hasScheme ? value : "http://" + value, UriKind.Absolute, out uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https") || String.IsNullOrEmpty(uri.Host) ||
                !String.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/" ||
                !String.IsNullOrEmpty(uri.Query) || !String.IsNullOrEmpty(uri.Fragment) || uri.Port == 0)
                throw new FormatException("请输入 IP／域名和端口，或 HTTP(S) 根地址。");
            if (!hasScheme && !Regex.IsMatch(authority.TrimEnd('/'), @"(?:\]|[^:]):[0-9]+$"))
            {
                UriBuilder builder = new UriBuilder(uri);
                builder.Port = 26040;
                uri = builder.Uri;
            }
            return new ServerEndpoint(uri);
        }

        public Uri Route(string route)
        {
            if (route == null || !Regex.IsMatch(route, @"\A/[A-Za-z0-9_./-]+\z") || route.Contains(".."))
                throw new ArgumentException("Invalid protocol route", "route");
            return new Uri(BaseUri, route);
        }

        public override string ToString() { return BaseUri.AbsoluteUri; }
    }

    public sealed class CnProtocolException : Exception
    {
        public int Code { get; private set; }
        public CnProtocolException(int code, string message) : base(message) { Code = code; }
    }

    public sealed class CnEnvelope
    {
        public JObject Common { get; internal set; }
        public JToken Payload { get; internal set; }
        public JObject Popup { get; internal set; }
    }

    public sealed class CnLogin
    {
        public long UserId { get; internal set; }
        public string SessionKey { get; internal set; }
        public ServerEndpoint Api { get; internal set; }
        public Uri VersionUrl { get; internal set; }
    }

    public sealed class CnConnectState
    {
        public bool UserCreated { get; internal set; }
        public long TutorialFlag { get; internal set; }
    }

    public sealed class CnCreatedUser
    {
        public long UserId { get; internal set; }
        public string Name { get; internal set; }
        public int ArthurType { get; internal set; }
    }

    public sealed class CnHomeState
    {
        public long UserId { get; internal set; }
        public string Name { get; internal set; }
        public long Level { get; internal set; }
        public long Ap { get; internal set; }
        public long ApMax { get; internal set; }
        public long Bp { get; internal set; }
        public long BpMax { get; internal set; }
        public long Gold { get; internal set; }
        public long ServerTime { get; internal set; }
    }

    public static class CnProtocol
    {
        public const string ClientVersion = "2.0.4";
        public const string ResourcePlatform = "iOS";
        public const string UnityVersion = "6000.3.26f1";
        private static readonly Regex SessionFormat = new Regex(@"\A[A-Za-z0-9_-]{16,128}\z");

        public static string CreateLogin(string uuid, string unityVersion)
        {
            if (String.IsNullOrWhiteSpace(uuid) || uuid.Length > 128) throw new ArgumentException("Invalid recovery guest UUID", "uuid");
            if (String.IsNullOrWhiteSpace(unityVersion)) throw new ArgumentException("Unity version required", "unityVersion");
            return new JObject {
                { "uuid", uuid }, { "clver", ClientVersion },
                { "resource_platform", ResourcePlatform }, { "unity_version", unityVersion }
            }.ToString(Formatting.None);
        }

        public static string CreateSessionRequest(string sessionKey, JObject payload)
        {
            if (sessionKey == null || !SessionFormat.IsMatch(sessionKey)) throw new FormatException("Invalid session key");
            // There is no separator: splitCNSessionPayload scans for the first '{' or '['.
            return sessionKey + (payload == null ? "" : payload.ToString(Formatting.None));
        }

        public static string CreateUserRequest(string sessionKey, string name, int arthurType)
        {
            if (String.IsNullOrWhiteSpace(name) || name.Length > 32 || name.Contains(" "))
                throw new ArgumentException("请输入不含空格的角色名称（最多 32 字符）。", "name");
            if (arthurType < 1 || arthurType > 4) throw new ArgumentException("请选择亚瑟职业。", "arthurType");
            return CreateSessionRequest(sessionKey, new JObject { { "name", name }, { "arthur_type", arthurType } });
        }

        public static CnCreatedUser ParseCreatedUser(string content, long expectedUserId)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            JObject user = payload == null ? null : payload["user"] as JObject;
            if (user == null) throw new FormatException("服务器未返回创建的角色。");
            JToken id = user["userid"], role = user["active_arthur_type"];
            string name = RequiredString(user, "name");
            if (id == null || id.Type != JTokenType.Integer || id.Value<long>() != expectedUserId || expectedUserId < 1 ||
                role == null || role.Type != JTokenType.Integer || role.Value<long>() < 1 || role.Value<long>() > 4)
                throw new FormatException("服务器返回的角色身份或职业无效。");
            return new CnCreatedUser { UserId = expectedUserId, Name = name, ArthurType = role.Value<int>() };
        }

        public static int ParseHealth(string content)
        {
            JObject health = ParseObject(content);
            if ((string)health["state"] != "PASS") throw new FormatException("服务器未报告就绪。");
            JToken port = health["battle_port"];
            if (port == null || port.Type != JTokenType.Integer) throw new FormatException("服务器缺少战斗端口。");
            int result = port.Value<int>();
            if (result < 1 || result > 65535) throw new FormatException("服务器战斗端口无效。");
            return result;
        }

        public static CnLogin ParseLogin(string content, ServerEndpoint requestedServer)
        {
            JObject response = ParseObject(content);
            CheckResult(response);
            string session = RequiredString(response, "sess_key");
            if (!SessionFormat.IsMatch(session)) throw new FormatException("服务器返回了无效会话。");
            JToken user = response["userid"];
            if (user == null || user.Type != JTokenType.Integer || user.Value<long>() < 1)
                throw new FormatException("服务器缺少有效角色编号。");
            ServerEndpoint api = ServerEndpoint.Parse(RequiredString(response, "api_url"));
            if (requestedServer == null || !SameOrigin(api.BaseUri, requestedServer.BaseUri))
                throw new FormatException("服务器公布的 API 地址与连接地址不同，请先检查服务端 advertised host 配置。");
            Uri version;
            if (!Uri.TryCreate(RequiredString(response, "version_url"), UriKind.Absolute, out version) ||
                !SameOrigin(version, requestedServer.BaseUri) || !String.IsNullOrEmpty(version.UserInfo))
                throw new FormatException("服务器资源清单地址无效或与连接地址不同。");
            return new CnLogin { UserId = user.Value<long>(), SessionKey = session, Api = api, VersionUrl = version };
        }

        public static CnEnvelope ParseEnvelope(string content)
        {
            if (content == null) throw new FormatException("服务器响应为空。");
            string[] lines = content.TrimStart('\uFEFF').Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            if (lines.Length != 2 && lines.Length != 3) throw new FormatException("服务器协议分段数量不正确。");
            JObject common = ParseObject(lines[0]);
            CheckResult(common);
            JToken payload = JToken.Parse(lines[1]);
            if (payload.Type != JTokenType.Object && payload.Type != JTokenType.Array)
                throw new FormatException("服务器业务数据必须是对象或数组。");
            return new CnEnvelope { Common = common, Payload = payload, Popup = lines.Length == 3 ? ParseObject(lines[2]) : null };
        }

        public static CnConnectState ParseConnect(string content)
        {
            CnEnvelope envelope = ParseEnvelope(content);
            JObject payload = envelope.Payload as JObject;
            if (payload == null) throw new FormatException("连接状态格式无效。");
            JToken created = payload["is_user"];
            JToken tutorial = payload["tutorial_flag"];
            if (created == null || created.Type != JTokenType.Integer || (created.Value<long>() != 0 && created.Value<long>() != 1))
                throw new FormatException("服务器缺少有效的角色创建状态。");
            if (tutorial == null || tutorial.Type != JTokenType.Integer)
                throw new FormatException("服务器缺少有效的教程状态。");
            return new CnConnectState { UserCreated = created.Value<long>() == 1, TutorialFlag = tutorial.Value<long>() };
        }

        public static CnHomeState ParseHome(string content, long expectedUserId)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            JObject user = payload == null ? null : payload["user"] as JObject;
            if (user == null) throw new FormatException("主页响应缺少角色信息。");
            long id = RequiredInteger(user, "userid");
            if (id < 1 || id != expectedUserId) throw new FormatException("主页角色与登录身份不一致。");
            return new CnHomeState {
                UserId = id, Name = RequiredString(user, "name"), Level = RequiredInteger(user, "lv"),
                Ap = RequiredInteger(user, "ap"), ApMax = RequiredInteger(user, "ap_max"),
                Bp = RequiredInteger(user, "bp"), BpMax = RequiredInteger(user, "bp_max"),
                Gold = RequiredInteger(user, "gold"), ServerTime = RequiredInteger(payload, "server_time")
            };
        }

        private static long RequiredInteger(JObject value, string name)
        {
            JToken token = value[name];
            if (token == null || token.Type != JTokenType.Integer) throw new FormatException("服务器缺少有效字段：" + name);
            return token.Value<long>();
        }

        private static bool SameOrigin(Uri a, Uri b)
        {
            return a.Scheme == b.Scheme && String.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase) && a.Port == b.Port;
        }

        private static JObject ParseObject(string content)
        {
            if (String.IsNullOrWhiteSpace(content)) throw new FormatException("服务器响应为空。");
            return JObject.Parse(content.TrimStart('\uFEFF'));
        }

        private static string RequiredString(JObject value, string name)
        {
            JToken token = value[name];
            if (token == null || token.Type != JTokenType.String || String.IsNullOrWhiteSpace(token.Value<string>()))
                throw new FormatException("服务器缺少字段：" + name);
            return token.Value<string>();
        }

        private static void CheckResult(JObject response)
        {
            JToken token = response["res_code"];
            if (token == null || token.Type != JTokenType.Integer) throw new FormatException("服务器缺少响应状态码。");
            int code = token.Value<int>();
            if (code != 0)
            {
                string message = response["res_str"] == null ? "服务器拒绝请求。" : response["res_str"].Value<string>();
                throw new CnProtocolException(code, String.IsNullOrWhiteSpace(message) ? "服务器拒绝请求。" : message);
            }
        }
    }
}
