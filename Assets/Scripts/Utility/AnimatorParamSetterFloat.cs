using UnityEngine;

namespace Inreal
{
    public class AnimatorParamSetterFloat : MonoBehaviour
    {
        public string Param = "MyParameter";

        public void Set(float value) {
            GetComponent<Animator>().SetFloat(Param, value);
        }
    }
}
