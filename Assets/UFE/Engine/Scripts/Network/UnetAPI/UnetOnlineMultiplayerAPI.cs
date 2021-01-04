using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using System;

public class UnetOnlineMultiplayerAPI : UnetHighLevelMultiplayerAPI{
	#region public override methods
	public override void DisconnectFromMatch(){
		this._networkManager.DropMatch((NetworkID)this._networkManager.NetworkId,this._networkManager.NodeId);
	}

	public override void JoinMatch(MatchInformation match, string password = null){
		this._networkManager.JoinMatch(match.unityNetworkId, password);
	}

	public override void JoinRandomMatch(){
		throw new NotImplementedException();
	}

	public override void StartSearchingMatches(int startPage = 0, int pageSize = 20, string filter = null){
        Debug.Log("Unet StartSearchingMatches");
        this._networkManager.StartSearchingMatches(startPage, pageSize, filter);
	}

	public override void StopSearchingMatches(){
		this._networkManager.StopSearchingMatches();
	}

	public override void CreateMatch(MatchCreationRequest request){
		this._networkManager.CreateMatch(request.matchName, request.publicMatch, request.password);
	}

	public override void DestroyMatch(){
		if (this._networkManager.NetworkId != (ulong)NetworkID.Invalid){
			this._networkManager.DestroyMatch((NetworkID)this._networkManager.NetworkId);
		}
	}
	#endregion
}
