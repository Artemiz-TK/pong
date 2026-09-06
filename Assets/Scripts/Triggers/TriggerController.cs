using Ball;
using Callback;
using UnityEngine;

public class TriggerController : MonoBehaviour
{
    public enum Side
    {
        Left,
        Right
    }

    [SerializeField]
    private Side m_Side;

    [SerializeField]
    private BallController m_BallPrefab;

    private async Awaitable OnTriggerEnter2D(
        Collider2D other)
    {
        if (!other.CompareTag("Ball"))
            return;

        var ball =
            other.GetComponent<BallController>();

        if (ball == null)
            return;

        var playerId =
            m_Side == Side.Left
                ? 2
                : 1;

        CallbackTrigger.ScoreChanged(playerId);

        Destroy(other.gameObject);

        var spawnDirection =
            playerId == 1
                ? Vector2.right
                : Vector2.left;

        await SpawnBall(spawnDirection);
    }

    private async Awaitable SpawnBall(Vector2 direction)
    {
        await Awaitable.WaitForSecondsAsync(.3f);

        var ball =
            Instantiate(
                m_BallPrefab,
                Vector3.zero,
                Quaternion.identity
            );

        ball.SetDirection(direction);
    }
}
