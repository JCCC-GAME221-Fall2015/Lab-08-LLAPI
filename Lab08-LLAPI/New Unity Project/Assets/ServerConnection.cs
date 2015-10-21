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

		NetworkTransport.Init (globalConfig);

		serverSocketID = NetworkTransport.AddHost (hostTopology, 7777);

		if (serverSocketID < 0) 
		{
			Debug.Log ("Server socket creation falied!");
		} 
		else 
		{
			Debug.Log ("Server socket creation success");
		}

		serverInitialized = true;
	} 

	// Update is called once per frame 
	void Update () { 
		if (!serverInitialized) 
		{
			return;
		}

		int recHostId;					//who received the message
		int connectionID;				//who sent the message
		int channelId;					//What channel the message was sent from
		int dataSize;					//how large teh message can be
		byte[] buffer = new byte[1024];	//the actual message
		byte error;						//if there is an error

		NetworkEventType networkEvent = NetworkEventType.DataEvent;

		do 
		{
			networkEvent = NetworkTransport.Receive(out recHostId, out connectionID, out channelId, buffer,
			                                        1024, out dataSize, out error);
			switch(networkEvent)
			{
				case NetworkEventType.Nothing:
					break;
				case NetworkEventType.ConnectEvent:
					//Server received disconnect event
					if(recHostId == serverSocketID){
						Debug.Log ("Server:Player " + connectionID.ToString() + " connected!");
					}
					break;
				case NetworkEventType.DataEvent:
				if(recHostId == serverSocketID){
				//Open a memory stream with a size equal to the buffer we set up earlier
				Stream memoryStream = new MemoryStream(buffer);

				//Create a binaryformatter to begin reading the information from the memory string
				BinaryFormatter binaryFormatter = new BinaryFormatter();

				//Create a binaryformatter to begin reading the information stored in the memory string
				//then convert that into a string
					string message = binaryFormatter.Deserialize(memoryStream).ToString();

					//debug out the message you worked so hard to figure out!
					Debug.Log ("Server: Received Data from " + 
					           connectionID.ToString() + 
					           "! Message: " + message);
				}
					break;
				case NetworkEventType.DisconnectEvent:
					//Server received disconnect event
					if(recHostId == serverSocketID)
					{
						Debug.Log ("Server: Received disconnect from " + connectionID.ToString());
					}
					break;
				default:
					break;
			}

		} while(networkEvent != NetworkEventType.Nothing);
	} 
}