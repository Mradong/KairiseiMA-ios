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
        private long questBattlePoints;
        private readonly List<List<int>> eventQuestGroups = new List<List<int>>();
        private List<int> eventQuestDifficulties;
        private List<CnSoloPartner> soloPartners;
        private readonly CnSoloPartyDraft soloParty = new CnSoloPartyDraft();
        private CnDeck selectedSoloDeck;
        private CnSoloPlayerLoadout selectedSoloLoadout;
        private bool showingDeck;
        private int deckIndex;
        private Vector2 deckScroll;
        private long[] deckDraft;
        private string deckError;
        private CnDeckSaveRecovery pendingDeckRecovery;
        private string pendingDeckServer;
        private long pendingDeckUserId;
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
        private Font font;
        private Texture2D partyAtlasTexture;
        private RecoveryAtlas partyAtlas;
        private RecoveryContentLoader content;
        private readonly CnEntryFlow entry = new CnEntryFlow();
        private CnEntryJournal entryJournal;
        private bool localInitializationFailed;
        private OriginalSceneRenderer originalScene;
        private GUIStyle startupLabel;
        private GUIStyle startupButton;
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
            font = Resources.Load<Font>("RecoveryChinese");
            if (font == null) font = Resources.Load<Font>("RecoveryFont");
            content = gameObject.AddComponent<RecoveryContentLoader>();
            content.RequiredBundles = new[] { "recovery/ui.dat", OriginalSceneRenderer.BundleName, OriginalNaviModel.BundleName };
            originalScene = gameObject.AddComponent<OriginalSceneRenderer>();
        }

        private IEnumerator Start()
        {
#if UNITY_IOS && !UNITY_EDITOR
            // The first screen is available before any server package has been downloaded.
            status = "连接服务器以下载游戏资源。";
            yield break;
#else
            // A local package is optional for editor fixtures. A clean checkout follows the server path.
            if (Resources.Load<TextAsset>("IosContentManifest") == null)
            {
                status = "连接服务器以下载游戏资源。";
                yield break;
            }
            busy = true;
            status = "正在检查并安装游戏资源…";
            yield return content.Initialize();
            try
            {
                if (!content.Ready) throw new InvalidOperationException(content.Error ?? "资源初始化未完成。");
                LoadPartyAtlas();
                originalScene.Initialize(content, font, this);
                status = "本地资源已就绪，请连接并登录。";
                localInitializationFailed = false;
            }
            catch (Exception error) { localInitializationFailed = true; status = "资源加载失败：" + error.Message; }
            finally { busy = false; }
            // The original title's OnStartLogin action starts the online flow.
#endif
        }

        private void OnGUI()
        {
            if (originalScene.Initialized) return;
            if (startupLabel == null)
            {
                startupLabel = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = true, alignment = TextAnchor.MiddleCenter };
                startupButton = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18 };
            }
            float scale = Screen.height / 640f;
            float width = Screen.width / scale;
            Rect panel = new Rect(width * 0.2f, 210, width * 0.6f, 220);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.Box(panel, GUIContent.none);
            string message = status;
            if (busy && entry.Step == CnEntryStep.Content && content.TotalBundles > 0)
                message += "\n" + String.Format("已校验 {0:N0} / {1:N0} MB（{2} / {3} 个包）",
                    content.VerifiedBytes / 1000000.0, content.TotalBytes / 1000000.0,
                    content.Installed + content.Reused, content.TotalBundles);
            GUI.Label(new Rect(panel.x + 20, panel.y + 15, panel.width - 40, 132), message, startupLabel);
            bool retry = entry.CanRetry || localInitializationFailed;
            if (!busy && GUI.Button(new Rect(panel.center.x - 60, panel.y + 155, 120, 44), retry ? "重试" : "连接", startupButton))
                BeginFromOriginalTitle();
            GUI.matrix = previous;
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
            showingDeck = false;
            quests = null; showingQuests = false;
            ClearSoloFormation();
            bonusKey = null;
            bonusNotice = "";
            serverContentReady = false;
            endpoint = null;
            verifiedAddress = null;
            try { endpoint = ServerEndpoint.Parse(address); }
            catch (Exception error) { EntryFailed(error); return; }
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
            showingDeck = false;
            quests = null; showingQuests = false;
            ClearSoloFormation();
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
                // A relogin can replace the session while keeping the same guest and server.
                if (pendingDeckRecovery != null && (pendingDeckServer != endpoint.ToString() || pendingDeckUserId != login.UserId))
                    pendingDeckRecovery = null;
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
            status = "正在下载并校验游戏资源…";
            yield return content.UpdateFromServer(requestedLogin);
            serverContentReady = content.Ready && login == requestedLogin;
            string uiError = null;
            bool firstScene = !originalScene.Initialized;
            if (serverContentReady)
            {
                try
                {
                    LoadPartyAtlas();
                    if (firstScene) originalScene.Initialize(content, font, this);
                    else originalScene.RefreshContent();
                    Debug.Log("Recovery server UI loaded: content=" + content.ActiveContentId + ", partyAtlas=" + (partyAtlas != null));
                }
                catch (Exception error) { serverContentReady = false; uiError = error.Message; }
            }
            busy = false;
            if (!serverContentReady) { EntryFailed(new InvalidOperationException("游戏资源加载失败：" + (uiError ?? content.Error))); yield break; }
            entry.ResourcesReady();
            Debug.Log("Recovery entry resources ready: installed=" + content.Installed + ", reused=" + content.Reused);
            // On first launch the player starts Connect from the downloaded original title.
            if (!firstScene) ReadConnect();
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

        // This callback is bound to the original Home card menu’s deck entry.
        public void OpenOriginalDeck()
        {
            if (busy || login == null || homeState == null || !originalScene.CanOpenWork()) return;
            ReadDecks();
        }

        // Read the authoritative deck before resuming any edit after a save attempt.
        private void ReadDecks()
        {
            if (busy || login == null || homeState == null || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/CardShow2"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "0", 1 } }), delegate(string response) {
                CnCardCollection current = CnProtocol.ParseCardShow(response);
                CnProtocol.SelectSoloDecks(current);
                cardCollection = current;
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
                ShowOriginalDeck();
                if (!String.IsNullOrEmpty(deckError)) originalScene.ShowWorkError(deckError, null);
            }, error => WorkReadFailed(error, ReadDecks)));
        }

        // The Home menu opens the original event list after the matching scene pack is available.
        public void OpenOriginalQuests()
        {
            if (busy || login == null || homeState == null || !originalScene.CanOpenWork()) return;
            ReadQuests();
        }

        // Listing quests can clear the server stage selection, so protect a pending local battle.
        private void ReadQuests()
        {
            if (busy || login == null || homeState == null || verifiedAddress != address) return;
            if (entryJournal == null || entryJournal.HasPendingBattle)
            {
                originalScene.ShowWorkError("请先处理未提交的战斗记录，再读取关卡。", null);
                return;
            }
            if (homeState.ArthurType < 1 || homeState.ArthurType > 5) { originalScene.ShowWorkError("服务器返回的当前职业无效。", null); return; }
            CnLogin requestedLogin = login;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/TeamBattleSoloShow"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "0", homeState.ArthurType } }), delegate(string response) {
                quests = CnProtocol.ParseSoloQuests(response, out long? points);
                questBattlePoints = points ?? homeState.Bp;
                questScroll = Vector2.zero; questIndex = 0; showingQuests = true;
                ClearSoloFormation();
                status = "已读取服务器单人关卡。";
                BindEventQuestGroups();
                ShowEventGroups();
            }, error => WorkReadFailed(error, ReadQuests)));
        }

        // Keep event categories distinct even when the server reuses a group ID.
        private void BindEventQuestGroups()
        {
            eventQuestGroups.Clear();
            var groups = new Dictionary<(string, long), List<int>>();
            for (int index = 0; index < quests.Count; index++)
            {
                CnQuest quest = quests[index];
                if (quest.Category == "普通") continue;
                var key = (quest.Category, quest.GroupId);
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new List<int>();
                    groups.Add(key, group);
                    eventQuestGroups.Add(group);
                }
                group.Add(index);
            }
            foreach (List<int> group in eventQuestGroups) group.Sort((a, b) => quests[b].BossId.CompareTo(quests[a].BossId));
        }

        // Readable labels are projected onto the original group tiles, then the selected difficulty tiles.
        private void ShowEventGroups()
        {
            if (eventQuestGroups.Count == 0)
            {
                originalScene.ShowWorkError("服务器当前没有活动副本。", null);
                return;
            }
            originalScene.OpenWorkPicker("QuestGroups", eventQuestGroups.Count, index =>
            {
                CnQuest entry = quests[eventQuestGroups[index][0]];
                return new Dictionary<string, string> { { "1806657225351454", entry.GroupName }, { "1208575754630950", entry.Category } };
            }, index =>
            {
                eventQuestDifficulties = eventQuestGroups[index];
                ShowEventDifficulties();
            }, () => originalScene.Show("Home"));
        }

        private void ShowEventDifficulties()
        {
            originalScene.OpenWorkPicker("QuestDifficulties", eventQuestDifficulties.Count, index =>
            {
                CnQuest quest = quests[eventQuestDifficulties[index]];
                return new Dictionary<string, string>
                {
                    { "1066778049879928", quest.Difficulty + (quest.Locked != 0 ? " · 未解锁" : quest.EntryRules.AllowsSolo ? "" : " · 仅多人") },
                    { "1294885792020211", quest.BpCost.ToString() },
                };
            }, index =>
            {
                if (busy) return;
                questIndex = eventQuestDifficulties[index];
                ReadSoloPartners();
            }, () => { if (!busy) ShowEventGroups(); });
        }

        // Apply the exact borrowed deck identity from the server list.
        private void ToggleQuestPartner(int index)
        {
            if (!busy && ToggleSoloDeck(soloPartners[index])) originalScene.RefreshWorkPicker();
        }

        // Reject choices that cannot fit any available slot, retaining the existing formation.
        private bool ToggleSoloDeck(CnSoloPartner candidate)
        {
            if (soloParty.Remove(candidate)) return true;
            try
            {
                soloParty.Add(candidate);
                if (soloParty.CanComplete(quests[questIndex], login.UserId, selectedSoloDeck)) return true;
                soloParty.Remove(candidate);
                throw new InvalidOperationException("该卡组不符合当前队伍的职业或来源要求。");
            }
            catch (InvalidOperationException error) { originalScene.ShowWorkError(error.Message, null); return false; }
        }

        // Reopen the same party after browsing decks. The chosen deck and helpers belong to this stage.
        private void ShowQuestPartners()
        {
            CnSoloEntryRules rules = quests[questIndex].EntryRules;
            var selectedOwnNames = new List<string>();
            for (int index = 0; index < soloParty.Count; index++)
                if (soloParty[index].UserId == login.UserId) selectedOwnNames.Add(soloParty[index].Name);
            string ownCaption = selectedOwnNames.Count == 0 ? "选择自有队友卡组" : "已选自有队友\n" + String.Join("\n", selectedOwnNames);
            originalScene.OpenQuestPartnerPicker(soloPartners.Count, index =>
            {
                CnSoloPartner partner = soloPartners[index];
                return new Dictionary<string, string>
                {
                    { "1771092198208730", partner.Name },
                    { "1649986673806370", (soloParty.Contains(partner) ? "已选  " : "") + "Lv." + partner.Level },
                };
            }, ToggleQuestPartner, () =>
            {
                if (busy) return;
                ClearSoloFormation();
                ShowEventDifficulties();
            }, selectedSoloDeck == null ? "选择我方卡组" : "已选卡组\n" + selectedSoloDeck.Name + "\n生命值 " + selectedSoloLoadout.Health,
                () => ReadSoloDecks(false), ownCaption, rules.AllowsPartnerSource(true) ? (Action)(() => ReadSoloDecks(true)) : null);
        }

        // A different stage or a return to the stage list invalidates the selected loadout and all partner slots.
        private void ClearSoloFormation()
        {
            soloPartners = null;
            soloParty.Clear();
            selectedSoloDeck = null;
            selectedSoloLoadout = null;
        }

        // Enter the local party editor. Own-deck and player-only stages skip the rental read.
        private void ReadSoloPartners()
        {
            if (busy || login == null || quests == null || questIndex < 0 || questIndex >= quests.Count || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            CnQuest selected = quests[questIndex];
            string restriction = SoloStageRestriction(selected);
            if (restriction != null)
            {
                originalScene.ShowWorkError(restriction, null);
                return;
            }
            ClearSoloFormation();
            if (!selected.EntryRules.AllowsPartnerSource(false))
            {
                soloPartners = new List<CnSoloPartner>();
                ShowQuestPartners();
                return;
            }
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/TeamBattleSoloPartnerShow"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "bossid", selected.BossId } }), delegate(string response) {
                soloPartners = CnProtocol.ParseSoloPartners(response);
                soloPartners.RemoveAll(partner => partner.UserId == requestedLogin.UserId);
                status = "已读取关卡队友。";
                ShowQuestPartners();
            }, error => WorkReadFailed(error, ReadSoloPartners)));
        }

        // Check cached publication, permissions and the last observed balance without sending battle admission.
        private string SoloStageRestriction(CnQuest selected)
        {
            if (selected.Locked != 0) return "此关卡尚未解锁。";
            if (selected.State < 0 || selected.State > 2 || selected.BpCost < 0) return "此关卡当前无法进入。";
            if (!selected.EntryRules.AllowsSolo) return "此关卡仅支持多人战斗。";
            if (selected.BpCost > questBattlePoints) return "当前战斗点不足。";
            return null;
        }

        // Refresh inventory from the current session before browsing primary or self-partner decks.
        private void ReadSoloDecks(bool forPartner)
        {
            if (busy || login == null || soloPartners == null || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/CardShow2"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, new JObject { { "0", 1 } }), delegate(string response) {
                CnCardCollection collection = CnProtocol.ParseCardShow(response);
                List<CnDeck> decks = CnProtocol.SelectSoloDecks(collection);
                decks.RemoveAll(deck => !UsableSoloDeck(collection, deck));
                if (selectedSoloDeck != null)
                {
                    CnDeck refreshed = decks.Find(deck => deck.ArthurType == selectedSoloDeck.ArthurType && deck.Index == selectedSoloDeck.Index);
                    if (selectedSoloLoadout.Matches(refreshed, collection)) selectedSoloDeck = refreshed;
                    else { selectedSoloDeck = null; selectedSoloLoadout = null; }
                }
                soloParty.ReconcileOwn(requestedLogin.UserId, decks);
                if (forPartner) decks.RemoveAll(deck => deck.ArthurType > 4);
                else decks.RemoveAll(deck => !quests[questIndex].EntryRules.AllowsPlayer(deck.ArthurType));
                if (decks.Count == 0)
                {
                    ShowQuestPartners();
                    originalScene.ShowWorkError("服务器没有可选择的卡组。", null);
                    return;
                }
                int index = selectedSoloDeck != null ? decks.IndexOf(selectedSoloDeck) : decks.FindIndex(deck => deck.ArthurType == homeState.ArthurType);
                ShowSoloDeck(collection, decks, Math.Max(0, index), forPartner);
            }, error => WorkReadFailed(error, () => ReadSoloDecks(forPartner))));
        }

        // The battle loadout needs ten instantiated main cards. The server falls back to the first card if the leader slot is invalid.
        private static bool UsableSoloDeck(CnCardCollection collection, CnDeck deck)
        {
            if (deck.Cards.Length != 10) return false;
            var definitions = new HashSet<long>();
            foreach (long id in deck.Cards)
                if (!collection.Cards.TryGetValue(id, out CnCard card) || card.Level <= 0 || !definitions.Add(card.CardId)) return false;
            long leader = deck.Wire["3"]?.Type == JTokenType.Integer ? (long)deck.Wire["3"] : 0;
            if (leader < 0 || leader >= deck.Cards.Length) leader = 0;
            JToken fame = collection.Cards[deck.Cards[leader]].Wire?["17"];
            return fame == null || (fame.Type == JTokenType.Integer && (long)fame >= 1 && (long)fame <= 100);
        }

        // Paging is read-only. Confirm either updates the leader or toggles an owned team member.
        private void ShowSoloDeck(CnCardCollection collection, List<CnDeck> decks, int index, bool forPartner)
        {
            CnDeck deck = decks[index];
            var selfPartner = new CnSoloPartner { UserId = login.UserId, ArthurType = deck.ArthurType, RentalIndex = deck.Index, Name = deck.Name };
            string title = forPartner && soloParty.Contains(selfPartner) ? "已选 · " + deck.Name : deck.Name;
            originalScene.ShowQuestDeck(title, Array.ConvertAll(deck.Cards, id => originalScene.DescribeDeckCard(collection, id)),
                index > 0 ? (Action)(() => { if (!busy) ShowSoloDeck(collection, decks, index - 1, forPartner); }) : null,
                index + 1 < decks.Count ? (Action)(() => { if (!busy) ShowSoloDeck(collection, decks, index + 1, forPartner); }) : null,
                () =>
                {
                    if (busy) return;
                    if (forPartner)
                    {
                        if (ToggleSoloDeck(selfPartner)) ShowQuestPartners();
                        return;
                    }
                    if (!soloParty.CanComplete(quests[questIndex], login.UserId, deck))
                    {
                        originalScene.ShowWorkError("该队长卡组与已选队友不匹配，请调整编队。", null);
                        return;
                    }
                    ReadSoloPlayerLoadout(collection, deck);
                }, () => { if (!busy) ShowQuestPartners(); });
        }

        // Confirm the primary deck only after the authenticated service validates the exact selection.
        private void ReadSoloPlayerLoadout(CnCardCollection reviewedInventory, CnDeck deck)
        {
            if (busy || login == null || soloPartners == null || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            string request = CnProtocol.CreateSoloPlayerLoadoutRequest(requestedLogin.SessionKey, deck);
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/TeamBattleSoloPlayerDeckShow"), request, response =>
            {
                CnSoloPlayerLoadout loadout = CnProtocol.ParseSoloPlayerLoadout(response, requestedLogin.UserId, deck);
                if (!loadout.Matches(deck, reviewedInventory))
                {
                    originalScene.ShowWorkError("卡组已变更，请重新读取卡组。", () => ReadSoloDecks(false));
                    return;
                }
                selectedSoloDeck = deck;
                selectedSoloLoadout = loadout;
                ShowQuestPartners();
            }, error =>
            {
                if (error is CnTransportException transport && transport.HttpStatus == 404)
                    originalScene.ShowWorkError("服务器尚未提供我方战斗卡组，请更新测试服务后重试。", () => ReadSoloPlayerLoadout(reviewedInventory, deck));
                else WorkReadFailed(error, () => ReadSoloPlayerLoadout(reviewedInventory, deck));
            }));
        }

        // Emit an unsent start request only after validating the final formation and its local battle marker.
        private string CreateSelectedSoloStartRequest()
        {
            if (busy || login == null || soloPartners == null || selectedSoloDeck == null || selectedSoloLoadout == null ||
                selectedSoloLoadout.UserId != login.UserId || verifiedAddress != address)
                throw new InvalidOperationException("请先完成副本编队。");
            if (entryJournal == null || entryJournal.HasPendingBattle) throw new InvalidOperationException("请先处理未提交的战斗记录。");
            CnQuest quest = quests[questIndex];
            string restriction = SoloStageRestriction(quest);
            if (restriction != null) throw new InvalidOperationException(restriction);
            return soloParty.CreateStartRequest(login.SessionKey, quest, login.UserId, selectedSoloDeck);
        }

        private void ShowOriginalDeck()
        {
            if (deckDraft == null || deckDraft.Length == 0 || deckDraft.Length > 10)
            {
                originalScene.ShowWorkError("服务器未返回可显示的主卡组。", null);
                return;
            }
            CnDeck deck = cardCollection.Decks[deckIndex];
            var visuals = Array.ConvertAll(deckDraft, id => originalScene.DescribeDeckCard(cardCollection, id));
            originalScene.ShowDeckEditor(deck.Name, visuals, ShowOwnedDeckInventory,
                deckIndex > 0 ? (Action)(() => ChangeOriginalDeck(-1)) : null,
                deckIndex + 1 < cardCollection.Decks.Count ? (Action)(() => ChangeOriginalDeck(1)) : null,
                deckDirty ? (Action)SaveDeck : null,
                () => LeaveOriginalDeck(() => originalScene.Show("Home")));
        }

        // A deck switch and a Home return use the same discard confirmation.
        private void LeaveOriginalDeck(Action leave)
        {
            if (deckDirty) originalScene.ConfirmDeckDiscard(leave);
            else leave();
        }

        private void ChangeOriginalDeck(int direction)
        {
            if (cardCollection == null) return;
            int next = deckIndex + direction;
            if (next < 0 || next >= cardCollection.Decks.Count) return;
            LeaveOriginalDeck(() => { deckIndex = next; PrepareDeckDraft(); ShowOriginalDeck(); });
        }

        private void ShowOwnedDeckInventory(int slot)
        {
            if (deckDraft == null || slot < 0 || slot >= deckDraft.Length) return;
            editSlot = slot;
            originalScene.OpenDeckInventory(cardChoices.Count, index => originalScene.DescribeDeckCard(cardCollection, cardChoices[index]),
                ApplyOwnedDeckCard, ShowOriginalDeck);
        }

        // Moving an already-equipped copy swaps its original slot. Different copies of the
        // same definition are validated by the same rules used for the outgoing write.
        private void ApplyOwnedDeckCard(int index)
        {
            long id = cardChoices[index];
            long[] changed = (long[])deckDraft.Clone();
            int existing = id == 0 ? -1 : Array.IndexOf(changed, id);
            if (existing >= 0) changed[existing] = changed[editSlot];
            changed[editSlot] = id;
            CnDeck deck = cardCollection.Decks[deckIndex];
            try { CnProtocol.CreateDeckSetRequest(login.SessionKey, deck, changed, cardCollection.Cards); }
            catch (Exception error) when (error is FormatException || error is ArgumentException)
            {
                originalScene.ShowWorkError(error.Message, null);
                return;
            }
            deckDraft = changed;
            deckDirty = false;
            for (int slot = 0; slot < changed.Length; slot++) if (changed[slot] != deck.Cards[slot]) { deckDirty = true; break; }
            ShowOriginalDeck();
        }

        private void PrepareDeckDraft()
        {
            deckScroll = Vector2.zero; editSlot = 0; cardChoice = 0; deckDirty = false; deckError = null;
            deckDraft = cardCollection.Decks.Count == 0 ? null : (long[])cardCollection.Decks[deckIndex].Cards.Clone();
            cardChoices = new List<long>(cardCollection.Cards.Keys); cardChoices.Sort(); cardChoices.Insert(0, 0);
        }

        // Snapshot the submitted draft before sending. Recovery always retries a read.
        private void SaveDeck()
        {
            if (busy || login == null || cardCollection == null || deckDraft == null || !deckDirty || verifiedAddress != address) return;
            CnDeck deck = cardCollection.Decks[deckIndex];
            string request;
            try { request = CnProtocol.CreateDeckSetRequest(login.SessionKey, deck, deckDraft, cardCollection.Cards); }
            catch (ArgumentException error) { deckError = status = error.Message; originalScene.ShowWorkError(status, null); return; }
            catch (FormatException error) { deckError = status = error.Message; originalScene.ShowWorkError(status, null); return; }
            pendingDeckRecovery = new CnDeckSaveRecovery(deck, deckDraft);
            pendingDeckServer = endpoint.ToString();
            pendingDeckUserId = login.UserId;
            showingDeck = false; cardCollection = null; deckDraft = null;
            StartCoroutine(Send("POST", login.Api.Route("/CardDeckSet"), request, delegate(string response) {
                CnProtocol.ParseEnvelope(response);
                ReadDecks();
            }, error => WorkReadFailed(error, ReadDecks, "保存结果未确认。请重新读取卡组后检查。")));
        }

        // Expired sessions return to the entry flow. Other failures offer the idempotent read.
        private void WorkReadFailed(Exception error, Action retry, string message = null)
        {
            if ((error is CnTransportException transport && transport.HttpStatus == 401) ||
                (error is CnProtocolException protocol && protocol.Code == -3208))
            {
                EntryFailed(error);
                return;
            }
            status = message ?? error.Message;
            originalScene.ShowWorkError(status, retry);
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
            catch (Exception error)
            {
                if (entry.Step == CnEntryStep.Create) EntryFailed(error);
                else
                {
                    status = error is ArgumentException ? "请输入不含空格的角色名称。" : error.Message;
                    originalScene.ShowError(status);
                }
                return;
            }
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
            if (entry.Step == CnEntryStep.Connect && connectState == null) { ReadConnect(); return; }
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
