using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Audio;

public class Hero : MonoBehaviourPun //El personaje de nuestros jugadores
{
    AudioSource asourc;
    PhotonView _view; //Referencia al PhotonView para poder sincronizar
    int _life; //Mi vida
    Animator _anim; //Referencia al animator
    Rigidbody rb;
    GameObject cam;
    bool _alreadyMoving; //Un booleano para evitar que si llegan demasiados paquetes de movimiento haya un desface
    bool canShoot = true;
    bool menuActive;
    GameObject defeat;
    GameObject win;
    Text lifeText;
    bool gameOver;
    public int kills;

    public AudioClip hurt;
    public AudioClip shot;
    public AudioClip heal;
    public GameObject gunPoint;
    public GameObject mesh;
    public GameObject head;
    public string player_name = "Player";
    public bool isFromThisClient;
    public GameObject cameraHolder;
    public TextMesh nameText;
    public float jumpForce = 150;
    private bool canJump = true;
    public PhotonNetworkManager networkManager;
    [Range(0, 100)]
    public float life = 100;

    void Start()
    {
        _view = GetComponent<PhotonView>(); //Obtengo el View
        _anim = GetComponent<Animator>(); //Obtengo el Animator
        asourc = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        _life = 3; //Seteo mi vida
        kills = 0;
        win = GameObject.Find("Canvas").transform.GetChild(3).gameObject;

        if (!isFromThisClient)
        {
            return;
        }
        else
        {
            networkManager = FindObjectOfType<PhotonNetworkManager>();
            lifeText = GameObject.Find("PlayerCanvas").transform.GetChild(2).GetComponent<Text>();
            defeat = GameObject.Find("Canvas").transform.GetChild(4).gameObject;
            win = GameObject.Find("Canvas").transform.GetChild(3).gameObject;
            Cursor.lockState = CursorLockMode.Locked;
            CameraSetUp();
            FindObjectsOfType<Controller>().Where(x => x.GetComponent<PhotonView>().IsMine).FirstOrDefault().myHero = this;
            player_name = FindObjectOfType<PhotonNetworkManager>().playerName;
            head.gameObject.SetActive(false);
            PlayerSyncName(player_name);
            ServerNetwork.Instance.RequestSetName();
        }
    }

    void Update()
    {
        if (gameOver) return;
        if (isFromThisClient)
        {
            CameraFollow();
            lifeText.text = life.ToString();

            if (kills > 0)
            {
                win.SetActive(true);
            }

            if (life <= 0)
            {
                Defeat();
            }
        }

        if (!_view.IsMine) return; //Si no soy yo que retorne.

        if (_life <= 0) //Si me quedo sin vida
            PhotonNetwork.Disconnect(); //Me desconecto
    }

    [PunRPC]
    void SyncName(string name)
    {
        player_name = name;
    }

    public void PlayerSyncName(string name)
    {
        _view.RPC("SyncName", RpcTarget.AllBuffered, name);
    }

    [PunRPC]
    void SetName(string name)
    {
        nameText.text = name;
    }

    public void RequestSetName(string name)
    {
        _view.RPC("SetName", RpcTarget.AllBuffered, name);
    }

    [PunRPC]
    void AddKill()
    {
        kills++;
    }

    public void LocalAddKill()
    {
        _view.RPC("AddKill", RpcTarget.All);
    }

    [PunRPC]
    public void CheckIfClient(Player owner)
    {
        if (owner == PhotonNetwork.LocalPlayer)
        {
            isFromThisClient = true;
        }
    }

    public void ServerCheckIfClient(Player owner)
    {
        if (!_view)
            _view = GetComponent<PhotonView>();
        _view.RPC("CheckIfClient", RpcTarget.All, owner);
    }

    public void SetMoving(bool v) //Funcion para actualizar mi animator
    {
        if (_anim) //Si tengo un animator
            _anim.SetBool("IsMoving", v); //Seteo el bool para la animacion
    }

    [PunRPC]
    public void TellIAmDefeated() //Funcion que se llama cuando uno muere
    {
        Debug.Log("GANE");
        PhotonNetwork.Disconnect(); //Me desconecto
    }

    public void ServerTakeDamage(int damage)
    {
        _view.RPC("TakeDamage", RpcTarget.AllBuffered, damage);
    }

    [PunRPC]
    void TakeDamage(int damage)
    {
        life -= damage;
    }

