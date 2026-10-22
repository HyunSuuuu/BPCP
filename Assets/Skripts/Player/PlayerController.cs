using R3;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace BurgerIdle.Player
{
    /// <summary>
    /// 조이스틱(가상 Gamepad 왼쪽 스틱) 또는 WASD 입력으로 XZ 평면을 이동한다. 이동 방향은 카메라 기준.
    /// NavMeshAgent가 있으면 NavMesh 안으로만 움직인다 (매장 확장에 따라 범위가 넓어지고,
    /// 시설은 통과하지 않고 미끄러지듯 돌아간다). NavMesh가 없는 씬이면 바닥 사각형 안으로 제한한다.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        const float MoveThreshold = 0.01f;
        const float NavMeshSearchRadius = 2f;
        const float TeleportSearchRadius = 10f;

        readonly ReactiveProperty<bool> isMoving = new ReactiveProperty<bool>(false);

        InputAction moveAction;
        PlayerStats stats;
        Transform cameraTransform;
        Rect bounds;
        float turnSpeed;
        NavMeshAgent agent;

        /// <summary>입력이 있는 동안 true. 해금 박스의 정지 판정에 쓴다.</summary>
        public ReadOnlyReactiveProperty<bool> IsMoving => isMoving;

        /// <param name="agent">NavMesh 이동 제한용 (없으면 bounds 사각형으로 제한)</param>
        public void Init(PlayerStats stats, Transform cameraTransform, Rect bounds, float turnSpeed, NavMeshAgent agent)
        {
            this.stats = stats;
            this.cameraTransform = cameraTransform;
            this.bounds = bounds;
            this.turnSpeed = turnSpeed;
            this.agent = agent;

            if (agent != null)
            {
                // 이동·회전은 이 스크립트가 직접 한다. 에이전트는 NavMesh 안으로 위치를 제한하는 용도
                agent.updateRotation = false;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                TrySnapToNavMesh();
            }
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
            var delta = direction * (stats.MoveSpeed.CurrentValue * Time.deltaTime);

            if (agent != null)
            {
                // NavMesh 경계·장애물을 따라 미끄러지듯 이동. NavMesh를 다시 굽는 중이라 잠시 벗어나 있으면 움직이지 않는다
                // (바닥 사각형으로 제한하면 다른 지역에 있을 때 엉뚱한 바닥 범위로 끌려감)
                if (TrySnapToNavMesh())
                    agent.Move(delta);
            }
            else
            {
                var position = transform.position + delta;
                position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
                position.z = Mathf.Clamp(position.z, bounds.yMin, bounds.yMax);
                transform.position = position;
            }

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        /// <summary>순간이동 (지역 이동). NavMesh가 있으면 가장 가까운 NavMesh 위로 옮긴다.</summary>
        public void Teleport(Vector3 position)
        {
            if (NavMesh.SamplePosition(position, out var hit, TeleportSearchRadius, NavMesh.AllAreas))
                position = hit.position;

            if (agent != null && agent.isActiveAndEnabled && agent.Warp(position))
                return;

            transform.position = position;
        }

        /// <summary>NavMesh 위에 있거나 가까운 NavMesh로 옮길 수 있으면 true.</summary>
        bool TrySnapToNavMesh()
        {
            if (!agent.isActiveAndEnabled)
                return false;
            if (agent.isOnNavMesh)
                return true;
            return NavMesh.SamplePosition(transform.position, out var hit, NavMeshSearchRadius, NavMesh.AllAreas)
                   && agent.Warp(hit.position);
        }
    }
}
