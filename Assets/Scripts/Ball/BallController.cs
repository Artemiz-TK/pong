using System;
using System.Threading;
using UnityEngine;

namespace Ball
{
    public class BallController : MonoBehaviour
    {
        public enum PermitAction
        {
            None,
            Spawn,
            Move
        }

        public struct WithInteraction
        {
            private readonly BallController m_Parent;

            public WithInteraction(BallController parent)
            {
                m_Parent = parent;
            }

            public BallController And() => m_Parent;
        }

        [Header("Movement")]
        [SerializeField] private float m_MoveSpeed = 5f;
        [SerializeField] private float m_MaxMoveSpeed = 15f;

        [Header("Bounce")]
        [SerializeField] private float m_MaxBounceAngle = 30f;

        private Vector2 m_Move = Vector2.right;

        private Rigidbody2D m_Body;

        private PermitAction m_CurrentAction =
            PermitAction.None;

        private int m_LastFlipId = -1;

        public int LastFlipId => m_LastFlipId;

        private void Awake()
        {
            m_Body = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            Permit(PermitAction.Move);
        }

        private void FixedUpdate()
        {
            Permit(PermitAction.Move);

            m_Body.linearVelocity =
                m_Move * m_MoveSpeed;
        }

        public BallController Initialize(Vector2 direction)
        {
            SetDirection(direction);

            return this;
        }

        public void RegisterFlipHit(int flipId)
        {
            m_LastFlipId = flipId;
        }

        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            m_Move = direction.normalized;
        }

        public BallController ReflectFromFlip(
            float normalizedImpact)
        {
            normalizedImpact =
                Mathf.Clamp(
                    normalizedImpact,
                    -1f,
                    1f
                );

            float angle =
                normalizedImpact *
                m_MaxBounceAngle;

            float directionX =
                -Mathf.Sign(m_Move.x);

            float directionY =
                Mathf.Tan(
                    angle * Mathf.Deg2Rad
                );

            SetDirection(
                new Vector2(
                    directionX,
                    directionY
                )
            );

            return this;
        }

        public BallController ReflectVertical()
        {
            m_Move.y = -m_Move.y;

            SetDirection(m_Move);

            return this;
        }

        public BallController MultiplyVelocity(
            float multiplier)
        {
            m_MoveSpeed *= multiplier;

            Permit(PermitAction.Move);

            return this;
        }

        public WithInteraction With(Vector2 direction)
        {
            SetDirection(direction);

            return new WithInteraction(this);
        }

        public BallController Permit(
            PermitAction action)
        {
            m_CurrentAction = action;

            switch (m_CurrentAction)
            {
                case PermitAction.Spawn:

                    Instantiate(
                        gameObject,
                        Vector3.zero,
                        transform.rotation
                    );

                    break;

                case PermitAction.Move:

                    m_MoveSpeed =
                        Mathf.Clamp(
                            m_MoveSpeed,
                            0f,
                            m_MaxMoveSpeed
                        );

                    break;
            }

            return this;
        }

        public async Awaitable<BallController> PermitAsync(
            PermitAction action,
            float seconds)
        {
            using var cts =
                new CancellationTokenSource();

            try
            {
                await Awaitable.WaitForSecondsAsync(
                    seconds,
                    cts.Token
                );

                Permit(action);
            }
            catch (OperationCanceledException)
            {
            }

            return this;
        }
    }
}