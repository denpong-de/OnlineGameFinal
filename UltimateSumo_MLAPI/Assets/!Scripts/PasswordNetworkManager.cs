using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MLAPI.Transports.PhotonRealtime;
using MLAPI.Transports;
using Random = UnityEngine.Random;

public class PasswordNetworkManager : MonoBehaviour
{
    [SerializeField] private InputField roomNameInputField;
    [SerializeField] private InputField passwordInputField;
    [SerializeField] private Text roomNameWaitingText;    
    [SerializeField] private Text passwordWaitingText;
    [SerializeField] private GameObject passwordEntryUI;
    [SerializeField] private GameObject waitingUI;
    [SerializeField] private GameObject gameoverUI;

    PhotonRealtimeTransport realtimeTransport;
    GameManager gameManager;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

        realtimeTransport = NetworkManager.Singleton.GetComponent<PhotonRealtimeTransport>();
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) { return; }

        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    public void Host()
    {
        if(roomNameInputField.text == "")
        {
            roomNameInputField.text = RandomStringGenerator();
        }
        realtimeTransport.RoomName = roomNameInputField.text;
        roomNameWaitingText.text = "Room Name: " + roomNameInputField.text;
        passwordWaitingText.text = "Password: " + passwordInputField.text;

        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost(new Vector3(-2f,0f,0f));

        waitingUI.SetActive(true);
        gameManager.gameStart = false;
    }

    public void Client()
    {
        realtimeTransport.RoomName = roomNameInputField.text;

        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(passwordInputField.text);
        NetworkManager.Singleton.StartClient();
    }

    public void Leave()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.StopHost();
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StopClient();
        }

        if (waitingUI.activeInHierarchy) { waitingUI.SetActive(false); }
        passwordEntryUI.SetActive(true);
        gameoverUI.SetActive(false);
    }

    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            HandleClientConnected(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            passwordEntryUI.SetActive(false);
            SetPlayerName(clientId);
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            passwordEntryUI.SetActive(true);
            gameoverUI.SetActive(false);
        }
    }

    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        string password = Encoding.ASCII.GetString(connectionData);

        bool approveConnection = password == passwordInputField.text;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        switch (NetworkManager.Singleton.ConnectedClients.Count)
        {
            case 1:
                spawnPos = new Vector3(2f, 0f, 0f);
                spawnRot = Quaternion.Euler(0f, 180f, 0f);
                break;
        }

        callback(true, null, approveConnection, spawnPos, spawnRot);
    }

    //---------------------------------- CHANGE NAME ---------------------------------------------
    void SetPlayerName(ulong clientid)
    {
        if(!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientid,out var networkClient))
        {
            return;
        }
        if(!networkClient.PlayerObject.TryGetComponent<PlayerBehav>(out var playerBehav))
        {
            return;
        }

        string playerName = PlayerPrefs.GetString("PlayerName","Player" + Random.Range(0,9999));
        playerBehav.SetPlayerNameServerRpc(playerName);
    }

    //------------------------------- roomName GENERATOR -----------------------------------------
    private string RandomStringGenerator()
    {
        string randomString = Path.GetRandomFileName();
        randomString = randomString.Replace(".", string.Empty);
        return randomString;
    }
}
