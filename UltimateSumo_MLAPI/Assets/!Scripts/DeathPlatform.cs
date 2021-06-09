using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathPlatform : MonoBehaviour
{
    [SerializeField] GameObject gameOverCanvas;
    Text playerName;

    GameObject[] players;

    private void Start()
    {
        playerName = gameOverCanvas.transform.GetChild(2).GetComponent<Text>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            players = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject player in players)
            {
                PlayerBehav playerBehav = player.GetComponent<PlayerBehav>();
                playerBehav.canMove = false;

                if (player.transform.name != collision.transform.name)
                {
                    playerName.text = player.transform.name;
                    playerBehav.ChangeWinCountServerRpc();
                }  
            }

            gameOverCanvas.SetActive(true);
        }
    }
}
