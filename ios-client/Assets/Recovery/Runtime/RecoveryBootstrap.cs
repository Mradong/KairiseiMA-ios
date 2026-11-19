using System;
using System.Collections;
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
        private string bonusKey;
        private string bonusNotice = "";
        private Vector2 bonusScroll;
        private string characterName = "";
        private int arthurType = 1;
        private bool serverContentReady;
        private UnityWebRequest pending;
        private Texture2D title;
        private Font font;
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
                status = "本地资源已就绪。输入服务器地址后检查连接。完整游戏仍在恢复中。";
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

        private void OnGUI()
        {
            if (title != null) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), title, ScaleMode.ScaleAndCrop);
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
            if (scale <= 0) return;
            bool showBonus = !String.IsNullOrEmpty(bonusNotice) && login != null && verifiedAddress == address;
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
            GUI.enabled = !busy && !showBonus;
            address = GUI.TextField(new Rect(100, 220, 1080, 58), address, 300, inputStyle);
            if (GUI.Button(new Rect(100, 300, 320, 64), busy ? "连接中…" : "检查连接", buttonStyle)) CheckHealth();
            GUI.enabled = !busy && !showBonus && endpoint != null && verifiedAddress == address;
            if (GUI.Button(new Rect(450, 300, 350, 64), "创建／登录测试游客", buttonStyle)) LoginGuest();
            GUI.enabled = !busy && !showBonus && login != null && serverContentReady && verifiedAddress == address;
            if (GUI.Button(new Rect(830, 300, 350, 64), "检查角色状态", buttonStyle)) ReadConnect();
            GUI.enabled = true;
            GUI.Label(new Rect(100, 390, 1080, 125), status, bodyStyle);
            if (connectState != null && connectState.UserCreated && login != null && verifiedAddress == address && serverContentReady)
            {
                GUI.enabled = !busy && !showBonus;
                if (GUI.Button(new Rect(995, 465, 185, 54), "进入主页", buttonStyle)) ReadHome();
                GUI.enabled = true;
                if (homeState != null)
                    GUI.Label(new Rect(100, 465, 880, 55), homeState.Name + "  等级 " + homeState.Level + "  体力 " + homeState.Ap + "/" + homeState.ApMax + "  战斗点 " + homeState.Bp + "/" + homeState.BpMax + "  金币 " + homeState.Gold, bodyStyle);
            }
            if (connectState != null && !connectState.UserCreated && login != null && verifiedAddress == address && serverContentReady)
            {
                GUI.enabled = !busy && !showBonus;
                characterName = GUI.TextField(new Rect(100, 465, 280, 54), characterName, 32, inputStyle);
                string[] roles = { "佣兵", "富豪", "盗贼", "歌姬" };
                for (int i = 0; i < roles.Length; i++)
                    if (GUI.Button(new Rect(400 + i * 145, 465, 135, 54), (arthurType == i + 1 ? "✓ " : "") + roles[i], buttonStyle)) arthurType = i + 1;
                if (GUI.Button(new Rect(995, 465, 185, 54), "创建角色", buttonStyle)) CreateCharacter();
                GUI.enabled = true;
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
            GUI.enabled = !busy && !showBonus;
            if (!showBonus && GUI.Button(new Rect(100, 540, 350, 64), "离线战斗内核验证", buttonStyle))
            {
                try { status = NativeBattleValidation.Run(); }
                catch (Exception error) { status = "战斗内核验证失败：" + error.Message; }
            }
            GUI.enabled = true;
            if (!showBonus) GUI.Label(new Rect(480, 540, 700, 95), "此项离线运行合成战斗。测试游客与 Windows 存档独立。", bodyStyle);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void CheckHealth()
        {
            login = null;
            connectState = null;
            homeState = null;
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
            bonusKey = null;
            bonusNotice = "";
            serverContentReady = false;
            string key;
            using (SHA256 hash = SHA256.Create())
                key = "ios-recovery.guest." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(endpoint.ToString()))).Replace("-", "");
            string uuid = PlayerPrefs.GetString(key, "");
            if (String.IsNullOrEmpty(uuid)) { uuid = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString(key, uuid); PlayerPrefs.Save(); }
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
            status = serverContentReady ? "测试游客与服务器资源已就绪，角色编号：" + login.UserId + "。可以检查角色状态。" : "服务器资源加载失败：" + content.Error;
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

        private IEnumerator Send(string method, Uri uri, string body, Action<string> receive)
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
                catch (Exception error) { status = error.Message; }
                finally { pending = null; busy = false; }
            }
        }
    }
}
