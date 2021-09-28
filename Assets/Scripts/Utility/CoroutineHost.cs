using System.Collections;
using UnityEngine;

public class CoroutineHost : MonoBehaviour
{
    protected static CoroutineHost _instance;

    public static CoroutineHost Instance {
        get {
            if (!_instance) {
                _instance = FindObjectOfType<CoroutineHost>();
                if (!_instance) {
                    var obj = new GameObject("_CoroutineHost");
                    _instance = obj.AddComponent<CoroutineHost>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    public static Coroutine Start(IEnumerator routine) {
        return Instance.StartCoroutine(routine);
    }

    public static void Stop(Coroutine routine) {
        Instance.StopCoroutine(routine);
    }

    public static void StopAll() {
        Instance.StopAllCoroutines();
    }
}
