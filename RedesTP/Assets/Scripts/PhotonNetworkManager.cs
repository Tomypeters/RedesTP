using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PhotonNetworkManager : MonoBehaviourPunCallbacks //Nuestro manager
{
    public GameObject canvas; //Una referencia al canvas para activarla y desactivarla.
    public GameObject loadingText;
    public GameObject bground;
    public GameObject playerCanvas;
    public GameObject winCanvas;
    public GameObject defeatCanvas;
    public GameObject menu;
    bool hostbtn; //Un bool para saber si estoy hosteando.
    public string playerName;
    bool menuActive;


    private void Start()
    {
        DontDestroyOnLoad(this); //Para que se mantenga en escenas
    }

    private void Update()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
            {
                bground.SetActive(false);
                loadingText.SetActive(false);
                playerCanvas.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                Lose();
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (!menuActive)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Debug.Log("Menu");
                    menu.SetActive(true);
                    menuActive = true;
                }

                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    menu.SetActive(false);
                    Debug.Log("No Menu");
                    menuActive = false;
                }
            }
        }
    }

    public void BTNConnectToServer() //Si puse "Join"
    {
        PhotonNetwork.ConnectUsingSettings(); //Trato de conectarme
    }

    public void BTNHostServer() //Si puse "Host"
    {
        hostbtn = true; //Pongo el bool de host en true
        PhotonNetwork.ConnectUsingSettings(); //Trato de conectarme
    }

    public override void OnConnectedToMaster() //Funcion que se llama cuando se conectó al Master
    {
        Debug.Log("CONECTO AL MASTER");
        if (!hostbtn)
        {
            playerName = FindObjectOfType<InputField>().text;
            Debug.Log("Agregue a " + playerName + "a el diccionario de photonnetwork piola");
        }
        canvas.SetActive(false); //Desactivo el canvas como feedback de que se conectó.
        loadingText.SetActive(true);
        PhotonNetwork.JoinLobby(TypedLobby.Default); //Me trato de unir al Lobby
    }

    public override void OnDisconnected(DisconnectCause cause) //Si no me pude conectar al Master (o me desconectó)
    {
        canvas.SetActive(true); //Vuelve a activarme el canvas
    }

    public override void OnJoinedLobby() //Funcion que se llama cuando se conecta al Lobby
    {
        Debug.Log("METIO AL LOBBY");
        if (hostbtn) //Si soy el host
        {
            PhotonNetwork.CreateRoom("MainRoom", new RoomOptions() { MaxPlayers = 5 }); //Creo una room y retorno. (y se conecta a la room creada automaticamente)
            return;
        }
        PhotonNetwork.JoinRandomRoom(); //Si no soy el host, me conecto a una Room Random (la unica que existe)
    }

    public override void OnJoinedRoom() //Funcion que se llama cuando entra a la Room
    {
        Debug.Log("SE METIO A UNA HABITACION");


        //PhotonNetwork.LoadLevel(1);
        if (hostbtn) //Si soy el host
            PhotonNetwork.Instantiate("ServerNetwork", Vector3.zero, Quaternion.identity); //Instancio un Server
        else
        {
            PhotonNetwork.Instantiate("Controller", Vector3.zero, Quaternion.identity); //Instancio un Controller
                                                                                        //ServerNetwork.Instance.RequestAddName((PhotonNetwork.LocalPlayer, playerNames[PhotonNetwork.LocalPlayer]));
        }//Sino
    }

    public override void OnJoinRandomFailed(short returnCode, string message) //Si no me pude conectar a la Random Room
    {
        Debug.Log("FALLO PORQUE " + message);
        PhotonNetwork.Disconnect(); //Desconectame
    }

    public override void OnCreatedRoom() //Funcion que se llama cuando se crea la Room
    {
        Debug.Log("CREO UN ROOM");
    }

    public void Disconnect()
    {
        canvas.SetActive(true);
        PhotonNetwork.Disconnect();
    }

    public void Lose()
    {
        defeatCanvas.SetActive(true);
    }
}
