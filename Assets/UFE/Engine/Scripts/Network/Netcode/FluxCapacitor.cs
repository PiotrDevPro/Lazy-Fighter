using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UFENetcode;
using FPLibrary;
using UFE3D;

public class FluxCapacitor {
	#region public class properties
	public static string PlayerIndexOutOfRangeMessage = 
	"The Player Index is {0}, but it should be in the [{1}, {2}] range.";

	public static string NetworkMessageFromUnexpectedPlayerMessage = 
	"The Network Message was sent by {0}, but it was expected to be sent by {1}.";
	#endregion

	#region public instance properties
	public bool AllowRollbacks{
		get{
            //---------------------------------------------------------------------------------------------------------
            // Take into account that we will disable the remote player input prediction
            // in menu screens because we want this algorithm to behave as the frame-delay
            // algorithm in those screens (they aren't ready for dealing with rollbacks).
            //---------------------------------------------------------------------------------------------------------
            // FIXME: The current code will probably fail at "pause screen" and "after battle screens".
            //
            // Because when we try to disable rollbacks again, it's possible we already have some predicted inputs 
            // from the other player. A possible hack would be reseting the UFE.currentNetworkFrame and the input 
            // buffer when we detect one of these events, but we aren't completely sure about the undesirable 
            // side-effects which can appear.
            //---------------------------------------------------------------------------------------------------------
#if UFE_LITE || UFE_BASIC || UFE_STANDARD
            return false;
#else
            return UFE.config.networkOptions.allowRollBacks && UFE.gameRunning && this.IsNetworkGame();
#endif
        }
	}

	public FluxGameHistory History{
		get{
			return this._history;
		}
	}

	public int NetworkFrameDelay{
		get{
			int frameDelay = 0;

			if (UFE.multiplayerAPI.Connections > 0){
				if (UFE.config.networkOptions.frameDelayType == global::NetworkFrameDelay.Auto){
					frameDelay = this.GetOptimalFrameDelay();

					if (this.AllowRollbacks){
						//---------------------------------------------------------------------------------------------
						// TODO: if one of the players get consistently more rollbacks than the other player, 
						// then we should increase the frame delay for that player in 1 or 2 frames because
						// using a greater frame-delay means having more input lag, but also less rollbacks.
						//---------------------------------------------------------------------------------------------
						// Another solution would be pausing the client which is receiving more rollbacks 
						// for a single frame in order to give the other client some time to catch up.
						//---------------------------------------------------------------------------------------------
					}
				}else{
					frameDelay = UFE.config.networkOptions.defaultFrameDelay;
				}
			}else if (UFE.config.networkOptions.applyFrameDelayOffline){
                if (UFE.config.networkOptions.frameDelayType == global::NetworkFrameDelay.Auto) {
					frameDelay = UFE.config.networkOptions.minFrameDelay;
				}else{
					frameDelay = UFE.config.networkOptions.defaultFrameDelay;
				}
			}

			return frameDelay;
		}
	}

	public FluxPlayerManager PlayerManager{
		get{
			return this._playerManager;
		}
	}
#endregion

#region public instance fields
	public FluxStates? savedState = null;
#endregion

#region protected instance fields
    protected Text debugger;
	protected StringBuilder _debugInfo = new StringBuilder();
	protected FluxGameHistory _history = new FluxGameHistory();
	protected long _maxCurrentFrameValue = long.MinValue;
	protected FluxPlayerManager _playerManager = new FluxPlayerManager();
	protected List<byte[]> _receivedNetworkMessages = new List<byte[]>();
	protected sbyte?[] _selectedOptions = new sbyte?[2];

	protected List<FluxSimpleState> _localSynchronizationStates = new List<FluxSimpleState>();
	protected List<FluxSimpleState> _remoteSynchronizationStates = new List<FluxSimpleState>();

