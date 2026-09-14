using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Network
{
    public class PongUdpClient : MonoBehaviour
    {
        [Header("Connection")] [SerializeField]
        private string m_ServerAddress = "127.0.0.1";

        [SerializeField] private int m_ServerPort = 5001;

        [Header("Game References")] [SerializeField]
        private Transform m_LocalFlip;

        [SerializeField] private Transform m_RemoteFlip;

        [SerializeField] private Transform m_Ball;

        [SerializeField] private UI.UIManager m_UIManager;

        private UdpClient m_Client;

        private IPEndPoint m_ServerEndpoint;

        private Thread m_ReceiveThread;

        private volatile bool m_Running;

        private int m_MyPlayerId = -1;

        private Vector3 m_RemoteFlipPosition;

        private Vector3 m_RemoteBallPosition;

        private bool m_HasRemoteFlip;

        private bool m_HasRemoteBall;

        private readonly ConcurrentQueue<Action>
            m_MainThreadActions = new();

        private void Start()
        {
            m_Client = new UdpClient();

            m_ServerEndpoint =
                new IPEndPoint(
                    IPAddress.Parse(
                        m_ServerAddress
                    ),
                    m_ServerPort
                );

            m_Client.Connect(
                m_ServerEndpoint
            );

            m_Running = true;

            m_ReceiveThread =
                new Thread(ReceiveData)
                {
                    IsBackground = true
                };

            m_ReceiveThread.Start();

            Send("HELLO");

            Debug.Log(
                "[CLIENT] Conectando ao servidor..."
            );
        }

        private void Update()
        {
            while (m_MainThreadActions.TryDequeue(
                       out var action))
            {
                action?.Invoke();
            }

            if (m_MyPlayerId <= 0)
                return;

            SendFlipPosition();

            if (m_MyPlayerId == 1)
            {
                SendBallPosition();
            }

            UpdateRemoteObjects();
        }
        
        public bool IsLocalPlayer(int playerId)
        {
            return m_MyPlayerId == playerId;
        }

        private void SendFlipPosition()
        {
            if (m_LocalFlip == null)
                return;

            var message =
                FormattableString.Invariant(
                    $"FLIP:{m_MyPlayerId};{m_LocalFlip.position.x:F3};{m_LocalFlip.position.y:F3}"
                );

            Send(message);
        }

        private void SendBallPosition()
        {
            if (m_Ball == null)
                return;

            string message =
                FormattableString.Invariant(
                    $"BALL:{m_Ball.position.x:F3};{m_Ball.position.y:F3}"
                );

            Send(message);
        }

        private void ReceiveData()
        {
            IPEndPoint remoteEndpoint =
                new(IPAddress.Any, 0);

            while (m_Running)
            {
                try
                {
                    byte[] data =
                        m_Client.Receive(
                            ref remoteEndpoint
                        );

                    string message =
                        Encoding.UTF8.GetString(data);

                    HandleMessage(message);
                }
                catch (SocketException)
                {
                    if (!m_Running)
                        break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[CLIENT] {exception}"
                    );
                }
            }
        }

        private void HandleMessage(
            string message)
        {
            Debug.Log(
                $"[CLIENT] Recebi: {message}"
            );

            if (message.StartsWith(
                    "ASSIGN:",
                    StringComparison.Ordinal))
            {
                if (!int.TryParse(
                        message.Substring(7),
                        out int id))
                {
                    return;
                }

                m_MainThreadActions.Enqueue(() =>
                {
                    m_MyPlayerId = id;

                    Debug.Log(
                        $"[CLIENT] Meu ID = {id}"
                    );
                });

                return;
            }

            if (message.StartsWith(
                    "FLIP:",
                    StringComparison.Ordinal))
            {
                ParseFlip(message);

                return;
            }

            if (message.StartsWith(
                    "BALL:",
                    StringComparison.Ordinal))
            {
                ParseBall(message);

                return;
            }

            if (message.StartsWith(
                    "STATE:SCORE;",
                    StringComparison.Ordinal))
            {
                ParseScore(message);

                return;
            }

            if (message == "FULL")
            {
                m_MainThreadActions.Enqueue(() =>
                {
                    Debug.LogWarning(
                        "[CLIENT] Servidor cheio."
                    );
                });
            }
        }

        private void ParseFlip(string message)
        {
            string payload =
                message.Substring(5);

            string[] parts =
                payload.Split(';');

            if (parts.Length != 3)
                return;

            if (!int.TryParse(
                    parts[0],
                    out int playerId))
            {
                return;
            }

            if (playerId == m_MyPlayerId)
                return;

            if (!float.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float x))
            {
                return;
            }

            if (!float.TryParse(
                    parts[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float y))
            {
                return;
            }

            m_RemoteFlipPosition =
                new Vector3(x, y, 0f);

            m_HasRemoteFlip = true;
        }

        private void ParseBall(string message)
        {
            string payload =
                message.Substring(5);

            string[] parts =
                payload.Split(';');

            if (parts.Length != 2)
                return;

            if (!float.TryParse(
                    parts[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float x))
            {
                return;
            }

            if (!float.TryParse(
                    parts[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float y))
            {
                return;
            }

            m_RemoteBallPosition =
                new Vector3(x, y, 0f);

            m_HasRemoteBall = true;
        }

        private void ParseScore(string message)
        {
            string payload =
                message.Substring(
                    "STATE:SCORE;".Length
                );

            string[] parts =
                payload.Split(';');

            if (parts.Length != 2)
                return;

            if (!int.TryParse(
                    parts[0],
                    out int player1Score))
            {
                return;
            }

            if (!int.TryParse(
                    parts[1],
                    out int player2Score))
            {
                return;
            }

            m_MainThreadActions.Enqueue(() =>
            {
                Debug.Log(
                    $"[CLIENT] Score: " +
                    $"{player1Score} x {player2Score}"
                );

                // Aqui vamos integrar seu UIManager.
            });
        }

        private void UpdateRemoteObjects()
        {
            if (m_HasRemoteFlip &&
                m_RemoteFlip != null)
            {
                m_RemoteFlip.position =
                    Vector3.Lerp(
                        m_RemoteFlip.position,
                        m_RemoteFlipPosition,
                        Time.deltaTime * 15f
                    );
            }

            if (m_MyPlayerId != 1 &&
                m_HasRemoteBall &&
                m_Ball != null)
            {
                m_Ball.position =
                    Vector3.Lerp(
                        m_Ball.position,
                        m_RemoteBallPosition,
                        Time.deltaTime * 20f
                    );
            }
        }

        public void SendScore(int playerId)
        {
            Send($"SCORE:{playerId}");
        }

        private void Send(string message)
        {
            if (m_Client == null)
                return;

            try
            {
                byte[] data =
                    Encoding.UTF8.GetBytes(message);

                Debug.Log(
                    $"[CLIENT] Enviando: {message}"
                );

                m_Client.Send(
                    data,
                    data.Length
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[CLIENT] Erro ao enviar: {exception}"
                );
            }
        }

        public int MyPlayerId =>
            m_MyPlayerId;

        private void OnApplicationQuit()
        {
            StopClient();
        }

        private void OnDestroy()
        {
            StopClient();
        }

        private void StopClient()
        {
            if (!m_Running)
                return;

            m_Running = false;

            try
            {
                m_Client?.Close();
            }
            catch
            {
            }

            if (m_ReceiveThread != null &&
                m_ReceiveThread.IsAlive)
            {
                m_ReceiveThread.Join(100);
            }

            m_Client = null;
        }
    }
}