using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;

public class UnetHighLevelNetworkManager : NetworkManager{
	#region protected static properties
	public readonly static short MessageId = 4353;
	#endregion

	#region public enum definitions
	public enum ServerType{None, Lan, Online}
	#endregion

	#region public event definitions: Common Events
	public event MultiplayerAPI.OnMessageReceivedDelegate OnMessageReceived;
	#endregion

	#region public class event definitions: Client Events
	public event MultiplayerAPI.OnDisconnectionDelegate OnDisconnection;
	public event MultiplayerAPI.OnJoinedDelegate OnJoined;
	public event MultiplayerAPI.OnJoinErrorDelegate OnJoinError;
	public event MultiplayerAPI.OnMatchesDiscoveredDelegate OnMatchesDiscovered;
	public event MultiplayerAPI.OnMatchDiscoveryErrorDelegate OnMatchDiscoveryError;
	#endregion

	#region public event definitions: Server Events
	public event MultiplayerAPI.OnMatchCreatedDelegate OnMatchCreated;
	public event MultiplayerAPI.OnMatchCreationErrorDelegate OnMatchCreationError;
	public event MultiplayerAPI.OnMatchDestroyedDelegate OnMatchDestroyed;
	public event MultiplayerAPI.OnPlayerConnectedToMatchDelegate OnPlayerConnectedToMatch;
	public event MultiplayerAPI.OnPlayerDisconnectedFromMatchDelegate OnPlayerDisconnectedFromMatch;
	#endregion

	#region public instance properties
	public int Connections{
		get{
			if (this.IsServerStarted()){
				int connections = 0;

				for (int i = 0; i < NetworkServer.connections.Count; ++i){
					if (NetworkServer.connections[i] != null){
						++connections;
					}
				}

				return connections;
			}else if (this.IsClientConnected()){
				return 1;
			}else{
				return 0;
			}
		}
	}

	public ulong NetworkId{
		get{
			return this._networkId;
		}
	}

	public NodeID NodeId{
		get{
			return this.NodeId;
		}
	}
    #endregion

    #region public instance fields
    public NetworkIdentity networkIdentity;
    public ServerType serverType = ServerType.None;
	public bool publicLanGame = false;
    #endregion

    #region protected instance fields
    protected UnetHighLevelNetworkDiscovery _networkdDiscovery;
	protected NetworkMatch _networkMatch;
	protected ulong _networkId = (ulong)NetworkID.Invalid;
	protected NodeID _nodeId = NodeID.Invalid;

	protected float _lanDiscoveryStartSearchTime = 0f;
	protected float _lanDiscoveryLastSearchTime = 0f;
	protected bool _searching = false;

	protected bool _lanDiscoveryEnabled{
		get{
			return this._networkdDiscovery != null && this._networkdDiscovery.isClient && this._searching;
		}
	}
	#endregion

	#region public override methods
	public new void ServerChangeScene(string newSceneName){
		throw new NotImplementedException();
	}

	public new void SetMatchHost(string newHost, int port, bool https){
		throw new NotImplementedException();
	}

	public new void SetupMigrationManager(NetworkMigrationManager manager){
		throw new NotImplementedException();
	}

	public new NetworkClient StartClient(){
		this.serverType = ServerType.None;
		return base.StartClient();
	}

	public new NetworkClient StartClient(MatchInfo matchInfo){
		this.serverType = ServerType.None;
		return base.StartClient(matchInfo);
	}

	public new NetworkClient StartClient(MatchInfo matchInfo, ConnectionConfig config){
		this.serverType = ServerType.None;
		return base.StartClient(matchInfo, config);
	}

	public new NetworkClient StartHost(){
		this.serverType = ServerType.Lan;
		return base.StartHost();
	}

	public new NetworkClient StartHost(MatchInfo matchInfo){
		this.serverType = ServerType.Online;
		return base.StartHost(matchInfo);
	}

	public new NetworkClient StartHost(ConnectionConfig config, int maxConnections){
		throw new NotImplementedException();
	}

