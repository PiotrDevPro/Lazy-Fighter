using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

public class UnetHighLevelNetworkDiscovery : NetworkDiscovery{
	#region public event definitions
	public event MultiplayerAPI.OnMatchesDiscoveredDelegate OnMatchesDiscovered;
	public event MultiplayerAPI.OnMatchDiscoveryErrorDelegate OnMatchesDiscoveryError;
	#endregion

	#region public override methods
	public override void OnReceivedBroadcast(string address, string port){
		this.RaiseOnLanGamesDiscovered(new ReadOnlyCollection<MultiplayerAPI.MatchInformation>(
			new MultiplayerAPI.MatchInformation[]{new MultiplayerAPI.MatchInformation(address)}
		));
	}
	#endregion

	#region protected instance methods
	protected virtual void RaiseOnLanGamesDiscovered(ReadOnlyCollection<MultiplayerAPI.MatchInformation> matches){
		if (this.OnMatchesDiscovered != null){
			this.OnMatchesDiscovered(matches);
		}
	}

	protected virtual void RaiseOnLanGamesDiscoveryError(){
		if (this.OnMatchesDiscoveryError != null){
			this.OnMatchesDiscoveryError();
		}
	}
	#endregion

	#region public static methods
	public static string Translate (byte[] bytes){
		char[] array = new char[bytes.Length / 2];
		Buffer.BlockCopy (bytes, 0, array, 0, bytes.Length);
		return new string (array);
	}

	public static byte[] Translate (string str)
	{
		byte[] array = new byte[str.Length * 2];
		Buffer.BlockCopy (str.ToCharArray (), 0, array, 0, array.Length);
		return array;
	}
	#endregion
}
