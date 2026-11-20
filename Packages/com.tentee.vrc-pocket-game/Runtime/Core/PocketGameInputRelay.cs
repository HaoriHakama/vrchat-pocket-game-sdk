using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

namespace VrcPocketGame
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PocketGameInputRelay : UdonSharpBehaviour
    {
        public PocketGameTerminalSession terminalSession;
        public PocketGameLocalTerminal localTerminal;
        public override void OnPickupUseDown()
        {
            if (localTerminal != null) { if (terminalSession == null) localTerminal.PocketTerminal_OnUseDown(); }
            else if (terminalSession != null) terminalSession.PocketTerminal_OnUseDown();
        }
        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            // Pickup標準の所有権要求は妨げない。ローカルの入力許可は所有権に依存しない。
            if (localTerminal != null) return terminalSession == null;
            return terminalSession != null && terminalSession.CanPlayerOwnTerminal(requestingPlayer, requestedOwner);
        }
    }
}
