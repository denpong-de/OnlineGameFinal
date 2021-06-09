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
    [Header("Scriptable Object")]
    public GameValueScriptableObject gameValues;

    //MOVE
    [Header("Move")]
    [SerializeField] private float moveSpeedWhenBlock;
    [SerializeField] private float moveSpeedNormal;
    private float moveSpeed;
    private Vector2 moveVec;
    bool isLeft;
    public bool canMove;

    //ATTACK
    GameObject attackArea;
    bool pressAttack;

    //BLOCK
    GameObject blockArea;
    bool pressBlock;
    float blockValue;

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
    bool pressThrow;

    //PLAYER NAME
    Text playerNameP1Text, playerNameP2Text;
    private NetworkVariableString playerName = new NetworkVariableString();
    public bool isChangeName
    {
        get { return _isChangeName; }
        set
        {
            _isChangeName = value;
            SetHostNameServerRpc();
        }
    }
    private bool _isChangeName;

    //COOLDOWN
    bool isCooldown;

    //KNOCKBACK
    Text knockbackP1Text, knockbackP2Text;
    private NetworkVariableFloat knockbackValue = new NetworkVariableFloat(0f);
    float knockbackMultiply;

    //WIN
    Text winCountP1Text, winCountP2Text;
    private NetworkVariableInt winCountValue = new NetworkVariableInt();

    //PLAY AGAIN
    public int isPlayAgain
    {
        get { return _isPlayAgain; }
        set
        {
            _isPlayAgain = value;
            ResetToSpawnPos();
            gameManager.gameStart = false;
            gameOverUI.SetActive(false);
            ChangeKnockbackValueServerRpc(0);
        }
    }
    private int _isPlayAgain;
    GameManager gameManager;
    private GameObject gameOverUI;

    //ANIMATION
    Animator animator;
    bool isWalkAnim;

    //DEBUGGING
    [Header("Debugging")]
    [SerializeField] bool forceBlock;

    //------------------------------ Awake, Start, Update --------------------------------------
    void Awake()
    {
        if (IsServer) { isLeft = true; }

        moveSpeed = moveSpeedNormal;

        attackArea = this.gameObject.transform.GetChild(1).gameObject;

        blockArea = this.gameObject.transform.GetChild(2).gameObject;

        throwArea = this.gameObject.transform.GetChild(3).gameObject;
        gameValues.switchSideTrigger = false; //Set to default

        playerNameP1Text = GameObject.FindGameObjectWithTag("NameT1").GetComponent<Text>();
        playerNameP2Text = GameObject.FindGameObjectWithTag("NameT2").GetComponent<Text>();
        gameValues.changeNameTrigger = false; //Set to default

        knockbackP1Text = GameObject.FindGameObjectWithTag("KnockT1").GetComponent<Text>();
        knockbackP1Text.text = "0%";
        knockbackP2Text = GameObject.FindGameObjectWithTag("KnockT2").GetComponent<Text>();
        knockbackP2Text.text = "0%";

        winCountP1Text = GameObject.FindGameObjectWithTag("WinT1").GetComponent<Text>();
        winCountP1Text.text = "Win: 0";
        winCountP2Text = GameObject.FindGameObjectWithTag("WinT2").GetComponent<Text>();
        winCountP2Text.text = "Win: 0";

        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        GameObject mainCanvas = GameObject.FindGameObjectWithTag("MainCanvas");
        gameOverUI = mainCanvas.gameObject.transform.GetChild(3).gameObject;

        animator = this.gameObject.transform.GetChild(0).GetComponent<Animator>();
    }

    private void Update()
    {
        if (!_isChangeName && IsOwnedByServer) //To check Knockback trigger on other client.
        {
            isChangeName = gameValues.changeNameTrigger;
        }

        if (_isPlayAgain != gameValues.playAgain)
        {
            isPlayAgain = gameValues.playAgain;
        }

        if (!pressBlock) { return; }
        if (isThrow == gameValues.switchSideTrigger) { return; }

        isThrow = gameValues.switchSideTrigger; //To check Knockback trigger on other client.
    }

    void FixedUpdate()
    {
        if (!canMove || isCooldown || gameManager.isCoutdown) 
        {
            isWalkAnim = false;
            MoveAnimServerRpc(isWalkAnim);
            return; 
        }

        //Check if player is walking.
        if(moveVec.x != 0)
        {
            isWalkAnim = true;
        }
        else
        {
            isWalkAnim = false;
        }

        Move();
        MoveAnimServerRpc(isWalkAnim);
    }

    //------------------------------ SYNC NETWORK VALUES ----------------------------------------
    void OnEnable()
    {
        knockbackValue.OnValueChanged += OnKnockbackValueChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;
        winCountValue.OnValueChanged += OnWinCountChanged;
    }

    void OnDisable()
    {
        knockbackValue.OnValueChanged -= OnKnockbackValueChanged;
        playerName.OnValueChanged -= OnPlayerNameChanged;
        winCountValue.OnValueChanged -= OnWinCountChanged;
    }

    //------------------------------------- MOVE ------------------------------------------------
    //Get values from New Input System.
    public void OnMove(InputValue value) 
    {
        if (!IsOwner) { return; }

        moveVec = value.Get<Vector2>();
    }

    private void Move()
    {
        this.transform.position += new Vector3(moveVec.x * moveSpeed, 0f, 0f) * Time.deltaTime;
    }

    private void StopPush()
    {
        if (!isLeft && moveVec.x == -1)
        {
            moveVec.x = 0;
        }
        else if (isLeft && moveVec.x == 1)
        {
            moveVec.x = 0;
        }
    }

    [ServerRpc]
    private void MoveAnimServerRpc(bool value)
    {
        MoveAnimClientRpc(value);
    }

    [ClientRpc]
    private void MoveAnimClientRpc(bool value)
    {
        animator.SetBool("Move", value);
    }

    //------------------------------------ ATTACK -----------------------------------------------
    public void OnAttack()
    {
        if (!IsOwner) { return; }

        if (pressBlock || pressThrow || pressAttack || isCooldown) { return; }

        AttackServerRpc();
        pressAttack = true;
        canMove = false;
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
        animator.SetTrigger("Attack");
        Invoke("AttackEnd", 0.3f);
    }

    void AttackEnd()
    {
        attackArea.SetActive(false);
        pressAttack = false;
        canMove = true;

        StartCoroutine(Cooldown(0.3f));
    }

    //------------------------------------ BLOCK -----------------------------------------------
    public void OnBlock(InputValue value)
    {
        if (!IsOwner) { return; }

        blockValue = value.Get<float>();
        if (pressAttack || pressThrow || isCooldown) { blockValue = 0; }

        switch (blockValue)
        {
            case 0:
                if (forceBlock) { break; } //For debugging
                pressBlock = false;
                moveSpeed = moveSpeedNormal;
                BlockServerRpc(pressBlock);
                break;
            case 1:
                pressBlock = true;
                moveSpeed = moveSpeedWhenBlock;
                BlockServerRpc(pressBlock);
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
                animator.SetBool("Block",true);
                break;
            case false:
                blockArea.SetActive(false);
                animator.SetBool("Block", false);
                break;
        }
    }

    //------------------------------------ THROW -----------------------------------------------
    public void OnThrow()
    {
        if (!IsOwner) { return; }

        if (pressBlock || pressAttack || isCooldown || pressThrow) { return; }

        ThrowServerRpc();
        canMove = false;
        pressThrow = true;
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
        animator.SetTrigger("Throw");
        Invoke("ThrowEnd", 0.3f);
    }

    private void ThrowEnd()
    {
        throwArea.SetActive(false);
        canMove = true;
        pressThrow = false;

        StartCoroutine(Cooldown(0.3f));
    }

    void SwitchSide()
    {
        switch (isLeft)
        {
            case true:
                this.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                this.transform.position = this.transform.position + new Vector3(2.5f, 0f, 0f);
                isLeft = false;
                break;
            case false:
                this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                this.transform.position = this.transform.position + new Vector3(-2.5f, 0f, 0f);
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

    //--------------------------------- PLAYER NAME --------------------------------------------
    void OnPlayerNameChanged(string oldValue, string newValue)
    {
        if (!IsClient) { return; }

        gameObject.name = newValue;

        if (IsOwnedByServer)
        {
            playerNameP1Text.text = gameObject.name;
        }
        else
        {
            playerNameP2Text.text = gameObject.name;
            gameValues.changeNameTrigger = true;
        }
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(string name)
    {
        playerName.Value = name;
    }

    [ServerRpc]
    private void SetHostNameServerRpc()
    {
        SetHostNameClientRpc();
    }

    [ClientRpc]
    private void SetHostNameClientRpc()
    {
        playerNameP1Text.text = gameObject.name;
    }

    //------------------------------------ COOLDOWN --------------------------------------------
    private IEnumerator Cooldown(float time)
    {
        isCooldown = true;
        yield return new WaitForSeconds(time);
        isCooldown = false;
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
        if(value == 0) { knockbackValue.Value = value; }
        else { knockbackValue.Value += value; } 
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

    //-------------------------------------- WIN -----------------------------------------------
    private void OnWinCountChanged(int oldValue, int newValue)
    {
        if (!IsClient) { return; }

        if (IsOwnedByServer)
        {
            winCountP1Text.text = "Win: " + newValue;
        }
        else 
        {
            winCountP2Text.text = "Win: " + newValue;
        }
    }

    [ServerRpc]
    public void ChangeWinCountServerRpc()
    {
        winCountValue.Value += 1;
    }

    [ServerRpc]
    private void MoveToSpawnPosServerRpc(Vector3 spawnPos, Vector3 spawnRot)
    {
        MoveToSpawnPosClientRpc(spawnPos, spawnRot);
    }

    [ClientRpc]
    private void MoveToSpawnPosClientRpc(Vector3 spawnPos, Vector3 spawnRot)
    {
        gameObject.transform.position = spawnPos;
        gameObject.transform.rotation = Quaternion.Euler(spawnRot);
    }

    //----------------------------------- PLAY AGAIN --------------------------------------------
    [ServerRpc]
    public void PlayAgainRequestServerRpc(bool value)
    {
        PlayAgainRequestClientRpc(value);
    }

    [ClientRpc]
    private void PlayAgainRequestClientRpc(bool value)
    {
        switch (value)
        {
            case true:
                gameValues.playAgainRequest = true;
                break;
            case false:
                gameValues.playAgain ++;
                gameValues.playAgainRequest = false;
                break;
        }
    }

    private void ResetToSpawnPos()
    {
        if (IsServer)
        {
            MoveToSpawnPosServerRpc(new Vector3(-2f, 0f, 0f), new Vector3(0f, 0f, 0f));
        }
        else
        {
            MoveToSpawnPosServerRpc(new Vector3(2f, 0f, 0f), new Vector3(0f, 180f, 0f));
        }
    }

    //---------------------------------- COLLISION ---------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) { return; }

        if (collision.gameObject.CompareTag("Attack") && !pressBlock)
        {
            Knockback(10);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StopPush();
        }
    }
}