	public new bool StartServer(){
		this.serverType = ServerType.Lan;
		return base.StartServer();
	}

	public new bool StartServer(MatchInfo matchInfo){
		this.serverType = ServerType.Online;
		return base.StartServer(matchInfo);
	}

	public new bool StartServer(ConnectionConfig config, int maxConnections){
		throw new NotImplementedException();
	}

	public new void StopClient(){
		this.serverType = ServerType.None;
		base.StopClient();
	}

	public new void StopHost(){
		this.serverType = ServerType.None;
		base.StopHost();
	}

	public new void StopServer(){
		this.serverType = ServerType.None;
		base.StopServer();
	}
	#endregion

	#region public instance methods
	public virtual void CreateMatch(string matchName = null, bool publicMatch = true, string password = null){
		// We can only have two players in this game
		this._networkMatch.CreateMatch(
			matchName ?? "Random Match", 
			2, 
			publicMatch,
            password ?? "", 
			"",
			"",
			0,
			0,
			this.OnMatchCreatedResponse
		);
	}

	public virtual void DestroyMatch(NetworkID networkId){
		this._networkMatch.DestroyMatch(networkId, 0, this.OnMatchDestroyedResponse);
		this._networkMatch = null;

		Destroy(this.gameObject.GetComponent<NetworkMatch>());
		this.Initialize();
	}

	public virtual void DropMatch(NetworkID networkId, NodeID nodeId){
		this._networkMatch.DropConnection(networkId, nodeId, 0, this.OnMatchDroppedResponse);
	}

	public virtual void JoinMatch(NetworkID networkId, string password = null){
		this._networkMatch.JoinMatch(networkId, password ?? "", "", "", 0, 0, this.OnMatchJoinedResponse);
	}

	public virtual void StartSearchingMatches(int startPage = 0, int pageSize = 20, string filter = null){
		this._searching = true;

		this._networkMatch.ListMatches(
			startPage, 
			pageSize, 
			filter ?? string.Empty,
			true,
			0,
			0,
			this.OnMatchListResponse
		);
	}

	public virtual void StopSearchingMatches(){
		this._searching = true;
	}

	public virtual void Initialize(){
		//this.logLevel = LogFilter.FilterLevel.Developer;

		if (this._networkdDiscovery == null){
			this._networkdDiscovery = this.GetComponent<UnetHighLevelNetworkDiscovery>();

			if (this._networkdDiscovery == null){
				this._networkdDiscovery = this.gameObject.AddComponent<UnetHighLevelNetworkDiscovery>();
			}



			string appKey = Application.companyName + "|" + Application.productName;

			this._networkdDiscovery.broadcastData = UFE.config.networkOptions.port.ToString();
			this._networkdDiscovery.broadcastInterval = Mathf.RoundToInt(UFE.config.networkOptions.lanDiscoveryBroadcastInterval * 1000f);
			this._networkdDiscovery.broadcastKey = Mathf.Abs(appKey.GetHashCode());
			this._networkdDiscovery.broadcastPort = UFE.config.networkOptions.lanDiscoveryPort;
			this._networkdDiscovery.broadcastVersion = Mathf.Abs(Application.version.GetHashCode());
			this._networkdDiscovery.showGUI = false;
			this._networkdDiscovery.useNetworkManager = false;
			//this._networkdDiscovery.OnLanGamesDiscovered += this.RaiseOnLanGamesDiscovered;
			//this._networkdDiscovery.OnLanGamesDiscoveryError += this.RaiseOnLanGamesDiscoveryError;
			this._networkdDiscovery.Initialize();

		}


		//this.StartMatchMaker ();
		//this.matchMaker.baseUri = new System.Uri ("https://ap1-mm.unet.unity3d.com");

		Transform matchObjectTransform = this.transform.Find ("networkMatch");
		if ( matchObjectTransform != null) {
			GameObject.Destroy (matchObjectTransform.gameObject);
		}

		GameObject matchObject = new GameObject ();
		matchObject.name = "networkMatch";
		matchObject.transform.SetParent(this.transform);
		matchObject.AddComponent<NetworkMatch> ();
		this._networkMatch = matchObject.GetComponent<NetworkMatch> ();
		this._networkMatch.baseUri = new System.Uri ("https://ap1-mm.unet.unity3d.com");

		this.maxDelay = 0;



		this._lanDiscoveryStartSearchTime = 0f;
		this._lanDiscoveryLastSearchTime = 0f;
		this._searching = false;
	}

