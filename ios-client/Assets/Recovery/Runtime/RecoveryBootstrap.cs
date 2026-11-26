using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Kairisei.Recovery
{
    public sealed class RecoveryBootstrap : MonoBehaviour
    {
        private string address;
        private string status = "输入服务器地址后连接并登录。";
        private string verifiedAddress;
        private bool busy;
        private ServerEndpoint endpoint;
        private CnLogin login;
        private CnConnectState connectState;
        private CnHomeState homeState;
        private CnCardCollection cardCollection;
        private List<CnQuest> quests;
        private bool showingQuests;
        private Vector2 questScroll;
        private int questIndex;
        private List<CnSoloPartner> soloPartners;
        private int soloPartnerIndex;
        private readonly CnSoloPartyDraft soloParty = new CnSoloPartyDraft();
        private List<CnDeck> soloDecks;
        private int soloDeckIndex;
        private bool showingDeck;
        private int deckIndex;
        private Vector2 deckScroll;
        private long[] deckDraft;
        private string deckError;
        private CnDeckSaveRecovery pendingDeckRecovery;
        private List<long> cardChoices;
        private int editSlot;
        private int cardChoice;
        private bool deckDirty;
        private string bonusKey;
        private string bonusNotice = "";
        private Vector2 bonusScroll;
        private string characterName = "";
        private int arthurType = 1;
        private bool serverContentReady;
        private UnityWebRequest pending;
        private Texture2D title;
        private Font font;
        private Texture2D partyAtlasTexture;
        private RecoveryAtlas partyAtlas;
        private RecoveryContentLoader content;
        private readonly CnEntryFlow entry = new CnEntryFlow();
        private CnEntryJournal entryJournal;
        private bool localInitializationFailed;
        private OriginalSceneRenderer originalScene;
        public CnEntryStep OriginalEntryStep { get { return entry.Step; } }
        public CnHomeState OriginalHome { get { return homeState; } }
        public bool OriginalBusy { get { return busy; } }
        public string OriginalMessage { get { return status; } }
        public string OriginalCharacterName { get { return characterName; } }
        public int OriginalArthurRole { get { return arthurType; } }
        public string OriginalBonusNotice { get { return bonusNotice; } }
        public bool OriginalCanRetry { get { return entry.CanRetry || localInitializationFailed; } }
        public string OriginalServerAddress { get { return address; } }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            address = PlayerPrefs.GetString("ios-recovery.server", "http://47.116.51.61:27040");
            title = Resources.Load<Texture2D>("RecoveryTitle");
            font = Resources.Load<Font>("RecoveryChinese");
            if (font == null) font = Resources.Load<Font>("RecoveryFont");
            content = gameObject.AddComponent<RecoveryContentLoader>();
            content.RequiredBundles = new[] { OriginalSceneRenderer.BundleName,OriginalNaviModel.BundleName };
            originalScene = gameObject.AddComponent<OriginalSceneRenderer>();
        }

        private IEnumerator Start()
        {
            busy = true;
            status = "正在检查并安装游戏资源…";
            yield return content.Initialize();
            try
            {
                if (!content.Ready) throw new InvalidOperationException(content.Error ?? "资源初始化未完成。");
                title = content.Load<Texture2D>("recovery/ui.dat", "title-background");
                LoadPartyAtlas();
                originalScene.Initialize(content, font, this);
                status = "本地资源已就绪，请连接并登录。";
                localInitializationFailed = false;
            }
            catch (Exception error) { localInitializationFailed = true; status = "资源加载失败：" + error.Message; }
            finally { busy = false; }
            // The original title's OnStartLogin action starts the online flow.
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (pending != null) { pending.Abort(); pending.Dispose(); pending = null; }
            login = null;
        }

        private void LoadPartyAtlas()
        {
            partyAtlasTexture = null; partyAtlas = null;
            try
            {
                Texture2D texture = content.Load<Texture2D>("recovery/ui.dat", "party");
                TextAsset frames = content.Load<TextAsset>("recovery/ui.dat", "party-frames");
                RecoveryAtlas atlas = new RecoveryAtlas(frames.text, texture.width, texture.height);
                for (int role = 1; role <= 4; role++) atlas.Frame("BattleLo_Job0" + role + "_01.png");
                partyAtlasTexture = texture; partyAtlas = atlas;
            }
            catch (Exception error) { Debug.LogWarning("Party atlas unavailable: " + error.Message); }
        }

        private void CheckHealth()
        {
            if (busy) return;
            if (localInitializationFailed) { StartCoroutine(Start()); return; }
            entry.BeginHealth(); entryJournal = null;
            login = null;
            connectState = null;
            homeState = null;
            cardCollection = null;
            pendingDeckRecovery = null;
            showingDeck = false;
            quests = null; showingQuests = false;
            bonusKey = null;
            bonusNotice = "";
            serverContentReady = false;
            endpoint = null;
            verifiedAddress = null;
            try { endpoint = ServerEndpoint.Parse(address); }
            catch (Exception error) { status = error.Message; return; }
            string requestedAddress = address;
            StartCoroutine(Send("GET", endpoint.Route("/healthz"), null, delegate(string response) {
                int battlePort = CnProtocol.ParseHealth(response);
                verifiedAddress = requestedAddress;
                PlayerPrefs.SetString("ios-recovery.server", endpoint.ToString());
                PlayerPrefs.Save();
                status = "连接成功，正在登录…";
                LoginGuest();
            }, EntryFailed));
        }

        private void LoginGuest()
        {
            if (busy || endpoint == null || verifiedAddress != address) return;
            entry.BeginLogin();
            login = null;
            connectState = null;
            homeState = null;
            cardCollection = null;
            pendingDeckRecovery = null;
            showingDeck = false;
            quests = null; showingQuests = false;
            bonusKey = null;
            bonusNotice = "";
            serverContentReady = false;
            string key;
            using (SHA256 hash = SHA256.Create())
                key = "ios-recovery.guest." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(endpoint.ToString()))).Replace("-", "");
            string uuid = PlayerPrefs.GetString(key, "");
            if (String.IsNullOrEmpty(uuid)) uuid = Guid.NewGuid().ToString("D");
            try { uuid = CnProtocol.NormalizeGuestUuid(uuid); }
            catch (Exception error) { EntryFailed(error); return; }
            PlayerPrefs.SetString(key, uuid); PlayerPrefs.Save();
            StartCoroutine(Send("POST", endpoint.Route("/local/client/login"), CnProtocol.CreateLogin(uuid, Application.unityVersion), delegate(string response) {
                entry.ReceiveLogin(response, endpoint);
                login = entry.Login;
                entryJournal = new CnEntryJournal(Application.persistentDataPath, endpoint, login.UserId);
                using (SHA256 hash = SHA256.Create())
                    bonusKey = "ios-recovery.bonus." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(endpoint.ToString() + "|" + login.UserId))).Replace("-", "");
                bonusNotice = PlayerPrefs.GetString(bonusKey, "");
                status = "登录成功，正在检查游戏资源…";
                StartCoroutine(UpdateServerContent(login));
            }, EntryFailed));
        }

        private IEnumerator UpdateServerContent(CnLogin requestedLogin)
        {
            busy = true;
            yield return content.UpdateFromServer(requestedLogin);
            serverContentReady = content.Ready && login == requestedLogin;
            string uiError = null;
            if (serverContentReady)
            {
                try
                {
                    title = content.Load<Texture2D>("recovery/ui.dat", "title-background");
                    LoadPartyAtlas();
                    Debug.Log("Recovery server UI loaded: title=" + title.width + "x" + title.height + ", partyAtlas=" + (partyAtlas != null));
                }
                catch (Exception error) { serverContentReady = false; uiError = error.Message; }
            }
            busy = false;
            if (!serverContentReady) { EntryFailed(new InvalidOperationException("游戏资源加载失败：" + (uiError ?? content.Error))); yield break; }
            entry.ResourcesReady();
            Debug.Log("Recovery entry resources ready: installed=" + content.Installed + ", reused=" + content.Reused);
            ReadConnect();
        }

        private void ReadConnect()
        {
            if (busy || login == null || !serverContentReady || verifiedAddress != address) return;
            entry.BeginConnect();
            StartCoroutine(Send("POST", login.Api.Route("/Connect"), CnProtocol.CreateSessionRequest(login.SessionKey, new JObject()), delegate(string response) {
                entry.ReceiveConnect(response);
                connectState = entry.CharacterState;
                if (entryJournal != null) entryJournal.ConfirmCharacterState();
                status = connectState.UserCreated ? "已有角色已确认，可以继续进入主页。" : "请输入名称并选择初始职业，然后创建角色。";
                if(connectState.UserCreated)originalScene.ConfirmHomeEntry();
            }, EntryFailed));
        }

        private void ReadHome()
        {
            if (busy || login == null || connectState == null || !connectState.UserCreated || !serverContentReady || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            bool previousUnconfirmed;
            try
            {
                if (originalScene == null || !originalScene.CanPresentHome)
                    throw new InvalidOperationException("主页场景资源尚未完整就绪，请先完成资源更新。");
                if (entryJournal == null) throw new InvalidOperationException("登录记录尚未就绪，请重新登录。");
                entry.BeginHome(entryJournal.HasPendingBattle);
                previousUnconfirmed = entryJournal.BeginHome();
            }
            catch (Exception error)
            {
                if (entry.Step == CnEntryStep.PendingBattle)
                {
                    status = error.Message;
                    originalScene.ShowError(status);
                }
                else EntryFailed(error);
                return;
            }
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/HomeShow"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, null), delegate(string response) {
                entry.ReceiveHome(response);
                homeState = entry.Home;
                if (previousUnconfirmed)
                {
                    const string uncertain = "上次主页请求结果未确认，奖励可能已发放；本次显示服务器当前状态。";
                    if (!bonusNotice.Contains(uncertain)) bonusNotice = String.IsNullOrEmpty(bonusNotice) ? uncertain : bonusNotice + "\n" + uncertain;
                }
                if (!String.IsNullOrEmpty(homeState.BonusNotice))
                {
                    bonusNotice = String.IsNullOrEmpty(bonusNotice) ? homeState.BonusNotice : bonusNotice + "\n" + homeState.BonusNotice;
                }
                if (!String.IsNullOrEmpty(bonusNotice)) { PlayerPrefs.SetString(bonusKey, bonusNotice); PlayerPrefs.Save(); }
                if(originalScene!=null)originalScene.NotifyHomeUpdated();
                status = "主页已更新。";
                try { entryJournal.ConfirmHome(); }
                catch (Exception) { status = "主页已读取，本地确认记录未能保存。重新登录后请核对奖励。"; }
            }, EntryFailed));
        }

        private void ReadDecks()
        {
            if (login == null || homeState == null || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/CardShow2"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "0", 1 } }), delegate(string response) {
                cardCollection = CnProtocol.ParseCardShow(response);
                deckIndex = 0; PrepareDeckDraft(); showingDeck = true;
                status = "已读取服务器卡组；卡牌名称与插画仍在恢复。";
                if (pendingDeckRecovery != null)
                {
                    int recoveredIndex; long[] recoveredDraft;
                    CnDeckRecoveryResult result = pendingDeckRecovery.Reconcile(cardCollection, out recoveredIndex, out recoveredDraft);
                    pendingDeckRecovery = null;
                    if (result == CnDeckRecoveryResult.Saved)
                    {
                        deckIndex = recoveredIndex; PrepareDeckDraft();
                        deckError = "已重新读取，主卡组与上次提交一致。";
                    }
                    else if (result == CnDeckRecoveryResult.RestoreDraft)
                    {
                        deckIndex = recoveredIndex; PrepareDeckDraft(); deckDraft = recoveredDraft; deckDirty = true;
                        deckError = "服务器主卡组未变化，已恢复未保存草稿。";
                    }
                    else deckError = "服务器卡组或库存已变化，请重新编辑。";
                    status = deckError;
                }
            }));
        }

        private void ReadQuests()
        {
            if (login == null || homeState == null || verifiedAddress != address) return;
            if (homeState.ArthurType < 1 || homeState.ArthurType > 5) { status = "服务器返回的当前职业无效。"; return; }
            CnLogin requestedLogin = login;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/TeamBattleSoloShow"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "0", homeState.ArthurType } }), delegate(string response) {
                quests = CnProtocol.ParseSoloQuests(response);
                questScroll = Vector2.zero; questIndex = 0; soloPartners = null; soloParty.Clear(); soloDecks = null; showingQuests = true;
                status = "已读取服务器单人关卡；进入战斗尚未接入。";
            }));
        }

        private string QuestText()
        {
            if (quests.Count == 0) return "服务器没有返回可显示的单人关卡。";
            var text = new StringBuilder();
            if (soloDecks != null && soloDecks.Count > 0)
                text.Append("我方卡组：").Append(soloDecks[soloDeckIndex].Name).Append("  职业 ").Append(soloDecks[soloDeckIndex].ArthurType).AppendLine().AppendLine();
            else text.AppendLine("请先读取卡组。").AppendLine();
            for (int i = 0; i < quests.Count; i++)
            {
                CnQuest quest = quests[i];
                text.Append(i == questIndex ? "▶ " : "   ").Append(quest.Category).Append("  ").Append(quest.GroupName).Append("  ").Append(quest.Difficulty)
                    .Append("  战斗点 ").Append(quest.BpCost).Append(quest.Locked != 0 ? "  已锁定" : "").AppendLine();
            }
            if (soloPartners != null)
            {
                text.AppendLine().AppendLine("可选队友：");
                foreach (CnSoloPartner partner in soloPartners)
                    text.Append("职业 ").Append(partner.ArthurType).Append("  ").Append(partner.Name).Append("  等级 ").Append(partner.Level).AppendLine();
            }
            if (soloParty.Count > 0)
            {
                text.AppendLine().AppendLine("已选队友：");
                for (int i = 0; i < soloParty.Count; i++) text.Append(soloParty[i].Name).Append("  职业 ").Append(soloParty[i].ArthurType).AppendLine();
            }
            return text.ToString();
        }

        private void ReadSoloPartners()
        {
            if (busy || login == null || quests == null || questIndex < 0 || questIndex >= quests.Count || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            CnQuest selected = quests[questIndex];
            soloPartners = null; soloParty.Clear();
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/TeamBattleSoloPartnerShow"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "bossid", selected.BossId } }), delegate(string response) {
                soloPartners = CnProtocol.ParseSoloPartners(response);
                soloPartnerIndex = 0;
                status = "已读取关卡队友；战斗入口尚未接入。";
            }));
        }

        private void ReadSoloDecks()
        {
            if (busy || login == null || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            soloDecks = null;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/CardShow2"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "0", 1 } }), delegate(string response) {
                CnCardCollection collection = CnProtocol.ParseCardShow(response);
                soloDecks = CnProtocol.SelectSoloDecks(collection);
                soloDeckIndex = 0;
                status = soloDecks.Count == 0 ? "服务器没有可选择的卡组。" : "已读取我方卡组；入场条件仍由服务器校验。";
            }));
        }

        private void PrepareDeckDraft()
        {
            deckScroll = Vector2.zero; editSlot = 0; cardChoice = 0; deckDirty = false; deckError = null;
            deckDraft = cardCollection.Decks.Count == 0 ? null : (long[])cardCollection.Decks[deckIndex].Cards.Clone();
            cardChoices = new List<long>(cardCollection.Cards.Keys); cardChoices.Sort(); cardChoices.Insert(0, 0);
        }

        private void SaveDeck()
        {
            if (busy || login == null || cardCollection == null || deckDraft == null || !deckDirty || verifiedAddress != address) return;
            string request;
            try { request = CnProtocol.CreateDeckSetRequest(login.SessionKey, cardCollection.Decks[deckIndex], deckDraft); }
            catch (Exception error) { deckError = error.Message; status = error.Message; return; }
            CnLogin requestedLogin = login;
            CnDeckSaveRecovery recovery = new CnDeckSaveRecovery(cardCollection.Decks[deckIndex], deckDraft);
            pendingDeckRecovery = null;
            // After any transport result, force a fresh read before another edit.
            showingDeck = false; cardCollection = null; deckDraft = null;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/CardDeckSet"), request, delegate(string response) {
                CnProtocol.ParseEnvelope(response);
                status = "服务器已确认保存。请重新读取卡组以查看当前状态。";
            }, delegate(Exception error) {
                pendingDeckRecovery = recovery;
                status = "保存结果未确认，请重新读取卡组后检查。";
            }));
        }

        private string DeckText()
        {
            if (cardCollection.Decks.Count == 0) return "服务器没有返回卡组。";
            CnDeck deck = cardCollection.Decks[deckIndex];
            var text = new StringBuilder();
            text.AppendLine(deck.Name + "  (" + (deckIndex + 1) + "/" + cardCollection.Decks.Count + ")");
            text.AppendLine("主卡组：");
            for (int i = 0; i < deck.Cards.Length; i++)
            {
                CnCard card;
                long id = deckDraft[i];
                text.Append(i + 1).Append(". ");
                if (id == 0) text.AppendLine("空");
                else if (cardCollection.Cards.TryGetValue(id, out card)) text.Append("卡牌 ").Append(card.CardId).Append("  等级 ").AppendLine(card.Level.ToString());
                else text.Append("未找到卡牌，唯一编号 ").AppendLine(id.ToString());
            }
            text.AppendLine("支援卡组：");
            for (int i = 0; i < deck.Support.Length; i++)
            {
                CnCard card;
                long id = deck.Support[i];
                text.Append(i + 1).Append(". ");
                if (id == 0) text.AppendLine("空");
                else if (cardCollection.Cards.TryGetValue(id, out card)) text.Append("卡牌 ").Append(card.CardId).Append("  等级 ").AppendLine(card.Level.ToString());
                else text.Append("未找到卡牌，唯一编号 ").AppendLine(id.ToString());
            }
            text.AppendLine("球槽唯一编号：" + String.Join(", ", Array.ConvertAll(deck.Spheres, delegate(long id) { return id.ToString(); })));
            text.AppendLine("伙伴槽唯一编号：" + String.Join(", ", Array.ConvertAll(deck.Buddies, delegate(long id) { return id.ToString(); })));
            return text.ToString();
        }

        private void CreateCharacter()
        {
            if (busy || login == null || connectState == null || connectState.UserCreated || !serverContentReady || verifiedAddress != address) return;
            string body;
            try { body = entry.BeginCreate(characterName, arthurType); entryJournal.BeginCreate(); }
            catch (Exception error) { if (entry.Step == CnEntryStep.Create) EntryFailed(error); else status = error.Message; return; }
            CnLogin requestedLogin = login;
            // A timeout can follow a successful server commit: require Connect before another attempt.
            connectState = null;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/UserCreate"), body, delegate(string response) {
                entry.ReceiveCreated(response);
                ReadConnect();
            }, EntryFailed));
        }

        private void EntryFailed(Exception error)
        {
            entry.Fail(error);
            login = entry.Login;
            if (login == null) { connectState = null; homeState = null; serverContentReady = false; }
            status = entry.CreationMayHaveCommitted ? "创建结果未确认。请重试查询角色状态，确认后再继续。"
                : entry.HomeMayHaveCommitted ? "主页请求未完成，奖励可能已发放。重试会重新查询当前角色状态。"
                : error is CnProtocolException && ((CnProtocolException)error).Code == -3208 ? "登录已失效。请重试登录，将继续使用同一游客身份。"
                : error.Message;
            if(originalScene!=null&&originalScene.Initialized)originalScene.ShowError(status);
        }

        private void RetryEntry()
        {
            if (busy) return;
            if (localInitializationFailed) { StartCoroutine(Start()); return; }
            if (!entry.CanRetry) return;
            switch (entry.PrepareRetry())
            {
                case CnEntryStep.Health: CheckHealth(); break;
                case CnEntryStep.Login: LoginGuest(); break;
                case CnEntryStep.Content: StartCoroutine(UpdateServerContent(login)); break;
                case CnEntryStep.Connect: ReadConnect(); break;
                case CnEntryStep.HomePermission: ReadHome(); break;
            }
        }

        public void BeginFromOriginalTitle()
        {
            if (busy) return;
            if (entry.CanRetry || localInitializationFailed) { RetryEntry(); return; }
            if (login == null) { CheckHealth(); return; }
            if (connectState != null && connectState.UserCreated) ReadHome();
        }

        public void SelectOriginalArthur(int role)
        {
            if (!busy && entry.Step == CnEntryStep.Character && role >= 1 && role <= 4) arthurType = role;
        }
        public void SetOriginalCharacterName(string value)
        {
            if (busy || entry.Step != CnEntryStep.Character) return;
            characterName = value ?? "";
            if (characterName.Length > 10) characterName = characterName.Substring(0, 10);
        }
        public void CreateOriginalCharacter() { CreateCharacter(); }
        public void EnterOriginalHome() { ReadHome(); }
        public void RetryOriginalEntry() { RetryEntry(); }
        public void AcknowledgeOriginalBonus()
        {
            if(String.IsNullOrEmpty(bonusKey))return;
            PlayerPrefs.DeleteKey(bonusKey);PlayerPrefs.Save();bonusNotice="";
        }

        private IEnumerator Send(string method, Uri uri, string body, Action<string> receive, Action<Exception> failed = null)
        {
            if (busy) yield break;
            busy = true;
            status = "正在连接服务器…";
            using (UnityWebRequest request = new UnityWebRequest(uri.AbsoluteUri, method))
            {
                pending = request;
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 20;
                request.redirectLimit = 0;
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                }
                yield return request.SendWebRequest();
                Exception failure = null;
                string response = null;
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        string text = request.downloadHandler.text;
                        if (request.responseCode == 400 && text != null && text.Contains("no content for the requested platform"))
                            throw new InvalidOperationException("服务器尚未提供与此 Unity 版本匹配的 iOS 资源，需要先构建并配置 iOS 资源包。");
                        throw new CnTransportException(request.responseCode, "连接失败（HTTP " + request.responseCode + "）：" + request.error);
                    }
                    if (request.downloadHandler.data.Length > 4 * 1024 * 1024) throw new FormatException("验证响应超过大小限制。");
                    response = request.downloadHandler.text;
                }
                catch (Exception error) { failure = error; }
                finally { if (pending == request) { pending = null; busy = false; } }
                try
                {
                    if (failure == null) receive(response);
                    else if (failed != null) failed(failure);
                    else status = failure.Message;
                }
                catch (Exception error) { status = error.Message; if (failed != null) failed(error); }
            }
        }
    }
}