	protected int _desynchronizations = 0;
	protected long _remotePlayerNextExpectedFrame;
	protected bool _rollbackBalancingApplied;
	protected long _timeToNetworkMessage;
#endregion

#region public instance constructors
	public FluxCapacitor() : this(0){}
	public FluxCapacitor(long currentFrame) : this(currentFrame, -1){}
	public FluxCapacitor(long currentFrame, int maxHistoryLength){
		this.Initialize(currentFrame, maxHistoryLength);
	}
#endregion


#region public instance methods
	public void DoFixedUpdate(){
		bool allowRollbacks = this.AllowRollbacks;
		long currentFrame = UFE.currentFrame;
		long frameDelay = this.NetworkFrameDelay;
		long remotePlayerLastFrameReceived = this._remotePlayerNextExpectedFrame - 1;
		long remotePlayerExpectedFrame = remotePlayerLastFrameReceived + frameDelay;


		//-------------------------------------------------------------------------------------------------------------
		// Check if it's a network game...
		//-------------------------------------------------------------------------------------------------------------
		bool isNetworkGame = this.IsNetworkGame();
		if (isNetworkGame){
			//---------------------------------------------------------------------------------------------------------
			// In that case, process the received the network messages...
			//---------------------------------------------------------------------------------------------------------
			this.ProcessReceivedNetworkMessages();
			remotePlayerLastFrameReceived = this._remotePlayerNextExpectedFrame - 1;
			remotePlayerExpectedFrame = remotePlayerLastFrameReceived + frameDelay;

			//---------------------------------------------------------------------------------------------------------
			// If rollback balancing is enabled and it hasn't been applied in the current frame,
			// check if we need to apply the rollback balancing on this client.
			//
			// In order to avoid visual glitches, we want apply the rollback balancing at most one frame every second,
			// but we can become more aggressive if the desynchronization between clients is very big. If one client 
			// simulation is far ahead of the other client simulation (1 second or more), we pause that simulation
			// until the other client has time to catch up.
			//---------------------------------------------------------------------------------------------------------
			long rollbackBalancingFrameDelay = System.Math.Max(frameDelay, (long)this.GetOptimalFrameDelay());
			if(
				currentFrame > remotePlayerExpectedFrame + (long)(UFE.config.fps)
				||
				(
					!this._rollbackBalancingApplied 
					&&
					(
						UFE.config.networkOptions.rollbackBalancing != NetworkRollbackBalancing.Disabled &&
						UFE.currentFrame % UFE.config.fps == 0 &&
						currentFrame > remotePlayerExpectedFrame + rollbackBalancingFrameDelay / 2
						||
						UFE.config.networkOptions.rollbackBalancing == NetworkRollbackBalancing.Aggressive &&
						(
							UFE.currentFrame % (UFE.config.fps / 4) == 0 &&
							currentFrame > remotePlayerExpectedFrame + rollbackBalancingFrameDelay * 2
							||
							UFE.currentFrame % (UFE.config.fps / 2) == 0 &&
							currentFrame > remotePlayerExpectedFrame + rollbackBalancingFrameDelay
							||
							UFE.currentFrame % UFE.config.fps == 0 &&
							currentFrame > remotePlayerExpectedFrame + rollbackBalancingFrameDelay / 2
						)
					)
				)
			){
				//-----------------------------------------------------------------------------------------------------
				// If the game simulation on this client is far ahead in front of the simulation on the other client,
				// we will pause this client for a single frame in order to give the other simulation some time to 
				// catch up.
				//-----------------------------------------------------------------------------------------------------
				if (UFE.config.debugOptions.desyncErrorLog){
					this._debugInfo.Append("\n\nGame paused for one frame (Rollback Balancing Algorithm)\n\n");
				}

				this._rollbackBalancingApplied = true;
				this.CheckOutgoingNetworkMessages(currentFrame);
				return;
			}else{
				this.ReadInputs(frameDelay, allowRollbacks);
				this.CheckOutgoingNetworkMessages(currentFrame);
			}
		}else{
			this.ReadInputs(frameDelay, allowRollbacks);
		}

		long firstFrameWhereRollbackIsRequired = this.PlayerManager.GetFirstFrameWhereRollbackIsRequired();
#if UFE_LITE || UFE_BASIC || UFE_STANDARD
        bool rollback = false;
#else
        bool rollback = firstFrameWhereRollbackIsRequired >= 0 && firstFrameWhereRollbackIsRequired < UFE.currentFrame;
#endif
        long lastFrameWithConfirmedInput = this.PlayerManager.GetLastFrameWithConfirmedInput();
		long lastFrameWithSynchronizationMessage = Math.Min(this.GetFirstLocalSynchronizationFrame(),this.GetFirstRemoteSynchronizationFrame());
		long lastFrameWithSynchronizedInput = firstFrameWhereRollbackIsRequired >= 0 ? firstFrameWhereRollbackIsRequired - 1L : lastFrameWithConfirmedInput;

		//-------------------------------------------------------------------------------------------------------------
		// Remove the information which is no longer necessary:
		//-------------------------------------------------------------------------------------------------------------
		// We need to leave the confirmed information for a few extra frames
		// because we may need them later during a rollback.
		//-------------------------------------------------------------------------------------------------------------
		while(
			this.PlayerManager.player1.inputBuffer.FirstFrame < currentFrame - 1L 
			&&
			this.PlayerManager.player1.inputBuffer.FirstFrame < lastFrameWithSynchronizedInput - 1L 
			&&
			this.PlayerManager.player1.inputBuffer.FirstFrame < this._remotePlayerNextExpectedFrame
			&&
			(
				!UFE.config.networkOptions.desynchronizationRecovery || true
				||
				this.PlayerManager.player1.inputBuffer.FirstFrame < lastFrameWithSynchronizationMessage - 1L
				||
				this.PlayerManager.player1.inputBuffer.MaxBufferSize > 0 && 
				this.PlayerManager.player1.inputBuffer.Count > this.PlayerManager.player1.inputBuffer.MaxBufferSize * 3/4
			)
		){
			this.PlayerManager.player1.inputBuffer.RemoveNextInput();
		}

		while(
			this.PlayerManager.player2.inputBuffer.FirstFrame < currentFrame - 1L 
			&&
			this.PlayerManager.player2.inputBuffer.FirstFrame < lastFrameWithSynchronizedInput - 1L 
			&&
			this.PlayerManager.player2.inputBuffer.FirstFrame < this._remotePlayerNextExpectedFrame
			&&
			(
				!UFE.config.networkOptions.desynchronizationRecovery || true
				||
				this.PlayerManager.player2.inputBuffer.FirstFrame < lastFrameWithSynchronizationMessage - 1L
				||
				this.PlayerManager.player2.inputBuffer.MaxBufferSize > 0 && 
				this.PlayerManager.player2.inputBuffer.Count > this.PlayerManager.player2.inputBuffer.MaxBufferSize * 3/4
			)
		){
			this.PlayerManager.player2.inputBuffer.RemoveNextInput();
		}

		while(
			this._history.FirstStoredFrame < currentFrame - 1L 
			&&
			this._history.FirstStoredFrame < lastFrameWithSynchronizedInput - 1L 
			&&
			this._history.FirstStoredFrame < this._remotePlayerNextExpectedFrame
			&&
			(
				!UFE.config.networkOptions.desynchronizationRecovery || true
				||
				this._history.FirstStoredFrame < lastFrameWithSynchronizationMessage - 1L 
				||
				this._history.MaxBufferSize > 0 && 
				this._history.Count > this._history.MaxBufferSize * 3/4
			)
		){
			this._history.RemoveNextFrame();
		}
	
		//-------------------------------------------------------------------------------------------------------------
		// Check if it's a network game and we need to apply a rollback...
		//-------------------------------------------------------------------------------------------------------------
		if (isNetworkGame){
			// Check if we need to rollback to a previous frame...
			if (rollback){
				if (allowRollbacks){
                    // In that case, execute the rollback...
                    this.Rollback(currentFrame, firstFrameWhereRollbackIsRequired, lastFrameWithConfirmedInput);
                }
                else{
					// If a desynchronization has happened and we don't allow rollbacks, 
					// show a log message and go to the "Connection Lost" screen.
					if (UFE.config.debugOptions.desyncErrorLog){
						this._debugInfo
							.Append("\n\nCurrent Frame: ").Append(UFE.currentFrame)
							.Append(" | Rollback Frame: ").Append(firstFrameWhereRollbackIsRequired).AppendLine();
					}

					this.ForceDisconnection("Game Desynchronized because a rollback was required, but not allowed.");
				}
			}

            if (UFE.config.debugOptions.debugMode && UFE.config.debugOptions.networkToggle) {
                debugger.enabled = true;
                debugger.text = "";
                //if (UFE.config.debugOptions.ping) debugger.text += "Ping:" + UFE.multiplayerAPI.GetLastPing() + " ms\n";
                if (UFE.config.debugOptions.frameDelay) debugger.text += "Frame Delay:" + frameDelay + "\n";
                if (UFE.config.debugOptions.currentLocalFrame) debugger.text += "Current Frame:" + UFE.currentFrame;
            } else {
                debugger.enabled = false;
            }
		}else {
			this._remotePlayerNextExpectedFrame = lastFrameWithSynchronizedInput + 1L;
		}


		//-------------------------------------------------------------------------------------------------------------
		// We need to update these values again because they may have changed during the rollback and fast-foward
		//-------------------------------------------------------------------------------------------------------------
		firstFrameWhereRollbackIsRequired = this.PlayerManager.GetFirstFrameWhereRollbackIsRequired();
		lastFrameWithConfirmedInput = this.PlayerManager.GetLastFrameWithConfirmedInput();
		lastFrameWithSynchronizedInput = firstFrameWhereRollbackIsRequired >= 0 ? firstFrameWhereRollbackIsRequired - 1L : lastFrameWithConfirmedInput;
		currentFrame = UFE.currentFrame;

		//-------------------------------------------------------------------------------------------------------------
		// If the game isn't paused and all players have entered their input for the current frame...
		//-------------------------------------------------------------------------------------------------------------
		bool isInputReady;
		if (this.PlayerManager.TryCheckIfInputIsReady(UFE.currentFrame, out isInputReady) && isInputReady){
			this.ApplyInputs(currentFrame, lastFrameWithSynchronizedInput);
			this._rollbackBalancingApplied = false;
        }
	}

	public virtual void Initialize(){
		this.Initialize(0);
	}

	public virtual void Initialize(long currentFrame){
		this.Initialize(currentFrame, -1);
	}

	public virtual void Initialize(long currentFrame, int maxHistoryLength){
		this._debugInfo.Length = 0;
		this._debugInfo.Append("PLAYER ").Append(UFE.GetLocalPlayer()).Append(" - SYNCHRONIZATION LOG\n\n\n");

        if (maxHistoryLength == -1) maxHistoryLength = UFE.config.networkOptions.maxBufferSize;
		this.savedState = null;
		this._maxCurrentFrameValue = long.MinValue;
		this._localSynchronizationStates.Clear();
		this._remoteSynchronizationStates.Clear();
		this._history.Initialize(currentFrame, maxHistoryLength);
		this._remotePlayerNextExpectedFrame = currentFrame;
		this._rollbackBalancingApplied = false;
		this._timeToNetworkMessage = 0L;
		this._desynchronizations = 0;

        int maxBufferSize = UFE.config.networkOptions.maxBufferSize;
//		if (!UFE.config.networkOptions.sendNetworkMessagesEveryFrame){
//			maxBufferSize = Mathf.Max(maxBufferSize, UFE.config.networkOptions.defaultFrameDelay * 4);
//		}

        this.PlayerManager.Initialize(0, maxBufferSize);
		//this.PlayerManager.Initialize(0, -1);

		UFE.currentFrame = currentFrame;
		UFE.OnRoundEnds -= this.OnRoundEnds;
		UFE.OnRoundEnds += this.OnRoundEnds;
		UFE.OnRoundBegins -= this.OnRoundBegin;
		UFE.OnRoundBegins += this.OnRoundBegin;
		UFE.multiplayerAPI.OnMessageReceived -= this.OnMessageReceived;
		UFE.multiplayerAPI.OnMessageReceived += this.OnMessageReceived;

        // DEBUGGER
        debugger = UFE.DebuggerText("Debugger", "", new Vector2(-Screen.width + 50, Screen.height - 180), TextAnchor.UpperLeft);
	}

	public virtual int GetOptimalFrameDelay(){
		return this.GetOptimalFrameDelay(UFE.multiplayerAPI.GetLastPing());
	}

	public virtual int GetOptimalFrameDelay(int ping){
		//-------------------------------------------------------------------------------------------------------------
		// Measure the time that a message needs to arrive at the other client and  calculate the duration
		// of each frame in seconds, so we can calculate the number of frames that will pass before the
		// network message arrives at the other client: that value will be the frame-delay.
		//-------------------------------------------------------------------------------------------------------------
		Fix64 latency = 0.001 * 0.5 * (Fix64)(ping);
		Fix64 frameDuration = (Fix64)1 / (Fix64)(UFE.config.fps);

		//-------------------------------------------------------------------------------------------------------------
		// Add one additional frame to the frame-delay, to compensate that messages could not being sent
		// until the next frame.
		//-------------------------------------------------------------------------------------------------------------
		int frameDelay = (int)FPMath.Ceiling(latency / frameDuration) + 1;
		return Mathf.Clamp(frameDelay,UFE.config.networkOptions.minFrameDelay,UFE.config.networkOptions.maxFrameDelay);
	}

