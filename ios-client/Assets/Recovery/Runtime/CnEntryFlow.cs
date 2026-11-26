using System;

namespace Kairisei.Recovery
{
    public enum CnEntryStep { Offline, Health, Login, Content, Connect, Character, Create, HomePermission, Home, HomeReady, Failed, PendingBattle }

    public sealed class CnTransportException : Exception
    {
        public long HttpStatus { get; private set; }
        public CnTransportException(long status, string message) : base(message) { HttpStatus = status; }
    }

    // The UI and transport use this same state. No retry can directly repeat UserCreate.
    public sealed class CnEntryFlow
    {
        public CnEntryStep Step { get; private set; }
        public CnEntryStep RetryStep { get; private set; }
        public CnLogin Login { get; private set; }
        public CnConnectState CharacterState { get; private set; }
        public CnHomeState Home { get; private set; }
        public bool CreationMayHaveCommitted { get; private set; }
        public bool HomeMayHaveCommitted { get; private set; }
        public bool CanRetry { get { return Step == CnEntryStep.Failed; } }

        public void Reset()
        {
            Step = CnEntryStep.Offline; RetryStep = CnEntryStep.Offline;
            Login = null; CharacterState = null; Home = null;
            CreationMayHaveCommitted = false; HomeMayHaveCommitted = false;
        }

        public void BeginHealth() { Reset(); Step = CnEntryStep.Health; }
        public void BeginLogin()
        {
            Step = CnEntryStep.Login; Login = null; CharacterState = null; Home = null;
        }

        public void ReceiveLogin(string response, ServerEndpoint endpoint)
        {
            Require(CnEntryStep.Login);
            Login = CnProtocol.ParseLogin(response, endpoint);
            Step = CnEntryStep.Content;
        }

        public void ResourcesReady()
        {
            Require(CnEntryStep.Content); Step = CnEntryStep.Connect;
        }

        public void BeginConnect()
        {
            if (Login == null) throw new InvalidOperationException("请先登录。");
            Step = CnEntryStep.Connect; CharacterState = null;
        }

        public void ReceiveConnect(string response)
        {
            Require(CnEntryStep.Connect);
            CharacterState = CnProtocol.ParseConnect(response);
            CreationMayHaveCommitted = false;
            Step = CharacterState.UserCreated ? CnEntryStep.HomePermission : CnEntryStep.Character;
        }

        public string BeginCreate(string name, int role)
        {
            Require(CnEntryStep.Character);
            string body = CnProtocol.CreateUserRequest(Login.SessionKey, name, role);
            CreationMayHaveCommitted = true; CharacterState = null; Step = CnEntryStep.Create;
            return body;
        }

        public CnCreatedUser ReceiveCreated(string response)
        {
            Require(CnEntryStep.Create);
            CnCreatedUser created = CnProtocol.ParseCreatedUser(response, Login.UserId);
            Step = CnEntryStep.Connect;
            return created;
        }

        public void BeginHome(bool hasPendingBattle)
        {
            if (Step != CnEntryStep.HomePermission && Step != CnEntryStep.HomeReady && Step != CnEntryStep.PendingBattle)
                throw new InvalidOperationException("请先确认角色状态。");
            if (hasPendingBattle)
            {
                Step = CnEntryStep.PendingBattle;
                throw new InvalidOperationException("存在未提交的战斗记录。请先恢复或完成结算，再进入主页。");
            }
            HomeMayHaveCommitted = true; Step = CnEntryStep.Home;
        }

        public void ReceiveHome(string response)
        {
            Require(CnEntryStep.Home);
            CnHomeState parsed = CnProtocol.ParseHome(response, Login.UserId);
            Home = parsed; HomeMayHaveCommitted = false; Step = CnEntryStep.HomeReady;
        }

        public void Fail(Exception error)
        {
            CnEntryStep failed = Step;
            CnProtocolException protocol = error as CnProtocolException;
            CnTransportException transport = error as CnTransportException;
            bool expired = (protocol != null && protocol.Code == -3208) || (transport != null && transport.HttpStatus == 401);
            RetryStep = expired ? CnEntryStep.Login : failed == CnEntryStep.Create ? CnEntryStep.Connect
                : failed == CnEntryStep.Home || failed == CnEntryStep.HomeReady ? CnEntryStep.HomePermission
                : failed == CnEntryStep.Character || failed == CnEntryStep.HomePermission ? CnEntryStep.Connect : failed;
            if (expired) { Login = null; CharacterState = null; }
            Step = CnEntryStep.Failed;
        }

        public CnEntryStep PrepareRetry()
        {
            if (!CanRetry) throw new InvalidOperationException("当前步骤不能重试。");
            Step = RetryStep;
            return Step;
        }

        private void Require(CnEntryStep step)
        {
            if (Step != step) throw new InvalidOperationException("登录步骤已变化，请重试当前步骤。");
        }
    }
}
