using System;
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

    public int tick = 0;
    public int clientTickOffset;

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
        
        
        tick++;
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("pressed at " + tick);
            UpdatePositions(myID);
        }
    }

    void CheckIncomingMessages()
    {
        //Do the networkStuff:
        string[] o = connection.getMessages();
        if (o.Length > 0)
        {
            foreach (var json in o)
            {
                ContainerPackage sendable = JsonUtility.FromJson<ContainerPackage>(json);
                
                switch (sendable.packageType)
                {
                    case 0:
                        HandleIncomingGamePackage(JsonUtility.FromJson<PositionPackage>(sendable.packageData));
                        break;
                    case 1:
                        HandleIncomingHandshake(JsonUtility.FromJson<HandshakePackage>(sendable.packageData));
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
    private void HandleIncomingHandshake(HandshakePackage package)
    {
        if (isServer)
        {
            // Determine client tick offset
            clientTickOffset = tick - package.tick;

            HandshakePackage handshake = new HandshakePackage();
            handshake.ids = new int[] { 0, 1 };
            handshake.positions = new Vector2[] { 
                new Vector2(players[0].transform.position.x, players[0].transform.position.y),
                new Vector2(players[1].transform.position.x, players[1].transform.position.y)
            };

            SendPackage(handshake);
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
        SendPackage(new HandshakePackage());
    }

    private void HandleIncomingGamePackage(PositionPackage p)
    {

        if (isServer)
        {
            Debug.Log("my tick: " + tick + " their tick: " + p.tick + " with offset: "  + (p.tick + clientTickOffset));
        }
        int pid = p.id;

        players[pid].transform.position = p.pos;
    }

    private void SendPackage<T>(T package)
    {
        ContainerPackage container = package switch
        {
            PositionPackage gameSendable => new ContainerPackage(0, JsonUtility.ToJson(package)),
            HandshakePackage handshakeSendable => new ContainerPackage(1, JsonUtility.ToJson(package)),
            _ => throw new ArgumentException("Wrong type!")
        };
        connection.Send(JsonUtility.ToJson(container));
    }


    public void UpdatePositions(int id)
    {
        //update sendData-object
        PositionPackage p = new PositionPackage();
        p.tick = tick;
        p.id = id;
        p.pos = players[id].transform.position;

        SendPackage(p);

    }
 
    void OnDestroy() {
        connection.Stop();
    }
}

