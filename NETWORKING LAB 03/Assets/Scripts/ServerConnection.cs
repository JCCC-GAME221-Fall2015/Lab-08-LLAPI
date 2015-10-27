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
			
		} while ( networkEvent != NetworkEventType.Nothing );

	}
}
