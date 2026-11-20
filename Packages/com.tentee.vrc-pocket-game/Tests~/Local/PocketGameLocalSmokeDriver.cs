#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public sealed class PocketGameLocalSmokeDriver : MonoBehaviour
{
    Action tick;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        if (!SessionState.GetBool("PocketGameSdk.LocalSmoke.Running", false)) return;
        new GameObject("Local smoke driver").AddComponent<PocketGameLocalSmokeDriver>();
    }
    void Awake()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PocketGameLocalSmoke")).First(t => t != null);
        tick = (Action)Delegate.CreateDelegate(typeof(Action), type.GetMethod("Tick"));
    }
    void Update() { tick(); }
}
#endif
