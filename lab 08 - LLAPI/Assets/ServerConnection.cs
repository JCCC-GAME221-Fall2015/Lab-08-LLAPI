using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.com
/// 
/// Description: ServerConnection 
/// </summary>
public class ServerConnection : MonoBehaviour {
    #region Fields

    int serverSocketID = -1;
    int maxConnections = 10;
    byte unreliableChannelID;
    byte reliableChannelID;
    bool serverInitialized = false;

    #endregion

    void Start() {
        GlobalConfig globalConfig = new GlobalConfig();
        globalConfig.ReactorModel = ReactorModel.FixRateReactor;
        globalConfig.ThreadAwakeTimeout = 10;

        ConnectionConfig connectionConfig = new ConnectionConfig();
        reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced);
        unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced);

        HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);

        NetworkTransport.Init(globalConfig);

        serverSocketID = NetworkTransport.AddHost(hostTopology, 7777);

        if (serverSocketID < 0) {
            print("Server connection failed");
        } else {
            print("server started");
        }

        serverInitialized = true;
        DontDestroyOnLoad(this);
    }

    void Update() {
        if (!serverInitialized) {
            return;
        }

        int recHostId; //who recieved message
        int connectionId; //who sent message
        int channelId; //what channel message sent from
        int dataSize; //how large message can be
        byte[] buffer = new byte[1024]; //actual message
        byte error; //if there is an error

        NetworkEventType networkEvent = NetworkEventType.DataEvent;

        do {
            networkEvent = NetworkTransport.Receive(out recHostId,
                                                    out connectionId,
                                                    out channelId,
                                                    buffer,
                                                    1024,
                                                    out dataSize,
                                                    out error);

            switch (networkEvent) {
                case NetworkEventType.ConnectEvent:
                    if (recHostId == serverSocketID) {
                        print("Server: Player " + connectionId.ToString() + " connected!");
                    }
                    break;
                case NetworkEventType.DataEvent:
                    if (recHostId == serverSocketID) {
                        //open memory stream to the size of the buffer
                        Stream memoryStream = new MemoryStream(buffer);

                        //create binary formatter to begin reading info from stream
                        BinaryFormatter binaryFormatter = new BinaryFormatter();

                        //use formatter to deserialize binary info stored in memory string and convert to string
                        string message = binaryFormatter.Deserialize(memoryStream).ToString();

                        //debug message
                        print("Server: received data from " + connectionId + "! Message: " + message);

                        //respond
                        RespondMessage(message, connectionId);
                    }
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (recHostId == serverSocketID) {
                        print("Server: Recieved disconnect from  " + connectionId.ToString());
                    }
                    break;
            }
        } while (networkEvent != NetworkEventType.Nothing);
    }

    void RespondMessage(string message, int playerId) {
        if (message == "FirstConnect") {
            print("Server: recieved first contact from: " + playerId);
            SendMessage("goto_NewScene", playerId);
        }

        //check if scene2 loadeed
        if (Application.loadedLevel != 1) {
            Application.LoadLevel(1);
        }
    }

    void SendMessage(SendMessageOptions message, int target) {
        byte error;
        byte[] buffer = new byte[1024];
        Stream memoryStream = new MemoryStream(buffer);
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        binaryFormatter.Serialize(memoryStream, message);

        NetworkTransport.Send(serverSocketID, target, reliableChannelID, buffer, (int) memoryStream.Position, out error);

        if (error != (byte)NetworkError.Ok) {
            NetworkError networkError = (NetworkError) error;
            print("Error: " + networkError.ToString());
        }
    }

}