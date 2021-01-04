using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.UI;

public class JoinGameScreen : UFEScreen {
	#region protected instance fields
	protected bool _connecting = false;
	protected IList<MultiplayerAPI.MatchInformation> _foundServers = new List<MultiplayerAPI.MatchInformation>();
	#endregion

	#region public override methods
	public override void OnShow(){
		base.OnShow ();
		this.StopSearchingLanGames();
	}
	#endregion

	#region public instance methods
	public virtual void GoToNetworkGameScreen(){
		this.StopSearchingLanGames();
		UFE.StartNetworkGameScreen();
	}

	public virtual void GoToConnectionLostScreen(){
		this.StopSearchingLanGames();
		UFE.StartConnectionLostScreen();
	}

	public virtual void RefreshGameList() {

	}

	public virtual void JoinGame(Text textUI) {
		this.StopSearchingLanGames();
		UFE.JoinGame(new MultiplayerAPI.MatchInformation(textUI.text, UFE.config.networkOptions.port));
	}

	public virtual void JoinFirstLanGame(){
		UFE.multiplayerAPI.OnMatchesDiscovered -= this.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError -= this.OnMatchDiscoveryError;

		UFE.multiplayerAPI.OnMatchesDiscovered += this.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError += this.OnMatchDiscoveryError;

		UFE.multiplayerAPI.StartSearchingMatches();
	}

	public virtual void StopSearchingLanGames(){
		UFE.multiplayerAPI.OnMatchesDiscovered -= this.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError -= this.OnMatchDiscoveryError;

		UFE.multiplayerAPI.StopSearchingMatches();
		this._foundServers.Clear();
		this._connecting = false;
	}
	#endregion

	#region protected instance methods
	protected virtual void OnJoined(MultiplayerAPI.JoinedMatchInformation match){
		UFE.multiplayerAPI.OnJoined -= this.OnJoined;
		UFE.multiplayerAPI.OnJoinError -= this.OnJoinError;
	}

	protected virtual void OnJoinError(){
		UFE.multiplayerAPI.OnJoined -= this.OnJoined;
		UFE.multiplayerAPI.OnJoinError -= this.OnJoinError;

		// Try to connect to other found matches
		this._connecting = false;
		this.TryConnect();
	}

	protected virtual void OnMatchesDiscovered(ReadOnlyCollection<MultiplayerAPI.MatchInformation> matches){
		this.StopSearchingLanGames();

		if (matches != null && matches.Count > 0){
			for (int i = 0; i < matches.Count; ++i){
				if (matches[i] != null){
					this._foundServers.Add(matches[i]);
				}
			}

			this.TryConnect();
		}else{
			this.GoToConnectionLostScreen();
		}
	}

	protected virtual void OnMatchDiscoveryError(){
		this.StopSearchingLanGames();
		this.GoToConnectionLostScreen();
	}

	protected virtual void OnLanGameNotFound(){
		this.GoToConnectionLostScreen();
	}

	protected virtual void TryConnect(){
		// First, we check that we aren't already connected to a client or a server...
		if (!UFE.multiplayerAPI.IsConnected() && !this._connecting){
			MultiplayerAPI.MatchInformation match = null;

			// After that, check if we have found one match with at least one player which isn't already full...
			while (match == null && this._foundServers.Count > 0){
				match = this._foundServers[0];
				this._foundServers.RemoveAt(0);
			}


			if (match != null){
				// In that case, try connecting to that match
				this._connecting = true;

				UFE.multiplayerAPI.OnJoined += this.OnJoined;
				UFE.multiplayerAPI.OnJoinError += this.OnJoinError;
				UFE.JoinGame(match);
			}else{
				// Otherwise, return a net a new match
				this.OnLanGameNotFound();
			}
		}
	}
	#endregion
}
