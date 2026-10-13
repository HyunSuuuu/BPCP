using BurgerIdle.Core;
using BurgerIdle.Economy;
using BurgerIdle.Pickup;
using BurgerIdle.Player;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.Unlock
{
    /// <summary>
    /// 해금 박스. 플레이어가 범위 안에서 일정 시간 정지하면 소지금을 서서히 투입하고(부분 지불 허용),
    /// 비용에 도달하면 박스가 사라지고 대상 오브젝트가 나타난다.
    /// </summary>
    [RequireComponent(typeof(InteractionZone))]
    public sealed class UnlockBox : MonoBehaviour
    {
        const float CoinInterval = 0.08f;

        [Header("Data")]
        [Tooltip("비용·필요 레벨")]
        [SerializeField] UnlockDefinition definition;
        [Tooltip("해금되면 나타날 씬 오브젝트. 게임 시작 시 자동으로 숨긴다. 이 박스의 자식이면 안 된다.")]
        [SerializeField] GameObject target;

        [Header("Visual (World Space UI)")]
        [Tooltip("돈이 들어올 때 살짝 튀는 대상 (보통 World Space Canvas)")]
        [SerializeField] Transform visualRoot;
        [Tooltip("Image Type = Filled. 투입 비율이 fillAmount가 된다")]
        [SerializeField] Image fillImage;
        [Tooltip("비용 표시 묶음 (비용 텍스트 + 돈 아이콘)")]
        [SerializeField] GameObject costGroup;
        [SerializeField] TMP_Text costText;
        [Tooltip("레벨 잠금 표시 묶음 (자물쇠 + 레벨 텍스트 + 별). 필요 레벨 미달일 때 표시")]
        [SerializeField] GameObject lockedGroup;
        [SerializeField] TMP_Text levelText;

        readonly ReactiveProperty<int> paid = new ReactiveProperty<int>(0);
        readonly ReactiveProperty<bool> isUnlocked = new ReactiveProperty<bool>(false);

        InteractionZone zone;
        CurrencyManager currency;
        PlayerController player;
        GameConfig config;
        PlayerLevel playerLevel;
        bool isLocked;
        Vector3 visualScale;
        Vector3 targetScale;
        float stillTime;
        float payBudget;
        float coinTimer;

        public UnlockDefinition Definition => definition;
        public GameObject Target => target;
        public ReadOnlyReactiveProperty<int> Paid => paid;
        public ReadOnlyReactiveProperty<bool> IsUnlocked => isUnlocked;
        public int Remaining => definition.cost - paid.Value;

        void Awake()
        {
            paid.AddTo(this);
            isUnlocked.AddTo(this);
            zone = GetComponent<InteractionZone>();
            if (visualRoot != null)
                visualScale = visualRoot.localScale;
        }

        public void Init(CurrencyManager currency, PlayerController player, GameConfig config, PlayerLevel playerLevel)
        {
            this.currency = currency;
            this.player = player;
            this.config = config;
            this.playerLevel = playerLevel;

            if (definition == null)
            {
                Debug.LogError($"[UnlockBox] {name}: UnlockDefinition이 비어 있습니다.", this);
                enabled = false;
                return;
            }

            if (target != null)
            {
                targetScale = target.transform.localScale;
                target.SetActive(false);
            }

            if (levelText != null)
                levelText.text = definition.requiredPlayerLevel.ToString();

            playerLevel.Level
                .Subscribe(level => SetLocked(level < definition.requiredPlayerLevel))
                .AddTo(this);
            paid.Subscribe(_ => UpdateVisual()).AddTo(this);
        }

        /// <summary>필요 레벨 미달이면 잠금 표시를 띄우고 돈을 받지 않는다.</summary>
        void SetLocked(bool locked)
        {
            isLocked = locked;
            if (costGroup != null)
                costGroup.SetActive(!locked);
            if (lockedGroup != null)
                lockedGroup.SetActive(locked);
        }

        void Update()
        {
            if (currency == null || isUnlocked.Value)
                return;

            var dt = Time.deltaTime;
            coinTimer -= dt;

            // 잠겨 있거나, 범위 밖이거나, 움직이면 정지 시간을 처음부터 다시 잰다
            if (isLocked || !zone.Contains(player.transform.position) || player.IsMoving.CurrentValue)
            {
                stillTime = 0f;
                payBudget = 0f;
                return;
            }

            stillTime += dt;
            if (stillTime < config.unlockStillTime)
                return;

            payBudget += definition.cost / config.unlockPayDuration * dt;
            var request = Mathf.Min((int)payBudget, Remaining);
            if (request <= 0)
                return;

            payBudget -= request;
            var spent = currency.SpendUpTo(request);
            if (spent == 0)
            {
                payBudget = 0f;
                return;
            }

            paid.Value += spent;
            SpawnCoin();

            if (Remaining <= 0)
                Complete();
        }

        void SpawnCoin()
        {
            if (config.moneyPrefab == null || coinTimer > 0f)
                return;

            coinTimer = CoinInterval;
            var coin = ObjectPools.Get(config.moneyPrefab, player.transform.position + Vector3.up * 1.5f, Quaternion.identity);
            coin.transform.DOJump(transform.position, 1f, 1, 0.3f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    coin.Release();
                    PunchVisual();
                });
        }

        void PunchVisual()
        {
            if (visualRoot == null || isUnlocked.Value)
                return;

            visualRoot.DOKill(true);
            visualRoot.localScale = visualScale;
            visualRoot.DOPunchScale(visualScale * 0.08f, 0.15f, 4, 0.5f);
        }

        void Complete()
        {
            isUnlocked.Value = true;
            if (definition.grantLevel > 0)
                playerLevel.RaiseTo(definition.grantLevel);

            transform.DOKill();
            transform.DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));

            if (target == null)
                return;

            target.SetActive(true);
            var t = target.transform;
            t.localScale = Vector3.zero;
            t.DOScale(targetScale, 0.35f).SetEase(Ease.OutBack).SetDelay(0.1f);
        }

        void UpdateVisual()
        {
            if (costText != null)
                costText.text = Remaining.ToString("N0");

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(paid.Value / (float)definition.cost);
        }
    }
}
