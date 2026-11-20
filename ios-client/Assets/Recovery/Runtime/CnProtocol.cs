using System;
using System.Collections.Generic;
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
        public long ArthurType { get; internal set; }
        public long Ap { get; internal set; }
        public long ApMax { get; internal set; }
        public long Bp { get; internal set; }
        public long BpMax { get; internal set; }
        public long Gold { get; internal set; }
        public long ServerTime { get; internal set; }
        public string BonusNotice { get; internal set; }
        public long? BackgroundId { get; internal set; }
        public long? NaviType { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    public sealed class CnCard
    {
        public long UniqueId { get; internal set; }
        public long CardId { get; internal set; }
        public long Level { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    public sealed class CnQuest
    {
        public long GroupId { get; internal set; }
        public string GroupName { get; internal set; }
        public long BossId { get; internal set; }
        public string Difficulty { get; internal set; }
        public long BpCost { get; internal set; }
        public long State { get; internal set; }
        public long Locked { get; internal set; }
        public string Category { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    public sealed class CnSoloBattleStart
    {
        public long Seed { get; internal set; }
        public long StartTime { get; internal set; }
        public int EndTurn { get; internal set; }
        public JArray Battles { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    public sealed class CnSoloPartner
    {
        public long UserId { get; internal set; }
        public long ArthurType { get; internal set; }
        public string Name { get; internal set; }
        public long Level { get; internal set; }
        public long RentalIndex { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    public sealed class CnSoloPartyDraft
    {
        private readonly List<CnSoloPartner> selected = new List<CnSoloPartner>();
        public int Count { get { return selected.Count; } }
        public CnSoloPartner this[int index] { get { return selected[index]; } }
        public void Add(CnSoloPartner partner)
        {
            if (partner == null || partner.UserId < 1 || partner.ArthurType < 1 || partner.ArthurType > 4 || partner.RentalIndex < 0 || partner.RentalIndex > 127)
                throw new ArgumentException("队友信息无效。");
            if (selected.Count >= 3) throw new InvalidOperationException("最多选择三名队友。");
            foreach (CnSoloPartner entry in selected)
                if (entry.UserId == partner.UserId) throw new InvalidOperationException("已经选择该队友。");
            selected.Add(partner);
        }
        public void RemoveLast() { if (selected.Count > 0) selected.RemoveAt(selected.Count - 1); }
        public void Clear() { selected.Clear(); }
        public JArray RequestPartners()
        {
            var result = new JArray();
            foreach (CnSoloPartner partner in selected)
                result.Add(new JObject { { "userid", partner.UserId }, { "arthur_type", partner.ArthurType }, { "deck_idx", partner.RentalIndex } });
            return result;
        }
        public string CreateStartRequest(string sessionKey, long bossId, CnDeck deck)
        {
            if (deck == null || deck.ArthurType < 1 || deck.ArthurType > 5 || deck.Index < 0 || deck.Index > 127)
                throw new ArgumentException("请选择有效的我方卡组。");
            return CnProtocol.CreateSoloBattleStartRequest(sessionKey, bossId, (int)deck.ArthurType, (int)deck.Index, RequestPartners());
        }
    }

    public sealed class CnDeck
    {
        public long ArthurType { get; internal set; }
        public long Index { get; internal set; }
        public string Name { get; internal set; }
        public long[] Cards { get; internal set; }
        public long[] Support { get; internal set; }
        public long[] Spheres { get; internal set; }
        public long[] Buddies { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    public sealed class CnCardCollection
    {
        public Dictionary<long, CnCard> Cards { get; internal set; }
        public List<CnDeck> Decks { get; internal set; }
    }

    public static class CnProtocol
    {
        public const string ClientVersion = "2.0.4";
        public const string ResourcePlatform = "iOS";
        public const string UnityVersion = "6000.3.26f1";
        private static readonly Regex SessionFormat = new Regex(@"\A[A-Za-z0-9_-]{16,128}\z");

        public static string NormalizeGuestUuid(string uuid)
        {
            Guid identity;
            if (uuid == null || (uuid.Length != 32 && uuid.Length != 36) || uuid != uuid.Trim() ||
                (!Guid.TryParseExact(uuid, "D", out identity) && !Guid.TryParseExact(uuid, "N", out identity)))
                throw new ArgumentException("Invalid recovery guest UUID", "uuid");
            return identity.ToString("D");
        }

        public static string CreateLogin(string uuid, string unityVersion)
        {
            uuid = NormalizeGuestUuid(uuid);
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
                UserId = id, Name = RequiredString(user, "name"), Level = RequiredInteger(user, "lv"), ArthurType = RequiredInteger(user, "active_arthur_type"),
                Ap = RequiredInteger(user, "ap"), ApMax = RequiredInteger(user, "ap_max"),
                Bp = RequiredInteger(user, "bp"), BpMax = RequiredInteger(user, "bp_max"),
                Gold = RequiredInteger(user, "gold"), ServerTime = RequiredInteger(payload, "server_time"),
                BonusNotice = DescribeHomeBonuses(payload), BackgroundId = OptionalNonNegativeInteger(payload, "background"),
                NaviType = OptionalNonNegativeInteger(user, "navi_type"), Wire = (JObject)payload.DeepClone()
            };
        }

        private static long? OptionalNonNegativeInteger(JObject source, string key)
        {
            JToken value = source[key];
            if (value == null || value.Type == JTokenType.Null) return null;
            if (value.Type != JTokenType.Integer || value.Value<long>() < 0) throw new FormatException("Invalid field: " + key);
            return value.Value<long>();
        }

        public static List<CnQuest> ParseSoloQuests(string content)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            if (payload == null) throw new FormatException("关卡响应格式无效。");
            var result = new List<CnQuest>();
            string[] keys = { "9", "10", "11", "12" };
            string[] names = { "普通", "活动", "钥匙", "特别活动" };
            for (int category = 0; category < keys.Length; category++)
            {
                JArray groups = payload[keys[category]] as JArray;
                if (groups == null) throw new FormatException("关卡分类缺失：" + keys[category]);
                foreach (JToken token in groups)
                {
                    JObject group = token as JObject;
                    if (group == null || !(group["10"] is JArray)) throw new FormatException("关卡组格式无效。");
                    long id = RequiredInteger(group, "0");
                    string groupName = FieldString(group, "4");
                    foreach (JToken item in (JArray)group["10"])
                    {
                        JObject boss = item as JObject;
                        if (boss == null) throw new FormatException("关卡格式无效。");
                        result.Add(new CnQuest { GroupId = id, GroupName = groupName, BossId = RequiredInteger(boss, "0"),
                            Difficulty = FieldString(boss, "4"), BpCost = RequiredInteger(boss, "5"), State = RequiredInteger(boss, "10"),
                            Locked = RequiredInteger(boss, "26"), Category = names[category], Wire = (JObject)boss.DeepClone() });
                    }
                }
            }
            return result;
        }

        public static string CreateSoloBattleStartRequest(string sessionKey, long bossId, int arthurType, int deckIndex, JArray partnerDecks)
        {
            if (bossId < 1 || arthurType < 1 || arthurType > 5 || deckIndex < 0 || deckIndex > 127 || partnerDecks == null || partnerDecks.Count > 3)
                throw new ArgumentException("Invalid solo battle selection");
            var partners = new JArray();
            foreach (JToken item in partnerDecks)
            {
                JObject partner = item as JObject;
                if (partner == null || partner.Count != 3 || RequiredInteger(partner, "userid") < 1 ||
                    RequiredInteger(partner, "arthur_type") < 1 || RequiredInteger(partner, "arthur_type") > 5 ||
                    RequiredInteger(partner, "deck_idx") < 0 || RequiredInteger(partner, "deck_idx") > 127)
                    throw new ArgumentException("Invalid partner deck selection");
                partners.Add(partner.DeepClone());
            }
            return CreateSessionRequest(sessionKey, new JObject { { "bossid", bossId }, { "deck_arthur_type", arthurType },
                { "deck_arthur_type_idx", deckIndex }, { "partner_deck_selects", partners } });
        }

        public static List<CnSoloPartner> ParseSoloPartners(string content)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            JArray groups = payload == null ? null : payload["arthurs"] as JArray;
            if (groups == null) throw new FormatException("Missing partner groups");
            var result = new List<CnSoloPartner>();
            foreach (JToken item in groups)
            {
                JObject group = item as JObject;
                JArray partners = group == null ? null : group["partners"] as JArray;
                if (partners == null) throw new FormatException("Invalid partner group");
                long role = RequiredInteger(group, "arthur_type");
                if (role < 1 || role > 4) throw new FormatException("Invalid partner profession");
                foreach (JToken token in partners)
                {
                    JObject partner = token as JObject;
                    if (partner == null || RequiredInteger(partner, "arthur_type") != role) throw new FormatException("Partner profession differs from group");
                    long id = RequiredInteger(partner, "userid"), level = RequiredInteger(partner, "lv"), rental = RequiredInteger(partner, "rental_idx");
                    if (id < 1 || level < 0 || rental < 0 || rental > 127) throw new FormatException("Invalid partner identity or deck");
                    result.Add(new CnSoloPartner { UserId = id, ArthurType = role, Name = RequiredString(partner, "name"), Level = level, RentalIndex = rental, Wire = (JObject)partner.DeepClone() });
                }
            }
            return result;
        }

        public static CnSoloBattleStart ParseSoloBattleStart(string content)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            JArray battles = payload == null ? null : payload["battles"] as JArray;
            if (battles == null || battles.Count == 0) throw new FormatException("Battle segments are missing");
            foreach (JToken item in battles)
            {
                JObject battle = item as JObject;
                if (battle == null || RequiredInteger(battle, "enemy_partyid") < 1 || RequiredInteger(battle, "enemy_type") < 0)
                    throw new FormatException("Invalid battle segment");
            }
            long seed = RequiredInteger(payload, "seed"), startTime = RequiredInteger(payload, "start_time"), endTurn = RequiredInteger(payload, "end_turn");
            if (startTime < 1 || endTurn < 0 || endTurn > Int32.MaxValue) throw new FormatException("Invalid battle start configuration");
            return new CnSoloBattleStart { Seed = seed, StartTime = startTime, EndTurn = (int)endTurn,
                Battles = (JArray)battles.DeepClone(), Wire = (JObject)payload.DeepClone() };
        }

        // Projects the partner DTO into the 18-field native UserData layout.
        // Callers still need the corresponding cards, skills and encounter data.
        public static int[] ProjectSoloPartnerUser(CnSoloBattleStart start, int partnerIndex)
        {
            if (start == null || start.Wire == null) throw new ArgumentNullException("start");
            JArray partners = start.Wire["partner_deck"] as JArray;
            if (partners == null || partnerIndex < 0 || partnerIndex >= partners.Count) throw new ArgumentOutOfRangeException("partnerIndex");
            JObject partner = partners[partnerIndex] as JObject;
            JObject avatar = partner == null ? null : partner["avatar"] as JObject;
            JArray parts = avatar == null ? null : avatar["avatar_partsids"] as JArray;
            if (partner == null || avatar == null || parts == null || parts.Count != 7) throw new FormatException("Partner avatar is incomplete.");
            int[] result = new int[18];
            result[0] = UserInt(partner, "userid");
            result[1] = UserInt(partner, "hp"); result[2] = result[1];
            result[3] = UserInt(partner, "atkp"); result[4] = UserInt(partner, "intp"); result[5] = UserInt(partner, "mndp");
            result[6] = UserInt(partner, "job_type"); result[7] = UserInt(partner, "arthur_type");
            result[8] = UserInt(start.Wire, "cost_initial"); result[9] = UserInt(avatar, "costumeid");
            if (result[0] <= 0 || result[1] <= 0 || result[6] < 1 || result[6] > 4 || result[7] < 1 || result[7] > 4)
                throw new FormatException("Partner identity or profession is invalid.");
            for (int i = 0; i < 7; i++)
            {
                if (parts[i].Type != JTokenType.Integer || parts[i].Value<long>() < 0 || parts[i].Value<long>() > Int32.MaxValue)
                    throw new FormatException("Partner avatar part is invalid.");
                result[10+i] = (int)parts[i].Value<long>();
            }
            result[17] = UserInt(start.Wire, "hold_max");
            return result;
        }

        private static int UserInt(JObject source, string key)
        {
            long value = RequiredInteger(source, key);
            if (value < 0 || value > Int32.MaxValue) throw new FormatException("Invalid partner field: " + key);
            return (int)value;
        }

        // The server supplies instance fields, while rarity, love rate, packed
        // skills and profile must come from the matching client master tables.
        public static byte[] ProjectSoloPartnerCard(CnSoloBattleStart start, int partnerIndex, int cardIndex,
            int loveRate, int rarity, int rarityPlus, byte[] normalSkill, byte[] bonusSkill, int[] profile)
        {
            if (start == null || start.Wire == null) throw new ArgumentNullException("start");
            JArray partners = start.Wire["partner_deck"] as JArray;
            if (partners == null || partnerIndex < 0 || partnerIndex >= partners.Count) throw new ArgumentOutOfRangeException("partnerIndex");
            JObject partner = partners[partnerIndex] as JObject;
            JArray cards = partner == null ? null : partner["deck"] as JArray;
            if (cards == null || cardIndex < 0 || cardIndex >= cards.Count) throw new ArgumentOutOfRangeException("cardIndex");
            JObject card = cards[cardIndex] as JObject;
            if (card == null || normalSkill == null || normalSkill.Length != 2436 || bonusSkill == null || bonusSkill.Length != 2436 ||
                profile == null || profile.Length != 15 || loveRate < 0 || rarity < 0 || rarityPlus < 0)
                throw new ArgumentException("完整卡牌主表与技能数据是必需的。");
            if (normalSkill[0] == 0 && normalSkill[1] == 0 && normalSkill[2] == 0 && normalSkill[3] == 0 &&
                bonusSkill[0] == 0 && bonusSkill[1] == 0 && bonusSkill[2] == 0 && bonusSkill[3] == 0)
                throw new ArgumentException("卡牌技能记录缺失。");
            int id = UserInt(card, "cardid"), hp = UserInt(card, "hp"), level = UserInt(card, "lv"), fame = UserInt(card, "fame");
            if (id == 0 || level == 0) throw new FormatException("Invalid partner card identity or level.");
            byte[] result = new byte[4960];
            int[] header = { id, hp, level, fame, loveRate, rarity, rarityPlus };
            for (int i = 0; i < header.Length; i++) WriteInt32(result, i * 4, header[i]);
            Buffer.BlockCopy(normalSkill, 0, result, 28, normalSkill.Length);
            Buffer.BlockCopy(bonusSkill, 0, result, 2464, bonusSkill.Length);
            for (int i = 0; i < profile.Length; i++) WriteInt32(result, 4900 + i * 4, profile[i]);
            return result;
        }

        private static void WriteInt32(byte[] data, int offset, int value)
        {
            unchecked { data[offset] = (byte)value; data[offset+1] = (byte)(value >> 8); data[offset+2] = (byte)(value >> 16); data[offset+3] = (byte)(value >> 24); }
        }

        public static CnCardCollection ParseCardShow(string content)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            JArray cards = payload == null ? null : payload["1"] as JArray;
            JArray decks = payload == null ? null : payload["3"] as JArray;
            if (cards == null || decks == null) throw new FormatException("服务器未返回卡牌或卡组列表。");
            var result = new CnCardCollection { Cards = new Dictionary<long, CnCard>(), Decks = new List<CnDeck>() };
            foreach (JToken token in cards)
            {
                JObject card = token as JObject;
                if (card == null) throw new FormatException("卡牌资料格式无效。");
                long id = RequiredInteger(card, "0"), cardId = RequiredInteger(card, "1"), level = RequiredInteger(card, "2");
                if (id <= 0 || cardId <= 0 || level < 0 || result.Cards.ContainsKey(id)) throw new FormatException("卡牌身份无效或重复。");
                result.Cards.Add(id, new CnCard { UniqueId = id, CardId = cardId, Level = level, Wire = (JObject)card.DeepClone() });
            }
            foreach (JToken token in decks)
            {
                JObject deck = token as JObject;
                if (deck == null) throw new FormatException("卡组资料格式无效。");
                JToken name = deck["8"];
                if (name == null || name.Type != JTokenType.String) throw new FormatException("卡组名称无效。");
                result.Decks.Add(new CnDeck { ArthurType = RequiredInteger(deck, "0"), Index = RequiredInteger(deck, "1"), Name = name.Value<string>(),
                    Cards = IntegerArray(deck, "4"), Support = IntegerArray(deck, "5"), Spheres = IntegerArray(deck, "6"), Buddies = IntegerArray(deck, "7"), Wire = (JObject)deck.DeepClone() });
            }
            return result;
        }

        public static List<CnDeck> SelectSoloDecks(CnCardCollection collection)
        {
            if (collection == null || collection.Decks == null) throw new ArgumentException("缺少卡组资料。");
            var selected = new List<CnDeck>();
            var identities = new HashSet<string>();
            foreach (CnDeck deck in collection.Decks)
            {
                if (deck == null || deck.ArthurType < 1 || deck.ArthurType > 5 || deck.Index < 0 || deck.Index > 127 ||
                    !identities.Add(deck.ArthurType.ToString() + ":" + deck.Index.ToString()))
                    throw new FormatException("服务器卡组身份无效或重复。");
                selected.Add(deck);
            }
            return selected;
        }

        public static string CreateDeckSetRequest(string sessionKey, CnDeck deck, long[] cards)
        {
            if (deck == null || deck.Wire == null || cards == null || cards.Length != deck.Cards.Length)
                throw new ArgumentException("卡组草稿无效。");
            JObject wire = deck.Wire;
            JObject draft = new JObject {
                { "arthur_type", RequiredInteger(wire, "0") }, { "idx", RequiredInteger(wire, "1") },
                { "job_type", RequiredInteger(wire, "2") }, { "leader_card_idx", RequiredInteger(wire, "3") },
                { "card_uniqid", new JArray(cards) }, { "support_card_uniqid", new JArray(deck.Support) },
                { "sphr_uniqid", new JArray(deck.Spheres) }, { "buddy_uniqid", new JArray(deck.Buddies) },
                { "name", deck.Name }, { "is_active", RequiredInteger(wire, "9") },
                { "is_rental", RequiredInteger(wire, "10") }, { "deck_rank", RequiredInteger(wire, "11") }
            };
            return CreateSessionRequest(sessionKey, new JObject { { "decks", new JArray(draft) } });
        }

        private static long[] IntegerArray(JObject value, string key)
        {
            JArray array = value[key] as JArray;
            if (array == null) throw new FormatException("服务器缺少卡组字段：" + key);
            var result = new long[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Type != JTokenType.Integer) throw new FormatException("卡组槽位无效。");
                result[i] = array[i].Value<long>();
            }
            return result;
        }

        private static string DescribeHomeBonuses(JObject payload)
        {
            var lines = new List<string>();
            foreach (string kind in new[] { "login_bonus_beginner", "login_bonus_daily", "login_bonus_total" })
            {
                JToken raw = payload[kind];
                if (raw == null || raw.Type == JTokenType.Null) continue;
                JArray entries = raw as JArray;
                if (entries == null || entries.Count == 0 || !(entries[0] is JObject)) throw new FormatException("登录奖励格式无效：" + kind);
                JObject entry = (JObject)entries[0];
                long day = RequiredInteger(entry, "day_num");
                JToken result = entry["reward"];
                if (kind == "login_bonus_beginner")
                {
                    JArray schedule = result as JArray;
                    if (schedule == null || day < 1 || day > schedule.Count) throw new FormatException("新手奖励日期无效。");
                    result = schedule[(int)day - 1];
                }
                JObject item = result == null ? null : result["reward"] as JObject;
                if (item == null) throw new FormatException("登录奖励内容无效。");
                string label = kind == "login_bonus_beginner" ? "新手" : kind == "login_bonus_daily" ? "每日" : "累计";
                lines.Add(label + "第 " + day + " 天：类型 " + RequiredInteger(item, "type") + "，编号 " + RequiredInteger(item, "reward_typeid") + "，数量 " + RequiredInteger(item, "num"));
            }
            return String.Join("\n", lines.ToArray());
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

        private static string FieldString(JObject value, string name)
        {
            JToken token = value[name];
            if (token == null || token.Type != JTokenType.String) throw new FormatException("服务器缺少文本字段：" + name);
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
