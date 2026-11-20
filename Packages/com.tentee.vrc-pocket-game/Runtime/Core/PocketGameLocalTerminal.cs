using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace VrcPocketGame
{
    /// <summary>One scene terminal per client; no pool, claim table or network ownership.</summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PocketGameLocalTerminal : UdonSharpBehaviour
    {
        public GameObject terminalRoot;
        public VRC_Pickup pickup;
        public PocketGameUi ui;
        public UdonBehaviour gameEvents;
        public TMPro.TMP_Text statusText;
        public float spawnDistance = .45f;
        private bool _restored;
        private bool _claimed;
        private bool _transition;
        private bool _requestingReturn;
        private bool _returnApproved;

        // 復元通知を取りこぼさないよう、このコントローラーは収納中も有効に保つ。
        private void Start()
        {
            if (terminalRoot != null) terminalRoot.SetActive(false);
            SetStatus(_restored ? "Use: call or recall your game" : "Loading player data…");
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            _restored = true;
            SetStatus("Use: call or recall your game");
        }

        public bool IsLocalPlayerDataRestored() { return _restored; }
        public bool IsLocalClaimant() { return _claimed && _restored && terminalRoot != null && terminalRoot.activeInHierarchy; }
        public bool CanUseGameInput() { return IsLocalClaimant() && !_transition && (ui == null || !ui.BlocksGameInput()); }

        public override void Interact() { CallOrRecall(); }
        public void CallOrRecall()
        {
            if (!_restored) { SetStatus("Loading player data…"); return; }
            if (_transition || Networking.LocalPlayer == null || terminalRoot == null) return;
            if (_claimed)
            {
                if (!CanUseGameInput()) return;
                PlaceAtKiosk();
                Send("PocketTerminal_OnRecalled");
                return;
            }
            _transition = true;
            PlaceAtKiosk();
            terminalRoot.SetActive(true);
            _claimed = true;
            if (ui != null) ui.ResetForSession();
            if (pickup != null) pickup.pickupable = true;
            Send("PocketTerminal_OnClaimed");
            _transition = false;
            SetStatus("Use: call or recall your game");
        }

        public void PocketTerminal_OnUseDown() { if (CanUseGameInput()) Send("PocketTerminal_OnUseDown"); }

        public void PocketTerminal_RequestReturn()
        {
            if (!CanUseGameInput() || _requestingReturn) return;
            // 承認はRequestReturnイベントの呼出し中だけ受け付ける。遅延した承認を次回へ持ち越さない。
            _requestingReturn = true;
            _returnApproved = false;
            Send("PocketTerminal_RequestReturn");
            _requestingReturn = false;
            if (!_returnApproved) return;
            _transition = true;
            Send("PocketTerminal_OnReturnStarted");
            if (pickup != null) { pickup.Drop(); pickup.pickupable = false; }
            _claimed = false;
            if (ui != null) ui.ResetForSession();
            Send("PocketTerminal_OnReleased");
            // 無効化前に通知する。成功は端末の収納を意味し、永続保存のサーバー確認ではない。
            Send("PocketTerminal_OnReturnSucceeded");
            terminalRoot.SetActive(false);
            _transition = false;
            _returnApproved = false;
            SetStatus("Use: call or recall your game");
        }

        public void PocketTerminal_ApproveReturn() { if (_requestingReturn && IsLocalClaimant()) _returnApproved = true; }
        private void PlaceAtKiosk()
        {
            if (pickup != null && pickup.IsHeld) pickup.Drop();
            terminalRoot.transform.SetPositionAndRotation(transform.position - transform.forward * spawnDistance, transform.rotation);
        }
        private void Send(string eventName) { if (gameEvents != null) gameEvents.SendCustomEvent(eventName); }
        private void SetStatus(string value) { if (statusText != null) statusText.text = value; }
    }
}
