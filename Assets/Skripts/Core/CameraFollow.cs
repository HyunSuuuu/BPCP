using UnityEngine;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 고정 각도 쿼터뷰로 대상을 따라간다. 회전은 바뀌지 않는다.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        Transform target;
        Vector3 offset;
        float sharpness;

        public void Init(Transform target, float pitch, float yaw, float distance, float sharpness)
        {
            this.target = target;
            this.sharpness = sharpness;

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rotation;
            offset = rotation * Vector3.back * distance;
            transform.position = target.position + offset;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            var desired = target.position + offset;
            var t = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }
    }
}
