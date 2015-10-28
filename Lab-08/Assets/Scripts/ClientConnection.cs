/**
 * @author Darrick Hilburn
 * 
 * This script is the client-side code for Low-Level API network programming.
 */

using UnityEngine;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Networking;

public class ClientConnection : MonoBehaviour
{
    const int INTERVAL_TIME = 10;
    const short DEFAULT_PORT = 7777;
    const int BUFFER_SIZE = 1024;

    int clientSocketID = -1;
    //Will store the unique identifier of the session that keeps the connection between the client
    //and the server. You use this ID as the 'target' when sending messages to the server.
    int clientServerConnectionID = -1;
    int maxConnections = 10;
    byte unreliableChannelID;
    byte reliableChannelID;
    bool isClientConnected = false;

    void Start()
    {
        DontDestroyOnLoad(this);

        //Build the global config
        GlobalConfig globalConfig = new GlobalConfig();
        globalConfig.ReactorModel = ReactorModel.FixRateReactor;
        globalConfig.ThreadAwakeTimeout = INTERVAL_TIME;
        //Build the channel config
        ConnectionConfig connectionConfig = new ConnectionConfig();
        reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced);
        unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced);
        //Create the host topology
        HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);
        //Initialize the network transport
        NetworkTransport.Init(globalConfig);
        //Open a socket for the client
        clientSocketID = NetworkTransport.AddHost(hostTopology, DEFAULT_PORT);
        //Make sure the client created the socket successfully
        if (clientSocketID < 0)
            Debug.Log("Client socket creation failed");
        else
            Debug.Log("Client socket creation successful");
        //Create a byte to store a possible error
        byte possibleError;
        //Connect to the server using 
        //int NetworkTransport.Connect(int socketConnectingFrom, string ipAddress, int port, 0, out byte possibleError)
        //Store the ID of the connection in clientServerConnectionID
        clientServerConnectionID = NetworkTransport.Connect(clientSocketID, "localhost", DEFAULT_PORT, 0, out possibleError);
        
        //Display the error (if it did error out)
        if (!possibleError.Equals((byte)NetworkError.Ok))
        {
            NetworkError error = (NetworkError)possibleError;
            Debug.Log("Error occurred: " + error.ToString());
        }
    }

    void Update()
    {
        //If the client failed to create the socket, leave this function
        if (!isClientConnected)
        {
            return;
        }
        PollBasics();
        //If the user pressed the Space key
        //Send a message to the server "FirstConnect"
        if (Input.GetKeyDown(KeyCode.Space))
            SendMessage("FirstConnect");
        //If the user pressed the R key
        //Send a message to the server "Random message!"
        if (Input.GetKeyDown(KeyCode.R))
            SendMessage("Random message!");
    }

    void SendMessage(string message)
    {
        //create a byte to store a possible error
        //Create a buffer to store the message
        //Create a memory stream to send the information through
        //Create a binary formatter to serialize and translate the message into binary
        //Serialize the message
        byte possibleError;
        byte[] buffer = new byte[BUFFER_SIZE];
        Stream memoryStream = new MemoryStream(buffer);
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        binaryFormatter.Serialize(memoryStream, message);
        //Send the message from this client, over the client server connection, using the reliable channel
        NetworkTransport.Send(clientServerConnectionID, clientSocketID, reliableChannelID, buffer, BUFFER_SIZE, out possibleError);
        //Display the error (if it did error out)
        if (!possibleError.Equals((byte)NetworkError.Ok))
        {
            NetworkError error = (NetworkError)possibleError;
            Debug.Log("Error occurred: " + error.ToString());
        }
    }

    void InterperateMessage(string message)
    {
        //if the message is "goto_NewScene"
        //load the level named "Scene2"
        if (message.Equals("goto_NewScene"))
            Application.LoadLevel("Scene2");
    }

    void PollBasics()
	{
		//prepare to receive messages by practicing good bookkeeping
        int recHostID,
            connectionID,
            channelID,
            dataSize;
        byte[] buffer = new byte[BUFFER_SIZE];
        byte possibleError;
        NetworkEventType networkEvent;

		//do
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
						//InterperateMessage(//the message to interperate);
				//if disconnection
					//verify that the message was meant for me, and that I am disconnecting from the current connection I have with the server
						//debug that I disconnected
						//set my bool that is keeping track if I am connected to a server to false
		//while (the network event I am receiving is not Nothing)
        do
        {
            networkEvent = NetworkTransport.Receive(out recHostID, out connectionID, out channelID, buffer, BUFFER_SIZE, out dataSize, out possibleError);
            switch (networkEvent)
            {
                case (NetworkEventType.Nothing): // Do nothing
                    break;
                case (NetworkEventType.ConnectEvent): // Broadcast that a client has connected
                    if (recHostID.Equals(clientSocketID))
                    {
                        Debug.Log("Connected to server " + clientServerConnectionID);
                        isClientConnected = true;
                    }
                    break;
                case (NetworkEventType.DataEvent): // Broadcast that a client is sending data
                    if (recHostID.Equals(clientSocketID) && isClientConnected)
                    {
                        Stream memoryStream = new MemoryStream(buffer);
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        string message = binaryFormatter.Deserialize(memoryStream).ToString();
                        Debug.Log("Received data from server " + clientServerConnectionID + ". Message: " + message);
                        InterperateMessage(message);
                    }
                    break;
                case (NetworkEventType.DisconnectEvent): // Broadcast that a client has disconnected
                    if (recHostID.Equals(clientSocketID))
                    {
                        Debug.Log("Disconnected from server " + clientServerConnectionID);
                        isClientConnected = false;
                    }
                    break;
            }
        } while(!networkEvent.Equals(NetworkEventType.Nothing));
	}
}	