using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MLAPI;
using MLAPI.Messaging;

public class PlayerBehav : NetworkBehaviour
{
    [SerializeField] private float moveSpeed;
    private Vector2 moveVec;

    GameObject AttackArea;

    void Start()
    {
        AttackArea = this.gameObject.transform.GetChild(1).gameObject;
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
        AttackArea.SetActive(true);
        Debug.Log(this.transform.name + " : is Attack");
    }
}