	public virtual bool IsServerStarted(){
		return NetworkServer.active;
	}

	public virtual bool SendNetworkMessage(byte[] bytes){
		if (this.IsClientConnected()){
			this.client.SendUnreliable(MessageId, new SerializedNetworkMessage(bytes));
			return true;
		}else if (this.IsServerStarted()){
			NetworkServer.SendUnreliableToAll(MessageId, new SerializedNetworkMessage(bytes));
			return true;
		}

		return false;
	}

	public virtual void StartBroadcastingLanGame(){
		if (this.serverType == ServerType.Lan && this.publicLanGame && !this._networkdDiscovery.isServer){
			// Debug.LogWarning("Start Broadcasting LAN Game");
			this._networkdDiscovery.StartAsServer();
		}
	}

	public virtual void StopBroadcastingLanGame(){
		if (this._networkdDiscovery.isServer){
			// Debug.LogWarning("Stop Broadcasting LAN Game");
			this._networkdDiscovery.StopBroadcast();
		}
	}

	public virtual void StartSearchingLanGames(){
		this._lanDiscoveryLastSearchTime = this._lanDiscoveryStartSearchTime = Time.timeSinceLevelLoad;
		this._searching = true;

		this.StopBroadcastingLanGame();

		if (!this._networkdDiscovery.isClient){
			// Debug.LogWarning("Searching LAN Games");
			this._networkdDiscovery.StartAsClient();
		}
	}

	public virtual void StopSearchingLanGames(){
		this._lanDiscoveryLastSearchTime = 0f;
		this._lanDiscoveryStartSearchTime = 0f;
		this._searching = false;

		if (this._networkdDiscovery.isClient){
			// Debug.LogWarning("Stop LAN Games Discovery");
			this._networkdDiscovery.StopBroadcast();
		}
	}
	#endregion

	#region protected override methods
	public override void OnClientConnect(NetworkConnection connection){
		base.OnClientConnect(connection);

		if (this.client != null){
			this.client.RegisterHandler(MessageId, this.OnMessage);
			this.client.Send(MsgType.Ready, new UnityEngine.Networking.NetworkSystem.ReadyMessage());
		}

		this.RaiseOnJoined(new MultiplayerAPI.JoinedMatchInformation());
	}

	public override void OnClientDisconnect(NetworkConnection connection){
		if (this.client != null){
			this.client.UnregisterHandler(MessageId);
		}

		base.OnClientDisconnect(connection);
		this.RaiseOnDisconnection();
	}

	public override void OnClientError(NetworkConnection connection, int errorCode){
		if (this.client != null){
			this.client.UnregisterHandler(MessageId);
		}

		base.OnClientError(connection, errorCode);
		this.RaiseOnJoinError("OnClientError (code "+ errorCode + ")");
	}

	public override void OnClientNotReady(NetworkConnection connection){
		if (this.client != null){
			this.client.UnregisterHandler(MessageId);
		}

		base.OnClientNotReady(connection);
		this.RaiseOnJoinError("OnClientNotReady");
	}

