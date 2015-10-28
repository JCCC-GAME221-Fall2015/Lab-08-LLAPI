using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Networking;

// Author: Nathan Boehning
// Purpose: Allow clients to connect to a host using LLAPI
public class ClientConnection : MonoBehaviour
{
    private int clientSocketID = -1;
    //Will store the unique identifier of the session that keeps the connection between the client
    //and the server. You use this ID as the 'target' when sending messages to the server.
    private int clientServerConnectionID = -1;
    private int maxConnections = 10;
    private byte unreliableChannelID;
    private byte reliableChannelID;
    private bool isClientConnected = false;

    private void Start()
    {
        DontDestroyOnLoad(this);

        //Build the global config
        GlobalConfig globalConfig = new GlobalConfig();

        //Build the channel config
        ConnectionConfig connectionConfig = new ConnectionConfig();

        // Create and set the reliable and unreliable channels
        reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced);
        unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced);

        //Create the host topology
        HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);

        //Initialize the network transport
        NetworkTransport.Init(globalConfig);

        //Open a socket for the client
        clientSocketID = NetworkTransport.AddHost(hostTopology, 7778);

        //Make sure the client created the socket successfully
        if (clientSocketID < 0)
        {
            Debug.Log("Client socket creation failed!");
        }
        else
        {
            Debug.Log("Client socket creation success!");
        }

        //Create a byte to store a possible error
        byte error;

        //Connect to the server using 
        //int NetworkTransport.Connect(int socketConnectingFrom, string ipAddress, int port, 0, out byte possibleError)

        clientServerConnectionID = NetworkTransport.Connect(clientSocketID, Network.player.ipAddress, 7777, 0, out error);

        //Store the ID of the connection in clientServerConnectionID

        //Display the error (if it did error out)
        if (error != (byte) NetworkError.Ok)
        {
            NetworkError networkError = (NetworkError) error;
            Debug.Log("Error: " + networkError);
        }

        isClientConnected = true;
    }

    private void Update()
    {
        //If the client failed to create the socket, leave this function
        if (!isClientConnected)
        {
            return;
        }

        PollBasics();

        //If the user pressed the Space key
        //Send a message to the server "FirstConnect"
        if (Input.GetKey(KeyCode.Space))
        {
            SendMessage("FirstConnect");
        }

        //If the user pressed the R key
        //Send a message to the server "Random message!"
        if (Input.GetKey(KeyCode.R))
        {
            SendMessage("RandomMessage");
        }
    }

    private void SendMessage(string message)
    {
        //create a byte to store a possible error
        byte error;

        //Create a buffer to store the message
        byte[] buffer = new byte[1024];

        //Create a memory stream to send the information through
        Stream memoryStream = new MemoryStream(buffer);

        //Create a binary formatter to serialize and translate the message into binary
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        //Serialize the message
        binaryFormatter.Serialize(memoryStream, message);

        //Send the message from this client, over the client server connection, using the reliable channel
        NetworkTransport.Send(clientSocketID, clientServerConnectionID, reliableChannelID, buffer,
            (int) memoryStream.Position, out error);

        //Display the error (if it did error out)
        if (error != (byte) NetworkError.Ok)
        {
            // Error is something that isn't okay, debug it out to see what the error is
            NetworkError networkError = (NetworkError) error;
            Debug.Log("Error: " + networkError);
        }

    }

    private void InterperateMessage(string message)
    {
        //if the message is "goto_NewScene"
        if (message.Equals("goto_NewScene"))
            Application.LoadLevel("Scene2");
        //load the level named "Scene2"
    }

    private void PollBasics()
    {
        int recHostID; // Who is receiving the message
        int connectionID; // Who sent the message
        int channelID; // What channel the message was sent from
        int dataSize; // How large the message can be
        byte[] buffer = new byte[1024]; // The actual message
        byte error; // If there is an error

        //prepare to receive messages by practicing good bookkeeping
        NetworkEventType networkEvent = NetworkEventType.DataEvent;


        //do
        do
        {
            networkEvent = NetworkTransport.Receive(out recHostID, out connectionID, out channelID, buffer, 1024,
                out dataSize, out error);

            switch (networkEvent)
            {
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.ConnectEvent:
                    if (recHostID == clientSocketID)
                    {
                        Debug.Log("Client " + connectionID + " connected.");
                        isClientConnected = true;
                    }
                    break;
                case NetworkEventType.DataEvent:
                    if (recHostID == clientSocketID)
                    {
                        // Open a memory stream with a size equal to the buffer
                        Stream memoryStream = new MemoryStream(buffer);

                        // Create a binary formatter to begin reading the information from the memory stream
                        BinaryFormatter binaryFormatter = new BinaryFormatter();

                        // Utilize the binary formatter to deserialize the binary information stored in the memory string
                        // and convert it back into a string
                        string message = binaryFormatter.Deserialize(memoryStream).ToString();

                        // Debug out the message
                        Debug.Log("Client: Received Data from: " + connectionID + "! Message: " + message);

                        // Respond to the message
                        InterperateMessage(message);
                    }
                    break;
                case NetworkEventType.DisconnectEvent:
                    if (recHostID == clientSocketID)
                    {
                        Debug.Log("Client " + connectionID + " disconnected.");
                        isClientConnected = false;
                    }
                    break;

            }
        } while (networkEvent != NetworkEventType.Nothing);
        //Receive network events
        //switch on the network event types
        //if nothing, do nothing
        //if connection
        //verify that the message was meant for me
        //debug out that i connected to the server, and display the ID of what I connected to
        //set my bool that is keeping track if I am connected to a server to true
        //if data event
        //verify that the message was meant for me and if I am connected to a server
        //decode the message (bring it through the memory stream, deseralize it, translate the binary)
        //Debug the message and the connection that the message was sent from 
        // InterperateMessage(/*the message to interperate*/);
        //if disconnection
        //verify that the message was meant for me, and that I am disconnecting from the current connection I have with the server
        //debug that I disconnected
        //set my bool that is keeping track if I am connected to a server to false
        //while (the network event I am receiving is not Nothing)
    }
}