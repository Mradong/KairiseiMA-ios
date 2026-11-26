using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Kairisei.Recovery
{
    // Only uncertainty markers, never an identity, name, session or response body.
    // The future battle owner must write PendingBattlePath before any admission.
    public sealed class CnEntryJournal
    {
        private readonly string homePath, createPath;
        public string PendingBattlePath { get; private set; }
        public bool HomeUnconfirmed { get { return File.Exists(homePath); } }
        public bool CreationUnconfirmed { get { return File.Exists(createPath); } }
        public bool HasPendingBattle { get { return File.Exists(PendingBattlePath); } }

        public CnEntryJournal(string persistentDirectory, ServerEndpoint server, long userId)
        {
            if (server == null || userId < 1 || String.IsNullOrWhiteSpace(persistentDirectory))
                throw new ArgumentException("登录记录范围无效。");
            string scope;
            using (SHA256 hash = SHA256.Create())
                scope = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(server.ToString() + "|" + userId))).Replace("-", "");
            string directory = Path.Combine(persistentDirectory, "RecoveryEntry");
            Directory.CreateDirectory(directory);
            homePath = Path.Combine(directory, scope + ".home-pending");
            createPath = Path.Combine(directory, scope + ".create-pending");
            PendingBattlePath = Path.Combine(directory, scope + ".battle-pending");
        }

        public bool BeginHome()
        {
            if (HasPendingBattle) throw new InvalidOperationException("请先处理未提交的战斗记录。");
            bool unconfirmed = HomeUnconfirmed;
            Mark(homePath);
            return unconfirmed;
        }

        public void BeginCreate() { Mark(createPath); }
        public void ConfirmHome() { File.Delete(homePath); }
        public void ConfirmCharacterState() { File.Delete(createPath); }

        private static void Mark(string target)
        {
            if (File.Exists(target)) return;
            string staging = target + ".new";
            File.WriteAllText(staging, "pending\n", new UTF8Encoding(false));
            File.Move(staging, target);
        }
    }
}
