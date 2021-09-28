using UnityEngine;

namespace Inreal
{
    public class AnimatorParamSetterTrigger : MonoBehaviour
    {
        public string Param = "MyParameter";

        public void Set() {
            GetComponent<Animator>().SetTrigger(Param);
        }
    }
}
