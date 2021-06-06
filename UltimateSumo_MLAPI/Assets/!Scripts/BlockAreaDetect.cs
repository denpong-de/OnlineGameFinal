using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockAreaDetect : MonoBehaviour
{
    PlayerBehav playerBehav;

    // Start is called before the first frame update
    void Start()
    {
        playerBehav = transform.parent.gameObject.GetComponent<PlayerBehav>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Throw") && playerBehav.isBlock)
        {
            playerBehav.SwitchSide();
        }
    }
}
