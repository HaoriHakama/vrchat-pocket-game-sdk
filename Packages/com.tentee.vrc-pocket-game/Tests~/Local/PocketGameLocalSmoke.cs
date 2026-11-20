#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VrcPocketGame;
using VrcPocketGame.Editor;

[InitializeOnLoad]
public static class PocketGameLocalSmoke
{
    const string Running = "PocketGameSdk.LocalSmoke.Running";
    static double deadline;
    static bool finished;
    static int phase;
    static int initial;
    static double ready;
    static PocketGameLocalSmoke()
    {
        if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
    }
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a disposable validation project.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PocketGameInstaller.InstallLocalIntoCurrentScene();
        var descriptor = new GameObject("Local smoke world").AddComponent<VRCSceneDescriptor>();
        descriptor.spawns = new[] { new GameObject("Spawn").transform };
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(0, -.1f, 0);
        floor.transform.localScale = new Vector3(20, .2f, 20);
        PocketGameTerminalBuilder.PreparePrograms(typeof(PocketGameLocalReturnProbe));
        var controller = UnityEngine.Object.FindObjectOfType<PocketGameLocalTerminal>();
        var probeRoot = new GameObject("Return probe");
        probeRoot.transform.SetParent(controller.terminalRoot.transform, false);
        var probe = UdonSharpEditor.UdonSharpComponentExtensions.AddUdonSharpComponent<PocketGameLocalReturnProbe>(probeRoot);
        probe.localTerminal = controller;
        probe.ui = controller.ui;
        UdonSharpEditor.UdonSharpEditorUtility.CopyProxyToUdon(probe);
        var counter = UnityEngine.Object.FindObjectOfType<VrcPocketGame.Sample.PocketLocalCounterGame>(true);
        counter.saveNamespace = "sdk.localsmoke." + DateTime.UtcNow.Ticks;
        UdonSharpEditor.UdonSharpEditorUtility.CopyProxyToUdon(counter);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/VrcPocketGameGenerated/LocalSmoke.unity");
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("VRC.SDK3.ClientSim.ClientSimSettings")).First(t => t != null);
        var settings = type.GetProperty("Instance").GetValue(null);
        type.GetField("enableClientSim").SetValue(settings, true);
        type.GetField("initializationDelay").SetValue(settings, 0f);
        type.GetField("spawnPlayer").SetValue(settings, true);
        type.GetField("localPlayerIsMaster").SetValue(settings, true);
        SessionState.SetBool(Running, true);
        finished = false;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    public static void Tick()
    {
        if (!SessionState.GetBool(Running, false) || finished || !EditorApplication.isPlaying) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
        try
        {
            var proxy = UnityEngine.Object.FindObjectOfType<PocketGameLocalTerminal>();
            if (proxy == null || Networking.LocalPlayer == null) { Wait(); return; }
            var controller = UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour(proxy);
            if (!(bool)controller.GetProgramVariable("_restored")) { Wait(); return; }
            var root = (GameObject)controller.GetProgramVariable("terminalRoot");
            var game = (UdonBehaviour)controller.GetProgramVariable("gameEvents");
            var uiProxy = (PocketGameUi)proxy.ui;
            var ui = UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour(uiProxy);
            if (phase == 0)
            {
                int before = Count(game);
                game.SendCustomEvent("OwnerAddOne");
                Check(Count(game) == before && !root.activeSelf, "stowed input blocked");
                controller.SendCustomEvent("CallOrRecall");
                Check(root.activeSelf, "call activates terminal");
                initial = Count(game);
                // 組込みPickupイベントはUdonのStart後に送る。CustomEventだけはStart前にも実行できる。
                ready = EditorApplication.timeSinceStartup + .5;
                phase = 1;
                return;
            }
            if (EditorApplication.timeSinceStartup < ready) return;
            game.SendCustomEvent("OwnerAddOne");
            Check(Count(game) == initial + 1, "UI game input");
            var relay = UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour(root.GetComponent<PocketGameInputRelay>());
            relay.OnPickupUseDown();
            Check(Count(game) == initial + 2, "Pickup Use event routing");
            ui.SendCustomEvent("ShowConfirm");
            game.SendCustomEvent("OwnerAddOne");
            var displaced = new Vector3(10, 10, 10);
            root.transform.position = displaced;
            controller.SendCustomEvent("CallOrRecall");
            Check(Count(game) == initial + 2 && root.transform.position == displaced, "modal blocks game and recall");
            ui.SendCustomEvent("ClosePanels");
            controller.SendCustomEvent("CallOrRecall");
            Check(root.transform.position != displaced && Count(game) == initial + 2, "recall preserves progress");
            var probe = root.transform.Find("Return probe").GetComponent<UdonBehaviour>();
            controller.SetProgramVariable("gameEvents", probe);
            controller.SendCustomEvent("PocketTerminal_ApproveReturn");
            controller.SendCustomEvent("PocketTerminal_RequestReturn");
            Check(root.activeSelf, "missing approval and stale approval cannot stow");
            Check((int)probe.GetProgramVariable("trace") == 1, "game refusal event delivered");
            probe.SetProgramVariable("trace", 0);
            probe.SetProgramVariable("approve", true);
            controller.SendCustomEvent("PocketTerminal_RequestReturn");
            Check((int)probe.GetProgramVariable("trace") == 1234, "return event order before deactivation");
            controller.SetProgramVariable("gameEvents", game);
            Check(!root.activeSelf, "approved return stows");
            controller.SendCustomEvent("CallOrRecall");
            Check(root.activeSelf && Count(game) == initial + 2, "re-call loads saved progress");
            Debug.Log("LOCAL CLIENTSIM PASS (single simulated client; no hardware or server persistence assertion)");
            Finish(0);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static int Count(UdonBehaviour game) { return (int)game.GetProgramVariable("count"); }
    static void Check(bool condition, string name) { if (!condition) throw new Exception("LOCAL SMOKE: " + name); Debug.Log("LOCAL CHECK PASS: " + name); }
    static void Wait() { if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Local restore/ClientSim initialization timed out."); }
    static void Finish(int code)
    {
        finished = true;
        SessionState.SetBool(Running, false);
        EditorApplication.Exit(code);
    }
}
#endif
