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
        public CnSoloEntryRules EntryRules { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    public sealed class CnSoloBattleStart
    {
        public long Seed { get; internal set; }
        public long StartTime { get; internal set; }
        public int EndTurn { get; internal set; }
        public bool ContinueAllowed { get; internal set; }
        public CnSoloPlayerLoadout Player { get; internal set; }
        public JArray Battles { get; internal set; }
        public JObject Wire { get; internal set; }
    }

    // Retain the exact ordered selection independently of the current login session.
    public sealed class CnSoloBattleSelection
    {
        public long UserId { get; private set; }
        public long BossId { get; private set; }
        public int ArthurType { get; private set; }
        public int DeckIndex { get; private set; }
        internal JArray Partners { get; private set; }

        internal CnSoloBattleSelection(long userId, long bossId, int arthurType, int deckIndex, JArray partners)
        {
            UserId = userId;
            BossId = bossId;
            ArthurType = arthurType;
            DeckIndex = deckIndex;
            Partners = (JArray)partners.DeepClone();
        }

        // Start and read-only recovery submit the same selection body with the current session.
        public string CreateRequest(string sessionKey)
        {
            return CnProtocol.CreateSoloBattleStartRequest(sessionKey, BossId, ArthurType, DeckIndex, Partners);
        }
    }

    // Point-in-time loadout from the authenticated player's selected deck.
    public sealed class CnSoloPlayerLoadout
    {
        public long UserId { get; internal set; }
        public long ArthurType { get; internal set; }
        public long Index { get; internal set; }
        public long Health { get; internal set; }
        internal JObject Wire { get; set; }

        // Keep the loadout tied to the inventory snapshot the player reviewed.
        public bool Matches(CnDeck deck, CnCardCollection inventory)
        {
            if (deck == null || inventory == null || ArthurType != deck.ArthurType || Index != deck.Index ||
                !JToken.DeepEquals(Wire["leader_card_idx"], deck.Wire["3"])) return false;
            return MatchCards((JArray)Wire["deck"], deck.Cards, inventory) &&
                MatchCards((JArray)Wire["support_deck"], deck.Support, inventory);
        }

        // Compare slot order, card instances' published definitions and available progression fields.
        private static bool MatchCards(JArray wire, long[] slots, CnCardCollection inventory)
        {
            if (wire.Count != slots.Length) return false;
            for (int index = 0; index < slots.Length; index++)
            {
                var entry = (JObject)wire[index];
                long cardId = (long)entry["cardid"];
                if (slots[index] == 0 && cardId == 0) continue;
                CnCard card;
                if (!inventory.Cards.TryGetValue(slots[index], out card) || card.CardId != cardId ||
                    card.Level != (long)entry["lv"] || !MatchIfPresent(card.Wire["5"], entry["love"]) ||
                    !MatchIfPresent(card.Wire["6"], entry["skill_lv"]) || !MatchIfPresent(card.Wire["17"], entry["fame"])) return false;
            }
            return true;
        }

        // Older CardShow2 profiles may omit auxiliary progression fields.
        private static bool MatchIfPresent(JToken reviewed, JToken current)
        {
            return reviewed == null || JToken.DeepEquals(reviewed, current);
        }
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

    // Decode the lobby's stage and slot restrictions before building an admission request.
    public sealed class CnSoloEntryRules
    {
        private readonly int[] roles = { 1, 2, 3, 4 };
        private readonly int[] sources = new int[4];
        private readonly bool alchemist, onlyOwnDecks;
        public bool AllowsSolo { get; private set; }

        // Follow the service: the first entry-limit row is authoritative, with ordinary slots as the default.
        internal CnSoloEntryRules(long stageType, JObject boss)
        {
            alchemist = stageType == 19 || stageType == 20;
            onlyOwnDecks = ReadRule(boss, "1", 0, Int32.MaxValue) != 0;
            AllowsSolo = ReadRule(boss, "24", 0, Int32.MaxValue) != 3;
            JToken limits = boss["entry_limit"];
            if (limits == null || limits.Type == JTokenType.Null) return;
            var rows = limits as JArray;
            if (rows == null) throw new FormatException("关卡编队限制格式无效。");
            if (rows.Count == 0) return;
            var slots = rows[0] as JObject;
            if (slots == null) throw new FormatException("关卡编队槽位格式无效。");
            for (int slot = 0; slot < roles.Length; slot++)
            {
                string prefix = "slot" + (slot + 1);
                roles[slot] = ReadRule(slots, prefix + "_arthur_type", -1, 4);
                sources[slot] = ReadRule(slots, prefix + "_partner_type", 0, 2);
            }
        }

        // The service defaults omitted scalar fields to zero. Reject unknown slot restrictions.
        private static int ReadRule(JObject fields, string field, int minimum, int maximum)
        {
            JToken token = fields[field];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type != JTokenType.Integer || (long)token < minimum || (long)token > maximum)
                throw new FormatException("关卡编队限制无效：" + field);
            return (int)token;
        }

        // The controlled deck takes the first eligible slot. Borrow-only slots cannot host it.
        private int PlayerSlot(long role)
        {
            if (!AllowsSolo || role < 1 || role > 4) return -1;
            for (int slot = 0; slot < roles.Length; slot++)
                if (sources[slot] != 2 && (roles[slot] == 0 || roles[slot] == role)) return slot;
            return -1;
        }

        // Determine which primary decks can be offered for this stage.
        public bool AllowsPlayer(long role) { return alchemist ? AllowsSolo && role == 5 : PlayerSlot(role) >= 0; }

        // Before picking the primary deck, expose both sources wherever an eligible layout allows them.
        public bool AllowsPartnerSource(bool own)
        {
            if (alchemist) return false;
            for (int role = 1; role <= 4; role++)
            {
                int player = PlayerSlot(role);
                if (player < 0) continue;
                for (int slot = 0; slot < roles.Length; slot++)
                    if (slot != player && roles[slot] >= 0 && SourceMatches(slot, own)) return true;
            }
            return false;
        }

        // Check the stage-wide own-deck restriction and the individual slot's source.
        private bool SourceMatches(int slot, bool own)
        {
            return own ? sources[slot] != 2 : !onlyOwnDecks && sources[slot] != 1;
        }

        // Validate partial parties against the chosen role or any eligible role before one is chosen.
        public bool CanComplete(long role, long userId, IReadOnlyList<CnSoloPartner> selected)
        {
            if (role != 0) return Place(role, userId, selected, false) != null;
            for (int candidate = 1; candidate <= 5; candidate++)
                if (Place(candidate, userId, selected, false) != null) return true;
            return false;
        }

        // Return partners in the server's enabled slot order. A complete entry must fill every slot.
        internal CnSoloPartner[] Place(long role, long userId, IReadOnlyList<CnSoloPartner> selected, bool complete)
        {
            if (alchemist) return AllowsPlayer(role) && selected.Count == 0 ? new CnSoloPartner[0] : null;
            int player = PlayerSlot(role);
            if (player < 0) return null;
            var slots = new List<int>(3);
            for (int slot = 0; slot < roles.Length; slot++)
                if (slot != player && roles[slot] >= 0) slots.Add(slot);
            if (selected.Count > slots.Count || (complete && selected.Count != slots.Count)) return null;
            var ordered = new CnSoloPartner[slots.Count];
            return AssignPartner(0, userId, selected, slots, ordered) ? ordered : null;
        }

        // Try each compatible free slot. At most three partners need assignment.
        private bool AssignPartner(int index, long userId, IReadOnlyList<CnSoloPartner> selected,
            List<int> slots, CnSoloPartner[] ordered)
        {
            if (index == selected.Count) return true;
            CnSoloPartner partner = selected[index];
            for (int target = 0; target < slots.Count; target++)
            {
                int slot = slots[target];
                if (ordered[target] != null || (roles[slot] != 0 && roles[slot] != partner.ArthurType) ||
                    !SourceMatches(slot, partner.UserId == userId)) continue;
                ordered[target] = partner;
                if (AssignPartner(index + 1, userId, selected, slots, ordered)) return true;
                ordered[target] = null;
            }
            return false;
        }
    }

    public sealed class CnSoloPartyDraft
    {
        private readonly List<CnSoloPartner> selected = new List<CnSoloPartner>();
        public int Count { get { return selected.Count; } }
        public CnSoloPartner this[int index] { get { return selected[index]; } }
        // A single account may supply different profession or rental decks.
        public void Add(CnSoloPartner partner)
        {
            if (partner == null || partner.UserId < 1 || partner.ArthurType < 1 || partner.ArthurType > 4 || partner.RentalIndex < 0 || partner.RentalIndex > 127)
                throw new ArgumentException("队友信息无效。");
            if (selected.Count >= 3) throw new InvalidOperationException("最多选择三名队友。");
            if (Contains(partner)) throw new InvalidOperationException("已经选择该队友卡组。");
            selected.Add(partner);
        }

        // Match the exact deck identity across reordered candidate lists and fresh inventory reads.
        private static bool SameDeck(CnSoloPartner a, CnSoloPartner b)
        {
            return a.UserId == b.UserId && a.ArthurType == b.ArthurType && a.RentalIndex == b.RentalIndex;
        }

        public bool Contains(CnSoloPartner partner) { return selected.Exists(entry => SameDeck(entry, partner)); }
        public bool Remove(CnSoloPartner partner) { return selected.RemoveAll(entry => SameDeck(entry, partner)) != 0; }
        public void RemoveLast() { if (selected.Count > 0) selected.RemoveAt(selected.Count - 1); }
        public void Clear() { selected.Clear(); }

        // Remove selected self decks that are no longer present or valid after a fresh CardShow2 read.
        public void ReconcileOwn(long userId, List<CnDeck> available)
        {
            selected.RemoveAll(partner => partner.UserId == userId &&
                !available.Exists(deck => deck.ArthurType == partner.ArthurType && deck.Index == partner.RentalIndex));
        }

        // A partial team can be edited as long as all its choices can occupy compatible slots.
        public bool CanComplete(CnQuest quest, long userId, CnDeck primary)
        {
            return (primary == null || !selected.Exists(partner => partner.UserId == userId &&
                partner.ArthurType == primary.ArthurType && partner.RentalIndex == primary.Index)) &&
                quest.EntryRules.CanComplete(primary == null ? 0 : primary.ArthurType, userId, selected);
        }

        // Encode selected identities without changing their index or profession.
        private static JArray EncodePartners(IEnumerable<CnSoloPartner> ordered)
        {
            var result = new JArray();
            foreach (CnSoloPartner partner in ordered)
                result.Add(new JObject { { "userid", partner.UserId }, { "arthur_type", partner.ArthurType }, { "deck_idx", partner.RentalIndex } });
            return result;
        }
        public JArray RequestPartners() { return EncodePartners(selected); }

        // Encode only a full, rule-compatible formation. Callers still own live admission and recovery.
        public string CreateStartRequest(string sessionKey, CnQuest quest, long userId, CnDeck deck)
        {
            return Confirm(quest, userId, deck).CreateRequest(sessionKey);
        }

        // Keep the server slot order stable even if the draft or session changes later.
        public CnSoloBattleSelection Confirm(CnQuest quest, long userId, CnDeck deck)
        {
            if (quest == null || quest.BossId < 1 || userId < 1 || deck == null || deck.ArthurType < 1 || deck.ArthurType > 5 || deck.Index < 0 || deck.Index > 127)
                throw new ArgumentException("请选择有效的我方卡组。");
            CnSoloPartner[] ordered = quest.EntryRules.Place(deck.ArthurType, userId, selected, true);
            if (ordered == null || !CanComplete(quest, userId, deck)) throw new InvalidOperationException("请按关卡要求补齐编队。");
            return new CnSoloBattleSelection(userId, quest.BossId, (int)deck.ArthurType, (int)deck.Index, EncodePartners(ordered));
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

    public enum CnDeckRecoveryResult { Saved, RestoreDraft, Conflict }

    // Only reconciles after an authoritative re-read; never sends a write.
    public sealed class CnDeckSaveRecovery
    {
        private readonly long arthurType, index;
        private static readonly string[] EditedAlongsideMain = { "2", "3", "5", "6", "7", "8", "9", "10" };
        private readonly long[] previous, proposed;
        private readonly JObject previousWire;

        public CnDeckSaveRecovery(CnDeck deck, long[] draft)
        {
            if (deck == null || deck.Wire == null || deck.Cards == null || draft == null || deck.Cards.Length != draft.Length)
                throw new ArgumentException("卡组草稿无效。");
            arthurType = deck.ArthurType; index = deck.Index;
            previous = (long[])deck.Cards.Clone(); proposed = (long[])draft.Clone();
            previousWire = (JObject)deck.Wire.DeepClone();
        }

        private static bool Same(long[] a, long[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public CnDeckRecoveryResult Reconcile(CnCardCollection collection, out int deckIndex, out long[] draft)
        {
            deckIndex = -1; draft = null;
            if (collection == null || collection.Decks == null || collection.Cards == null) return CnDeckRecoveryResult.Conflict;
            for (int i = 0; i < collection.Decks.Count; i++)
            {
                CnDeck deck = collection.Decks[i];
                if (deck == null || deck.ArthurType != arthurType || deck.Index != index) continue;
                if (deckIndex != -1) { deckIndex = -1; return CnDeckRecoveryResult.Conflict; }
                deckIndex = i;
            }
            if (deckIndex < 0) return CnDeckRecoveryResult.Conflict;
            CnDeck currentDeck = collection.Decks[deckIndex];
            // CardDeckSet also writes these fields. A recalculated rank may legitimately change.
            if (currentDeck.Wire == null) return CnDeckRecoveryResult.Conflict;
            foreach (string field in EditedAlongsideMain)
                if (!JToken.DeepEquals(previousWire[field], currentDeck.Wire[field])) return CnDeckRecoveryResult.Conflict;
            long[] current = currentDeck.Cards;
            if (Same(current, proposed)) return CnDeckRecoveryResult.Saved;
            if (!Same(current, previous)) return CnDeckRecoveryResult.Conflict;
            foreach (long card in proposed)
                if (card != 0 && !collection.Cards.ContainsKey(card)) return CnDeckRecoveryResult.Conflict;
            draft = (long[])proposed.Clone();
            return CnDeckRecoveryResult.RestoreDraft;
        }
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
            long? points;
            return ParseSoloQuests(content, out points);
        }

        // Field 0 is the live account balance, refreshed by TeamBattleSoloShow.
        public static List<CnQuest> ParseSoloQuests(string content, out long? battlePoints)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            if (payload == null) throw new FormatException("关卡响应格式无效。");
            battlePoints = OptionalNonNegativeInteger(payload, "0");
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
                    long stageType = OptionalNonNegativeInteger(group, "1") ?? 0;
                    string groupName = FieldString(group, "4");
                    foreach (JToken item in (JArray)group["10"])
                    {
                        JObject boss = item as JObject;
                        if (boss == null) throw new FormatException("关卡格式无效。");
                        result.Add(new CnQuest { GroupId = id, GroupName = groupName, BossId = RequiredInteger(boss, "0"),
                            Difficulty = FieldString(boss, "4"), BpCost = RequiredInteger(boss, "5"), State = RequiredInteger(boss, "10"),
                            Locked = RequiredInteger(boss, "26"), Category = names[category], EntryRules = new CnSoloEntryRules(stageType, boss),
                            Wire = (JObject)boss.DeepClone() });
                    }
                }
            }
            return result;
        }

        public static string CreateSoloBattleStartRequest(string sessionKey, long bossId, int arthurType, int deckIndex, JArray partnerDecks)
        {
            return CreateSessionRequest(sessionKey, SoloBattleSelectionPayload(bossId, arthurType, deckIndex, partnerDecks));
        }

        // Store only the request fields. Account and connection scope come from the journal path.
        internal static string SaveSoloBattleSelection(CnSoloBattleSelection selection)
        {
            return SoloBattleSelectionPayload(selection.BossId, selection.ArthurType, selection.DeckIndex, selection.Partners)
                .ToString(Formatting.None);
        }

        // Validate the journal independently of current card inventory, stage rules and session.
        internal static CnSoloBattleSelection LoadSoloBattleSelection(string text, long userId)
        {
            JObject saved;
            try { saved = JObject.Parse(text, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }); }
            catch (JsonException error) { throw new FormatException("本地战斗编队记录格式无效。", error); }
            if (saved.Count != 4 || userId < 1) throw new FormatException("本地战斗编队记录不完整。");
            long bossId = RequiredInteger(saved, "bossid");
            int role = UserInt(saved, "deck_arthur_type"), index = UserInt(saved, "deck_arthur_type_idx");
            var payload = SoloBattleSelectionPayload(bossId, role, index, saved["partner_deck_selects"] as JArray);
            return new CnSoloBattleSelection(userId, bossId, role, index, (JArray)payload["partner_deck_selects"]);
        }

        // Keep on-disk and transport copies in the same validated order and wire shape.
        private static JObject SoloBattleSelectionPayload(long bossId, int arthurType, int deckIndex, JArray partnerDecks)
        {
            if (bossId < 1 || arthurType < 1 || arthurType > 5 || deckIndex < 0 || deckIndex > 127 || partnerDecks == null || partnerDecks.Count > 3)
                throw new ArgumentException("Invalid solo battle selection");
            var partners = new JArray();
            foreach (JToken item in partnerDecks)
            {
                JObject partner = item as JObject;
                if (partner == null || partner.Count != 3 || RequiredInteger(partner, "userid") < 1 ||
                    RequiredInteger(partner, "arthur_type") < 1 || RequiredInteger(partner, "arthur_type") > 4 ||
                    RequiredInteger(partner, "deck_idx") < 0 || RequiredInteger(partner, "deck_idx") > 127)
                    throw new ArgumentException("Invalid partner deck selection");
                partners.Add(partner.DeepClone());
            }
            return new JObject { { "bossid", bossId }, { "deck_arthur_type", arthurType },
                { "deck_arthur_type_idx", deckIndex }, { "partner_deck_selects", partners } };
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

        // The account identity comes from the session. This request only carries the exact deck key.
        public static string CreateSoloPlayerLoadoutRequest(string sessionKey, CnDeck deck)
        {
            if (deck == null || deck.ArthurType < 1 || deck.ArthurType > 5 || deck.Index < 0 || deck.Index > 127)
                throw new ArgumentException("请选择有效的我方卡组。");
            return CreateSessionRequest(sessionKey, new JObject {
                { "deck_arthur_type", deck.ArthurType }, { "deck_arthur_type_idx", deck.Index }
            });
        }

        // Fail closed when the service returns a different owner, role, deck index or malformed battle slots.
        public static CnSoloPlayerLoadout ParseSoloPlayerLoadout(string content, long userId, CnDeck selected)
        {
            JObject payload = ParseEnvelope(content).Payload as JObject;
            if (selected == null) throw new ArgumentNullException("selected");
            return ReadSoloBattleLoadout(payload == null ? null : payload["player_deck"] as JObject,
                userId, selected.ArthurType, selected.Index);
        }

        // Both primary and partner battle projections must match the original account, profession and deck index.
        private static CnSoloPlayerLoadout ReadSoloBattleLoadout(JObject deck, long userId, long role, long index)
        {
            if (deck == null || userId < 1 || RequiredInteger(deck, "userid") != userId ||
                RequiredInteger(deck, "arthur_type") != role || RequiredInteger(deck, "rental_idx") != index)
                throw new FormatException("服务器返回的战斗卡组与当前账号或所选卡组不一致。");
            long hp = UserInt(deck, "hp"), job = UserInt(deck, "job_type");
            if (hp == 0 || job < 1 || job > 5) throw new FormatException("我方战斗参数无效。");
            UserInt(deck, "atkp");
            UserInt(deck, "intp");
            UserInt(deck, "mndp");
            RequiredInteger(deck, "leader_card_idx");
            JArray main = ReadBattleLoadoutSlots(deck, "deck", 10, 10);
            JArray support = ReadBattleLoadoutSlots(deck, "support_deck", 0, 10);
            if (UserInt(deck, "support_card_unlock_slot_num") > support.Count) throw new FormatException("我方支援卡槽位无效。");
            ValidateBattleLoadoutSlots(main, "cardid", true);
            ValidateBattleLoadoutSlots(support, "cardid", false);
            ValidateBattleLoadoutSlots(ReadBattleLoadoutSlots(deck, "sphrs", 3, 3), "sphrid", false);
            ValidateBattleLoadoutSlots(ReadBattleLoadoutSlots(deck, "buddys", 5, 5), "buddyid", false);
            return new CnSoloPlayerLoadout { UserId = userId, ArthurType = role, Index = index, Health = hp,
                Wire = (JObject)deck.DeepClone() };
        }

        // Check the actual CN slot vector lengths before accessing their entries.
        private static JArray ReadBattleLoadoutSlots(JObject deck, string field, int minimum, int maximum)
        {
            var slots = deck[field] as JArray;
            if (slots == null || slots.Count < minimum || slots.Count > maximum)
                throw new FormatException("我方战斗卡组槽位无效：" + field);
            return slots;
        }

        // A zero ID is valid only in optional equipment slots. Real slots must have an instance level.
        private static void ValidateBattleLoadoutSlots(JArray slots, string field, bool required)
        {
            foreach (JToken token in slots)
            {
                var slot = token as JObject;
                if (slot == null) throw new FormatException("我方战斗卡组槽位格式无效。");
                int id = UserInt(slot, field), level = UserInt(slot, "lv");
                if ((id == 0 && (required || level != 0)) || (id != 0 && level == 0))
                    throw new FormatException("我方战斗卡组的装备编号或等级无效。");
                if (field != "cardid") continue;
                UserInt(slot, "love");
                if (UserInt(slot, "fame") > 100 || !(slot["skill_lv"] is JArray) && (slot["skill_lv"] == null || slot["skill_lv"].Type != JTokenType.Null))
                    throw new FormatException("我方战斗卡牌的名声或技能等级无效。");
                var skills = slot["skill_lv"] as JArray;
                if (skills != null && skills.Count > 6) throw new FormatException("我方战斗卡牌的技能等级超出范围。");
                if (skills != null)
                    foreach (JToken skill in skills)
                        if (skill.Type != JTokenType.Integer || (long)skill < 0 || (long)skill > Int32.MaxValue)
                            throw new FormatException("我方战斗卡牌的技能等级无效。");
            }
        }

        // Decode archived original responses and their enemy wave identities.
        public static CnSoloBattleStart ParseSoloBattleStart(string content)
        {
            return ReadSoloBattleStart(ParseEnvelope(content).Payload as JObject);
        }

        // Both live start and recovery must return the original party and the accepted continue policy.
        public static CnSoloBattleStart ParseSoloBattleStart(string content, CnSoloBattleSelection selection)
        {
            if (selection == null) throw new ArgumentNullException("selection");
            CnSoloBattleStart opening = ParseSoloBattleStart(content);
            JObject payload = opening.Wire;
            var partners = payload["partner_deck"] as JArray;
            var continueAllowed = payload["continue_allowed"];
            if (RequiredInteger(payload, "bossid") != selection.BossId || partners == null ||
                partners.Count != selection.Partners.Count || continueAllowed == null || continueAllowed.Type != JTokenType.Boolean ||
                opening.Seed < Int32.MinValue || opening.Seed > UInt32.MaxValue)
                throw new FormatException("战斗开场与原编队不一致。");
            opening.Player = ReadSoloBattleLoadout(payload["player_deck"] as JObject,
                selection.UserId, selection.ArthurType, selection.DeckIndex);
            for (int i = 0; i < partners.Count; i++)
            {
                var expected = (JObject)selection.Partners[i];
                ReadSoloBattleLoadout(partners[i] as JObject, RequiredInteger(expected, "userid"),
                    RequiredInteger(expected, "arthur_type"), RequiredInteger(expected, "deck_idx"));
            }
            opening.ContinueAllowed = (bool)continueAllowed;
            return opening;
        }

        // Validate common fields shared by archived and current opening responses.
        private static CnSoloBattleStart ReadSoloBattleStart(JObject payload)
        {
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

        // Validate known ownership and identical card definitions before building the write.
        // Different definitions in the same family and equipment limits are validated by the service.
        public static string CreateDeckSetRequest(string sessionKey, CnDeck deck, long[] cards, IDictionary<long, CnCard> owned = null)
        {
            if (deck == null || deck.Wire == null || cards == null || cards.Length != deck.Cards.Length)
                throw new ArgumentException("卡组草稿无效。");
            var instances = new HashSet<long>();
            var definitions = new HashSet<long>();
            foreach (long card in cards)
            {
                if (card == 0) continue;
                if (card < 0) throw new ArgumentException("主卡组中有无效的卡牌。");
                if (!instances.Add(card)) throw new ArgumentException("同一张卡牌不能重复放入主卡组，请调整后再保存。");
                if (owned == null) continue;
                CnCard ownedCard;
                if (!owned.TryGetValue(card, out ownedCard)) throw new ArgumentException("主卡组中有未持有的卡牌。");
                if (!definitions.Add(ownedCard.CardId)) throw new ArgumentException("同种卡牌不能重复放入主卡组，请调整后再保存。");
            }
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
