using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum VictoryCondition { AllEnemiesDead, VictoryTargetDead }

    [SerializeField] private VictoryCondition victoryCondition = VictoryCondition.VictoryTargetDead;

    [Header("UI")]
    [SerializeField] private GameObject endScreen;      // Canvas-Kind, im Editor deaktiviert
    [SerializeField] private TMP_Text endText;
    [SerializeField] private float restartDelay = 5f;

    private bool isGameOver;
    public bool IsGameOver => isGameOver;

    public void ReportEnemyDeath(UnitStats deadStats, int remainingEnemies)
    {
        if (isGameOver) return;

        switch (victoryCondition)
        {
            case VictoryCondition.AllEnemiesDead:
                if (remainingEnemies <= 0) EndGame("SIEG");
                break;

            case VictoryCondition.VictoryTargetDead:
                if (deadStats != null && deadStats.IsVictoryTarget) EndGame("KÖNIGSMÖRDER!");
                break;
        }
    }

    public void ReportPlayerDeath()
    {
        if (isGameOver) return;
        EndGame("GAME OVER");
    }

    private void EndGame(string message)
    {
        isGameOver = true;

        if (endScreen != null) endScreen.SetActive(true);
        if (endText != null) endText.text = message;

        Time.timeScale = 0f;
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        // Realtime, weil timeScale auf 0 steht und WaitForSeconds sonst nie ablaeuft
        yield return new WaitForSecondsRealtime(restartDelay);

        Time.timeScale = 1f;   // MUSS zurueckgesetzt werden, sonst startet die Szene eingefroren
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}