using System;
using UnityEngine;
using UnityEngine.UI;

public class StartPopup : MonoBehaviour
{
    public ConveyorBeltGrid firstConveyorBeltGrid;
    public Button startButton;

    private void Start()
    {
        startButton.onClick.AddListener(StartGame);
    }

    private void StartGame()
    {
        firstConveyorBeltGrid.enableSpawning = true;
        gameObject.SetActive(false);
    }
}