	public override void OnClientSceneChanged(NetworkConnection connection){
		base.OnClientSceneChanged(connection);

		if (this.client != null){
			this.client.UnregisterHandler(MessageId);
		}

		this.RaiseOnJoinError("OnClientSceneChanged");
	}

//	public override void OnMatchCreate(CreateMatchResponse response){
//		base.OnMatchJoined();
//		Debug.LogWarning("OnMatchCreate");
//	}
//
//	public override void OnMatchJoined(JoinMatchResponse matchInfo){
//		Debug.LogWarning("OnMatchJoined");
//	}
//
//	public override void OnMatchList(ListMatchResponse matchList){
//		Debug.LogWarning("OnMatchList");
//	}
//
//	public override void OnServerAddPlayer(NetworkConnection connection, short playerControllerId){
//		Debug.LogWarning("OnMatchOnServerAddPlayerList: " + connection.address);
//		base.OnServerAddPlayer(connection, playerControllerId);
//	}

	public override void OnServerConnect(NetworkConnection connection){
		this.StopSearchingLanGames();
		base.OnServerConnect(connection);
		this.RaiseOnPlayerConnectedToMatch(new MultiplayerAPI.PlayerInformation(networkIdentity));
	}

	public override void OnServerDisconnect(NetworkConnection connection){
		this.StopSearchingLanGames();
		base.OnServerDisconnect(connection);
		this.RaiseOnPlayerDisconnectedFromMatch(new MultiplayerAPI.PlayerInformation(networkIdentity));
	}

	public override void OnServerError(NetworkConnection connection, int errorCode){
		this.StopSearchingLanGames();
		base.OnServerError(connection, errorCode);
		this.RaiseOnPlayerDisconnectedFromMatch(new MultiplayerAPI.PlayerInformation(networkIdentity));
	}

	//public override void OnServerReady(NetworkConnection connection);
	//public override void OnServerRemovePlayer(NetworkConnection connection, Networking.PlayerController player);
	//public override void OnServerSceneChanged(string sceneName);

	public override void OnStartHost(){
		this.StopSearchingLanGames();
		base.OnStartHost();
		this.StartBroadcastingLanGame();

		// Debug.LogWarning("Start Host");
		NetworkServer.RegisterHandler(MessageId, this.OnMessage);
		this.RaiseOnMatchCreated(new MultiplayerAPI.CreatedMatchInformation(null, (NetworkID)this._networkId, this._nodeId));
	}

	public override void OnStartServer(){
		this.StopSearchingLanGames();
		base.OnStartServer();
		this.StartBroadcastingLanGame();

		// Debug.LogWarning("Start Server");
		NetworkServer.RegisterHandler(MessageId, this.OnMessage);
		this.RaiseOnMatchCreated(new MultiplayerAPI.CreatedMatchInformation(null, (NetworkID)this._networkId, this._nodeId));
	}

	public override void OnStopClient(){
		if (this.client != null){
			this.client.UnregisterHandler(MessageId);
		}

		base.OnStopClient();
		this.RaiseOnDisconnection();
	}

	public override void OnStopServer(){
		NetworkServer.UnregisterHandler(MessageId);
		this.StopBroadcastingLanGame();
		base.OnStopServer();
		this.RaiseOnMatchDestroyed();
	}
	#endregion

	#region protected instance methods
	protected virtual void OnMessage(NetworkMessage message){
		SerializedNetworkMessage msg = message.ReadMessage<SerializedNetworkMessage>();
		this.RaiseOnMessageReceived(msg.bytes);
	}
	#endregion

	#region protected instance methods: Common Events
	protected virtual void RaiseOnMessageReceived(byte[] bytes){
		if (this.OnMessageReceived != null){
			// TODO: check if the player which has sent the message is the same that appears in the message body
			this.OnMessageReceived(bytes);
		}
	}
	#endregion

	#region protected instance methods: Client Events
	protected virtual void RaiseOnDisconnection(){
		this._networkId = (ulong)NetworkID.Invalid;
		this._nodeId = NodeID.Invalid;

		if (this.OnDisconnection != null){
			this.OnDisconnection();
		}
	}

	protected virtual void RaiseOnJoined(MultiplayerAPI.JoinedMatchInformation match){
		if (this.OnJoined != null){
			this.OnJoined(match);
		}
	}