	public virtual void RequestOptionSelection(int player, sbyte option){
		if (player == 1 || player == 2){
			this._selectedOptions[player-1] = option;
		}
	}


	public virtual void StartReplay(FluxGameReplay replay){
		if (replay != null && replay.Player1InputBuffer != null && replay.Player2InputBuffer != null){
            FluxStateTracker.LoadGameState(replay.InitialState);
			this.PlayerManager.GetPlayer(1)._inputBuffer = replay.Player1InputBuffer;
			this.PlayerManager.GetPlayer(2)._inputBuffer = replay.Player2InputBuffer;
		}
	}
#endregion

#region protected instance mehtods
	protected virtual void ApplyInputs(long currentFrame, long lastSynchronizedFrame){
		bool synchronized = currentFrame <= lastSynchronizedFrame;

		//-------------------------------------------------------------------------------------------------------------
		// Retrieve the player 1 input in the previous frame
		//-------------------------------------------------------------------------------------------------------------
		UFEController player1Controller = this.PlayerManager.player1.inputController;

		FrameInput? player1PreviousFrameInput;
		bool foundPlayer1PreviousFrameInput = 
			this.PlayerManager.TryGetInput(1, currentFrame - 1, out player1PreviousFrameInput) &&
			player1PreviousFrameInput != null;

		if (!foundPlayer1PreviousFrameInput) player1PreviousFrameInput = new FrameInput(FrameInput.NullSelectedOption);

		Tuple<Dictionary<InputReferences, InputEvents>, sbyte?> player1PreviousTuple = 
			player1Controller.inputReferences.GetInputEvents(player1PreviousFrameInput.Value);

		IDictionary<InputReferences, InputEvents> player1PreviousInputs = player1PreviousTuple.Item1;
		sbyte? player1PreviousSelectedOption = player1PreviousTuple.Item2;

		//-------------------------------------------------------------------------------------------------------------
		// Retrieve the player 1 input in the current frame
		//-------------------------------------------------------------------------------------------------------------
		FrameInput? player1CurrentFrameInput;
		bool foundPlayer1CurrentFrameInput = 
			this.PlayerManager.TryGetInput(1, currentFrame, out player1CurrentFrameInput) &&
			player1CurrentFrameInput != null;

		if (!foundPlayer1CurrentFrameInput) player1CurrentFrameInput = new FrameInput(FrameInput.NullSelectedOption);

		Tuple<Dictionary<InputReferences, InputEvents>, sbyte?> player1CurrentTuple = 
			player1Controller.inputReferences.GetInputEvents(player1CurrentFrameInput.Value);

		IDictionary<InputReferences, InputEvents> player1CurrentInputs = player1CurrentTuple.Item1;
		sbyte? player1CurrentSelectedOption = player1CurrentTuple.Item2;

		int? player1SelectedOptions = null;
		if (player1CurrentSelectedOption != null && player1CurrentSelectedOption != player1PreviousSelectedOption){
			player1SelectedOptions = player1CurrentSelectedOption;
		}

		//-------------------------------------------------------------------------------------------------------------
		// Retrieve the player 2 input in the previous frame
		//-------------------------------------------------------------------------------------------------------------
		UFEController player2Controller = this.PlayerManager.player2.inputController;

		FrameInput? player2PreviousFrameInput;
		bool foundPlayer2PreviousFrameInput = 
			this.PlayerManager.TryGetInput(2, currentFrame - 1, out player2PreviousFrameInput) && 
			player2PreviousFrameInput != null ;
		
		if (!foundPlayer2PreviousFrameInput) player2PreviousFrameInput = new FrameInput(FrameInput.NullSelectedOption);

		Tuple<Dictionary<InputReferences, InputEvents>, sbyte?> player2PreviousTuple = 
			player2Controller.inputReferences.GetInputEvents(player2PreviousFrameInput.Value);

		IDictionary<InputReferences, InputEvents> player2PreviousInputs = player2PreviousTuple.Item1;
		sbyte? player2PreviousSelectedOption = player2PreviousTuple.Item2;


		//-------------------------------------------------------------------------------------------------------------
		// Retrieve the player 2 input in the current frame
		//-------------------------------------------------------------------------------------------------------------
		FrameInput? player2CurrentFrameInput;
		bool foundPlayer2CurrentFrameInput = 
			this.PlayerManager.TryGetInput(2, currentFrame, out player2CurrentFrameInput) &&
			player2CurrentFrameInput != null;

		if (!foundPlayer2CurrentFrameInput) player2CurrentFrameInput = new FrameInput(FrameInput.NullSelectedOption);

		Tuple<Dictionary<InputReferences, InputEvents>, sbyte?> player2CurrentTuple = 
			player2Controller.inputReferences.GetInputEvents(player2CurrentFrameInput.Value);

		IDictionary<InputReferences, InputEvents> player2CurrentInputs = player2CurrentTuple.Item1;
		sbyte? player2CurrentSelectedOption = player2CurrentTuple.Item2;

		int? player2SelectedOptions = null;
		if (player2CurrentSelectedOption != null && player2CurrentSelectedOption != player2PreviousSelectedOption){
			player2SelectedOptions = player2CurrentSelectedOption;
		}

		//-------------------------------------------------------------------------------------------------------------
		// Set the Random Seed
		//-------------------------------------------------------------------------------------------------------------
        UnityEngine.Random.InitState((int)currentFrame);

		//-------------------------------------------------------------------------------------------------------------
		// If the inputs are confirmed, send a synchronization message with the player positions at the current frame,
		// so the other client can check if the game remains synchronized across clients.
		//-------------------------------------------------------------------------------------------------------------
		FluxStates currentState = FluxStateTracker.SaveGameState(currentFrame);

        if (synchronized && this.IsNetworkGame()){
			int player = UFE.GetLocalPlayer();

			//---------------------------------------------------------------------------------------------------------
			// Check if we should send a synchronization message
			//---------------------------------------------------------------------------------------------------------
			if(
				UFE.config.networkOptions.synchronizationMessageFrequency == NetworkSynchronizationMessageFrequency.EveryFrame 
				||
				UFE.config.networkOptions.synchronizationMessageFrequency == NetworkSynchronizationMessageFrequency.EverySecond && 
				UFE.currentFrame % UFE.config.fps == 0
			){
				FluxSimpleState? receivedState = this.GetRemoteSynchronizationState(currentFrame);
				FluxSimpleState expectedState = new FluxSimpleState(currentState);

				UFE.multiplayerAPI.SendNetworkMessage(new SynchronizationMessage(player, currentFrame, expectedState));

				//-----------------------------------------------------------------------------------------------------
				// After sending the network message, check if we already have a "received state" for that frame
				//-----------------------------------------------------------------------------------------------------
				if (receivedState != null){
					//-------------------------------------------------------------------------------------------------
					// In that case, check if the current and the received value match.
					//-------------------------------------------------------------------------------------------------
					if (!this.SynchronizationCheck(expectedState, receivedState.Value, currentFrame)){
						return;
					}
				}else{
					//-------------------------------------------------------------------------------------------------
					// Otherwise, save the current value so we can try it again later.
					//-------------------------------------------------------------------------------------------------
					this._localSynchronizationStates.Add(expectedState);
				}
			}
		}

		//-------------------------------------------------------------------------------------------------------------
		// Before updating the state of the game, save the current state and the input that will be applied 
		// to reach the next frame state
		//-------------------------------------------------------------------------------------------------------------
		this._history.TrySetState(
			currentState,
			new FluxFrameInput(
				player1PreviousFrameInput.Value,
				player1CurrentFrameInput.Value,
				player2PreviousFrameInput.Value,
				player2CurrentFrameInput.Value
			)
		);

        //-------------------------------------------------------------------------------------------------------------
        // Write Debug Information
        //-------------------------------------------------------------------------------------------------------------
        if (!synchronized) {
            GenerateDebugLog(
                currentFrame,
                lastSynchronizedFrame,
                new FluxFrameInput(
                    player1PreviousFrameInput.Value,
                    player1CurrentFrameInput.Value,
                    player2PreviousFrameInput.Value,
                    player2CurrentFrameInput.Value
                ));
        }

		//-------------------------------------------------------------------------------------------------------------
		// Update the game state
		//-------------------------------------------------------------------------------------------------------------
        //if (UFE.gameRunning && !UFE.isPaused()) {
        if (!UFE.isPaused()) {
            this.UpdateTimer();
			this.UpdatePlayer(1, currentFrame, lastSynchronizedFrame, player1PreviousInputs, player1CurrentInputs);
			this.UpdatePlayer(2, currentFrame, lastSynchronizedFrame, player2PreviousInputs, player2CurrentInputs);
            this.UpdateInstantiatedObjects(currentFrame, lastSynchronizedFrame);
            if (UFE.cameraScript != null) UFE.cameraScript.DoFixedUpdate();
            if (UFE.gameRunning && !UFE.IsTimerPaused()) CheckEndRoundConditions();

            this.ExecuteSynchronizedDelayedActions();
        }

        this.ExecuteLocalDelayedActions();

		this.UpdateGUI(
			player1PreviousInputs, 
			player1CurrentInputs, 
			player1SelectedOptions,
			player2PreviousInputs, 
			player2CurrentInputs,
			player2SelectedOptions
		);

		this.PlayerManager.player1.inputController.DoFixedUpdate();
		this.PlayerManager.player2.inputController.DoFixedUpdate();

		//-------------------------------------------------------------------------------------------------------------
		// Finally, increment the frame count
		//-------------------------------------------------------------------------------------------------------------
		this._maxCurrentFrameValue = Math.Max(this._maxCurrentFrameValue, currentFrame);
		UFE.currentFrame = currentFrame + 1;
	}