    public void CameraSetUp()
    {
        cam = GameObject.Find("Main Camera");
        cam.transform.SetParent(cameraHolder.transform);
        cam.transform.forward = cameraHolder.transform.forward;
    }

    public void CameraFollow()
    {
        cam.transform.position = cameraHolder.transform.position;
        CameraRotation(new Vector3(-Input.GetAxis("Mouse Y"), 0, 0));
    }

    public void CameraRotation(Vector3 y)
    {
        mesh.transform.Rotate(y);
        cam.transform.Rotate(y);
    }

    public void Rotate(Vector3 x)
    {
        transform.Rotate(x);
    }

    public void Move(Vector3 dir) //Funcion para hacer mover al jugador desde el server
    {
        if (!_alreadyMoving) //Si no me estoy moviendo
        {
            transform.position += (transform.forward * dir.y + transform.right * dir.x) * 2 * Time.deltaTime * 5f; //Me muevo

            _alreadyMoving = true; //Seteo el bool en true para evitar que se siga moviendo más de lo permitido
            StartCoroutine(WaitToMoveAgain()); //Comienzo la corrutina para que pueda volver a moverse en el siguiente frame
        }
    }

    public void Jump()
    {
        if (canJump)
        {
            rb.AddForce(Vector3.up * jumpForce * Time.deltaTime, ForceMode.Impulse);
            canJump = false;
        }
    }


    IEnumerator WaitToMoveAgain() //Corrutina para evitar que se mueva más de lo permitido
    {
        yield return new WaitForEndOfFrame(); //Al termino del siguiente frame
        _alreadyMoving = false; //Vuelvo a poder moverme
    }
    IEnumerator WaitToShootAgain()
    {
        yield return new WaitForSeconds(0.2f);
        canShoot = true;
    }

    public void Shoot(Vector3 v)
    {
        if (canShoot)
        {
            var bullet = PhotonNetwork.Instantiate("Bullet", gunPoint.transform.position, transform.rotation);
            bullet.transform.forward = v;
            bullet.GetComponent<Bullet>().shooter = this;
            StartCoroutine(WaitToShootAgain());
            canShoot = false;
            RequestShotSound();
        }
    }

    public void RequestShotSound()
    {
        _view.RPC("ShotSound", RpcTarget.All);
    }

    [PunRPC]
    void ShotSound()
    {
        asourc.PlayOneShot(shot, 0.5f);
    }

    public void RequestHurtSound()
    {
        _view.RPC("HurtSound", RpcTarget.All);
    }

    [PunRPC]
    void HurtSound()
    {
        asourc.PlayOneShot(hurt, 0.5f);
    }

    public void RequestHealSound()
    {
        _view.RPC("HealSound", RpcTarget.All);
    }

    [PunRPC]
    void HealSound()
    {
        asourc.PlayOneShot(heal, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_view.IsMine) return; //Si no soy yo, retorno

        if (other.gameObject.layer == 9)
        {
            ServerTakeDamage(-50);
            RequestHealSound();
            PhotonNetwork.Destroy(other.gameObject);
        }


        if (other.GetComponent<Bullet>() == null) return; //Si no tiene el componente bala, retorno
        //if (!isFromThisClient) return;
        if (other.GetComponent<Bullet>().shooter != this) //Si la bala no es mia
        {
            //ServerNetwork.Instance.PlayerRequestTakeDamage(PhotonNetwork.LocalPlayer, 10);
            ServerTakeDamage(10);
            RequestHurtSound();
            other.GetComponent<Bullet>().DestroyThisBullet(); //Destruyo la bala
            if (life <= 0)
            {
                ServerNetwork.Instance.RequestGameOver();
                
                
            }
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8)
        {
            canJump = true;
        }
    }

    [PunRPC]
    void Die()
    {
        _anim.SetBool("Dead", true);
        var controller = FindObjectsOfType<Controller>().Select(x => x.myHero).Where(x => x == this).FirstOrDefault();
        PhotonNetwork.Destroy(controller.gameObject);
    }

    public void Defeat()
    {
        defeat.SetActive(true);
        _view.RPC("Die", RpcTarget.AllBuffered);
    }

    public void Win()
    {
        win.SetActive(true);
    }

    public void OnApplicationQuit()
    {
        //ServerNetwork.Instance.PlayerRequestDisconnect(PhotonNetwork.LocalPlayer);
        PhotonNetwork.Destroy(gameObject);
        PhotonNetwork.Disconnect();
    }
}