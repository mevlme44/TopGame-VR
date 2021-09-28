namespace UnityEngine
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance {
            get { return JamSuite.SingletonHelper<T>.Instance; }
        }
    }
}

namespace JamSuite
{
    public class SingletonHelper<T> where T : UnityEngine.Object
    {
        public static T Instance {
            get {
                if (!_instance) _instance = UnityEngine.Object.FindObjectOfType<T>();
                return _instance;
            }
        }

        private static T _instance;
    }
}
