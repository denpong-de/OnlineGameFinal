using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    bool canMove = true;

    //ATTACK
    GameObject attackArea;

    //BLOCK
    GameObject blockArea;
    bool isBlock;
    public GameValueScriptableObject gameValues;

    //THROW
    GameObject throwArea;
    public bool isThrow
    {
        get { return _isThrow; }
        set
        {
            _isThrow = value;
            canMove = false;
            SwitchSideDelay(0.5f);
            StartCoroutine(KnockbackDelay(0.5f, 25f));
        }
    }
    private bool _isThrow;

    //KNOCKBACK
    Text knockbackP1Text, knockbackP2Text;
    private NetworkVariableFloat knockbackValue = new NetworkVariableFloat(0f);
    float knockbackMultiply;

    //DEBUGGING
    [Header("Debugging")]
    [SerializeField] bool forceBlock;

    void Awake()
    {
        if (IsServer) { isLeft = true; }

        attackArea = this.gameObject.transform.GetChild(1).gameObject;

        blockArea = this.gameObject.transform.GetChild(2).gameObject;

        throwArea = this.gameObject.transform.GetChild(3).gameObject;
        gameValues.switchSideTrigger = false; //Set to default

        knockbackP1Text = GameObject.FindGameObjectWithTag("KnockT1").GetComponent<Text>();
        knockbackP1Text.text = "0%";
        knockbackP2Text = GameObject.FindGameObjectWithTag("KnockT2").GetComponent<Text>();
        knockbackP2Text.text = "0%";
    }

    private void Update()
    {
        if (!isBlock) { return; }
        if (isThrow == gameValues.switchSideTrigger) { return; }

        isThrow = gameValues.switchSideTrigger;
    }

    void FixedUpdate()
    {
        if (!canMove) { return; }

        Move();
    }

    //------------------------------ SYNC NETWORK VALUES ----------------------------------------
    void OnEnable()
    {
        knockbackValue.OnValueChanged += OnKnockbackValueChanged;
    }

    void OnDisable()
    {
        knockbackValue.OnValueChanged -= OnKnockbackValueChanged;
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
                if (forceBlock) { break; }
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
        canMove = false;
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

    private void ThrowEnd()
    {
        throwArea.SetActive(false);
        canMove = true;
    }

    void SwitchSide()
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

    public void SwitchSideDelay(float time)
    {
        Invoke("SwitchSide", time);
    }

    [ServerRpc]
    public void IsThrowServerRpc()
    {
        IsThrowClientRpc();
    }

    [ClientRpc]
    private void IsThrowClientRpc()
    {
        switch (gameValues.switchSideTrigger)
        {
            case true:
                gameValues.switchSideTrigger = false;
                break;
            case false:
                gameValues.switchSideTrigger = true;
                break;
        }
    }

    //---------------------------------- KNOCKBACK ---------------------------------------------
    void OnKnockbackValueChanged(float oldValue, float newValue)
    {
        if (!IsClient) { return; }

        if (IsOwnedByServer)
        {
            knockbackP1Text.text = newValue + "%";
        }
        else
        {
            knockbackP2Text.text = newValue + "%";
        }
    }

    [ServerRpc]
    public void ChangeKnockbackValueServerRpc(float value)
    {
        knockbackValue.Value += value;
    }

    private void Knockback(float value)
    {
        knockbackMultiply = 1 + (knockbackValue.Value / 100);

        switch (isLeft)
        {
            case true:
                this.transform.position += new Vector3(-50f * knockbackMultiply, 0f, 0f) * Time.deltaTime;
                break;
            case false:
                this.transform.position += new Vector3(50f * knockbackMultiply, 0f, 0f) * Time.deltaTime;
                break;
        }

        ChangeKnockbackValueServerRpc(value);
        canMove = true;
    }

    private IEnumerator KnockbackDelay(float time,float value)
    {
        yield return new WaitForSeconds(time);
        Knockback(value);
    }

    //---------------------------------- COLLISION ---------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) { return; }

        if (collision.gameObject.CompareTag("Attack") && !isBlock)
        {
            Knockback(10);
        }
    }
}