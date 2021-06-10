using MLAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameValueScriptableObject gameValues;

    [SerializeField] private GameObject waitingUI;
    [SerializeField] private Text countdownText;
    public GameObject[] players;
    bool isReady; 
    public bool gameStart;
    public bool isCoutdown;
    int countdownValue = 4;

    [SerializeField] private Button playAgainButton;

    private void Start()
    {
        gameValues.playAgainRequest = false;
        gameValues.playAgain = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameStart) { return; }

        if (!isReady)
        {
            players = GameObject.FindGameObjectsWithTag("Player");

            if (players.Length == 2)
            {
                waitingUI.SetActive(false);
                isReady = true;
            }
        }
        else
        {
            if (!gameStart)
            {
                if (!countdownText.isActiveAndEnabled) { countdownText.enabled = true; }
                StartCoroutine(CountDown());
                isCoutdown = true;
                gameStart = true;
                isReady = false;
                playAgainButton.interactable = true;
            }
        }
    }

    private IEnumerator CountDown()
    {
        countdownValue--;

        yield return new WaitForSeconds(1f);

        if(countdownValue > 0)
        {
            countdownText.text = countdownValue.ToString();
            StartCoroutine(CountDown());

        }
        else if(countdownValue == 0)
        {
            countdownText.fontSize = 100;
            countdownText.text = "Fight";
            SetCanMove(true);
            StartCoroutine(CountDown());
        }
        else
        {
            countdownText.text = "4";
            countdownText.fontSize = 250;
            countdownText.enabled = false;
            isCoutdown = false;
            countdownValue = 4;
        }
    }

    private void  SetCanMove(bool value)
    {
        foreach (GameObject player in players)
        {
            PlayerBehav playerBehav = player.GetComponent<PlayerBehav>();
            playerBehav.canMove = value;
        }
    }

    //--------------------------------- REVENGE MODE --------------------------------------------
    public void PlayAgain()
    {
        PlayAgainRequest(NetworkManager.Singleton.LocalClientId);
    }

    void PlayAgainRequest(ulong clientid)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientid, out var networkClient))
        {
            return;
        }
        if (!networkClient.PlayerObject.TryGetComponent<PlayerBehav>(out var playerBehav))
        {
            return;
        }

        if (!gameValues.playAgainRequest)
        {
            playerBehav.PlayAgainRequestServerRpc(true);
            playAgainButton.interactable = false;
        }
        else
        {
            playerBehav.PlayAgainRequestServerRpc(false);
        }
    }
}
