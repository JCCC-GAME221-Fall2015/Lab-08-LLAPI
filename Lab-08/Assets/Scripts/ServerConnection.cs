using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Networking;

public class ServerConnection : MonoBehaviour {

	int serverSocketID = -1;
	int maxConnections = 10;
	byte unreliableChannelID;
	byte reliableChannelID;
	bool serverInitilized = false;

	// Use this for initialization
	void Start () {
        DontDestroyOnLoad(this);

		GlobalConfig globalConfig = new GlobalConfig ();
		globalConfig.ReactorModel = ReactorModel.FixRateReactor;
		globalConfig.ThreadAwakeTimeout = 10;

		ConnectionConfig connectionConfig = new ConnectionConfig ();
		reliableChannelID = connectionConfig.AddChannel (QosType.ReliableSequenced);
		unreliableChannelID = connectionConfig.AddChannel (QosType.UnreliableSequenced);

		HostTopology hostTopology = new HostTopology (connectionConfig, maxConnections);

		NetworkTransport.Init (globalConfig);
		serverSocketID = NetworkTransport.AddHost (hostTopology, 7777);

		if (serverSocketID < 0)
			Debug.Log ("Server socket creation failed");
		else
			Debug.Log ("Server socket creation success");

		serverInitilized = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (!serverInitilized) {
			return;
		}

		int recHostId;			//who received the message
		int connectionId; 		//who sent the message
		int channelId;			//what channel the message was sent from
		int dataSize;			//how large the message can be
		byte[] buffer = new byte[1024];		//the actual message
		byte error;				//if there is an error

		NetworkEventType networkEvent = NetworkEventType.DataEvent;

		do {
			networkEvent = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, buffer, 1024, out dataSize, out error);

			switch(networkEvent)
			{
			case NetworkEventType.Nothing:
				break;
			case NetworkEventType.ConnectEvent:
				//server received disconnect event
				if(recHostId == serverSocketID){
					Debug.Log ("Server: Player " + connectionId.ToString() + " connected");
				}
				break;
			case NetworkEventType.DataEvent:
                    //Verify the server is the intended target^
                    if(recHostId == serverSocketID)
                    {
                        //open a memory stream with a size equal to the buffer we set up earlier
                        MemoryStream memoryStream = new MemoryStream(buffer);

                        //Create a binary formatter to begin reading the information from the member stream
                        BinaryFormatter binaryFormatter = new BinaryFormatter();

                        //utilize the binary formatter to deserialize the binary information stored i nthe memory string
                        //then convert that into a string
                        string message = binaryFormatter.Deserialize(memoryStream).ToString();

                        //debug out the message
                        Debug.Log("Server: Received Data from " + connectionId.ToString() + ". Message: " + message);

                        RespondMessage(message, connectionId);
                    }
				break;
			case NetworkEventType.DisconnectEvent:
                    //server received disconnect event
                    if(recHostId == serverSocketID)
                    {
                        Debug.Log("Server: Received disconnect from " + connectionId.ToString());
                    }
				break;
			}
		} while (networkEvent != NetworkEventType.Nothing);
	}

    void SendMessage(string message, int target)
    {
        byte error;
        byte[] buffer = new byte[1024];
        MemoryStream memoryStream = new MemoryStream(buffer);
        BinaryFormatter binaryFormatter = new BinaryFormatter();

        binaryFormatter.Serialize(memoryStream, message);

        //Who is sending, where to, what channel, what infor, how much infro, if there is an error
        NetworkTransport.Send(serverSocketID, target, reliableChannelID, buffer, (int)memoryStream.Position, out error);

        //error is always assigned and it uses ok to donate that there is nothing wrong
        if(error != (byte)NetworkError.Ok)
        {
            NetworkError networkError = (NetworkError)error;
            Debug.Log("Error: " + networkError.ToString());
        }
    }

    void RespondMessage(string message, int playerID)
    {
        //Student will fill this out
        if(message == "FirstConnect")
        {
            Debug.Log("First Conection: " + playerID);
        }
        SendMessage("goto_Scene2", playerID);
        if(Application.loadedLevelName == "TestScene")
        {
            Application.LoadLevel("Scene2");
        }
    }
}
