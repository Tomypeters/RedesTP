using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Bullet : MonoBehaviour
{
    private PhotonView _view; //Referencia al PhotonView para poder sincronizar
    public Hero shooter;

    void Awake()
    {
        _view = GetComponent<PhotonView>(); //Obtengo mi PhotonView
        
        StartCoroutine(Die());
    }

    void Update()
    {
        transform.position += transform.forward * 20 * Time.deltaTime; //Me muevo
    }

    public bool AreYouMine() //Funcion para saber si la bala es mía (referendo al jugador)
    {
        return _view.IsMine;
    }

    public void DestroyThisBullet() //Funcion para destruir la bala en todos los clientes
    {
        //PhotonNetwork.Destroy(gameObject);
        //_view.RPC("DestroyMe", RpcTarget.OthersBuffered); //Hago un RPC para que me destruyan.
        _view.RPC("DestroyMe", RpcTarget.AllBuffered); //Hago un RPC para que me destruyan.
    }

    [PunRPC]
    void DestroyMe() //Funcion que destruye la bala
    {
        if (_view.IsMine) //Sólo si soy yo
        PhotonNetwork.Destroy(gameObject); //Me destruyo
    }

    IEnumerator Die()
    {
        yield return new WaitForSeconds(6);
        DestroyMe();
    }
}
