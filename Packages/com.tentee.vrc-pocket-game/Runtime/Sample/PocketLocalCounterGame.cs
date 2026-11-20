using UdonSharp;
using TMPro;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace VrcPocketGame.Sample
{
    /// <summary>Local sample: only this client runs the game; persistence remains game-owned.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PocketLocalCounterGame : PocketGameBehaviour
    {
        public int count;
        public TMP_Text countText;
        public TMP_Text ownerText;
        [Tooltip("Unique reverse-domain-ish prefix owned by this game. Change it when copying the sample.")]
        public string saveNamespace = "tentee.pocket.localcounter.v1";
        private int _savedCount;

        public override void OnPlayerRestored(VRCPlayerApi player) { if (player != null && player.isLocal) Load(); }
        public void OwnerAddOne()
        {
            if (!CanUseGameInput()) return;
            if (count < int.MaxValue) count++;
            Save();
            Refresh();
        }
        public void ConfirmReset()
        {
            if (!IsLocalClaimant() || ui == null || !ui.IsConfirmOpen()) return;
            count = 0;
            Save();
            ui.ClosePanels();
            Refresh();
        }
        protected override void OnTerminalClaimed() { if (ui != null) ui.ResetForSession(); if (IsLocalClaimant()) { Load(); count = _savedCount; } Refresh(); }
        protected override void OnTerminalUseDown() { OwnerAddOne(); }
        protected override void OnTerminalRecalled() { Refresh(); }
        protected override void OnTerminalReleased() { Refresh(); }
        protected override void OnTerminalAudienceChanged() { Refresh(); }
        protected override bool OnTerminalReturnRequested() { Save(); return true; }
        protected override void OnTerminalReturnStarted() { if (ownerText != null) ownerText.text = "Saving and stowing…"; }
        protected override void OnTerminalReturnSucceeded() { Refresh(); }
        protected override void OnTerminalReturnFailed() { if (ownerText != null) ownerText.text = "Could not stow; try again"; }
        protected override void OnTerminalReturnCancelled() { Refresh(); }
        private void Load()
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (local == null || !PlayerData.HasKey(local, Key("schema")) || PlayerData.GetInt(local, Key("schema")) != 1) { _savedCount = 0; return; }
            _savedCount = PlayerData.GetInt(local, Key("count"));
        }
        // キーはゲーム固有で安定させる。観戦用の一時データをPlayerDataへ保存しない。
        private void Save()
        {
            if (!IsLocalClaimant() || !IsLocalPlayerDataRestored()) return;
            PlayerData.SetInt(Key("schema"), 1);
            PlayerData.SetInt(Key("count"), count);
        }
        private string Key(string field) { return saveNamespace + "." + field; }
        private void Refresh() { if (countText != null) countText.text = "Count: " + count; if (ownerText != null) ownerText.text = IsLocalClaimant() ? "You own this game" : "Spectating"; }
    }
}
