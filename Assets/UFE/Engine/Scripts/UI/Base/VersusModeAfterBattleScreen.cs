using UnityEngine;
using System;
using System.Reflection;

public class VersusModeAfterBattleScreen : UFEScreen {
	#region protected enum definitions
	protected enum Option{
		RepeatBattle = 0,
		CharacterSelectionScreen = 1,
		StageSelectionScreen = 2,
		MainMenu = 3,
	}
	#endregion

	#region public instance methods
	public virtual void GoToCharacterSelectionScreen(){
		this.TrySelectOption((int)VersusModeAfterBattleScreen.Option.CharacterSelectionScreen, UFE.GetLocalPlayer());
	}

	public virtual void GoToMainMenu(){
		this.TrySelectOption((int)VersusModeAfterBattleScreen.Option.MainMenu, UFE.GetLocalPlayer());
	}

	public virtual void GoToStageSelectionScreen(){
		this.TrySelectOption((int)VersusModeAfterBattleScreen.Option.StageSelectionScreen, UFE.GetLocalPlayer());
	}

	public virtual void RepeatBattle(){
		this.TrySelectOption((int)VersusModeAfterBattleScreen.Option.RepeatBattle, UFE.GetLocalPlayer());
	}
	#endregion

	#region public override methods
	public override void SelectOption(int option, int player){
		VersusModeAfterBattleScreen.Option selectedOption = (VersusModeAfterBattleScreen.Option)option;
		if (selectedOption == VersusModeAfterBattleScreen.Option.CharacterSelectionScreen){
			UFE.StartCharacterSelectionScreen();
		}else if (selectedOption == VersusModeAfterBattleScreen.Option.MainMenu){
			UFE.StartMainMenuScreen();
		}else if (selectedOption == VersusModeAfterBattleScreen.Option.StageSelectionScreen){
			UFE.StartStageSelectionScreen();
		}else if (selectedOption == VersusModeAfterBattleScreen.Option.RepeatBattle){
			UFE.StartLoadingBattleScreen();
		}
	}
	#endregion

	#region protected virtual methods
	protected virtual void TrySelectOption(int option, int player){
		// Check if he was playing online or not...
		if (!UFE.isConnected){
			// If it's a local game, go to the selected screen immediately...
			this.SelectOption(option, player);
		}else{
			// If it's an online game, we need to inform the other client about the screen we want to go...
			int localPlayer = UFE.GetLocalPlayer();
			if (localPlayer == player){
				// We don't invoke the SelectOption() method immediately because we are using the frame-delay 
				// algorithm to keep players synchronized, so we can't invoke the SelectOption() method
				// until the other player has received the message with our choice.
				UFE.fluxCapacitor.RequestOptionSelection(player, (sbyte)option);
			}
		}
	}
	#endregion
}
