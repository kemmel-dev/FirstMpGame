using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//https://forum.unity.com/threads/simple-udp-implementation-send-read-via-mono-c.15900/

public class NetworkScript : MonoBehaviour {
    private UdpConnection connection;
    public bool isServer = true;
    int myID;

    bool upKey, downKey, leftKey, rightKey;


    public PlayerScript[] players = new PlayerScript[2];
    Sendable sendable = new Sendable();
    GameSendable handshakeSendable = new GameSendable();
    GameSendable gameSendable = new GameSendable();

    void Start() {
        
        string sendIp = "127.0.0.1";
        
        int sendPort, receivePort;
        if (isServer) {
            sendPort = 8881;
            receivePort = 11000;
            myID = 0;
        } else {
            sendPort = 11000;
            receivePort = 8881;
            myID = 1;
        }

        connection = new UdpConnection();
        connection.StartConnection(sendIp, sendPort, receivePort);

        if (!isServer)
            RequestHandshake();
    }
 
    void FixedUpdate() {
        //Check input...
        if (upKey) {
            players[myID].transform.Translate(0, .1f, 0);
            UpdatePositions(myID);
        }
        if (downKey)
        {
            players[myID].transform.Translate(0, -.1f, 0);
            UpdatePositions(myID);
        }
        if (leftKey)
        {
            players[myID].transform.Translate(-.1f, 0, 0);
            UpdatePositions(myID);
        }
        if (rightKey)
        {
            players[myID].transform.Translate(.1f, 0, 0);
            UpdatePositions(myID);
        }

        //network stuff:
        CheckIncomingMessages();
            
    }

    public void Update()
    {
        //handling keyboard (in Update, because FixedUpdate isnt meant for that(!))
        if (Input.GetKeyDown("w")) upKey = true;       
        if (Input.GetKeyUp("w")) upKey = false;
        if (Input.GetKeyDown("s")) downKey = true;
        if (Input.GetKeyUp("s")) downKey = false;
        if (Input.GetKeyDown("a")) leftKey = true;
        if (Input.GetKeyUp("a")) leftKey = false;
        if (Input.GetKeyDown("d")) rightKey = true;
        if (Input.GetKeyUp("d")) rightKey = false;
    }

    void CheckIncomingMessages()
    {
        //Do the networkStuff:
        string[] o = connection.getMessages();
        if (o.Length > 0)
        {
            foreach (var json in o)
            {
                JsonUtility.FromJsonOverwrite(json, sendable);

                switch (sendable.packageType)
                {
                    case 0:
                        HandleIncomingGamePackage(JsonUtility.FromJson<GameSendable>(json));
                        break;
                    case 1:
                        HandleIncomingHandshake(JsonUtility.FromJson<HandshakeSendable>(json));
                        break;
                    default:
                        break;
                }
            }
        }

    }

    /// <summary>
    /// Send handshake data or apply handshake data
    /// </summary>
    private void HandleIncomingHandshake(HandshakeSendable package)
    {
        if (isServer)
        {
            UpdatePositions(0);
            UpdatePositions(1);
        }
        else
        {
            foreach (int pid in package.ids)
            {
                players[pid].transform.position = new Vector3(package.positions[pid].x, package.positions[pid].y);
            }
        }
    }

    private void RequestHandshake()
    {
        SendPackage(new HandshakeSendable());
    }

    private void HandleIncomingGamePackage(GameSendable p)
    {
        int pid = p.id;
        Vector2 pos = new Vector2(p.x, p.y);

        players[pid].transform.position = pos;
    }

    private void SendPackage<T>(T package)
    {
        string json = JsonUtility.ToJson(package);
        connection.Send(json);
    }


    public void UpdatePositions(int id)
    {
        //update sendData-object
        GameSendable p = new GameSendable();
        p.id = id;
        p.x = players[id].transform.position.x;
        p.y = players[id].transform.position.y;

        SendPackage(p);

    }
 
    void OnDestroy() {
        connection.Stop();
    }
}

