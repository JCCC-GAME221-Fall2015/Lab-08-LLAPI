using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters;
using UnityEngine.Networking;

public class ServerConnection : MonoBehaviour {

	int serverSocketID = -1;
	int maxConnections = 10;
	byte unreliableChannelID;
	byte reliableChannelID;
	bool serverInitilized = false;

	// Use this for initialization
	void Start () {
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
				break;
			case NetworkEventType.DisconnectEvent:
				break;
			}
		} while (networkEvent != NetworkEventType.Nothing);
	
	}
}
