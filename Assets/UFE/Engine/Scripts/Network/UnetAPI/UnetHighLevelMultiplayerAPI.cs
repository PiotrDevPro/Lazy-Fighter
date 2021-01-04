using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

public abstract class UnetHighLevelMultiplayerAPI : MultiplayerAPI{
	#region public override properties
	public override int Connections{
		get{
			return this._networkManager != null ? this._networkManager.Connections : 0;
		}
	}

	public override MultiplayerAPI.PlayerInformation Player{
		get{
			return new MultiplayerAPI.PlayerInformation(this._networkManager.networkIdentity);
		}
	}

	public override float SendRate{
		get{
			return this._networkManager.maxDelay;
		}
		set{
			this._networkManager.maxDelay = value;
		}
	}
	#endregion

	#region protected instance fields
	protected UnetHighLevelNetworkManager _networkManager;
	#endregion

	#region public override methods
	public override NetworkState GetConnectionState(){
		if (NetworkServer.active){
			return NetworkState.Server;
		}else if (this._networkManager.IsClientConnected()){
			return NetworkState.Client;
		}else{
			return NetworkState.Disconnected;
		}
	}

	public override int GetLastPing(){
		if (this.IsClient()){
			return this._networkManager.client.GetRTT();
		/*}else if (this.IsServer()){
			for (int i = 0; i < NetworkServer.connections.Count; ++i) {
				NetworkConnection c = NetworkServer.connections[i];
				if (c != null){
					byte error;
					return NetworkTransport.GetCurrentRtt(c.hostId, c.connectionId, out error);
				}
			}*/
		}
		return 0;
	}

	public override void Initialize(string uuid){
		if (uuid != null){
			this._uuid = uuid;

			if (this._networkManager == null){
				this._networkManager = transform.parent.gameObject.GetComponent<UnetHighLevelNetworkManager>();

				if (this._networkManager == null){
					this._networkManager = transform.parent.gameObject.AddComponent<UnetHighLevelNetworkManager>();
				}

				this._networkManager.OnMessageReceived += this.RaiseOnMessageReceived;

				this._networkManager.OnDisconnection += this.RaiseOnDisconnection;
				this._networkManager.OnJoined += this.RaiseOnJoined;
				this._networkManager.OnJoinError += this.RaiseOnJoinError;
				this._networkManager.OnMatchesDiscovered += this.RaiseOnMatchesDiscovered;
				this._networkManager.OnMatchDiscoveryError += this.RaiseOnMatchDiscoveryError;

				this._networkManager.OnMatchCreated += this.RaiseOnMatchCreated;
				this._networkManager.OnMatchCreationError += this.RaiseOnMatchCreationError;
				this._networkManager.OnMatchDestroyed += this.RaiseOnMatchDestroyed;
				this._networkManager.OnPlayerConnectedToMatch += this.RaiseOnPlayerConnectedToMatch;;
				this._networkManager.OnPlayerDisconnectedFromMatch += this.RaiseOnPlayerDisconnectedFromMatch;

				this._networkManager.autoCreatePlayer = false;
			}

			this._networkManager.Initialize();
			this.RaiseOnInitializationSuccessful();
		}else{
			this.RaiseOnInitializationError();
		}
	}
	#endregion

	#region protected override methods
	protected override bool SendNetworkMessage(byte[] bytes){
		return this._networkManager.SendNetworkMessage(bytes);
	}
	#endregion
}