    protected void CheckEndRoundConditions() {
        if (UFE.GetControlsScript(1).myInfo.currentLifePoints == 0 || UFE.GetControlsScript(2).myInfo.currentLifePoints == 0) {
            UFE.FireAlert(UFE.config.selectedLanguage.ko, null);

            if (UFE.GetControlsScript(1).myInfo.currentLifePoints == 0) UFE.PlaySound(UFE.GetControlsScript(1).myInfo.deathSound);
            if (UFE.GetControlsScript(2).myInfo.currentLifePoints == 0) UFE.PlaySound(UFE.GetControlsScript(2).myInfo.deathSound);

            UFE.PauseTimer();
            if (!UFE.config.roundOptions.allowMovementEnd) {
                UFE.config.lockMovements = true;
                UFE.config.lockInputs = true;
            }

            if (UFE.config.roundOptions.slowMotionKO) {
                UFE.timeScale = UFE.timeScale * UFE.config.roundOptions._slowMoSpeed;
                UFE.DelaySynchronizedAction(this.ReturnTimeScale, UFE.config.roundOptions._slowMoTimer);
                UFE.DelaySynchronizedAction(this.EndRound, 1 / UFE.config.roundOptions._slowMoSpeed);
            } else {
                UFE.DelaySynchronizedAction(this.EndRound, (Fix64)1);
            }
        }
    }

    public void ReturnTimeScale() {
        UFE.timeScale = UFE.config._gameSpeed;
    } 

    public void EndRound() {
        ControlsScript p1ControlScript = UFE.GetControlsScript(1);
        ControlsScript p2ControlScript = UFE.GetControlsScript(2);

        // Make sure both characters are grounded
        if (!p1ControlScript.Physics.IsGrounded() || !p2ControlScript.Physics.IsGrounded()) {
            UFE.DelaySynchronizedAction(this.EndRound, .5);
            return;
        }

        UFE.config.lockMovements = true;
        UFE.config.lockInputs = true;

        // Reset Stats
        p1ControlScript.KillCurrentMove();
        p2ControlScript.KillCurrentMove();

        p1ControlScript.ResetDrainStatus(true);
        p2ControlScript.ResetDrainStatus(true);

        // Clear All Projectiles
        foreach (ProjectileMoveScript projectileMoveScript in p1ControlScript.projectiles) {
            if (projectileMoveScript != null) projectileMoveScript.destroyMe = true;
        }
        foreach (ProjectileMoveScript projectileMoveScript in p2ControlScript.projectiles) {
            if (projectileMoveScript != null) projectileMoveScript.destroyMe = true;
        }

        // Check Winner
        if (p1ControlScript.myInfo.currentLifePoints == 0 && p2ControlScript.myInfo.currentLifePoints == 0) {
            UFE.FireAlert(UFE.config.selectedLanguage.draw, null);
            UFE.DelaySynchronizedAction(this.NewRound, UFE.config.roundOptions._newRoundDelay);
        } else {
            if (p1ControlScript.myInfo.currentLifePoints == 0) {
                SetWinner(p2ControlScript);
            } else if (p2ControlScript.myInfo.currentLifePoints == 0) {
                SetWinner(p1ControlScript);
            }
        }
    }

    protected void SetWinner(ControlsScript winner) {
        ++winner.roundsWon;
        UFE.FireRoundEnds(winner.myInfo, winner.opInfo);

        // Start New Round or End Game
        if (winner.roundsWon > Mathf.Ceil(UFE.config.roundOptions.totalRounds / 2) || winner.challengeMode != null) {
            winner.SetMoveToOutro();
            UFE.DelaySynchronizedAction(this.KillCam, UFE.config.roundOptions._endGameDelay);
            UFE.FireGameEnds(winner.myInfo, winner.opInfo);
        } else {
            UFE.DelaySynchronizedAction(this.NewRound, UFE.config.roundOptions._newRoundDelay);
        }
    }

    protected void NewRound() {
        ControlsScript p1ControlScript = UFE.GetControlsScript(1);
        ControlsScript p2ControlScript = UFE.GetControlsScript(2);

        p1ControlScript.potentialBlock = false;
        p2ControlScript.potentialBlock = false;
        if (UFE.config.roundOptions.resetPositions) {
            CameraFade.StartAlphaFade(UFE.config.gameGUI.roundFadeColor, false, (float)UFE.config.gameGUI.roundFadeDuration / 2);
            UFE.DelaySynchronizedAction(this.StartNewRound, UFE.config.gameGUI.roundFadeDuration / 2);
        } else {
            UFE.DelaySynchronizedAction(this.StartNewRound, (Fix64)2);
        }

        if (p1ControlScript.challengeMode != null) p1ControlScript.challengeMode.Run();
    }
    

	protected void StartNewRound(){
        ControlsScript p1ControlScript = UFE.GetControlsScript(1);
        ControlsScript p2ControlScript = UFE.GetControlsScript(2);

        UFE.config.currentRound ++;
		UFE.ResetTimer();

        p1ControlScript.ResetData(true);
        p2ControlScript.ResetData(false);
        if (UFE.config.roundOptions.resetPositions) {
            CameraFade.StartAlphaFade(UFE.config.gameGUI.roundFadeColor, true, (float)UFE.config.gameGUI.roundFadeDuration / 2);
            p1ControlScript.cameraScript.ResetCam();
        }

		UFE.config.lockInputs = true;
		UFE.ResetRoundCast();
		UFE.CastNewRound();

		if (UFE.config.roundOptions.allowMovementStart) {
			UFE.config.lockMovements = false;
		}else{
			UFE.config.lockMovements = true;
		}
	}

    protected void KillCam() {
        UFE.GetControlsScript(1).cameraScript.killCamMove = true;
    }

