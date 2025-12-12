using UnityEngine;
using UnityEngine.UI;

public class PackButton : MonoBehaviour
{
    public Button button;
    public EndPopup endPopup;
    public ConveyorBeltGrid[] grids;
    public Player player;
    
    void Start()
    {
        button.onClick.AddListener(Pack);
    }

    private void Pack()
    {
        SetUp();
        endPopup.Display();
    }
    
    private void SetUp()
    {
        foreach (var grid in grids)
        {
            grid.enableSpawning = false;
            grid.shouldMoveItems = false;
        }
        
        player.CanControl = false;
    }

}
