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
    //MOVE
    [SerializeField] private float moveSpeed;
    private Vector2 moveVec;
    bool isLeft;

    //ATTACK
    GameObject attackArea;

    //BLOCK
    GameObject blockArea;
    public bool isBlock;

    //THROW
    GameObject throwArea;

    void Start()
    {
        if (IsServer) { isLeft = true; }

        attackArea = this.gameObject.transform.GetChild(1).gameObject;

        blockArea = this.gameObject.transform.GetChild(2).gameObject;

        throwArea = this.gameObject.transform.GetChild(3).gameObject;
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

    //------------------------------------ BLOCK -----------------------------------------------
    public void OnBlock(InputValue value)
    {
        if (!IsOwner) { return; }

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
                blockArea.SetActive(true);
                break;
            case false:
                blockArea.SetActive(false);
                break;
        }
    }

    //------------------------------------ THROW -----------------------------------------------
    public void OnThrow()
    {
        if (!IsOwner) { return; }

        ThrowServerRpc();
    }

    [ServerRpc]
    private void ThrowServerRpc()
    {
        ThrowClientRpc();
    }

    [ClientRpc]
    private void ThrowClientRpc()
    {
        throwArea.SetActive(true);
        Invoke("ThrowEnd", 0.3f);
    }

    public void ThrowEnd()
    {
        throwArea.SetActive(false);
    }

    public void SwitchSide()
    {
        switch (isLeft)
        {
            case true:
                this.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                this.transform.position = this.transform.position + new Vector3(2f, 0f, 0f);
                isLeft = false;
                break;
            case false:
                this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                this.transform.position = this.transform.position + new Vector3(-2f, 0f, 0f);
                isLeft = true;
                break;
        }
    }

    //---------------------------------- COLLISION ---------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) { return; }

        if (collision.gameObject.CompareTag("Attack") && !isBlock)
        {
            switch (isLeft)
            {
                case true:
                    this.transform.position += new Vector3(-50f, 0f, 0f) * Time.deltaTime;
                    break;
                case false:
                    this.transform.position += new Vector3(50f, 0f, 0f) * Time.deltaTime;
                    break;
            }
        }
    }
}