    protected virtual void GenerateDebugLog(long currentFrame, long lastSynchronizedFrame, FluxFrameInput frameInput) {
        if (UFE.config.debugOptions.desyncErrorLog) {
            FluxStates state;
            bool historyRetrieved =
                this._history.TryGetState(currentFrame, out state) &&
                state.player1.controlsScript &&
                state.player2.controlsScript;

            this._debugInfo.Append("\nDesync detected at frame ").Append(currentFrame).AppendLine();

            if (frameInput.Player1PreviousInput.Equals(null)) {
                this._debugInfo
                    .Append("\nPrevious Player 1 Input not found at frame: ").Append(currentFrame)
                    .Append("\nLast Synchronized Frame: ").Append(lastSynchronizedFrame)
                    .Append("\nFirst Input at Player 1 Input Buffer: ").Append(this.PlayerManager.player1.inputBuffer.FirstFrame)
                    .Append("\nLast Input at Player 1 Input Buffer: ").Append(this.PlayerManager.player1.inputBuffer.LastFrame)
                    .Append("\nLast Confirmed Input for Player 1: ").Append(this.PlayerManager.player1.inputBuffer.GetLastFrameWithConfirmedInput())
                    .AppendLine();
            }
            
            if (frameInput.Player2PreviousInput.Equals(null)) {
                this._debugInfo
                    .Append("\nPrevious Player 2 Input not found at frame: ").Append(currentFrame)
                    .Append("\nLast Synchronized Frame: ").Append(lastSynchronizedFrame)
                    .Append("\nFirst Input at Player 2 Input Buffer: ").Append(this.PlayerManager.player2.inputBuffer.FirstFrame)
                    .Append("\nLast Input at Player 2 Input Buffer: ").Append(this.PlayerManager.player2.inputBuffer.LastFrame)
                    .Append("\nLast Confirmed Input for Player 2: ").Append(this.PlayerManager.player2.inputBuffer.GetLastFrameWithConfirmedInput())
                    .AppendLine();
            }

            this._debugInfo
                .Append("\nPlayer1 Previous Input: ").Append(frameInput.Player1PreviousInput)
                .Append("\nPlayer1 Current Input: ").Append(frameInput.Player1CurrentInput);

            if (historyRetrieved) {
                this._debugInfo
                    .Append("\nPlayer1 Position: (")
                    .Append(state.player1.shellTransform.fpPosition.x)
                    .Append(", ")
                    .Append(state.player1.shellTransform.fpPosition.y)
                    .Append(", ")
                    .Append(state.player1.shellTransform.fpPosition.z)
                    .Append(")");

                if (state.player1.moveSet.animator.currentAnimationData.mecanimAnimationData != null) {
                    this._debugInfo
                        .Append("\nPlayer1 Animation: " + state.player1.moveSet.animator.currentAnimationData.mecanimAnimationData.clipName)
                        .Append("\nPlayer1 Animation Time: " + state.player1.moveSet.animator.currentAnimationData.mecanimAnimationData.secondsPlayed);
                }
            }

            this._debugInfo
                .Append("\nPlayer2 Previous Input: ").Append(frameInput.Player2PreviousInput)
                .Append("\nPlayer2 Current Input: ").Append(frameInput.Player2CurrentInput);

            if (historyRetrieved) {
                this._debugInfo
                    .Append("\nPlayer2 Position: (")
                    .Append(state.player2.shellTransform.fpPosition.x)
                    .Append(", ")
                    .Append(state.player2.shellTransform.fpPosition.y)
                    .Append(", ")
                    .Append(state.player2.shellTransform.fpPosition.z)
                    .Append(")")
                    .Append("\nPlayer2 Basic Move: " + state.player2.currentBasicMove)
                    .Append("\nPlayer2 Move: " + (state.player2.currentMove.move != null ? state.player2.currentMove.move.moveName : string.Empty));

                if (state.player2.moveSet.animator.currentAnimationData.mecanimAnimationData != null) {
                    this._debugInfo
                        .Append("\nPlayer2 Animation: " + state.player2.moveSet.animator.currentAnimationData.mecanimAnimationData.clipName)
                        .Append("\nPlayer2 Animation Time: " + state.player2.moveSet.animator.currentAnimationData.mecanimAnimationData.secondsPlayed);
                }
            }

            this._debugInfo.AppendLine();
        }
    }

	protected virtual void CheckOutgoingNetworkMessages(long currentFrame){
		//---------------------------------------------------------------------------------------------------------
		// Check if we need to send a network message
		//---------------------------------------------------------------------------------------------------------
		if (UFE.config.networkOptions.inputMessageFrequency == NetworkInputMessageFrequency.EveryFrame){
			//-----------------------------------------------------------------------------------------------------
			// We may want to send a network message every frame...
			//-----------------------------------------------------------------------------------------------------
			this.SendNetworkMessages();
		}else{
			//-----------------------------------------------------------------------------------------------------
			// Or we may want to send a network message every few frames...
			//-----------------------------------------------------------------------------------------------------
			if (this._timeToNetworkMessage <= 0L){
				this.SendNetworkMessages();
			}else{
				int localPlayer = UFE.GetLocalPlayer();
				if (localPlayer > 0){
					FrameInput? previousFrameInput;
					FrameInput? currentFrameInput;

					if(
						this.PlayerManager.TryGetInput(localPlayer, currentFrame - 1, out previousFrameInput) &&
						previousFrameInput != null &&
						this.PlayerManager.TryGetInput(localPlayer, currentFrame, out currentFrameInput) &&
						currentFrameInput != null &&
						!previousFrameInput.Value.Equals(currentFrameInput.Value)
					){
						//-----------------------------------------------------------------------------------------
						// Even if we want to send the network message every few frames, 
						// we send the network message immediately if the local player
						// input has changed since the previous frame.
						//
						// We do this to avoid "mega-rollbacks" which can kill the game
						// performance during the "fast-forward" phase.
						//-----------------------------------------------------------------------------------------
						this.SendNetworkMessages();
					}
				}
			}

			--this._timeToNetworkMessage;
		}
	}

    protected virtual void ExecuteLocalDelayedActions() {
        // Check if we need to execute any delayed "local action" (such as playing a sound or GUI)
        for (int i = UFE.delayedLocalActions.Count - 1; i >= 0; --i) {
            DelayedAction action = UFE.delayedLocalActions[i];
            --action.steps;

            if (action.steps <= 0) {
                action.action();
                UFE.delayedLocalActions.RemoveAt(i);
            }
        }
    }

    protected virtual void ExecuteSynchronizedDelayedActions() {
        // Check if we need to execute any delayed "synchronized action" (game actions)
        for (int i = UFE.delayedSynchronizedActions.Count - 1; i >= 0; --i) {
            DelayedAction action = UFE.delayedSynchronizedActions[i];
            --action.steps;

            if (action.steps <= 0) {
                action.action();
                UFE.delayedSynchronizedActions.RemoveAt(i);
            }
        }
    }

	protected virtual void ForceDisconnection(string disconnectionCause){
		if (UFE.config.networkOptions.disconnectOnDesynchronization){
			++this._desynchronizations;

			if (this._desynchronizations > UFE.config.networkOptions.allowedDesynchronizations){
				if (!string.IsNullOrEmpty(disconnectionCause)){
					Debug.LogError(disconnectionCause);
				}
				Debug.LogError(this._debugInfo.ToString());

				this._debugInfo.Length = 0;
				this._debugInfo.Append("PLAYER ").Append(UFE.GetLocalPlayer()).Append(" - SYNCHRONIZATION LOG\n\n\n");


				if (UFE.multiplayerAPI.IsClient()){
					UFE.multiplayerAPI.DisconnectFromMatch();
				}else if (UFE.multiplayerAPI.IsServer()){
					UFE.multiplayerAPI.DestroyMatch();
				}
			}else{
				Debug.LogWarning(disconnectionCause);
			}
		}else{
			if (!string.IsNullOrEmpty(disconnectionCause)){
				Debug.LogError(disconnectionCause);
			}
			Debug.LogError(this._debugInfo.ToString());

			this._debugInfo.Length = 0;
			this._debugInfo.Append("PLAYER ").Append(UFE.GetLocalPlayer()).Append(" - SYNCHRONIZATION LOG\n\n\n");
		}

	}

	protected virtual FluxSimpleState? GetLocalSynchronizationState(long frame){
		for (int i = 0; i < this._localSynchronizationStates.Count; ++i){
			if (this._localSynchronizationStates[i].frame == frame){
				return this._localSynchronizationStates[i];
			}
		}

		return null;
	}

	protected virtual FluxSimpleState? GetRemoteSynchronizationState(long frame){
		for (int i = 0; i < this._remoteSynchronizationStates.Count; ++i){
			if (this._remoteSynchronizationStates[i].frame == frame){
				return this._remoteSynchronizationStates[i];
			}
		}

		return null;
	}

	protected virtual long GetFirstLocalSynchronizationFrame(){
		long frame = -1L;

		for (int i = this._localSynchronizationStates.Count - 1; i >= 0; --i){
			if (frame < 0 || frame > this._localSynchronizationStates[i].frame){
				frame = this._localSynchronizationStates[i].frame;
			}
		}

		return frame;
	}

	protected virtual long GetFirstRemoteSynchronizationFrame(){
		long frame = -1L;

		for (int i = this._remoteSynchronizationStates.Count - 1; i >= 0; --i){
			if (frame < 0 || frame > this._remoteSynchronizationStates[i].frame){
				frame = this._remoteSynchronizationStates[i].frame;
			}
		}

		return frame;
	}

	protected virtual long GetLastLocalSynchronizationFrame(){
		long frame = -1L;

		for (int i = this._localSynchronizationStates.Count - 1; i >= 0; --i){
			frame = Math.Max(frame, this._localSynchronizationStates[i].frame);
		}

		return frame;
	}

