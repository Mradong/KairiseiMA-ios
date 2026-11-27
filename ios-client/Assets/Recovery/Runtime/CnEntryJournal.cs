using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Kairisei.Recovery
{
    // Preserve entry uncertainty and the exact paid-start selection within one service/account scope.
    public sealed class CnEntryJournal
    {
        private const int MaximumSoloSelectionBytes = 4096;
        private readonly string homePath, createPath;
        private readonly long userId;
        public string PendingBattlePath { get; private set; }
        public bool HomeUnconfirmed { get { return File.Exists(homePath); } }
        public bool CreationUnconfirmed { get { return File.Exists(createPath); } }
        public bool HasPendingBattle { get { return File.Exists(PendingBattlePath); } }

        public CnEntryJournal(string persistentDirectory, ServerEndpoint server, long userId)
        {
            if (server == null || userId < 1 || String.IsNullOrWhiteSpace(persistentDirectory))
                throw new ArgumentException("登录记录范围无效。");
            this.userId = userId;
            string scope;
            using (SHA256 hash = SHA256.Create())
                scope = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(server.ToString() + "|" + userId))).Replace("-", "");
            string directory = Path.Combine(persistentDirectory, "RecoveryEntry");
            Directory.CreateDirectory(directory);
            homePath = Path.Combine(directory, scope + ".home-pending");
            createPath = Path.Combine(directory, scope + ".create-pending");
            PendingBattlePath = Path.Combine(directory, scope + ".battle-pending");
        }

        // Write durably before admission. A second attempt must recover the first selection.
        public void BeginSoloOpening(CnSoloBattleSelection selection)
        {
            if (selection == null || selection.UserId != userId)
                throw new ArgumentException("本地战斗记录与登录账号不一致。", "selection");
            if (HasPendingBattle) throw new InvalidOperationException("请先处理未提交的战斗记录。");
            byte[] data = Encoding.UTF8.GetBytes(CnProtocol.SaveSoloBattleSelection(selection));
            if (data.Length > MaximumSoloSelectionBytes) throw new FormatException("本地战斗编队记录超出大小限制。");
            string staged = PendingBattlePath + "." + Guid.NewGuid().ToString("N") + ".new";
            try
            {
                using (var file = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    file.Write(data, 0, data.Length);
                    file.Flush(true);
                }
                File.Move(staged, PendingBattlePath);
            }
            finally { if (File.Exists(staged)) File.Delete(staged); }
        }

        // Reopen the original request after app relaunch. An unknown marker still blocks Home.
        public CnSoloBattleSelection ReadPendingSoloOpening()
        {
            if (!HasPendingBattle) return null;
            var file = new FileInfo(PendingBattlePath);
            if (file.Length == 0 || file.Length > MaximumSoloSelectionBytes)
                throw new FormatException("本地战斗编队记录大小无效。");
            return CnProtocol.LoadSoloBattleSelection(File.ReadAllText(PendingBattlePath, Encoding.UTF8), userId);
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
