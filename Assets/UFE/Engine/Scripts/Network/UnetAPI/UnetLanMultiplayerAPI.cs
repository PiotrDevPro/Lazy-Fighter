using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using System;

public class UnetLanMultiplayerAPI : UnetHighLevelMultiplayerAPI{
	#region public override methods
	public override void DisconnectFromMatch(){
		this._networkManager.StopClient();
	}

	public override void JoinMatch(MatchInformation match, string password = null){
		this._networkManager.networkAddress = match.connections[0].publicAddress;
		this._networkManager.networkPort = match.connections[0].port;
		this._networkManager.StartClient();
	}

	public override void JoinRandomMatch(){
		throw new NotImplementedException();
	}

	public override void StartSearchingMatches(int startPage = 0, int pageSize = 20, string filter = null)
    {
        Debug.Log("Lan StartSearchingMatches");
        this._networkManager.StartSearchingLanGames();
	}

	public override void StopSearchingMatches(){
		this._networkManager.StopSearchingLanGames();
	}

	public override void CreateMatch(MatchCreationRequest request){
		if (this._networkManager.IsServerStarted()){
			this.RaiseOnMatchCreationError();
		}else{
			this._networkManager.maxConnections = request.maxPlayers;
			this._networkManager.networkPort = request.port;
			this._networkManager.publicLanGame = request.publicMatch;

			bool ok = this._networkManager.StartServer();
			if (!ok){
				this.RaiseOnMatchCreationError();
			}
		}
	}

	public override void DestroyMatch(){
		if (this._networkManager.IsServerStarted()){
			this._networkManager.StopServer();
		}
	}
	#endregion
}
