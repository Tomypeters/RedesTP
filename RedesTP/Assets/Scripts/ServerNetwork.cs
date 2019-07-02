using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class ServerNetwork : MonoBehaviourPun //Va a ser el Server de nuestro Full-Authoritative
{

    public static ServerNetwork Instance { get; private set; } //Un singleton para poder accederlo de cualquier lado
    PhotonView _view; //El PhotonView para que se sincronice
    public Dictionary<Player, Hero> players = new Dictionary<Player, Hero>(); //Diccionario para enlazar el jugador actual con su hero
    public Player serverReference; //Una referencia para saber quien es el servidor


    private void Awake()
    {
        _view = GetComponent<PhotonView>(); //Obtengo el PhotonView

        if (!Instance) //Si no asigne el singleton todavía
        {
            if (_view.IsMine) //Y sólo si soy yo
                _view.RPC("SetReferenceToSelf", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer); //Hago un RPC para asignarles el ServerReference
        }
        else //Si ya está asignado
            PhotonNetwork.Destroy(gameObject); //Lo destruyo
    }

    [PunRPC]
    public void AddPlayer(Player p) //Funcion para añadir los jugadores al juego
    {
        if (!_view.IsMine) //Si no es el server, retorno.
            return;
        var newHero = PhotonNetwork.Instantiate("Player",
                        new Vector3(Random.Range(0, 3),
                        Random.Range(0, 3),
                        Random.Range(0, 3)),
                        Quaternion.identity).GetComponent<Hero>(); //Instancio el jugador
        players.Add(p, newHero); //Lo añado al diccionario enlazando el jugador con su Hero
        newHero.ServerCheckIfClient(p);
        newHero.transform.position = new Vector3(Random.Range(-40, 40), 1, Random.Range(-25, 25));
        
        //newHero.nameText.text = newHero.player_name;
        //newHero.ServerCreateControllers(p);
        foreach (var item in players)
        {
            Debug.Log(item);
        }
    }

    [PunRPC]
    public void SetReferenceToSelf(Player p) //Funcion para asignar el serverReference
    {
        Instance = this; //Asigno el singleton
        serverReference = p; //Asingo el serverReference con el jugador que me pasaron
        if (!PhotonNetwork.IsMasterClient) //Sólo si no soy el server
            _view.RPC("AddPlayer", serverReference, PhotonNetwork.LocalPlayer); //Llamo al server para agregarme como jugador.
    }

    [PunRPC]
    void RequestMove(Vector3 dir, Player p) //Funcion para solicitar movimiento
    {
        if (!_view.IsMine) //Si no es el server, retorno.
            return;
        if (players.ContainsKey(p)) //Si el jugador está en el diccionario
            players[p].Move(dir); //Permito que se mueva
    }

    [PunRPC]
    void SetMoving(Player p, bool v) //Funcion para sincronizar la animación de movimiento
    {
        if (!_view.IsMine) //Si no es el server, retorno.
            return;
        if (players.ContainsKey(p)) //Si el jugador está en el diccionario
            players[p].SetMoving(v); //Permito que sincronice el estado
    }

    public void PlayerRequestMove(Vector3 dir, Player p) //Funcion que va a llamar cada player para solicitar movimiento
    {
        _view.RPC("RequestMove", serverReference, dir, p); //RPC para pedirle al server que YO me quiero mover
        _view.RPC("SetMoving", RpcTarget.All, p, dir != Vector3.zero); //RPC para sincronizar en todos mi animación de movimiento.
    }

    [PunRPC]
    void RequestShoot(Player p, Vector3 v)
    {
        if (!_view.IsMine) return;
        if (players.ContainsKey(p)) players[p].Shoot(v);
    }

    public void PlayerRequestShoot(Player p, bool input, Vector3 v)
    {
        if (input) _view.RPC("RequestShoot", serverReference, p, v);
    }

    [PunRPC]
    void RequestJump(Player p)
    {
        if (!_view.IsMine) return;
        if (players.ContainsKey(p)) players[p].Jump();
    }

    public void PlayerRequestJump(Player p, bool input)
    {
        if (input) _view.RPC("RequestJump", serverReference, p);
    }


    [PunRPC]
    void RequestRotate(Vector3 y, Player p)
    {
        if (!_view.IsMine) return;
        if (players.ContainsKey(p)) players[p].Rotate(y);
    }

    public void PlayerRequestRotate(Vector3 y, Player p)
    {
        _view.RPC("RequestRotate", serverReference, y, p);
    }

    [PunRPC]
    void RequestTakeDamage(Player p, int damage)
    {
        //if (!_view.IsMine) return;
        if (players.ContainsKey(p))
        {
            players[p].ServerTakeDamage(damage);
            Debug.Log("me dolio");
        }

        else
        {
            Debug.Log("NO TENES PLAYER AMIGO");
        }
    }

    public void PlayerRequestTakeDamage(Player p, int damage)
    {
        _view.RPC("RequestTakeDamage", serverReference, p, damage);
    }

    [PunRPC]
    public void PlayerDisconnect(Player p)
    {
        if (!_view.IsMine) return;
        players.Remove(p);
        PhotonNetwork.DestroyPlayerObjects(p);
    }

    public void PlayerRequestDisconnect(Player p)
    {
        _view.RPC("PlayerDisconnect", serverReference, p);
    }

    [PunRPC]
    void GameOver()
    {
        foreach (var item in players)
        {
            if (item.Value.life > 0) 
            {
                //item.Value.Win();
                //item.Value.kills++;
                item.Value.LocalAddKill();
            }
        }
    }

    public void RequestGameOver()
    {
        _view.RPC("GameOver", serverReference);
    }

    [PunRPC]
    void SetName()
    {
        foreach (var item in players)
        {
            item.Value.RequestSetName(item.Value.player_name);
        }
    }

    public void RequestSetName()
    {
        _view.RPC("SetName", serverReference);
    }


    public IEnumerable<TextMesh> names()
    {
        return players.Select(x => x.Value.nameText);
    }



    public Hero MyHero(Player p)
    {
        if(players.ContainsKey(p)) return players[p];
        return null;

    }

    
}
