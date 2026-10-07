using System.Collections.Generic;
using UnityEngine;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 여러 지점을 차례로 직선 이동한다. 손님·차량·배달원의 출입문 경유 이동에 쓴다. (Transform 이동만 사용)
    /// </summary>
    public sealed class RouteWalker
    {
        const float ArriveThreshold = 0.02f;

        readonly List<Vector3> points = new List<Vector3>();
        int index;

        public bool IsDone => index >= points.Count;

        public void Start(params Vector3[] route)
        {
            points.Clear();
            points.AddRange(route);
            index = 0;
        }

        /// <summary>한 프레임 이동한다. 마지막 지점에 도착하면 true.</summary>
        public bool Step(Transform mover, float speed, float turnSpeed)
        {
            if (IsDone)
                return true;

            var position = mover.position;
            var target = points[index];
            target.y = position.y;

            Face(mover, target - position, turnSpeed);
            mover.position = Vector3.MoveTowards(position, target, speed * Time.deltaTime);

            if ((target - mover.position).sqrMagnitude <= ArriveThreshold * ArriveThreshold)
                index++;

            return IsDone;
        }

        /// <summary>수평 방향으로 서서히 돌아본다.</summary>
        public static void Face(Transform mover, Vector3 direction, float turnSpeed)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            mover.rotation = Quaternion.RotateTowards(mover.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }
    }
}
