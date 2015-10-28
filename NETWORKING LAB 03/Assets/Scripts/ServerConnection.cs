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
	void Start () {
		DontDestroyOnLoad(this);

		GlobalConfig globalConfig = new GlobalConfig ();
		globalConfig.ReactorModel = ReactorModel.FixRateReactor;
		globalConfig.ThreadAwakeTimeout = 10;

		ConnectionConfig connectionConfig = new ConnectionConfig ();
		reliableChannelID = connectionConfig.AddChannel (QosType.ReliableSequenced);
		unreliableChannelID = connectionConfig.AddChannel (QosType.UnreliableSequenced);

		HostTopology hostTopology = new HostTopology (connectionConfig, maxConnections);
		
		NetworkTransport.Init(globalConfig);

		serverSocketID = NetworkTransport.AddHost (hostTopology, 7777);

		if (serverSocketID < 0)
			Debug.Log ("Server socket creation failed!");
		else
			Debug.Log ("Server socket creation success");

		serverInitialized = true;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if(!serverInitialized)
			return;
		
		int recHostId;
		int connectionId;
		int channelId;
		int dataSize;
		byte[] buffer = new byte[1024];
		byte error;

		NetworkEventType networkEvent = NetworkEventType.DataEvent;

		do
		{
			networkEvent = NetworkTransport.Receive( out recHostId , out connectionId , out channelId , buffer , 1024 , out dataSize , out error );
			
			switch(networkEvent)
			{
				case NetworkEventType.Nothing:
					break;
				case NetworkEventType.ConnectEvent:
					if( recHostId == serverSocketID )
					{
						Debug.Log ("Server: Player " + connectionId.ToString () + " connected!" );
					}
					break;
				case NetworkEventType.DataEvent:
					if( recHostId == serverSocketID )
					{
						Stream memoryStream = new MemoryStream(buffer); 
						BinaryFormatter binaryFormatter = new BinaryFormatter();
						string message = binaryFormatter.Deserialize( memoryStream ).ToString ();
						Debug.Log ("Server: Received Data from " + connectionId.ToString () + "! Message: " + message );
						RespondMessage(message, connectionId);
					}
					break;
				case NetworkEventType.DisconnectEvent:
					if( recHostId == serverSocketID )
					{
						Debug.Log ("Server: Received disconnect from " + connectionId.ToString () );
					}
					break;
			}
			
		} while ( networkEvent != NetworkEventType.Nothing );

	}

	void SendMessage(string message, int target)
	{
		byte error;
		byte[] buffer = new byte[1024];
		Stream memoryStream = new MemoryStream(buffer);
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		
		binaryFormatter.Serialize (memoryStream, message);
		NetworkTransport.Send (serverSocketID, target, reliableChannelID, buffer, (int)memoryStream.Position, out error);
		
		if (error != (byte)NetworkError.Ok)
		{
			NetworkError networkError = (NetworkError) error;
			Debug.Log ("Error: " + networkError.ToString ());
		}
	}

	void RespondMessage(string message, int playerID)
	{
		if (message == "FirstConnect")
		{
			Debug.Log ("message was FirstConnect! from player " + playerID.ToString());
			SendMessage("goto_NewScene", playerID);
			if (Application.loadedLevelName != "Scene2")
				Application.LoadLevel("Scene2");
		}
	}
}
