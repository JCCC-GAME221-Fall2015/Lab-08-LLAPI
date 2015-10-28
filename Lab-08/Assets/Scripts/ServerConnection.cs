using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class ServerConnection : MonoBehaviour 
{
    const int INTERVAL_TIME = 10;
    const short DEFAULT_PORT = 7777;
    const int BUFFER_SIZE = 1024;

    int serverSocketID = -1; // Unique number server can be identified by.
    int maxConnections = 10; // How many clients can connect to the server.
    byte unreliableChannelID; // Unique number identifying a channel utilizing UDP.
    byte reliableChannelID; // Unique number identifyig a channel utilizing TCP.
    bool serverInitialized = false; // Determines if the server is running or not.

	// Use this for initialization
	void Start () 
    {
        GlobalConfig globalConfig = new GlobalConfig();
        // Set the Global Config to receive data in fixed 10ms intervals.
        globalConfig.ReactorModel = ReactorModel.FixRateReactor;
        globalConfig.ThreadAwakeTimeout = INTERVAL_TIME;

        ConnectionConfig connectionConfig = new ConnectionConfig();
        // Add data channels to the network
        reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced);
        unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced);
        // Combine channels with the maximum number of connections
        HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);
        // Initialize the Network
        NetworkTransport.Init(globalConfig);
        // Open the network socket
        serverSocketID = NetworkTransport.AddHost(hostTopology, DEFAULT_PORT);
        if (serverSocketID < 0) 
            Debug.Log("Server socket creation failed");
        else 
            Debug.Log("Server socket creation successful");
        // Note that the server is running
        serverInitialized = true;
        DontDestroyOnLoad(this);
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (!serverInitialized)
        {
            return;
        }

        int recHostID; // Who receives message
        int connectionID; // Who sent message
        int channelID; // what channel message was sent from
        int dataSize; // how large message can be
        byte[] buffer = new byte[BUFFER_SIZE]; // actual message
        byte error; // Flag for errors
        NetworkEventType networkEvent = NetworkEventType.DataEvent;

        do
        {
            // Get a Network Event
            networkEvent = NetworkTransport.Receive(out recHostID, out connectionID, out channelID, buffer, BUFFER_SIZE, out dataSize, out error);
            // Determine what to do based on the event received
            switch (networkEvent)
            {
                case(NetworkEventType.Nothing): // Do nothing
                    break;
                case(NetworkEventType.ConnectEvent): // Broadcast that a client has connected
                    if (recHostID.Equals(serverSocketID))
                    {
                        Debug.Log("Server: Player " + connectionID.ToString() + " connected");
                    }
                    break;
                case(NetworkEventType.DataEvent): // Broadcast that a client is sending data
                    if (recHostID.Equals(serverSocketID))
                    {
                        Stream memoryStream = new MemoryStream(buffer);
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        string message = binaryFormatter.Deserialize(memoryStream).ToString();
                        Debug.Log("Server: Received data from " + connectionID.ToString() + ". Message: " + message);
                        RespondMessage(message, recHostID);
                    }
                    break;
                case(NetworkEventType.DisconnectEvent): // Broadcast that a client has disconnected
                    if (recHostID.Equals(serverSocketID))
                    {
                        Debug.Log("Server: Player " + connectionID.ToString() + " disconnected");
                    }
                    break;
            }
        } while(networkEvent != NetworkEventType.Nothing);
	}

    void SendMessage(string message, int target)
    {
        byte error;
        byte[] buffer = new byte[BUFFER_SIZE];
        Stream memoryStream = new MemoryStream(buffer);
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        binaryFormatter.Serialize(memoryStream, message);
        NetworkTransport.Send(serverSocketID, target, reliableChannelID, buffer, (int)memoryStream.Position, out error);
        if(!error.Equals((byte)NetworkError.Ok))
        {
            NetworkError networkError = (NetworkError)error;
            Debug.Log("Error occurred: " + networkError.ToString());
        }
    }

    void RespondMessage(string message, int playerID)
    {
        if (message.Equals("FirstConnect"))
        {
            Debug.Log("Player " + playerID + " has sent a first connection.");
            SendMessage("goto_NewScene", playerID);
            if (!Application.loadedLevelName.Equals("Scene2"))
                Application.LoadLevel("Scene2");
        }
    }
}
