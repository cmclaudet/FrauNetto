using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndPopup : MonoBehaviour
{
    public Button restartButton;
    public Player player;
    public Bag bag;
    public TextMeshProUGUI descriptionText;
    public CanvasGroup canvasGroup;
    
    void Start()
    {
        restartButton.onClick.AddListener(Restart);
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Display()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        var occupiedCellCount = bag.GetOccupiedCellCount();
        var totalCellCount = bag.GetTotalCellCount();
        var fillPercent = Mathf.RoundToInt(occupiedCellCount / (float)totalCellCount);
        var timeSeconds = player.TotalTime;
        var score = 1000f * fillPercent / timeSeconds;

        descriptionText.text = GetDescriptionText(occupiedCellCount, totalCellCount, Mathf.RoundToInt(timeSeconds), Mathf.RoundToInt(score));
    }

    private string GetDescriptionText(int amount, int total, int timeSeconds, int score)
    {
        return $"Bag filled: {amount}/{total}\n\nTime taken: {timeSeconds}s\n\n<b>Score: {score}</b>";
    }
}
