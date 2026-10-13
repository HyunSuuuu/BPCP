using System.Collections.Generic;
using UnityEngine;

namespace BurgerIdle.Workers
{
    public enum TaskType
    {
        None,
        /// <summary>기계 출력 지점에서 음식 집기 → 이후 Deliver</summary>
        Carry,
        /// <summary>들고 있는 음식을 받는 곳(카운터)에 내려놓기</summary>
        Deliver,
        /// <summary>테이블 쓰레기 집기 → 이후 Dispose</summary>
        Clean,
        /// <summary>들고 있는 쓰레기를 쓰레기통에 버리기</summary>
        Dispose,
        /// <summary>카운터(일반·드라이브스루) 응대 위치에서 판매</summary>
        Sell,
        /// <summary>포장 테이블의 포장 위치에서 포장</summary>
        Pack,
    }

    /// <summary>
    /// 알바 작업 예약판. 한 대상(기계 출력 지점, 테이블 쓰레기, 카운터)은 알바 한 명만 맡도록 예약한다.
    /// 중복 배정과 한 곳에 몰리는 것을 막는다. 플레이어는 예약하지 않는다.
    /// </summary>
    public static class TaskBoard
    {
        static readonly Dictionary<object, object> reservations = new Dictionary<object, object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => reservations.Clear();

        public static bool IsReserved(object target, object by) =>
            reservations.TryGetValue(target, out var owner) && owner != by;

        public static bool TryReserve(object target, object by)
        {
            if (IsReserved(target, by))
                return false;
            reservations[target] = by;
            return true;
        }

        public static void Release(object target, object by)
        {
            if (target != null && reservations.TryGetValue(target, out var owner) && owner == by)
                reservations.Remove(target);
        }
    }
}
