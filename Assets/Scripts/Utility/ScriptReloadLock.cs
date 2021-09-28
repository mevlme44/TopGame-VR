using UnityEngine;

public class ScriptReloadLock : MonoBehaviour
{
#if UNITY_EDITOR
    void OnEnable() {
        UnityEditor.EditorApplication.LockReloadAssemblies();
    }

    void OnDisable() {
        UnityEditor.EditorApplication.UnlockReloadAssemblies();
    }
#endif
}