	protected virtual long GetLastRemoteSynchronizationFrame(){
		long frame = -1L;

		for (int i = this._remoteSynchronizationStates.Count - 1; i >= 0; --i){
			frame = Math.Max(frame, this._remoteSynchronizationStates[i].frame);
		}

		return frame;
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Determines whether this instance is network game.
	/// </summary>
	/// <remarks>
	/// If there is at least one remote player, then it's a network player; otherwise, it's a local game.
	/// </remarks>
	/// <returns><c>true</c> if this instance is network game; otherwise, <c>false</c>.</returns>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual bool IsNetworkGame(){
		return this.PlayerManager.AreThereRemoteCharacters();
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// This method is invoked remotely to update the player inputs.
	/// </summary>
	/// <param name="serializedMessage">Serialized message.</param>
	/// <param name="msgInfo">Message info.</param>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void OnMessageReceived(byte[] bytes){
		this._receivedNetworkMessages.Add(bytes);
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// This method is invoked by the engine at the start of the round.
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void OnRoundBegin(int currentRound){
		// We set the desynchronizations count to zero at the start of each round
		this._desynchronizations = 0;
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Raises the round ends event.
	/// </summary>
	/// <param name="winner">Winner.</param>
	/// <param name="loser">Loser.</param>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void OnRoundEnds(UFE3D.CharacterInfo winner, UFE3D.CharacterInfo loser){
		if (UFE.config.debugOptions.desyncErrorLog && this._desynchronizations > 0 && this._debugInfo.Length > 0){
			Debug.LogWarning(this._debugInfo.ToString());
		}
		this._desynchronizations = 0;
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Processes the pending network messages.
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void ProcessReceivedNetworkMessages(){
		foreach (byte[] serializedMessage in this._receivedNetworkMessages){
			if (serializedMessage != null && serializedMessage.Length > 0){
				NetworkMessageType messageType = (NetworkMessageType)serializedMessage[0];
				if (messageType == NetworkMessageType.InputBuffer){
					this.ProcessInputBufferMessage(new InputBufferMessage(serializedMessage));
				}else if (messageType == NetworkMessageType.Syncronization){
					this.ProcessSynchronizationMessage(new SynchronizationMessage(serializedMessage));
				}
			}
		}
		this._receivedNetworkMessages.Clear();
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Processes the specified input network package.
	/// </summary>
	/// <param name="package">Network package.</param>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void ProcessInputBufferMessage(InputBufferMessage package){
		// Check if the player number included in the package is valid...
		int playerIndex = package.PlayerIndex;
		if (playerIndex <= 0 || playerIndex > FluxPlayerManager.NumberOfPlayers){
			throw new IndexOutOfRangeException(string.Format(
				FluxCapacitor.PlayerIndexOutOfRangeMessage, 
				playerIndex, 
				1, 
				FluxPlayerManager.NumberOfPlayers
			));
		}


		// TODO: check if the client that sent the message is the same client which controls that player...
//		FluxPlayer player = this.PlayerManager.GetPlayer(playerIndex);
//		if (player.NetworkPlayer != msgInfo.sender){
//			throw new Exception(string.Format(
//				FluxGameManager.NetworkMessageFromUnexpectedPlayerMessage,
//				msgInfo.sender,
//				player.NetworkPlayer
//			));
//		}

		long previousGetLastFrameWithConfirmedInput = this.PlayerManager.GetLastFrameWithConfirmedInput();

		this._remotePlayerNextExpectedFrame = Math.Max(
			this._remotePlayerNextExpectedFrame,
			package.Data.NextExpectedFrame
		);

		// If we want to send only the input changes, we need to remove repeated inputs from the buffer...
		if (UFE.config.networkOptions.onlySendInputChanges){
			int count = package.Data.InputBuffer.Count;

			if (count > 0){
				// First, process the inputs of the first frame in the list...
				this.ProcessInput(playerIndex, package.Data.InputBuffer[0], previousGetLastFrameWithConfirmedInput);

				// Iterate over the rest of the items of the list except the last one...
				for (int i = 1; i < package.Data.InputBuffer.Count; ++i){
					Tuple<long, FrameInput> previousInput = package.Data.InputBuffer[i - 1];
					Tuple<long, FrameInput> currentInput = package.Data.InputBuffer[i];

					if (previousInput != null && currentInput != null){
						// Repeat the previous input from the last updated frame to the frame before the new input
						for (long j = previousInput.Item1 + 1L; j < currentInput.Item1; ++j){
							this.ProcessInput(
								playerIndex, 
								new Tuple<long, FrameInput>(j, new FrameInput(previousInput.Item2)), 
								previousGetLastFrameWithConfirmedInput
							);
						}

						// Now process the new input
						this.ProcessInput(playerIndex, currentInput, previousGetLastFrameWithConfirmedInput);
					}
				}
			}
		}else{
			for (int i = 0; i < package.Data.InputBuffer.Count; ++i){
				this.ProcessInput(playerIndex, package.Data.InputBuffer[i], previousGetLastFrameWithConfirmedInput);
			}
		}
	}

	protected virtual void ProcessInput(int playerIndex, Tuple<long, FrameInput> frame, long lastFrameWithConfirmedInput){
		long currentFrame = frame.Item1;
		this.PlayerManager.TrySetConfirmedInput(playerIndex, currentFrame, frame.Item2);

        //long firstFrameWhereRollbackIsRequired = this.PlayerManager.GetFirstFrameWhereRollbackIsRequired();
        //bool rollbackRequired = firstFrameWhereRollbackIsRequired>=0 && firstFrameWhereRollbackIsRequired<currentFrame;
    }

	protected virtual void ProcessSynchronizationMessage(SynchronizationMessage msg){
		if(UFE.config.networkOptions.synchronizationMessageFrequency!=NetworkSynchronizationMessageFrequency.Disabled){
			FluxSimpleState? expectedState = this.GetLocalSynchronizationState(msg.CurrentFrame);
			FluxSimpleState receivedState = msg.Data;

			//-------------------------------------------------------------------------------------------------------------
			// When we receive a synchronization message, check if we already have an "expected state" for that frame
			//-------------------------------------------------------------------------------------------------------------
			if (expectedState != null){
				//---------------------------------------------------------------------------------------------------------
				// In that case, check if the expected and the received value match.
				//---------------------------------------------------------------------------------------------------------
				this.SynchronizationCheck(expectedState.Value, receivedState, msg.CurrentFrame);
			}else{
				//---------------------------------------------------------------------------------------------------------
				// Otherwise, save the received value so we can try it again later.
				//---------------------------------------------------------------------------------------------------------
				this._remoteSynchronizationStates.Add(receivedState);
			}
		}
	}

	protected virtual void SendNetworkMessages(){
		int localPlayer = UFE.GetLocalPlayer();

		if (localPlayer > 0){
			FluxPlayer local = this.PlayerManager.GetPlayer(localPlayer);

			// And send a message with their current "confirmed input" buffer.
			if (local != null && local.inputBuffer != null){
				IList<Tuple<long, FrameInput>> confirmedInputBuffer = 
					local.inputBuffer.GetConfirmedInputBuffer(this._remotePlayerNextExpectedFrame);

				// If we want to send only the input changes, we need to remove repeated inputs from the buffer...
				if (UFE.config.networkOptions.onlySendInputChanges && confirmedInputBuffer.Count > 1){
					IList<Tuple<long, FrameInput>> tempInputBuffer = confirmedInputBuffer;

					// So copy the first item of the list
					confirmedInputBuffer = new List<Tuple<long, FrameInput>>();
					confirmedInputBuffer.Add(tempInputBuffer[0]);

					// Iterate over the rest of the items in the list, except the last one
					for (int i = 1; i < tempInputBuffer.Count - 1; ++i){
						// If the player inputs has changed since the last frame, add the item to the list
						Tuple<long, FrameInput> currentInput = tempInputBuffer[i];
						Tuple<long, FrameInput> lastInput = confirmedInputBuffer[confirmedInputBuffer.Count - 1];

						if (lastInput != null && currentInput != null && !currentInput.Item2.Equals(lastInput.Item2)){
							confirmedInputBuffer.Add(currentInput);
						}
					}

					// Copy the last item of the list
					confirmedInputBuffer.Add(tempInputBuffer[tempInputBuffer.Count - 1]);
				}

				if (confirmedInputBuffer.Count > 0){
					//Debug.Log("Message Sent: " + confirmedInputBuffer.Count + " frames");

					InputBufferMessage msg = new InputBufferMessage(
						localPlayer, 
						local.inputBuffer.FirstFrame, 
						new InputBufferMessageContent(this.PlayerManager.GetNextExpectedFrame(), confirmedInputBuffer)
					);

					UFE.multiplayerAPI.SendNetworkMessage(msg);


					if (UFE.config.networkOptions.inputMessageFrequency == NetworkInputMessageFrequency.EveryFrame){
						this._timeToNetworkMessage = 1L;
					}else if (UFE.config.networkOptions.inputMessageFrequency == NetworkInputMessageFrequency.EveryOtherFrame){
						this._timeToNetworkMessage = 2L;
					}else{
						//this._timeToNetworkMessage = (long)(this.NetworkFrameDelay) / 2L;
						this._timeToNetworkMessage = (long)(this.NetworkFrameDelay) / 4L;
					}
				}
			}
		}
	}

	protected virtual void Rollback(long currentFrame, long rollbackFrame, long lastFrameWithConfirmedInputs){
		this.Rollback(currentFrame, rollbackFrame, lastFrameWithConfirmedInputs, null);
	}

	protected virtual void Rollback(
		long currentFrame, 
		long rollbackFrame, 
		long lastFrameWithConfirmedInputs,
		FluxSimpleState? overriddenGameState
	){
#if UFE_LITE || UFE_BASIC || UFE_STANDARD
        Debug.LogError("Rollback not installed.");
#else
        // Retrieve the first stored frame and check if we can rollback to the specified frame...
        long firstStoredFrame = Math.Max(this.PlayerManager.player1.inputBuffer.FirstFrame, this.PlayerManager.player2.inputBuffer.FirstFrame);
		if (rollbackFrame > firstStoredFrame){
			// Show the debug information to help us understand what has happened
			FluxPlayerInputBuffer p1Buffer = this.PlayerManager.player1.inputBuffer;
			FluxPlayerInputBuffer p2Buffer = this.PlayerManager.player2.inputBuffer;
			FluxPlayerInput p1Input = p1Buffer[p1Buffer.GetIndex(rollbackFrame)];
			FluxPlayerInput p2Input = p2Buffer[p2Buffer.GetIndex(rollbackFrame)];

			if (UFE.config.debugOptions.desyncErrorLog){
				this._debugInfo.AppendLine().AppendLine()
					.Append("Rollback from frame ").Append(currentFrame).Append(" to frame ").Append(rollbackFrame)
					.Append("\n(First Stored Input: ").Append(firstStoredFrame).Append(")")
					.Append("\n(Last Confirmed Input: ").Append(lastFrameWithConfirmedInputs).Append(")")
					.Append("\n\nPlayer 1 Predicted:   ").Append(p1Input.PredictedInput)
					.Append("\nPlayer 1 Confirmed:   ").Append(p1Input.ConfirmedInput)
					.Append("\nPlayer 1 Requires Rollback: ").Append(!p1Input.ArePredictedAndConfirmedInputsEqual())
					.Append("\n\nPlayer 2 Predicted:   ").Append(p2Input.PredictedInput)
					.Append("\nPlayer 2 Confirmed:   ").Append(p2Input.ConfirmedInput)
					.Append("\nPlayer 2 Requires Rollback: ").Append(!p2Input.ArePredictedAndConfirmedInputsEqual())
					.AppendLine().AppendLine();
			}

			// Update the predicted inputs with the inputs which have been already confirmed
			for (long i = rollbackFrame; i <= lastFrameWithConfirmedInputs; ++i){
				this.PlayerManager.TryOverridePredictionWithConfirmedInput(1, i);
				this.PlayerManager.TryOverridePredictionWithConfirmedInput(2, i);
			}

			// Check if we should override the current game state
			if (overriddenGameState != null){
				KeyValuePair<FluxStates, FluxFrameInput> pair;

				if (this._history.TryGetStateAndInput(rollbackFrame, out pair)){
					// Override partially the GameState in the history 
					// so we have a chance of resynchronization...
                    FluxStateTracker.LoadGameState(pair.Key);
					pair.Key.Override(overriddenGameState.Value);
					this._history.TrySetState(pair);

					// Store the new state into the local synchronization states 
					// to try to pass the next synchronization check...
					FluxSimpleState simpleState = new FluxSimpleState(pair.Key);
					for (int i = 0; i < this._localSynchronizationStates.Count; ++i){
						if (this._localSynchronizationStates[i].frame == simpleState.frame){
							this._localSynchronizationStates[i] = simpleState;
						}
					}
				}
			}

			// Reset the game to the state it had on the last consistent frame...
            this._history = FluxStateTracker.LoadGameState(this._history, rollbackFrame);

			// And simulate all the frames after that fast-forward, so we return to the previous frame again...
			long fastForwardTarget = Math.Min(UFE.currentFrame, this._remotePlayerNextExpectedFrame - 1);
			long maxFastForwards = Math.Max((long)(UFE.config.networkOptions.maxFastForwards), (currentFrame - fastForwardTarget)/2L);
			long currentFastForwards = 0L;

			while (UFE.currentFrame < currentFrame && currentFastForwards < maxFastForwards){
				this.ApplyInputs(UFE.currentFrame, lastFrameWithConfirmedInputs);
				++currentFastForwards;
			}
		}else{
			this._debugInfo.AppendLine().AppendLine()
				.Append("Rollback from frame ").Append(currentFrame).Append(" to frame ").Append(rollbackFrame)
				.Append("\nFailed because the specified frame is no longer stored in the Game History.")
				.AppendLine().AppendLine();
			
		}
#endif
    }

    protected virtual void ReadInputs(long frameDelay, bool allowRollbacks) {
        //-------------------------------------------------------------------------------------------------------------
        // Read the player inputs (ensuring that there aren't any "holes" created by variable frame-delay).
        //-------------------------------------------------------------------------------------------------------------
        for (int i = 0; i <= frameDelay * 2; ++i) {
            long frame = UFE.currentFrame + (long)(i);

            for (int j = 1; j <= FluxPlayerManager.NumberOfPlayers; ++j) {
                if (this.PlayerManager.ReadInputs(j, frame, this._selectedOptions[j - 1], allowRollbacks)) {
                    this._selectedOptions[j - 1] = null;
                }
            }
        }
    }

	protected virtual bool SynchronizationCheck(
		FluxSimpleState expectedState, 
		FluxSimpleState receivedState, 
		long frame
	){
		return this.SynchronizationCheck(expectedState, receivedState, frame, true);
	}

	protected virtual bool SynchronizationCheck(
		FluxSimpleState expectedState, 
		FluxSimpleState receivedState, 
		long frame,
		bool allowRecoveryFromDesynchronizations
	){
		float distanceThreshold = UFE.config.networkOptions.floatDesynchronizationThreshold;

		// As we want to be as permissive as possible as long as we can recover from the desynchronization,
		// we aren't as interested in comparing players absolute positions as we are interesting in comparing
		// their positions relative each other.
		Vector3 expectedRelativePosition = (expectedState.p1.position - expectedState.p2.position);
		Vector3 receivedRelativePosition = (receivedState.p1.position - receivedState.p2.position);

		if(
			expectedState.frame == receivedState.frame 
			&&
			(
				!UFE.config.networkOptions.desynchronizationRecovery &&
				FPLibrary.FPMath.Abs(expectedState.p1.life - receivedState.p1.life ) <= distanceThreshold &&
                FPLibrary.FPMath.Abs(expectedState.p2.life - receivedState.p2.life) <= distanceThreshold
				||
				UFE.config.networkOptions.desynchronizationRecovery &&
                FPLibrary.FPMath.Abs(expectedState.p1.life - receivedState.p1.life) <= distanceThreshold &&
                FPLibrary.FPMath.Abs(expectedState.p1.gauge - receivedState.p1.gauge) <= distanceThreshold &&
                FPLibrary.FPMath.Abs(expectedState.p2.life - receivedState.p2.life) <= distanceThreshold &&
                FPLibrary.FPMath.Abs(expectedState.p2.gauge - receivedState.p2.gauge) <= distanceThreshold &&
				Mathf.Abs(expectedRelativePosition.x - receivedRelativePosition.x) <= distanceThreshold	&&
				Mathf.Abs(expectedRelativePosition.y - receivedRelativePosition.y) <= distanceThreshold	&&
				Mathf.Abs(expectedRelativePosition.z - receivedRelativePosition.z) <= distanceThreshold
			)
		){
			//---------------------------------------------------------------------------------------------------------
			// If the game state received from the network message is equal to the stored state,
			// everything is ok, we can delete previous messages.
			//---------------------------------------------------------------------------------------------------------
			// Debug.Log("Synchroned!\tFrame = " + msg.CurrentFrame + "\nExpected State: " + state +  "\nReceived State: " + msg.Data);

			for (int i = this._localSynchronizationStates.Count - 1; i >= 0; --i){
				if (this._localSynchronizationStates[i].frame <= receivedState.frame){
					this._localSynchronizationStates.RemoveAt(i);
				}
			}

			for (int i = this._remoteSynchronizationStates.Count - 1; i >= 0; --i){
				if (this._remoteSynchronizationStates[i].frame <= receivedState.frame){
					this._remoteSynchronizationStates.RemoveAt(i);
				}
			}

			return true;
		}else{ 
			//---------------------------------------------------------------------------------------------------------
			// If a desynchronization has happened, check if we should try to recover from the desynchronization
			// so show a log message and check if we should exit from the network game.
			//---------------------------------------------------------------------------------------------------------
			long firstStoredInput = Math.Max(
				this.PlayerManager.player1.inputBuffer.FirstFrame, 
				this.PlayerManager.player2.inputBuffer.FirstFrame
			);

			int localPlayer = UFE.GetLocalPlayer();
			long rollbackFrame = localPlayer == 1 ? frame - 1L : frame;

			if(
				allowRecoveryFromDesynchronizations &&
				rollbackFrame > firstStoredInput &&
				rollbackFrame >= this._history.FirstStoredFrame &&
				UFE.config.networkOptions.desynchronizationRecovery &&
				this.AllowRollbacks
			){
				
				long lastFrameWithConfirmedInput = this.PlayerManager.GetLastFrameWithConfirmedInput();
				long currentFrame = UFE.currentFrame;

				if (localPlayer == 1){
					this._debugInfo.Append("\n\n\nDesynchronization detected, expecting the other client will recover from the desynchronization.\n\n");
					this.Rollback(currentFrame, rollbackFrame, lastFrameWithConfirmedInput);
					return true;
				}else{
					this._debugInfo.Append("\n\n\nDesynchronization detected, trying to recover from the desynchronization.\n\n");
					this.Rollback(currentFrame, rollbackFrame, lastFrameWithConfirmedInput, receivedState);
					return this.SynchronizationCheck(expectedState, receivedState, frame, false);
				}
			}else{
				//-----------------------------------------------------------------------------------------------------
				// If a desynchronization has happened and we can't or don't want to recover from the 
				// desynchronization, show a log message and check if we should exit from the network game.
				//-----------------------------------------------------------------------------------------------------
				string expectedStateString = expectedState.ToString();
				string receivedStateString = receivedState.ToString();

				this._debugInfo
					.Append("\n\n\nSYNCHRONIZATION LOST!!!")
					.Append("\nFrame: ").Append(frame)
					.Append("\nExpected State: ").Append(expectedStateString)
					.Append("\nReceived State: ").Append(receivedStateString).AppendLine().AppendLine();

				this._localSynchronizationStates.Clear();
				this._remoteSynchronizationStates.Clear();

				this.ForceDisconnection(string.Format(
					"SYNCHRONIZATION LOST!!!\nFrame: {0}\nExpected State: {1}\nReceived State: {2}",
					frame,
					expectedStateString,
					receivedStateString
				));

				return false;
			}
		}
	}

	protected virtual void UpdateGUI(
		IDictionary<InputReferences, InputEvents> player1PreviousInputs,
		IDictionary<InputReferences, InputEvents> player1CurrentInputs,
		int? player1SelectedOptions,
		IDictionary<InputReferences, InputEvents> player2PreviousInputs,
		IDictionary<InputReferences, InputEvents> player2CurrentInputs,
		int? player2SelectedOptions
	){

		if (CameraFade.instance.enabled){
			CameraFade.instance.DoFixedUpdate();
		}

        if (UFE.battleGUI != null) {
			if (player1SelectedOptions != null){
				UFE.battleGUI.SelectOption(player1SelectedOptions.Value, 1);
			}

			if (player2SelectedOptions != null){
				UFE.battleGUI.SelectOption(player2SelectedOptions.Value, 2);
			}

			UFE.battleGUI.DoFixedUpdate(
				player1PreviousInputs,
				player1CurrentInputs,
				player2PreviousInputs,
				player2CurrentInputs
            );

		}

        if (UFE.isControlFreak2Installed && UFE.touchControllerBridge != null) {
            UFE.touchControllerBridge.DoFixedUpdate();
        } else if (UFE.isControlFreak1Installed) {
            if (UFE.gameRunning && UFE.controlFreakPrefab != null && !UFE.controlFreakPrefab.activeSelf) {
                UFE.controlFreakPrefab.SetActive(true);
            } else if (!UFE.gameRunning && UFE.controlFreakPrefab != null && UFE.controlFreakPrefab.activeSelf) {
                UFE.controlFreakPrefab.SetActive(false);
            }
        }

		if (UFE.currentScreen != null){
			if (player1SelectedOptions != null){
				UFE.currentScreen.SelectOption(player1SelectedOptions.Value, 1);
			}

			if (player2SelectedOptions != null){
				UFE.currentScreen.SelectOption(player2SelectedOptions.Value, 2);
			}

			UFE.currentScreen.DoFixedUpdate(
				player1PreviousInputs,
				player1CurrentInputs,
				player2PreviousInputs,
				player2CurrentInputs
			);
		}

		if (UFE.canvasGroup.alpha == 0){
			UFE.canvasGroup.alpha = 1;
		}
	}

	protected virtual void UpdateTimer(){
		if (UFE.config.roundOptions.hasTimer && UFE.timer > 0 && !UFE.IsTimerPaused()) {
			if (UFE.gameMode != GameMode.TrainingRoom 
                && UFE.gameMode != GameMode.ChallengeMode
                && !UFE.config.trainingModeOptions.freezeTime){
                UFE.timer -= UFE.fixedDeltaTime * (UFE.config.roundOptions._timerSpeed * .01);
			}

			if (UFE.timer < UFE.intTimer) {
				UFE.intTimer --;
				UFE.FireTimer((float)UFE.timer);
			}
		}
		if (UFE.timer < 0){
			UFE.timer = 0;
		}
		if (UFE.intTimer < 0){
			UFE.intTimer = 0;
		}
        
		ControlsScript p1ControlsScript = UFE.GetControlsScript(1);
		ControlsScript p2ControlsScript = UFE.GetControlsScript(2);

		if (UFE.timer == 0 && p1ControlsScript != null && !UFE.config.lockMovements){
			Fix64 p1LifePercentage = p1ControlsScript.myInfo.currentLifePoints/(Fix64)p1ControlsScript.myInfo.lifePoints;
            Fix64 p2LifePercentage = p2ControlsScript.myInfo.currentLifePoints / (Fix64)p2ControlsScript.myInfo.lifePoints;
			UFE.PauseTimer();
			UFE.config.lockMovements = true;
			UFE.config.lockInputs = true;

			UFE.FireTimeOver();

            
            // Check Winner
            if (p1LifePercentage == p2LifePercentage) {
                UFE.FireAlert(UFE.config.selectedLanguage.draw, null);
                UFE.DelaySynchronizedAction(this.NewRound, 1);
            } else {
                if (p1LifePercentage > p2LifePercentage) SetWinner((p1LifePercentage > p2LifePercentage) ? p1ControlsScript : p2ControlsScript);
            }
		}
	}

    protected virtual void UpdateInstantiatedObjects(long currentFrame, long lastSynchronizedFrame)
    {
        foreach (InstantiatedGameObject entry in UFE.instantiatedObjects.ToArray())
        {
            if (entry.gameObject == null) continue;
            if (entry.mrFusion != null && entry.gameObject.activeInHierarchy) entry.mrFusion.UpdateBehaviours();
            if (entry.destructionFrame != null) entry.gameObject.SetActive(currentFrame > entry.creationFrame && currentFrame < entry.destructionFrame);
        }

        // Memory Cleaner
        if (UFE.instantiatedObjects.Count > 0 && (UFE.instantiatedObjects.Count > UFE.config.networkOptions.spawnBuffer || !UFE.gameRunning)) {
            if (!UFE.instantiatedObjects[0].gameObject.activeInHierarchy)
            {
                UnityEngine.Object.Destroy(UFE.instantiatedObjects[0].gameObject);
                UFE.instantiatedObjects.RemoveAt(0);
            }
        }
    }

    protected virtual void UpdatePlayer(int player, long currentFrame, long lastSynchronizedFrame, IDictionary<InputReferences, InputEvents> previousInputs, IDictionary<InputReferences, InputEvents> currentInputs) {
		ControlsScript controlsScript = UFE.GetControlsScript(player);

		if (controlsScript != null) {
			if (controlsScript.MoveSet != null && controlsScript.MoveSet.MecanimControl != null) {
				controlsScript.MoveSet.MecanimControl.DoFixedUpdate();
			}
			if (controlsScript.MoveSet != null && controlsScript.MoveSet.LegacyControl != null) {
				controlsScript.MoveSet.LegacyControl.DoFixedUpdate();
            }
            controlsScript.DoFixedUpdate(previousInputs, currentInputs);

            if (controlsScript.projectiles.Count > 0) {
				controlsScript.projectiles.RemoveAll(item => item.IsDestroyed() || item == null);
			}
		}
	}
#endregion
}
