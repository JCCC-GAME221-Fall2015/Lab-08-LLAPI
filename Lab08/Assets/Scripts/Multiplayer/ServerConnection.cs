using UnityEngine;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.Networking;

public class ServerConnection : MonoBehaviour
{

	private int serverSocketID = -1;
	private int maxConnections = 10;
	private byte unreliableChannelID;
	private byte reliableChannelID;
	private bool serverInitialized = false;

	// Use this for initialization
	void Start ()
	{
		// Creates a new GlobalConfig that determines how it will react when receiving packets
		GlobalConfig globalConfig = new GlobalConfig();

		// Fixed rate makes it so it handles packets periodically (ie every 100 ms)
		globalConfig.ReactorModel = ReactorModel.FixRateReactor;

		// Sets so it deals with packets every 10 ms
		globalConfig.ThreadAwakeTimeout = 10;

		// Create a channel for packets to be sent across
		ConnectionConfig connectionConfig = new ConnectionConfig();

		// Create and set the reliable and unreliable channels
		reliableChannelID = connectionConfig.AddChannel(QosType.ReliableSequenced);
		unreliableChannelID = connectionConfig.AddChannel(QosType.UnreliableSequenced);

		// Create a host topology that determines the maximum number of clients the server can have
		HostTopology hostTopology = new HostTopology(connectionConfig, maxConnections);

		// Initialize the server
		NetworkTransport.Init(globalConfig);

		// Open the socket on your network
		serverSocketID = NetworkTransport.AddHost(hostTopology, 7777);

		// Check to see that the socket (and server) was successfully initialized
		if (serverSocketID < 0)
		{
			Debug.Log("Server socket creation failed!");
		}
		else
		{
			Debug.Log("Server socket creation success");
			serverInitialized = true;
		}
	}
	
	// Update is called once per frame
	void Update ()
    {

	    if (!serverInitialized)
	    {
	        return;
	    }

	    int recHostID;                      // Who is receiving the message
	    int connectionID;                   // Who sent the message
	    int channelID;                      // What channel the message was sent from
	    int dataSize;                       // How large the message can be
	    byte[] buffer = new byte[1024];     // The actual message
	    byte error;                         // If there is an error

        // The type of message that was sent
        NetworkEventType networkEvent = NetworkEventType.DataEvent;

	    do
	    {
	        networkEvent = NetworkTransport.Receive(out recHostID, out connectionID, out channelID, buffer, 1024, out dataSize,
	            out error);

	        switch (networkEvent)
	        {
                case NetworkEventType.Nothing:
	                break;
                case NetworkEventType.ConnectEvent:
                    // Server received connect event
	                if (recHostID == serverSocketID)
	                {
	                    Debug.Log("Server: Player " + connectionID + " connected!");
	                }
	                break;
                case NetworkEventType.DataEvent:
	                break;
                case NetworkEventType.DisconnectEvent:
                    // Server received disconnect event
                    if (recHostID == serverSocketID)
                    {
                        Debug.Log("Server: Received discconect from " + connectionID);
                    }
                    break;

	        }
	    } while (networkEvent != NetworkEventType.Nothing);
    }
}
