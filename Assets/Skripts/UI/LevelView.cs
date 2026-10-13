using BurgerIdle.Economy;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 레벨·경험치 표시. 레벨 숫자(형식 지정 가능), 경험치 바, "현재 / 필요량". 레벨이 오르면 레벨 숫자가 튄다.
    /// HUD의 레벨 묶음 프리팹 루트에 붙인다. 연결하지 않은 칸은 무시한다.
    /// </summary>
    public sealed class LevelView : MonoBehaviour
    {
        [SerializeField] TMP_Text levelText;
        [Tooltip("레벨 표시 형식. {0}이 레벨 숫자 (예: {0}, Lv {0})")]
        [SerializeField] string levelFormat = "{0}";
        [Tooltip("Image Type = Filled, Horizontal")]
        [SerializeField] Image expBar;
        [Tooltip("\"현재 / 필요량\" 표시. 최대 레벨이면 MAX")]
        [SerializeField] TMP_Text expText;
        [SerializeField] float barDuration = 0.3f;

        public void Bind(PlayerLevel playerLevel)
        {
            playerLevel.Level
                .Subscribe(level =>
                {
                    if (levelText != null)
                        levelText.text = string.IsNullOrEmpty(levelFormat) || !levelFormat.Contains("{0")
                            ? level.ToString()
                            : string.Format(levelFormat, level);
                })
                .AddTo(this);

            Observable.CombineLatest(playerLevel.ExpInLevel, playerLevel.ExpToNext, (current, need) => (current, need))
                .Subscribe(x =>
                {
                    if (expText != null)
                        expText.text = x.need > 0 ? $"{x.current:N0} / {x.need:N0}" : "MAX";
                })
                .AddTo(this);

            playerLevel.Progress
                .Subscribe(progress =>
                {
                    if (expBar == null)
                        return;
                    expBar.DOKill();
                    expBar.DOFillAmount(progress, barDuration).SetLink(expBar.gameObject);
                })
                .AddTo(this);

            playerLevel.Level
                .Pairwise()
                .Where(pair => pair.Item2 > pair.Item1)
                .Subscribe(_ => PunchLevel())
                .AddTo(this);
        }

        void PunchLevel()
        {
            if (levelText == null)
                return;

            levelText.transform.DOKill(true);
            levelText.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 0.5f).SetLink(levelText.gameObject);
        }
    }
}
