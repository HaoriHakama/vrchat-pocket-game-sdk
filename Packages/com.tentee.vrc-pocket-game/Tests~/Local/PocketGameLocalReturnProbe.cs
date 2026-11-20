using UdonSharp;
using VrcPocketGame;

// テスト専用。承認拒否と、無効化前に通知が完了する契約を観測する。
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PocketGameLocalReturnProbe : PocketGameBehaviour
{
    public bool approve;
    public int trace;
    protected override bool OnTerminalReturnRequested() { trace = trace * 10 + 1; return approve; }
    protected override void OnTerminalReturnStarted() { trace = trace * 10 + 2; }
    protected override void OnTerminalReleased() { trace = trace * 10 + 3; }
    protected override void OnTerminalReturnSucceeded() { trace = trace * 10 + 4; }
}