	protected virtual void RaiseOnJoinError(string error){
		if (this.OnJoinError != null){
            Debug.LogError(error);
			this.OnJoinError();
		}
	}

	protected virtual void RaiseOnMatchesDiscovered(ReadOnlyCollection<MultiplayerAPI.MatchInformation> matches){
		if (this.OnMatchesDiscovered != null){
			this.OnMatchesDiscovered(matches);
		}
	}

	protected virtual void RaiseOnMatchDiscoveryError(){
		if (this.OnMatchDiscoveryError != null){
			this.OnMatchDiscoveryError();
		}
	}
	#endregion

	#region protected instance methods: Server Events
	protected virtual void RaiseOnMatchCreated(MultiplayerAPI.CreatedMatchInformation match){
		if (this.OnMatchCreated != null){
			this.OnMatchCreated(match);
		}
	}

	protected virtual void RaiseOnMatchCreationError(){
		if (this.OnMatchCreationError != null){
			this.OnMatchCreationError();
		}
	}

	protected virtual void RaiseOnMatchDestroyed(){
		this._networkId = (ulong)NetworkID.Invalid;
		this._nodeId = NodeID.Invalid;

		if (this.OnMatchDestroyed != null){
			this.OnMatchDestroyed();
		}
	}

	protected virtual void RaiseOnPlayerConnectedToMatch(MultiplayerAPI.PlayerInformation player){
		if (this.OnPlayerConnectedToMatch != null){
			this.OnPlayerConnectedToMatch(player);
		}
	}

	protected virtual void RaiseOnPlayerDisconnectedFromMatch(MultiplayerAPI.PlayerInformation player){
		if (this.OnPlayerDisconnectedFromMatch != null){
			this.OnPlayerDisconnectedFromMatch(player);
		}
	}
	#endregion

	#region MonoBehaviour methods
	protected virtual void Update(){
//		if (this.IsServerStarted() && this.Connections > 0){
//			System.Text.StringBuilder sb = new System.Text.StringBuilder();
//
//			sb.Append("Connections to this server: ").Append(this.Connections);
//			for (int i = 0; i < NetworkServer.connections.Count; ++i){
//				NetworkConnection connection = NetworkServer.connections[i];
//
//				if (connection != null){
//					sb.AppendLine();
//					sb	.Append(connection.address ?? "<Null>")	.Append("\t| ")
//						.Append(connection.connectionId)		.Append("\t| ")
//						.Append(connection.hostId)				.Append("\t| ")
//						.Append(connection.isReady)				.Append("\t| ")
//						.Append(connection.lastMessageTime);
//				}
//			}
//
//			Debug.Log(sb.ToString());
//		}

		// Check if we're searching LAN games...
		if (this._lanDiscoveryEnabled){
			float timeSinceLevelLoad = Time.timeSinceLevelLoad;

			if(
				UFE.config.networkOptions.lanDiscoverySearchTimeout > 0 &&
				timeSinceLevelLoad > this._lanDiscoveryStartSearchTime + UFE.config.networkOptions.lanDiscoverySearchTimeout
			){
				// If we have defined a timeout and the timeout has passed without having found any LAN server,
				// we return an error
				if (this._networkdDiscovery.broadcastsReceived != null && this._networkdDiscovery.broadcastsReceived.Count > 0){
					List<MultiplayerAPI.MatchInformation> servers = new List<MultiplayerAPI.MatchInformation>();
					foreach (string address in this._networkdDiscovery.broadcastsReceived.Keys){
						servers.Add(new MultiplayerAPI.MatchInformation(address));
					}

					this.StopSearchingLanGames();
					this.RaiseOnMatchesDiscovered(new ReadOnlyCollection<MultiplayerAPI.MatchInformation>(servers));
				}else{
					this.StopSearchingLanGames();
					this.RaiseOnMatchDiscoveryError();
				}
			}else if(timeSinceLevelLoad > this._lanDiscoveryLastSearchTime + UFE.config.networkOptions.lanDiscoverySearchInterval){
				// If we have reached the next "search interval, return the found servers (if any)
				if (this._networkdDiscovery.broadcastsReceived != null && this._networkdDiscovery.broadcastsReceived.Count > 0){
					List<MultiplayerAPI.MatchInformation> servers = new List<MultiplayerAPI.MatchInformation>();
					foreach (string address in this._networkdDiscovery.broadcastsReceived.Keys){
						servers.Add(new MultiplayerAPI.MatchInformation(address));
					}

					this.StopSearchingLanGames();
					this.RaiseOnMatchesDiscovered(new ReadOnlyCollection<MultiplayerAPI.MatchInformation>(servers));
				}else{
					this._lanDiscoveryLastSearchTime = timeSinceLevelLoad;
				}
			}
		}
	}
	#endregion

