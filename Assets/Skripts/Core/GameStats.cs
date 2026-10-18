using UnityEngine;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 한 판의 간단한 통계 (클리어 기록용). 플레이 시간과 총 판매 수.
    /// 판매 수 = 카운터·드라이브스루에서 판 개수 + 배달 성공 수량.
    /// </summary>
    public static class GameStats
    {
        static float startTime;

        public static int Sales { get; private set; }

        /// <summary>시작부터 지금까지 실제 시간 (초, 배속 치트 영향 없음)</summary>
        public static float PlaySeconds => Time.unscaledTime - startTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            startTime = 0f;
            Sales = 0;
        }

        /// <summary>게임 시작 시 GameBootstrap이 호출한다.</summary>
        public static void StartSession()
        {
            startTime = Time.unscaledTime;
            Sales = 0;
        }

        public static void AddSales(int count) => Sales += Mathf.Max(0, count);
    }
}
