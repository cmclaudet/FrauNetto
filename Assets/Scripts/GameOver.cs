using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public Button RestartButton;
    public CinemachineCamera GameOverCamera;
    public ConveyorBeltGrid[] grids;
    public Player player;
    public CanvasGroup canvasGroup;
    
    void Start()
    {
        RestartButton.onClick.AddListener(Restart);
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetUp()
    {
        foreach (var grid in grids)
        {
            grid.enableSpawning = false;
            grid.shouldMoveItems = false;
        }
        
        player.CanControl = false;
    }

    public void Display()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        GameOverCamera.Priority = 10;
    }
}
