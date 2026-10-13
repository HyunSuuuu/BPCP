using Unity.AI.Navigation;
using UnityEngine;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 움직이는 오브젝트(손님, 들고 다니는 물건, 돈, 플레이어)를 NavMesh 굽기에서 뺀다.
    /// 빼지 않으면 다시 구울 때 그 자리에 구멍이 생겨 플레이어·알바가 막힌다.
    /// </summary>
    public static class NavMeshIgnore
    {
        public static void Mark(GameObject target)
        {
            if (target.TryGetComponent<NavMeshModifier>(out var modifier))
            {
                modifier.ignoreFromBuild = true;
                return;
            }

            modifier = target.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
        }
    }
}
