using UnityEngine;

namespace Inreal
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorParamSetterBool : MonoBehaviour
    {
        public string Param = "MyParameter";

        public void Set(bool on) {
            GetComponent<Animator>().SetBool(Param, on);
        }
    }
}
