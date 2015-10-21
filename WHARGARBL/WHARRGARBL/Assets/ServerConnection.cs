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
	void Start ()
    {
        GlobalConfig globalConfig = new GlobalConfig();
        globalConfig.ReactorModel = ReactorModel.FixRateReactor;
        globalConfig.ThreadAwakeTimeout = 10;

        ConnectionConfig connectionConfig = new ConnectionConfig();
        reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced);
        unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced);

        HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);

        NetworkTransport.Init(globalConfig);

        serverSocketID = NetworkTransport.AddHost(hostTopology, 7777);

        if(serverSocketID < 0)
        {
            Debug.Log("Server socket creation failed!");
        }
        else
        {
            Debug.Log("Server socket creation success!");
            serverInitialized = true;
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
	    if (serverInitialized)
        {
            int recHostID;                  //Who received the message
            int connectionID;               //who sent the message
            int channelID;                  //what channel the message was sent from
            int dataSize;                   //how large the message can be
            byte[] buffer = new byte[1024]; //the actual message
            byte error;                     //if there is an error
        }
	}
}
