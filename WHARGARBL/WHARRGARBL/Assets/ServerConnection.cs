using UnityEngine;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Networking;

public class ServerConnection : MonoBehaviour {


    int serverSocketID = -1;
    int maxConnections = 10;
    byte unreliableChannelID;
    byte reliableChannelID;
    bool serverInitialized = false;

	// Use this for initialization
	void Start ()
    {
        DontDestroyOnLoad(this);
        GlobalConfig globalConfig = new GlobalConfig();
        globalConfig.ReactorModel = ReactorModel.FixRateReactor;
        globalConfig.ThreadAwakeTimeout = 10;

        ConnectionConfig connectionConfig = new ConnectionConfig();
        reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced);
        unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced);

        HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);

        NetworkTransport.Init(globalConfig);

        serverSocketID = NetworkTransport.AddHost(hostTopology, 7777);

        if(serverSocketID < 0)
        {
            Debug.Log("Server socket creation failed!");
        }
        else
        {
            Debug.Log("Server socket creation success!");
            serverInitialized = true;
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
	    if (serverInitialized)
        {
            int recHostID;                  //Who received the message
            int connectionID;               //who sent the message
            int channelID;                  //what channel the message was sent from
            int dataSize;                   //how large the message can be
            byte[] buffer = new byte[1024]; //the actual message
            byte error;                     //if there is an error

            NetworkEventType networkEvent = NetworkEventType.DataEvent;

            do
            {
                networkEvent = NetworkTransport.Receive(out recHostID, out connectionID, out channelID, buffer, 1024, out dataSize, out error);

                switch (networkEvent)
                {
                    case NetworkEventType.Nothing:
                        break;
                    case NetworkEventType.ConnectEvent:
                        if (recHostID == serverSocketID)
                        {
                            Debug.Log("Server: Player " + connectionID.ToString() + " connected!");
                        }
                        break;
                    case NetworkEventType.DataEvent:
                        if (recHostID == serverSocketID)
                        {
                            Stream memoryStream = new MemoryStream(buffer);

                            BinaryFormatter binaryFormatter = new BinaryFormatter();

                            string message = binaryFormatter.Deserialize(memoryStream).ToString();

                            Debug.Log("Server: Received Data from " + connectionID.ToString() + "! Message: " + message);

                            RespondMessage(message, connectionID);
                        }
                        break;

                    case NetworkEventType.DisconnectEvent:
                        if (recHostID == serverSocketID)
                        {
                            Debug.Log("Server: Received disconnect from " + connectionID.ToString());
                        }
                        break;
                }
            } while (networkEvent != NetworkEventType.Nothing);
        }
	}

    void SendMessage(string message, int target)
    {
        byte error;
        byte[] buffer = new byte[1024];
        Stream memoryStream = new MemoryStream(buffer);
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        binaryFormatter.Serialize(memoryStream, message);

        NetworkTransport.Send(serverSocketID, target, reliableChannelID, buffer, (int)memoryStream.Position, out error);

        if (error != (byte)NetworkError.Ok)
        {
            NetworkError networkError = (NetworkError)error;
            Debug.Log("Error: " + networkError.ToString());
        }
    }

    void RespondMessage(string message, int clientID)
    {
        if (message == "FirstConnect")
        {
            Debug.Log("Server: Received First Connect message from " + clientID);
            SendMessage("goto_NewScene", clientID);
            if (Application.loadedLevelName != "Scene2")
            {
                Application.LoadLevel("Scene2");
            }
        }
    }
}
