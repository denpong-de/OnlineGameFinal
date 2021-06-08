using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject waitingUI;
    [SerializeField] private Text countdownText;
    public GameObject[] players;
    bool isReady; 
    public bool gameStart;
    int countdownValue = 4;

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
                StartCoroutine(CountDown());
                gameStart = true;
                isReady = false;
                if (players != null) { Array.Clear(players, 0, players.Length); }
            }
        }

        Debug.Log(players.Length);
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
            StartCoroutine(CountDown());
        }
        else
        {
            countdownText.enabled = false;
            countdownValue = 4;
        }
    }
}
