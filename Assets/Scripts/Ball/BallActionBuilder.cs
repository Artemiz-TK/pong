using System;
using System.Threading;
using UnityEngine;

namespace Ball
{
    // Interfaces que ditam a ordem permitida das funções
    public interface IBallDirectionStep
    {
        IBallModifierStep WithDirection(Vector2 direction = default);
        IBallModifierStep WithDirection(Action context);
        IBallModifierStep RevertingXAxis(); // Em vez de passar uma Action, usamos o contrato fluente
        IBallModifierStep KeepingCurrentDirection(); // Para quando você só quer aplicar o multiplicador/flip
    }

    public interface IBallModifierStep
    {
        IBallModifierStep MultipliedBy(float multiplier);
        // Na interface IBallModifierStep:
        IBallModifierStep ReflectFromFlip(float normalizedImpact, float flipperPositionX);
        IBallModifierStep ReflectVertical();
        Awaitable ExecuteAsync(float delayInSeconds); void Execute();
    }

    // O Builder que implementa a fluência através de Structs (Performance Máxima)
    public struct BallActionBuilder : IBallDirectionStep, IBallModifierStep
    {
        private readonly BallController m_Target;
        private Vector2 m_PendingDirection;
        private float m_SpeedMultiplier;
        private bool m_ShouldReflectVertical;
        private bool m_ShouldRevertX; // Nova flag
        private float? m_NormalizedImpact;
        private float m_FlipperPositionX;

        // Inicializador estático
        public static IBallDirectionStep For(BallController ball) => new BallActionBuilder(ball);

        private BallActionBuilder(BallController ball)
        {
            m_Target = ball;
            m_PendingDirection = Vector2.zero;
            m_SpeedMultiplier = 1f;
            m_ShouldReflectVertical = false;
            m_ShouldRevertX = false;
            m_NormalizedImpact = null;
            m_FlipperPositionX = 0f;
        }

        public IBallModifierStep WithDirection(Vector2 direction = default)
        {
            m_PendingDirection = direction;
            return this; // Retorna a própria struct modificada mudando o contrato para a próxima interface
        }

        public IBallModifierStep WithDirection(Action context)
        {
            context?.Invoke();
            return this; // Retorna a própria struct modificada mudando o contrato para a próxima interface
        }

        public IBallModifierStep RevertingXAxis()
        {
            m_ShouldRevertX = true;
            return this;
        }

        public IBallModifierStep KeepingCurrentDirection()
        {
            // Não faz nada com a direção, apenas avança para os modificadores
            return this;
        }

        public IBallModifierStep MultipliedBy(float multiplier)
        {
            m_SpeedMultiplier *= multiplier;
            return this;
        }

        public IBallModifierStep ReflectVertical()
        {
            m_ShouldReflectVertical = true;
            return this;
        }

        public IBallModifierStep ReflectFromFlip(float normalizedImpact, float flipperPositionX)
        {
            m_NormalizedImpact = Mathf.Clamp(normalizedImpact, -1f, 1f);
            m_FlipperPositionX = flipperPositionX;
            return this;
        }

        // Renomeado de "Permit" para "Execute" (Mais comum em APIs profissionais de comando)
        public void Execute()
        {
            if (m_Target == null) return;

            // 1. Primeiro aplicamos as mudanças de direção base
            if (m_PendingDirection != Vector2.zero)
                m_Target.SetDirection(m_PendingDirection);

            if (m_ShouldRevertX)
                m_Target.RevertXAxis();

            if (m_ShouldReflectVertical)
                m_Target.ReflectVertical();

            // 2. Depois aplicamos os modificadores de física
            if (!Mathf.Approximately(m_SpeedMultiplier, 1f))
                m_Target.ModifySpeed(m_SpeedMultiplier);

            // 3. Por último, o Flip Reflection, coletando a direção atualizada em tempo de execução
            if (m_NormalizedImpact.HasValue)
                ApplyFlipReflection();
        }

        // Versão assíncrona usando o Awaitable moderno do Unity
        public async Awaitable ExecuteAsync(float delayInSeconds)
        {
            using var cts = new CancellationTokenSource();
            try
            {
                await Awaitable.WaitForSecondsAsync(delayInSeconds, cts.Token);
                Execute();
            }
            catch (OperationCanceledException) { }
        }

        private void ApplyFlipReflection()
        {
            // 1. Descobrimos matematicamente de qual lado o player está em relação à bola
            // Se a bola está à direita do player, ela DEVE ir para a direita (+1). Se está à esquerda, vai para a esquerda (-1).
            float directionX = Mathf.Sign(m_Target.transform.position.x - m_FlipperPositionX);

            // 2. Calculamos o ângulo correto convertendo graus em um vetor de direção plano (Cos e Sin)
            // O impacto normalizado define o ângulo de saída baseado na direção X
            float angleInRadians = m_NormalizedImpact.Value * m_Target.MaxBounceAngle * Mathf.Deg2Rad;

            // Montamos o vetor unitário perfeito
            float outX = directionX * Mathf.Cos(angleInRadians);
            float outY = Mathf.Sin(angleInRadians);

            Vector2 finalDirection = new Vector2(outX, outY).normalized;

            // 3. Aplica a direção final na bola
            m_Target.SetDirection(finalDirection);
        }
    }
}
