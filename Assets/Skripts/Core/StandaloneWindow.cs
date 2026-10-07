using UnityEngine;

namespace BurgerIdle.Core
{
    /// <summary>
    /// PC 빌드 전용. 세로(9:16) 게임이므로 모니터 높이에 맞춘 세로 창으로 시작한다.
    /// PC 빌드는 마지막 창 크기를 레지스트리에 저장해 다음 실행에 쓰기 때문에, Player Settings 값 대신 시작 시 강제로 맞춘다.
    /// </summary>
    public static class StandaloneWindow
    {
        const float Aspect = 9f / 16f;
        const float ScreenHeightRatio = 0.85f; // 작업 표시줄·창 제목줄 여유

#if UNITY_STANDALONE && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            var height = Mathf.RoundToInt(Display.main.systemHeight * ScreenHeightRatio);
            var width = Mathf.RoundToInt(height * Aspect);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
#endif
    }
}
