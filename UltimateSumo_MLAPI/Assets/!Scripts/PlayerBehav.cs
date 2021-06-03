using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using System;

public class PlayerBehav : NetworkBehaviour
{
    [SerializeField] private float moveSpeed;
    private Vector2 moveVec;

    GameObject attackArea;

    GameObject blockDebugging;

    void Start()
    {
        attackArea = this.gameObject.transform.GetChild(1).gameObject;

        blockDebugging = this.gameObject.transform.GetChild(2).gameObject;
    }

    void FixedUpdate()
    {
        Move();
    }

    //------------------------------------- MOVE ------------------------------------------------
    //Get values from New Input System.
    public void OnMove(InputValue value) 
    {
        moveVec = value.Get<Vector2>();
    }

    private void Move()
    {
        this.transform.position += new Vector3(moveVec.x * moveSpeed, 0f, 0f) * Time.deltaTime;
    }

    //------------------------------------ ATTACK -----------------------------------------------
    public void OnAttack()
    {
        if (!IsOwner) { return; }

        AttackServerRpc();
    }

    [ServerRpc]
    private void AttackServerRpc()
    {
        AttackClientRpc();
    }

    [ClientRpc]
    private void AttackClientRpc()
    {
        attackArea.SetActive(true);
        Invoke("AttackEnd", 0.3f);
    }

    void AttackEnd()
    {
        attackArea.SetActive(false);
    }

    //------------------------------------ Block -----------------------------------------------
    bool isBlock;
    public void OnBlock(InputValue value)
    {
        if (!IsOwner) { return; }

        Debug.Log(value.Get<float>());
        switch (value.Get<float>())
        {
            case 0:
                isBlock = false;
                BlockServerRpc(isBlock);
                break;
            case 1:
                isBlock = true;
                BlockServerRpc(isBlock);
                break;
        }
    }

    [ServerRpc]
    private void BlockServerRpc(bool newValue)
    {
        BlockClientRpc(newValue);
    }

    [ClientRpc]
    private void BlockClientRpc(bool newValue)
    {
        switch (newValue)
        {
            case true:
                blockDebugging.SetActive(true);
                break;
            case false:
                blockDebugging.SetActive(false);
                break;
        }
    }

    //---------------------------------- Collision ---------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) { return; }

        if (collision.gameObject.CompareTag("Attack") && !isBlock)
        {
            if (IsServer)
            {
                this.transform.position += new Vector3(-50f, 0f, 0f) * Time.deltaTime;
            }
            else
            {
                this.transform.position += new Vector3(50f, 0f, 0f) * Time.deltaTime;
            }
        }
    }
}
