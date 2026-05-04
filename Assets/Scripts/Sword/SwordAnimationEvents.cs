using UnityEngine;

namespace Sword
{
    public class SwordAnimationEvents : MonoBehaviour
    {
        public void StopSwordHitbox()
        {
            SwordController.Instance.StopSwingHitbox();
        }
    }
}
