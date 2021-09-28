using UnityEngine;

namespace JamSuite.Persistent
{
    public static class AutoSpawn
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void SpawnAll() {
            foreach (var prefab in Resources.LoadAll<GameObject>("AutoSpawn")) {
                var go = GameObject.Instantiate(prefab);
                go.name = prefab.name;
                GameObject.DontDestroyOnLoad(go);
            }
        }
    }
}