	#region protected instance methods
	protected virtual void OnMatchCreatedResponse(bool success, string extendedInfo, MatchInfo responseData) {
		if (success && !this.IsClientConnected()){
			//Utility.SetAccessTokenForNetwork(response.networkId, new NetworkAccessToken(response.accessTokenString));
			//NetworkServer.Listen(new MatchInfo(response), UFE.config.networkOptions.port);

			this._networkId = (ulong)responseData.networkId;
			this._nodeId = responseData.nodeId;

			this.StartServer(responseData);
		}else{
			this.DestroyMatch((NetworkID)responseData.networkId);
		}
	}

	protected virtual void OnMatchDestroyedResponse(bool success, string message){
		if (success){
			this.RaiseOnMatchDestroyed();

			if (this.IsServerStarted()){
				this.StopServer();
			}
		}
	}

	protected virtual void OnMatchDroppedResponse(bool success, string message){
		if (success){
			this.RaiseOnMatchDestroyed();

			if (this.client != null && this.client.isConnected){
				this.client.Disconnect();
			}
		}
	}

	protected virtual void OnMatchJoinedResponse(bool success, string accessTokenString, MatchInfo matchInfo){
		if (success && !this.IsServerStarted()){
			NetworkAccessToken currentToken = Utility.GetAccessTokenForNetwork((NetworkID)matchInfo.networkId);

			if (currentToken == null || !currentToken.IsValid()){
				Utility.SetAccessTokenForNetwork((NetworkID)matchInfo.networkId, matchInfo.accessToken);
			}

			this._networkId = (ulong)matchInfo.networkId;
			this._nodeId = matchInfo.nodeId;

			this.client = this.StartClient(matchInfo);
			this.client.RegisterHandler(MessageId, this.OnMessage);
		}else{
			this.DropMatch((NetworkID)matchInfo.networkId, matchInfo.nodeId);
			this.RaiseOnJoinError("OnMatchJoinedResponse");
		}
	}

	protected void OnMatchListResponse(bool success, string extendedInfo, List<MatchInfoSnapshot> matches){
		if (this._searching){
			if (success && matches != null){
				List<MultiplayerAPI.MatchInformation> list = new List<MultiplayerAPI.MatchInformation>();

				for (int i = 0; i < matches.Count; ++i){
					list.Add(new MultiplayerAPI.MatchInformation(matches[i]));
				}

				this.RaiseOnMatchesDiscovered(new ReadOnlyCollection<MultiplayerAPI.MatchInformation>(list));
			}else{
				this.RaiseOnMatchDiscoveryError();
			}
		}
	}

	protected virtual ReadOnlyCollection<string> ParseNetworkDiscoveryResponse(Dictionary<string, NetworkBroadcastResult> response){
		List<string> addresses = new List<string>();

		if (response != null){
			foreach (NetworkBroadcastResult result in response.Values){
				addresses.Add(
					result.serverAddress.Replace("::ffff:", string.Empty) + ":" + 
					UnetHighLevelNetworkDiscovery.Translate(result.broadcastData)
				);
			}
		}

		return new ReadOnlyCollection<string>(addresses);
	}
	#endregion
}
