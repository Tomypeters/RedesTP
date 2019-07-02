using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Controller : MonoBehaviourPun //El objeto que nos permite interactuar como jugadores (Controller)
{
    PhotonView _view; //Referencia al PhotonView para poder sincronizar
    public Hero myHero;
    Transform cam;
    void Start()
    {
        _view = GetComponent<PhotonView>(); //Obtengo mi PhotonView

        if (!_view.IsMine)
        {
            return;
        }
        cam = FindObjectOfType<Camera>().transform;
    }

    void Update()
    {
        if (!_view.IsMine) //Si no soy yo, retorno
            return;

        if (!myHero)
        {
            myHero = ServerNetwork.Instance.MyHero(PhotonNetwork.LocalPlayer);
        }
        //Hago una request constantemente al servidor para sincronizar mi movimiento.
        //ServerNetwork.Instance.PlayerRequestRotate(Input.GetAxis("Mouse X"), PhotonNetwork.LocalPlayer);
        ServerNetwork.Instance.PlayerRequestMove(new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0), PhotonNetwork.LocalPlayer);
        ServerNetwork.Instance.PlayerRequestRotate(new Vector3(0,Input.GetAxis("Mouse X"),0), PhotonNetwork.LocalPlayer);
        ServerNetwork.Instance.PlayerRequestShoot(PhotonNetwork.LocalPlayer, Input.GetKey(KeyCode.Mouse0), cam.transform.forward);
        ServerNetwork.Instance.PlayerRequestJump(PhotonNetwork.LocalPlayer, Input.GetKey(KeyCode.Space));
    }
}
