using System;
using System.Linq;
using TMPro;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Components;
using VrcPocketGame;
using VrcPocketGame.Editor;
public static class PocketGameLocalValidation
{
    public static void RunBatch()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") == null)
            throw new InvalidOperationException("Import TMP Essential Resources before running validation.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PocketGameInstaller.InstallIntoCurrentScene();
        var pooled = GameObject.Find(PocketGameInstaller.RootName);
        Report("pool", pooled);
        PocketGameInstaller.InstallLocalIntoCurrentScene();
        var local = UnityEngine.Object.FindObjectOfType<PocketGameLocalTerminal>();
        var pooledRelay = pooled.GetComponentInChildren<PocketGameInputRelay>(true);
        pooledRelay.localTerminal = local;
        Reject(() => PocketGameSceneValidator.ValidateAll(), "mixes terminal modes");
        pooledRelay.localTerminal = null;
        Report("local", GameObject.Find(PocketGameInstaller.RootName + " Local"));
        PocketGameSceneValidator.ValidateLocal(local);
        Check(!local.terminalRoot.activeSelf, "initially stowed");
        var sync = local.terminalRoot.AddComponent<VRCObjectSync>();
        Reject(() => PocketGameSceneValidator.ValidateLocal(local), "network object sync");
        UnityEngine.Object.DestroyImmediate(sync);
        local.ui.terminalSession = pooled.GetComponentInChildren<PocketGameTerminalSession>(true);
        Reject(() => PocketGameSceneValidator.ValidateLocal(local), "UI wiring");
        local.ui.terminalSession = null;
        var backing = local.gameEvents;
        var source = backing.programSource;
        backing.programSource = null;
        Reject(() => PocketGameSceneValidator.ValidateLocal(local), "program asset");
        backing.programSource = source;
        var graphRoot = new GameObject("Invalid synced graph");
        graphRoot.transform.SetParent(local.terminalRoot.transform, false);
        var graph = graphRoot.AddComponent<VRC.Udon.UdonBehaviour>();
        var graphSource = ScriptableObject.CreateInstance<VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UdonGraphProgramAsset>();
        graph.programSource = graphSource;
        graph.SyncMethod = VRC.SDKBase.Networking.SyncType.Manual;
        Reject(() => PocketGameSceneValidator.ValidateLocal(local), "SyncType.None");
        UnityEngine.Object.DestroyImmediate(graphRoot);
        UnityEngine.Object.DestroyImmediate(graphSource);
        PocketGameSceneValidator.ValidateAll();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/VrcPocketGameGenerated/LocalValidation.unity");
        Debug.Log("LOCAL STRUCTURE PASS");
    }
    static void Report(string mode, GameObject root)
    {
        Debug.Log("STRUCTURE COUNT " + mode + " objects=" + root.GetComponentsInChildren<Transform>(true).Length + " components=" + root.GetComponentsInChildren<Component>(true).Length);
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Reject(Action action, string expected)
    {
        try { action(); } catch (InvalidOperationException e) { Check(e.Message.Contains(expected), e.Message); return; }
        throw new Exception("Validator accepted invalid configuration: " + expected);
    }
}
