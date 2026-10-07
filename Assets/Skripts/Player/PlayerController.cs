using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BurgerIdle.Player
{
    /// <summary>
    /// 조이스틱(가상 Gamepad 왼쪽 스틱) 또는 WASD 입력으로 XZ 평면을 이동한다. 이동 방향은 카메라 기준.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        const float MoveThreshold = 0.01f;

        readonly ReactiveProperty<bool> isMoving = new ReactiveProperty<bool>(false);

        InputAction moveAction;
        PlayerStats stats;
        Transform cameraTransform;
        Rect bounds;
        float turnSpeed;

        /// <summary>입력이 있는 동안 true. 해금 박스의 정지 판정에 쓴다.</summary>
        public ReadOnlyReactiveProperty<bool> IsMoving => isMoving;

        public void Init(PlayerStats stats, Transform cameraTransform, Rect bounds, float turnSpeed)
        {
            this.stats = stats;
            this.cameraTransform = cameraTransform;
            this.bounds = bounds;
            this.turnSpeed = turnSpeed;
        }

        void Awake()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            isMoving.AddTo(this);
        }

        void OnEnable() => moveAction.Enable();

        void OnDisable() => moveAction.Disable();

        void OnDestroy() => moveAction.Dispose();

        void Update()
        {
            if (stats == null)
                return;

            var input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            var moving = input.sqrMagnitude > MoveThreshold * MoveThreshold;
            isMoving.Value = moving;
            if (!moving)
                return;

            var yaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            var direction = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);

            var position = transform.position + direction * (stats.MoveSpeed.CurrentValue * Time.deltaTime);
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
            position.z = Mathf.Clamp(position.z, bounds.yMin, bounds.yMax);
            transform.position = position;

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
