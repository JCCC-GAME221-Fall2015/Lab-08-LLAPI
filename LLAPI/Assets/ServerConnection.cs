using UnityEngine;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Networking;

public class ServerConnection : MonoBehaviour
{
    int serverSocketID = -1;
    int maxConnections = 10;

    byte unreliableChannelID;
    byte reliableChannelID;

    bool serverInitialized = false;

    // Use this for initialization
    void Start()
    {
        GlobalConfig globalConfig = new GlobalConfig(); //Stores information such as packet sizes
        globalConfig.ReactorModel = ReactorModel.FixRateReactor; //Determines how the network reacts to incoming packets
        globalConfig.ThreadAwakeTimeout = 10; //Amount of time in milliseconds between updateing packets

        ConnectionConfig connectionConfig = new ConnectionConfig(); //Stores all channels
        reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced); //TCP Channel
        unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced); //UDP Channel

        HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);

        NetworkTransport.Init(globalConfig);

        serverSocketID = NetworkTransport.AddHost(hostTopology, 7777);
        Debug.Log(serverSocketID);

        serverInitialized = true;
    }
    // Update is called once per frame
    void Update()
    {
        if (!serverInitialized)
        {
            return;
        }

        int recHostId;                          //who receiving the message
        int connectionId;                       //who sent the message
        int channelId;                          //What channel the message was sent from
        int dataSize;                           //How large the message can be
        byte[] buffer = new byte[1024];         //The actual message
        byte error;                             //If thier is an error

        NetworkEventType networkEvent = NetworkEventType.DataEvent;

        do
        {
            networkEvent = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, buffer,
                1024, out dataSize, out error);

            switch (networkEvent)
            {
                case NetworkEventType.Nothing:
                    break;

                case NetworkEventType.ConnectEvent:
                    // Server received disconnect event
                    if (recHostId == serverSocketID)
                    {
                        Debug.Log("Server: Player " + connectionId.ToString() + " connected!");
                    }
                    break;

                case NetworkEventType.DataEvent:
                    //verify the server is the intended target
                    if (recHostId == serverSocketID)
                    { 
                        //Open a memory stream with a size equal to the buffer we set up earlier
                        Stream memoryStream = new MemoryStream(buffer);

                        //Create a binary formatter to begin reading the information from the memory stream
                        BinaryFormatter binaryFormatter = new BinaryFormatter();

                        //utilize the binary formatter to deserialize the binary information stored in the memory string
                        //then convert that into a string
                        string message = binaryFormatter.Deserialize(memoryStream).ToString();

                        //debug out the message you worked so hard to figure out!
                        Debug.Log("Server: Received Data from " + connectionId.ToString() + "! Message: " + message);

                        RespondMessage(message, recHostId);
                    }
                    break;

                case NetworkEventType.DisconnectEvent:
                    // Server received disconnect event
                    if (recHostId == serverSocketID)
                    {
                        Debug.Log("Server: Player " + connectionId.ToString() + " connected!");
                    }
                    break;
            }

        } while (networkEvent != NetworkEventType.Nothing);
    }

    void SendMessage(string message, int target)
    {
        byte error;
        byte[] buffer = new byte[1024];
        Stream memoryStream = new MemoryStream(buffer);
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        binaryFormatter.Serialize(memoryStream, message);

        NetworkTransport.Send(serverSocketID, target, reliableChannelID, buffer, (int)memoryStream.Position,            out error);

        if (error != (byte)NetworkError.Ok)
        {
            NetworkError networkError = (NetworkError)error;
            Debug.Log("Error: " + networkError.ToString());
        }    }

    void RespondMessage(string message, int playerID)
    {
        //Finish This    }
}
