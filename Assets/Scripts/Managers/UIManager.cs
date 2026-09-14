using Callback;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Manager that it's only in a scene and is responsible for updating the UI elements, such as the score display.
    /// </summary>
    /// <remarks>
    /// This class listens for score changes and updates the corresponding UI elements accordingly.
    /// It subscribes to the OnScoreAdded event from the CallbackTrigger class to receive notifications when the score changes.
    /// Once that its class only it's in a scene, it dosn't need to be a singleton, as it will be destroyed when the scene is unloaded.
    /// </remarks>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Player References")]
        [SerializeField] private TMP_Text m_Player1Score;
        [SerializeField] private TMP_Text m_Player2Score;

        [Header("UI Victory Text")]
        [SerializeField] private TMP_Text m_VictoryText;

        private void OnEnable()
        {
            CallbackTrigger.OnScoreAdded += UpdateScore;
            CallbackTrigger.OnGameOvered += GameOver;
        }

        private void OnDisable()
        {
            CallbackTrigger.OnScoreAdded -= UpdateScore;
            CallbackTrigger.OnGameOvered -= GameOver;
        }

        private void UpdateScore(int value, int player)
        {
            switch (player)
            {
                case 1:
                    m_Player1Score.text = value.ToString();
                    break;
                case 2:
                    m_Player2Score.text = value.ToString();
                    break;
            }
        }

        private void GameOver(int player)
        {
            m_VictoryText.text = $"O jogador {player} ganhou.";
            
        }
    }
}