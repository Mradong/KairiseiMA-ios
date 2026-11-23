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
        private string status = "输入服务器地址后检查连接。此工程尚未包含完整游戏玩法。";
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
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle inputStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            address = PlayerPrefs.GetString("ios-recovery.server", "");
            title = Resources.Load<Texture2D>("RecoveryTitle");
            font = Resources.Load<Font>("RecoveryChinese");
            if (font == null) font = Resources.Load<Font>("RecoveryFont");
            content = gameObject.AddComponent<RecoveryContentLoader>();
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
                status = "本地资源已就绪。新安装 " + content.Installed + "，缓存复用 " + content.Reused + "。输入服务器地址后检查连接。";
            }
            catch (Exception error) { status = "资源加载失败：" + error.Message; }
            finally { busy = false; }
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

        private void OnGUI()
        {
            if (title != null) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), title, ScaleMode.ScaleAndCrop);
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
            if (scale <= 0) return;
            bool showBonus = !String.IsNullOrEmpty(bonusNotice) && login != null && verifiedAddress == address;
            bool showDeck = showingDeck && cardCollection != null && login != null && verifiedAddress == address;
            bool showQuest = showingQuests && quests != null && login != null && verifiedAddress == address;
            Matrix4x4 previousMatrix = GUI.matrix;
            float left = safe.x + (safe.width - 1280f * scale) / 2f;
            float top = Screen.height - safe.yMax + (safe.height - 720f * scale) / 2f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(left, top, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            if (headingStyle == null)
            {
                headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, wordWrap = true };
                bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
                inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 25, alignment = TextAnchor.MiddleLeft };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 23 };
                if (font != null) { headingStyle.font = font; bodyStyle.font = font; inputStyle.font = font; buttonStyle.font = font; }
            }
            Color previousColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.9f);
            GUI.DrawTexture(new Rect(60, 65, 1160, 590), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(new Rect(100, 95, 1080, 60), "乖离性 MA · iOS 恢复验证", headingStyle);
            GUI.Label(new Rect(100, 167, 1080, 45), "服务器地址（例如 192.168.1.10:26040）", bodyStyle);
            GUI.enabled = !busy && !showBonus && !showDeck && !showQuest;
            address = GUI.TextField(new Rect(100, 220, 1080, 58), address, 300, inputStyle);
            if (GUI.Button(new Rect(100, 300, 320, 64), busy ? "连接中…" : "检查连接", buttonStyle)) CheckHealth();
            GUI.enabled = !busy && !showBonus && !showDeck && !showQuest && endpoint != null && verifiedAddress == address;
            if (GUI.Button(new Rect(450, 300, 350, 64), "创建／登录测试游客", buttonStyle)) LoginGuest();
            GUI.enabled = !busy && !showBonus && !showDeck && !showQuest && login != null && serverContentReady && verifiedAddress == address;
            if (GUI.Button(new Rect(830, 300, 350, 64), "检查角色状态", buttonStyle)) ReadConnect();
            GUI.enabled = true;
            GUI.Label(new Rect(100, 390, 1080, 125), status, bodyStyle);
            if (connectState != null && connectState.UserCreated && login != null && verifiedAddress == address && serverContentReady)
            {
                GUI.enabled = !busy && !showBonus && !showDeck && !showQuest;
                if (GUI.Button(new Rect(995, 465, 185, 54), "进入主页", buttonStyle)) ReadHome();
                if (homeState != null && GUI.Button(new Rect(460, 540, 320, 64), "查看单人关卡", buttonStyle)) ReadQuests();
                if (homeState != null && GUI.Button(new Rect(800, 540, 380, 64), "查看服务器卡组", buttonStyle)) ReadDecks();
                GUI.enabled = true;
                if (homeState != null)
                {
                    GUI.Label(new Rect(100, 465, 800, 55), homeState.Name + "  等级 " + homeState.Level + "  体力 " + homeState.Ap + "/" + homeState.ApMax + "  战斗点 " + homeState.Bp + "/" + homeState.BpMax + "  金币 " + homeState.Gold, bodyStyle);
                    if (partyAtlas != null && partyAtlasTexture != null && homeState.ArthurType >= 1 && homeState.ArthurType <= 4)
                    {
                        float[] uv = partyAtlas.Frame("BattleLo_Job0" + homeState.ArthurType + "_01.png");
                        GUI.DrawTextureWithTexCoords(new Rect(915, 462, 50, 52), partyAtlasTexture, new Rect(uv[0], uv[1], uv[2], uv[3]));
                    }
                }
            }
            if (connectState != null && !connectState.UserCreated && login != null && verifiedAddress == address && serverContentReady)
            {
                GUI.enabled = !busy && !showBonus && !showDeck && !showQuest;
                characterName = GUI.TextField(new Rect(100, 465, 280, 54), characterName, 32, inputStyle);
                string[] roles = { "佣兵", "富豪", "盗贼", "歌姬" };
                for (int i = 0; i < roles.Length; i++)
                    if (GUI.Button(new Rect(400 + i * 145, 465, 135, 54), (arthurType == i + 1 ? "✓ " : "") + roles[i], buttonStyle)) arthurType = i + 1;
                if (GUI.Button(new Rect(995, 465, 185, 54), "创建角色", buttonStyle)) CreateCharacter();
                GUI.enabled = true;
            }
            if (showDeck && !showBonus)
            {
                GUI.enabled = true;
                GUI.color = new Color(0, 0, 0, 0.96f);
                GUI.DrawTexture(new Rect(200, 90, 880, 540), Texture2D.whiteTexture);
                GUI.color = previousColor;
                string deckText = DeckText();
                GUI.Label(new Rect(240, 115, 800, 50), "服务器卡组", headingStyle);
                if (!String.IsNullOrEmpty(deckError)) GUI.Label(new Rect(490, 115, 540, 50), deckError, bodyStyle);
                deckScroll = GUI.BeginScrollView(new Rect(240, 175, 800, 210), deckScroll, new Rect(0, 0, 760, Mathf.Max(210, bodyStyle.CalcHeight(new GUIContent(deckText), 750))));
                GUI.Label(new Rect(0, 0, 750, bodyStyle.CalcHeight(new GUIContent(deckText), 750)), deckText, bodyStyle);
                GUI.EndScrollView();
                if (deckDraft != null && deckDraft.Length > 0)
                {
                    if (GUI.Button(new Rect(240, 400, 120, 48), "上一槽", buttonStyle)) editSlot = (editSlot + deckDraft.Length - 1) % deckDraft.Length;
                    if (GUI.Button(new Rect(370, 400, 120, 48), "下一槽", buttonStyle)) editSlot = (editSlot + 1) % deckDraft.Length;
                    if (GUI.Button(new Rect(510, 400, 120, 48), "上一卡", buttonStyle)) cardChoice = (cardChoice + cardChoices.Count - 1) % cardChoices.Count;
                    if (GUI.Button(new Rect(640, 400, 120, 48), "下一卡", buttonStyle)) cardChoice = (cardChoice + 1) % cardChoices.Count;
                    if (GUI.Button(new Rect(780, 400, 220, 48), "放入选中槽", buttonStyle)) { deckDraft[editSlot] = cardChoices[cardChoice]; deckDirty = true; deckError = null; }
                    CnCard selected;
                    string choice = cardChoices[cardChoice] == 0 ? "空" : cardCollection.Cards.TryGetValue(cardChoices[cardChoice], out selected) ? "卡牌 " + selected.CardId + " 等级 " + selected.Level : "不可用";
                    GUI.Label(new Rect(240, 460, 760, 48), "选中槽位 " + (editSlot + 1) + "；候选 " + choice + (deckDirty ? "（未保存）" : ""), bodyStyle);
                }
                GUI.enabled = !busy && !deckDirty;
                if (GUI.Button(new Rect(240, 530, 150, 55), "上一组", buttonStyle) && deckIndex > 0) { deckIndex--; PrepareDeckDraft(); }
                if (GUI.Button(new Rect(400, 530, 150, 55), "下一组", buttonStyle) && deckIndex + 1 < cardCollection.Decks.Count) { deckIndex++; PrepareDeckDraft(); }
                GUI.enabled = !busy && deckDirty;
                if (GUI.Button(new Rect(580, 530, 190, 55), "保存卡组", buttonStyle)) SaveDeck();
                GUI.enabled = !busy;
                if (GUI.Button(new Rect(790, 530, 210, 55), deckDirty ? "放弃并返回" : "返回", buttonStyle)) showingDeck = false;
            }
            if (showQuest && !showBonus && !showDeck)
            {
                GUI.enabled = true;
                GUI.color = new Color(0, 0, 0, 0.96f);
                GUI.DrawTexture(new Rect(200, 90, 880, 540), Texture2D.whiteTexture);
                GUI.color = previousColor;
                GUI.Label(new Rect(240, 115, 800, 50), "单人关卡", headingStyle);
                GUI.enabled = !busy;
                if (GUI.Button(new Rect(670, 115, 150, 45), "读取卡组", buttonStyle)) ReadSoloDecks();
                GUI.enabled = !busy && soloDecks != null && soloDecks.Count > 0;
                if (GUI.Button(new Rect(830, 115, 150, 45), "切换卡组", buttonStyle)) soloDeckIndex = (soloDeckIndex + 1) % soloDecks.Count;
                string questText = QuestText();
                float height = bodyStyle.CalcHeight(new GUIContent(questText), 750);
                questScroll = GUI.BeginScrollView(new Rect(240, 175, 800, 245), questScroll, new Rect(0, 0, 760, Mathf.Max(245, height)));
                GUI.Label(new Rect(0, 0, 750, height), questText, bodyStyle);
                GUI.EndScrollView();
                GUI.enabled = !busy && quests.Count > 0;
                if (GUI.Button(new Rect(240, 515, 110, 55), "上一关", buttonStyle) && questIndex > 0) { questIndex--; soloPartners = null; soloParty.Clear(); }
                if (GUI.Button(new Rect(360, 515, 110, 55), "下一关", buttonStyle) && questIndex + 1 < quests.Count) { questIndex++; soloPartners = null; soloParty.Clear(); }
                if (GUI.Button(new Rect(480, 515, 200, 55), "查看队友", buttonStyle)) ReadSoloPartners();
                if (soloPartners != null && soloPartners.Count > 0)
                {
                    CnSoloPartner candidate = soloPartners[soloPartnerIndex];
                    GUI.Label(new Rect(240, 425, 760, 38), "候选：" + candidate.Name + "  职业 " + candidate.ArthurType + "  等级 " + candidate.Level + "  已选 " + soloParty.Count + "/3", bodyStyle);
                    if (GUI.Button(new Rect(240, 465, 150, 42), "上一位", buttonStyle)) soloPartnerIndex = (soloPartnerIndex + soloPartners.Count - 1) % soloPartners.Count;
                    if (GUI.Button(new Rect(400, 465, 150, 42), "下一位", buttonStyle)) soloPartnerIndex = (soloPartnerIndex + 1) % soloPartners.Count;
                    if (GUI.Button(new Rect(560, 465, 170, 42), "加入队伍", buttonStyle)) { try { soloParty.Add(candidate); } catch (Exception error) { status = error.Message; } }
                    if (GUI.Button(new Rect(740, 465, 170, 42), "撤销队友", buttonStyle)) soloParty.RemoveLast();
                }
                GUI.enabled = !busy;
                if (GUI.Button(new Rect(760, 530, 240, 55), "返回", buttonStyle)) showingQuests = false;
            }
            if (showBonus)
            {
                GUI.enabled = true;
                GUI.color = new Color(0, 0, 0, 0.96f);
                GUI.DrawTexture(new Rect(260, 120, 760, 485), Texture2D.whiteTexture);
                GUI.color = previousColor;
                GUI.Label(new Rect(300, 145, 680, 45), "登录奖励", headingStyle);
                bonusScroll = GUI.BeginScrollView(new Rect(300, 200, 680, 300), bonusScroll, new Rect(0, 0, 640, Mathf.Max(300, bodyStyle.CalcHeight(new GUIContent(bonusNotice), 630))));
                GUI.Label(new Rect(0, 0, 630, bodyStyle.CalcHeight(new GUIContent(bonusNotice), 630)), bonusNotice, bodyStyle);
                GUI.EndScrollView();
                if (GUI.Button(new Rect(490, 525, 300, 55), "知道了", buttonStyle))
                {
                    PlayerPrefs.DeleteKey(bonusKey); PlayerPrefs.Save(); bonusNotice = "";
                }
            }
            GUI.enabled = !busy && !showBonus && !showDeck && !showQuest;
            if (!showBonus && !showDeck && !showQuest && GUI.Button(new Rect(100, 540, 350, 64), "离线战斗内核验证", buttonStyle))
            {
                try { status = NativeBattleValidation.Run(); }
                catch (Exception error) { status = "战斗内核验证失败：" + error.Message; }
            }
            GUI.enabled = true;
            if (!showBonus && !showDeck && !showQuest && homeState == null) GUI.Label(new Rect(480, 540, 700, 95), "此项离线运行合成战斗。测试游客与 Windows 存档独立。", bodyStyle);
            // Draw after modal panels so the build version stays visible while
            // navigating and in screenshots, without duplicating build metadata.
            GUI.Label(new Rect(1030, 660, 180, 36), "v" + Application.version, bodyStyle);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void CheckHealth()
        {
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
                status = "已连接到游戏服务器。战斗端口：" + battlePort + "。可以继续测试 iOS 资源和游客登录。";
            }));
        }

        private void LoginGuest()
        {
            if (endpoint == null || verifiedAddress != address) return;
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
            catch (Exception error) { status = error.Message; return; }
            PlayerPrefs.SetString(key, uuid); PlayerPrefs.Save();
            StartCoroutine(Send("POST", endpoint.Route("/local/client/login"), CnProtocol.CreateLogin(uuid, Application.unityVersion), delegate(string response) {
                login = CnProtocol.ParseLogin(response, endpoint);
                using (SHA256 hash = SHA256.Create())
                    bonusKey = "ios-recovery.bonus." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(endpoint.ToString() + "|" + login.UserId))).Replace("-", "");
                bonusNotice = PlayerPrefs.GetString(bonusKey, "");
                status = "测试游客登录成功，正在检查服务器资源…";
                StartCoroutine(UpdateServerContent(login));
            }));
        }

        private IEnumerator UpdateServerContent(CnLogin requestedLogin)
        {
            // The login request releases its busy flag after invoking its callback.
            yield return null;
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
            status = serverContentReady ? "测试游客与服务器资源已就绪。新安装 " + content.Installed + "，缓存复用 " + content.Reused + "。可以检查角色状态。" : "服务器资源加载失败：" + (uiError ?? content.Error);
            busy = false;
        }

        private void ReadConnect()
        {
            if (login == null || !serverContentReady || verifiedAddress != address) return;
            StartCoroutine(Send("POST", login.Api.Route("/Connect"), CnProtocol.CreateSessionRequest(login.SessionKey, new JObject()), delegate(string response) {
                connectState = CnProtocol.ParseConnect(response);
                homeState = null;
                status = connectState.UserCreated ? "服务器确认角色已创建。进入主页会结束未完成的单人战斗，并可能领取登录奖励；奖励展示尚未恢复。" : "请输入名称并选择职业，然后创建角色。";
            }));
        }

        private void ReadHome()
        {
            if (login == null || connectState == null || !connectState.UserCreated || !serverContentReady || verifiedAddress != address) return;
            CnLogin requestedLogin = login;
            homeState = null;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/HomeShow"), CnProtocol.CreateSessionRequest(requestedLogin.SessionKey, null), delegate(string response) {
                homeState = CnProtocol.ParseHome(response, requestedLogin.UserId);
                if (!String.IsNullOrEmpty(homeState.BonusNotice))
                {
                    bonusNotice = String.IsNullOrEmpty(bonusNotice) ? homeState.BonusNotice : bonusNotice + "\n" + homeState.BonusNotice;
                    PlayerPrefs.SetString(bonusKey, bonusNotice); PlayerPrefs.Save();
                }
                status = "已读取服务器主页角色状态；主页场景与导航仍在恢复。";
            }));
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
            if (login == null || connectState == null || connectState.UserCreated || !serverContentReady || verifiedAddress != address) return;
            string body;
            try { body = CnProtocol.CreateUserRequest(login.SessionKey, characterName, arthurType); }
            catch (Exception error) { status = error.Message; return; }
            CnLogin requestedLogin = login;
            // A timeout can follow a successful server commit: require Connect before another attempt.
            connectState = null;
            StartCoroutine(Send("POST", requestedLogin.Api.Route("/UserCreate"), body, delegate(string response) {
                CnCreatedUser created = CnProtocol.ParseCreatedUser(response, requestedLogin.UserId);
                status = "角色“" + created.Name + "”已创建。请再次检查角色状态；主页尚未接入。";
            }));
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
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        string text = request.downloadHandler.text;
                        if (request.responseCode == 400 && text != null && text.Contains("no content for the requested platform"))
                            throw new InvalidOperationException("服务器尚未提供与此 Unity 版本匹配的 iOS 资源，需要先构建并配置 iOS 资源包。");
                        throw new InvalidOperationException("连接失败（HTTP " + request.responseCode + "）：" + request.error);
                    }
                    if (request.downloadHandler.data.Length > 4 * 1024 * 1024) throw new FormatException("验证响应超过大小限制。");
                    receive(request.downloadHandler.text);
                }
                catch (Exception error) { status = error.Message; if (failed != null) failed(error); }
                finally { pending = null; busy = false; }
            }
        }
    }
}
