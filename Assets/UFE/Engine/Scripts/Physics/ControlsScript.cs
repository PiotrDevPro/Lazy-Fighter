using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using FPLibrary;
using UFE3D;

public class ControlsScript : MonoBehaviour {

    #region trackable definitions
    public Fix64 afkTimer;
    public int airJuggleHits;
    public AirRecoveryType airRecoveryType;
    public bool applyRootMotion;
    public bool blockStunned;
    public Fix64 comboDamage;
    public Fix64 comboHitDamage;
    public int comboHits;
    public int consecutiveCrumple;
    public BasicMoveReference currentBasicMove;
    public Fix64 currentDrained;
    public string currentHitAnimation;
    public PossibleStates currentState;
    public SubStates currentSubState;

    public MoveInfo DCMove;
    public CombatStances DCStance;
    public bool firstHit;
    public Fix64 gaugeDPS;
    public bool hitDetected;
    public Fix64 hitAnimationSpeed;
    public Fix64 hitStunDeceleration;
    public bool inhibitGainWhileDraining;
    public bool isAirRecovering;
    public bool isBlocking;
    public bool isDead;
    public bool ignoreCollisionMass;
    public bool introPlayed;
    public bool lit;
    public UFE3D.CharacterInfo myInfo;
    public int mirror;
    public Fix64 normalizedDistance;
    public Fix64 normalizedJumpArc;
    public bool outroPlayed;
    public bool potentialBlock;
    public Fix64 potentialParry;
    public bool roundMsgCasted;
    public int roundsWon;
    public bool shakeCamera;
    public bool shakeCharacter;
    public Fix64 shakeDensity;
    public Fix64 shakeCameraDensity;
    public StandUpOptions standUpOverride;
    public Fix64 standardYRotation;
    public Fix64 storedMoveTime;
    public Fix64 stunTime;
    public Fix64 totalDrain;

    public PullIn activePullIn;
    public Hit currentHit;
    public MoveInfo currentMove;
    public MoveInfo storedMove;

    public PhysicsScript Physics { get { return this.myPhysicsScript; } set { myPhysicsScript = value; } }
    public MoveSetScript MoveSet { get { return this.myMoveSetScript; } set { myMoveSetScript = value; } }
    public HitBoxesScript HitBoxes { get { return this.myHitBoxesScript; } set { myHitBoxesScript = value; } }

    public Dictionary<ButtonPress, Fix64> inputHeldDown = new Dictionary<ButtonPress, Fix64>();
    public List<ProjectileMoveScript> projectiles = new List<ProjectileMoveScript>();

    public FPTransform worldTransform;
    public FPTransform localTransform;
    #endregion


    public Shader[] normalShaders;
    public Color[] normalColors;
    
	private PhysicsScript myPhysicsScript;
	private MoveSetScript myMoveSetScript;
    private HitBoxesScript myHitBoxesScript;

	private PhysicsScript opPhysicsScript;
	private HitBoxesScript opHitBoxesScript;

	public HeadLookScript headLookScript;
	public GameObject emulatedCam;
	public CameraScript cameraScript;

    public Text debugger;
    public string aiDebugger { get; set; }
    public CharacterDebugInfo debugInfo;
    public int playerNum;

    //private ActionSequence[] currentActionSequence;
    
    [HideInInspector] public GameObject character;
    [HideInInspector] public GameObject opponent;
    [HideInInspector] public UFE3D.CharacterInfo opInfo;
    [HideInInspector] public ChallengeMode challengeMode;
    [HideInInspector] public ControlsScript opControlsScript;

    void Start() {
        foreach (ButtonPress bp in System.Enum.GetValues(typeof(ButtonPress))) {
            inputHeldDown.Add(bp, 0);
        }

        worldTransform = gameObject.AddComponent<FPTransform>();

		if (gameObject.name == "Player1") {
            //transform.position = new Vector3(UFE.config.roundOptions.p1XPosition, .009f, 0);
            worldTransform.position = new FPVector(UFE.config.roundOptions._p1XPosition, .009, 0);
			opponent = GameObject.Find("Player2");
			if (myInfo == null)
                Debug.LogError("Player 1 character not found! Make sure you have set the characters correctly in the Editor");

            opInfo = UFE.config.player2Character;
			mirror = -1;
			playerNum = 1;
            debugInfo = UFE.config.debugOptions.p1DebugInfo;

            if (UFE.gameMode == GameMode.TrainingRoom) {
                myInfo.currentLifePoints = (Fix64)myInfo.lifePoints * (UFE.config.trainingModeOptions.p1StartingLife / 100);
                myInfo.currentGaugePoints = (Fix64)myInfo.maxGaugePoints * (UFE.config.trainingModeOptions.p1StartingGauge / 100);
            } else {
                myInfo.currentLifePoints = (Fix64)myInfo.lifePoints;
            }
		}else{
            //transform.position = new Vector3(UFE.config.roundOptions.p2XPosition, .009f, 0);
            worldTransform.position = new FPVector(UFE.config.roundOptions._p2XPosition, .009, 0);
            opponent = GameObject.Find("Player1");
            if (myInfo == null) 
				Debug.LogError("Player 2 character not found! Make sure you have set the characters correctly in the Editor");

            opInfo = UFE.config.player1Character;
			mirror = 1;
			playerNum = 2;
            debugInfo = UFE.config.debugOptions.p2DebugInfo;

            if (UFE.gameMode == GameMode.TrainingRoom) {
                myInfo.currentLifePoints = (Fix64)myInfo.lifePoints * (UFE.config.trainingModeOptions.p2StartingLife / 100);
                myInfo.currentGaugePoints = (Fix64)myInfo.maxGaugePoints * (UFE.config.trainingModeOptions.p2StartingGauge / 100);
            } else {
                myInfo.currentLifePoints = myInfo.lifePoints;
            }
		}

		if (myInfo.characterPrefabStorage == StorageMode.Legacy && myInfo.characterPrefab == null) 
			Debug.LogError("Character prefab for "+ gameObject.name +" not found. Make sure you have selected a prefab character in the Character Editor");

        
        if (myInfo.characterPrefabStorage == StorageMode.Legacy) {
            character = Instantiate(myInfo.characterPrefab);
        } else {
            character = Instantiate(Resources.Load<GameObject>(myInfo.prefabResourcePath));
        }
		//character = Instantiate(myInfo.characterPrefab);
		character.transform.parent = transform;

        localTransform = character.AddComponent<FPTransform>();
        localTransform.rotation = myInfo.initialRotation;
        //localTransform.rotation = new FPQuaternion((FP)character.transform.rotation.x, (FP)character.transform.rotation.y, (FP)character.transform.rotation.z, (FP)character.transform.rotation.w);

        standardYRotation = localTransform.eulerAngles.y;
        //standardYRotation = character.transform.rotation.eulerAngles.y;
        

        myMoveSetScript = character.AddComponent<MoveSetScript>();
        if (myPhysicsScript == null) myPhysicsScript = GetComponent<PhysicsScript>();
        myHitBoxesScript = character.GetComponent<HitBoxesScript>();
        cameraScript = transform.parent.GetComponent<CameraScript>();

        myMoveSetScript.controlsScript = this;
        myMoveSetScript.hitBoxesScript = myHitBoxesScript;
        myHitBoxesScript.controlsScript = this;
        myHitBoxesScript.moveSetScript = myMoveSetScript;
        myPhysicsScript.controlScript = this;
        myPhysicsScript.moveSetScript = myMoveSetScript;


        if (myInfo.headLook.enabled){
			character.AddComponent<HeadLookScript>();
			headLookScript = character.GetComponent<HeadLookScript>();
			headLookScript.segments = myInfo.headLook.segments;
			headLookScript.nonAffectedJoints = myInfo.headLook.nonAffectedJoints;
			headLookScript.effect = myInfo.headLook.effect;
			headLookScript.overrideAnimation = !myInfo.headLook.overrideAnimation;
			
			foreach(BendingSegment segment in headLookScript.segments) {
				segment.firstTransform = myHitBoxesScript.GetTransform(segment.bodyPart).parent.transform;
				segment.lastTransform = myHitBoxesScript.GetTransform(segment.bodyPart);
			}
			
			foreach(NonAffectedJoints nonAffectedJoint in headLookScript.nonAffectedJoints) 
				nonAffectedJoint.joint = myHitBoxesScript.GetTransform(nonAffectedJoint.bodyPart);
		}

		if (UFE.config.roundOptions.allowMovementStart) {
			UFE.config.lockMovements = false;
		}else{
			UFE.config.lockMovements = true;
        }

        if (playerNum == 2) {
            //testCharacterRotation(100, true);
            UFE.FireGameBegins();
        }

        if (playerNum == 1 && UFE.gameMode == GameMode.ChallengeMode) {
            challengeMode = gameObject.AddComponent<ChallengeMode>();
            challengeMode.cScript = this;
            //challengeMode.Start();
        }
	}
	
	private bool isAxisRested(IDictionary<InputReferences, InputEvents> currentInputs){
		if (currentState == PossibleStates.Down) return true;
		if (UFE.config.lockMovements) return true;
		foreach (InputReferences inputRef in currentInputs.Keys) {
			if (inputRef.inputType == InputType.Button) continue;
            if (currentInputs[inputRef].axisRaw != 0) {
                if (inputRef.inputType == InputType.HorizontalAxis && !myMoveSetScript.basicMoves.moveEnabled) return true;
                if (inputRef.inputType == InputType.VerticalAxis) {
                    if (currentInputs[inputRef].axisRaw > 0 && !myMoveSetScript.basicMoves.jumpEnabled) return true;
                    if (currentInputs[inputRef].axisRaw < 0 && !myMoveSetScript.basicMoves.crouchEnabled) return true;
                }
            }
		}
		return true;
	}

	public void ForceMirror(bool toggle)
    {
        if (UFE.config.characterRotationOptions.autoMirror)
        {
            if (myInfo.animationType == AnimationType.Legacy)
            {
                float xScale = Mathf.Abs(character.transform.localScale.x) * (toggle ? -1 : 1);
                character.transform.localScale = new Vector3(xScale, character.transform.localScale.y, character.transform.localScale.z);
            }
            else
            {
                myMoveSetScript.SetMecanimMirror(toggle);
                if (!myInfo.useAnimationMaps) myHitBoxesScript.InvertHitBoxes(toggle);
            }
        }
        myHitBoxesScript.currentMirror = toggle;
    }
	
	public void InvertRotation(){
		standardYRotation = -standardYRotation;
	}

	private void testCharacterRotation(Fix64 rotationSpeed){
		testCharacterRotation(rotationSpeed, false);
	}

	private void testCharacterRotation(Fix64 rotationSpeed, bool forceMirror){
        if ((mirror == -1 || forceMirror) && worldTransform.position.x > opControlsScript.worldTransform.position.x) {
			mirror = 1;
            potentialBlock = false;
			InvertRotation();
            ForceMirror(true);
            UFE.FireSideSwitch(mirror, myInfo);

        } else if ((mirror == 1 || forceMirror) && worldTransform.position.x < opControlsScript.worldTransform.position.x) {
			mirror = -1;
			potentialBlock = false;
            InvertRotation();
            ForceMirror(false);
            UFE.FireSideSwitch(mirror, myInfo);
		}

        if (UFE.config.networkOptions.disableRotationBlend &&
            (UFE.isConnected || UFE.config.debugOptions.emulateNetwork)) {
            fixCharacterRotation();

        } else {
            FPQuaternion newRotation = FPQuaternion.Slerp(
                worldTransform.rotation, 
				FPQuaternion.AngleAxis(standardYRotation, FPVector.up), 
				(UFE.fixedDeltaTime * rotationSpeed)
			);
            
            localTransform.rotation = newRotation;
        }

	}
	
	private void fixCharacterRotation(){
		if (currentState == PossibleStates.Down) return;
        
        FPQuaternion fixedRotation = FPQuaternion.AngleAxis(standardYRotation, FPVector.up);
        localTransform.rotation = fixedRotation;
	}

	private void validateRotation(){
		if (!myPhysicsScript.IsGrounded() || myPhysicsScript.freeze || currentMove != null) fixCharacterRotation();

		if (myPhysicsScript.freeze) return;
		if (currentState == PossibleStates.Down) return;
		if (currentMove != null && (!currentMove.autoCorrectRotation || currentMove.frameWindowRotation > currentMove.currentFrame)) return;
		if (myPhysicsScript.IsJumping() && !UFE.config.characterRotationOptions.rotateWhileJumping) return;
		if (currentSubState == SubStates.Stunned && !UFE.config.characterRotationOptions.fixRotationWhenStunned) return;
		if (isBlocking && !UFE.config.characterRotationOptions.fixRotationWhenBlocking) return;
		if (UFE.config.characterRotationOptions.rotateOnMoveOnly && myMoveSetScript.IsAnimationPlaying("idle")) return;

		testCharacterRotation(UFE.config.characterRotationOptions._rotationSpeed);
	}

	public void DoFixedUpdate(
		IDictionary<InputReferences, InputEvents> previousInputs,
		IDictionary<InputReferences, InputEvents> currentInputs
	){
        // Once per game
		if (opponent == null){
			return;
		}

		if (opControlsScript == null || opPhysicsScript == null	|| opHitBoxesScript == null){
			opControlsScript = opponent.GetComponent<ControlsScript>();
            opPhysicsScript = opponent.GetComponent<PhysicsScript>();
            opHitBoxesScript = opponent.GetComponentInChildren<HitBoxesScript>();

            if (myInfo.isAlt) {
                if (myInfo.alternativeCostumes[myInfo.selectedCostume].enableColorMask) {
                    Renderer[] charRenders = character.GetComponentsInChildren<Renderer>();
                    foreach (Renderer charRender in charRenders) {
                        charRender.material.color = myInfo.alternativeCostumes[myInfo.selectedCostume].colorMask;
                        //charRender.material.shader = Shader.Find("VertexLit");
                        //charRender.material.SetColor("_Emission", myInfo.alternativeColor);
                    }
                }
            }

            Renderer[] charRenderers = character.GetComponentsInChildren<Renderer>();
            List<Shader> shaderList = new List<Shader>();
            List<Color> colorList = new List<Color>();
            foreach (Renderer char_rend in charRenderers) {
                //if (char_rend.material.HasProperty("color") && char_rend.material.HasProperty("shader")){ 
                shaderList.Add(char_rend.material.shader);
                colorList.Add(char_rend.material.color);
                //}
            }
            normalShaders = shaderList.ToArray();
            normalColors = colorList.ToArray();

            myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.idle);

            
            if (playerNum == 2) testCharacterRotation(100, true);
        }


		// Apply Training / Challenge Mode Options
        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && ((playerNum == 1 && UFE.config.trainingModeOptions.p1Life == LifeBarTrainingMode.Refill) 
            || (playerNum == 2 && UFE.config.trainingModeOptions.p2Life == LifeBarTrainingMode.Refill))) {
			if (!UFE.FindDelaySynchronizedAction(this.RefillLife))
                UFE.DelaySynchronizedAction(this.RefillLife, UFE.config.trainingModeOptions.refillTime);
		}

        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && ((playerNum == 1 && UFE.config.trainingModeOptions.p1Gauge == LifeBarTrainingMode.Refill) 
            || (playerNum == 2 && UFE.config.trainingModeOptions.p2Gauge == LifeBarTrainingMode.Refill))) {
			if (!UFE.FindDelaySynchronizedAction(this.RefillGauge))
                UFE.DelaySynchronizedAction(this.RefillGauge, UFE.config.trainingModeOptions.refillTime);
		}

        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && myInfo.currentGaugePoints < myInfo.maxGaugePoints 
            && ((playerNum == 1 && UFE.config.trainingModeOptions.p1Gauge == LifeBarTrainingMode.Infinite) 
            || (playerNum == 2 && UFE.config.trainingModeOptions.p2Gauge == LifeBarTrainingMode.Infinite))) RefillGauge();

        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && myInfo.currentLifePoints < myInfo.lifePoints 
            && ((playerNum == 1 && UFE.config.trainingModeOptions.p1Life == LifeBarTrainingMode.Infinite) 
            || (playerNum == 2 && UFE.config.trainingModeOptions.p2Life == LifeBarTrainingMode.Infinite))) RefillLife();

        
        //Update Hitboxes Position Map
        myHitBoxesScript.UpdateMap(myMoveSetScript.GetCurrentClipFrame(myHitBoxesScript.bakeSpeed));


        // Resolve move
        resolveMove();


		// Check inputs
		translateInputs(previousInputs, currentInputs);


		// Validate rotation
		validateRotation();


		// Gauge Drain
        if (gaugeDPS != 0) {
            myInfo.currentGaugePoints -= ((myInfo.maxGaugePoints * (gaugeDPS / 100)) / UFE.config.fps);
            if (gaugeDPS != 0) currentDrained += (gaugeDPS / UFE.config.fps);
            if (totalDrain != 0 && (myInfo.currentGaugePoints <= 0 || currentDrained >= totalDrain)) {
                ResetDrainStatus(false);
            }
        }


		// Input Viewer
		List<InputReferences> inputList = new List<InputReferences>();
        string inputDebugger = "";
		foreach (InputReferences inputRef in currentInputs.Keys){
			if (debugger != null && UFE.config.debugOptions.debugMode && debugInfo.inputs){
                inputDebugger += inputRef.inputButtonName + " - "+ inputHeldDown[inputRef.engineRelatedButton] + " (" + currentInputs[inputRef].axisRaw + ")\n";
			}
			if (inputHeldDown[inputRef.engineRelatedButton] > 0 && inputHeldDown[inputRef.engineRelatedButton] <= (2 / (Fix64)UFE.config.fps)){
				inputList.Add(inputRef);
                UFE.FireButton(inputRef.engineRelatedButton, myInfo);
			}
		}
		UFE.CastInput(inputList.ToArray(), playerNum);


        // Apply Root Motion
        if (applyRootMotion || (currentMove != null && currentMove.applyRootMotion))
        {
            FPVector newPosition = worldTransform.position;
            if (myMoveSetScript.animationPaused) {
                newPosition.x += myHitBoxesScript.GetDeltaPosition().x * myMoveSetScript.GetAnimationSpeed() * UFE.timeScale;
                newPosition.y += myHitBoxesScript.GetDeltaPosition().y * myMoveSetScript.GetAnimationSpeed() * UFE.timeScale;
            } else {
                newPosition.x += myHitBoxesScript.GetDeltaPosition().x * UFE.timeScale;
                newPosition.y += myHitBoxesScript.GetDeltaPosition().y * UFE.timeScale;
            }
            worldTransform.position = newPosition;
        }
        else
        {
            localTransform.position = new FPVector(0, 0, 0);
        }


		// Force stand state
		if (!myPhysicsScript.freeze
			&& !isDead
            && currentSubState != SubStates.Stunned
			&& introPlayed
			&& myPhysicsScript.IsGrounded()
			&& !myPhysicsScript.IsMoving()
		    && currentMove == null
            && !myMoveSetScript.IsBasicMovePlaying(myMoveSetScript.basicMoves.idle)
			&& !myMoveSetScript.IsAnimationPlaying("fallStraight")
			&& isAxisRested(currentInputs)
			&& !myPhysicsScript.isTakingOff
			&& !myPhysicsScript.isLanding
			&& !blockStunned
			&& currentState != PossibleStates.Crouch
			&& !isBlocking
		    ){

                myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.idle);
			    currentState = PossibleStates.Stand;
			    currentSubState = SubStates.Resting;
			    if (UFE.config.blockOptions.blockType == BlockType.AutoBlock 
                    && myMoveSetScript.basicMoves.blockEnabled) potentialBlock = true;
		}

        if (myMoveSetScript.IsAnimationPlaying("idle")
            && !UFE.config.lockInputs 
		    && !UFE.config.lockMovements
            && !myPhysicsScript.freeze) {
            afkTimer += UFE.fixedDeltaTime;
            if (afkTimer >= myMoveSetScript.basicMoves.idle._restingClipInterval) {
                afkTimer = 0;
                int clipNum = FPRandom.Range(2, 6);
                if (myMoveSetScript.AnimationExists("idle_" + clipNum)) {
                    myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.idle, "idle_" + clipNum, false);
                }
            }
        } else {
            afkTimer = 0;
        }


        // Character colliders based on collision mass and body colliders
        //normalizedDistance = Mathf.Clamp01(Vector3.Distance(opponent.transform.position, transform.position) / UFE.config.cameraOptions.maxDistance);
        normalizedDistance = FPMath.Clamp(FPVector.Distance(opControlsScript.worldTransform.position, worldTransform.position) / UFE.config.cameraOptions._maxDistance, 0, 1);
        if (!ignoreCollisionMass && !opControlsScript.ignoreCollisionMass) {
			Fix64 pushForce = myHitBoxesScript.TestCollision(worldTransform.position, opControlsScript.worldTransform.position, opHitBoxesScript.hitBoxes);
			if (pushForce > 0) {
				if (worldTransform.position.x < opControlsScript.worldTransform.position.x) {
                    worldTransform.Translate(new FPVector(-.1 * pushForce, 0, 0));
				}else{
                    worldTransform.Translate(new FPVector(.1 * pushForce, 0, 0));
				}
				if (opControlsScript.worldTransform.position.x == UFE.config.selectedStage._rightBoundary){
                    opControlsScript.worldTransform.Translate(new FPVector(-.2 * pushForce, 0, 0));
				}
			}

			pushForce = myInfo.physics._groundCollisionMass - FPVector.Distance(opControlsScript.worldTransform.position, worldTransform.position);
			if (pushForce > 0) {
				if (worldTransform.position.x < opControlsScript.worldTransform.position.x) {
                    worldTransform.Translate(new FPVector(-.5 * pushForce, 0, 0));
                    //transform.Translate(new Vector3(-.5f * pushForce, 0, 0));
				}else{
                    worldTransform.Translate(new FPVector(.5 * pushForce, 0, 0));
					//transform.Translate(new Vector3(.5f * pushForce, 0, 0));
				}
				if (opControlsScript.worldTransform.position.x == UFE.config.selectedStage._rightBoundary){
                    opControlsScript.worldTransform.Translate(new FPVector(-.2 * pushForce, 0, 0));
                    //opponent.transform.Translate(new Vector3(-.2f * pushForce, 0, 0));
				}
			}
		}



		// Shake character
		if (shakeDensity > 0) {
			shakeDensity -= UFE.fixedDeltaTime;
			if (myHitBoxesScript.isHit && myPhysicsScript.freeze){
				if (shakeCharacter) shake();
			}
		}else if (shakeDensity < 0) {
			shakeDensity = 0;
			shakeCharacter = false;
		}

        // Shake camera
        if (shakeCameraDensity > 0) {
            shakeCameraDensity -= UFE.fixedDeltaTime * 3;
            if (shakeCamera) shakeCam();
            if (UFE.config.groundBounceOptions.shakeCamOnBounce && myPhysicsScript.isGroundBouncing) shakeCam();
            if (UFE.config.wallBounceOptions.shakeCamOnBounce && myPhysicsScript.isWallBouncing) shakeCam();
		}else if (shakeCameraDensity < 0) {
            shakeCameraDensity = 0;
			shakeCamera = false;
		}


		// Validate Parry
		if (potentialParry > 0){
			potentialParry -= UFE.fixedDeltaTime;
			if (potentialParry <= 0) potentialParry = 0;
		}


		// Update head movement
		if (headLookScript != null && opHitBoxesScript != null) 
			headLookScript.target = opHitBoxesScript.GetPosition(myInfo.headLook.target).ToVector();


        // Execute Move
        if (currentMove != null) {
            //myHitBoxesScript.UpdateMap(currentMove.currentFrame);
            ReadMove(currentMove);
        }


		// Apply Stun
		if ((currentSubState == SubStates.Stunned || blockStunned) && stunTime > 0 && !myPhysicsScript.freeze && !isDead)
			ApplyStun(previousInputs, currentInputs);


		// Apply Forces
		myPhysicsScript.ApplyForces(currentMove);


        // Once per round
        if ((gameObject.name == "Player1" && !introPlayed && currentMove == null) ||
            (gameObject.name == "Player2" && !introPlayed && opControlsScript.introPlayed && currentMove == null)) {
            KillCurrentMove();
            CastMove(myMoveSetScript.intro, true, true, false);
            if (currentMove == null) {
                introPlayed = true;
                UFE.CastNewRound();
            }
        }

        // Test Current Challenge
        if (challengeMode != null && challengeMode.complete) {
            UFE.FireAlert("Success", myInfo); // TODO
            if (challengeMode.moveToNext) {
                UFE.DelaySynchronizedAction(this.StartNextChallenge, .6);
            } else {
                UFE.DelaySynchronizedAction(UFE.fluxCapacitor.EndRound, (Fix64)5);
            }
            challengeMode.Stop();
        }


        // Update Unity Transforms with Fixed Point Transforms
        transform.position = worldTransform.position.ToVector();
        character.transform.localPosition = localTransform.position.ToVector();
        character.transform.rotation = localTransform.rotation.ToQuaternion();

        
		// Run Debugger
        if (debugger != null && UFE.config.debugOptions.debugMode) {
			debugger.text = "";
            if (UFE.config.debugOptions.debugMode &&
                (!UFE.config.debugOptions.trainingModeDebugger || UFE.gameMode == GameMode.TrainingRoom)) {
                debugger.text += "FPS: " + (1.0f / UFE.fixedDeltaTime) + "\n";
                debugger.text += "-----Character Info-----\n";
                if (debugInfo.lifePoints) debugger.text += "Life Points: " + myInfo.currentLifePoints + "\n";
                if (debugInfo.position) debugger.text += "Position: " + worldTransform.position + "\n";
                if (debugInfo.currentState) debugger.text += "State: " + currentState + "\n";
                if (debugInfo.currentState) debugger.text += "Taking Off: " + myPhysicsScript.isTakingOff + "\n";
                if (debugInfo.currentSubState) debugger.text += "Sub State: " + currentSubState + "\n";
                if (debugInfo.currentState) debugger.text += "Potential Block: " + potentialBlock + "\n";
                if (debugInfo.currentState) debugger.text += "Is Blocking: " + isBlocking + "\n";
                if (debugInfo.stunTime && stunTime > 0) debugger.text += "Stun Time: " + stunTime + "\n";
                //debugger.text += "MS Displacement: " + myMoveSetScript.GetDeltaDisplacement() + "\n";
                if (opControlsScript != null && opControlsScript.comboHits > 0) {
                    debugger.text += "Current Combo\n";
					if (debugInfo.comboHits) debugger.text += "- Total Hits: "+ opControlsScript.comboHits + "\n";
                    if (debugInfo.comboDamage) {
                        debugger.text += "- Total Damage: " + opControlsScript.comboDamage + "\n";
                        debugger.text += "- Hit Damage: " + opControlsScript.comboHitDamage + "\n";
                    }
                }

				// Other uses
				if (potentialParry > 0) debugger.text += "Parry Window: "+ potentialParry + "\n";
				//debugger.text += "Air Jumps: "+ myPhysicsScript.currentAirJumps + "\n";
				//debugger.text += "Horizontal Force: "+ myPhysicsScript.horizontalForce + "\n";
				//debugger.text += "Vertical Force: "+ myPhysicsScript.verticalForce + "\n";

                if (UFE.config.debugOptions.p1DebugInfo.currentMove && currentMove != null) {
                    debugger.text += "-----Move Info-----\n";
					debugger.text += "Move: "+ currentMove.name +"\n";
                    debugger.text += "Frames: "+ currentMove.currentFrame +"/"+ currentMove.totalFrames +"\n";
                    debugger.text += "Tick: "+ currentMove.currentTick +"\n";
                    debugger.text += "Animation Speed: " + myMoveSetScript.GetAnimationSpeed() + "\n";
					/*if (currentMove.chargeMove) {
						debugger.text += "First Input Charge: "+ myMoveSetScript.chargeValues[currentMove.buttonSequence[0]] + "\n";
					}*/
					//debugger.text += "StartupFrames: "+ currentMove.moveClassification.startupSpeed +" \n";
                }
			}
            if (inputDebugger != "") debugger.text += inputDebugger;
            if (aiDebugger != null && debugInfo.aiWeightList) debugger.text += aiDebugger;
		}
    }

	private bool testMoveExecution(ButtonPress buttonPress, bool inputUp){
		return testMoveExecution(new ButtonPress[]{buttonPress}, inputUp);
	}
	
	private bool testMoveExecution(ButtonPress[] buttonPresses, bool inputUp){
        MoveInfo tempMove = myMoveSetScript.GetMove(buttonPresses, 0, currentMove, false);
        if (tempMove != null) {
            storedMove = tempMove;
			storedMoveTime = (UFE.config.executionBufferTime / (Fix64)UFE.config.fps);
			return true;
		}
		return false;
	}
	
	private void resolveMove(){
		if (myPhysicsScript.freeze) return;
		if (storedMoveTime > 0) storedMoveTime -= UFE.fixedDeltaTime;
		if (storedMoveTime <= 0 && storedMove != null){
			storedMoveTime = 0;
			if (UFE.config.executionBufferType != ExecutionBufferType.NoBuffer) storedMove = null;
		}

        if (currentMove != null && storedMove == null && !opControlsScript.isDead)
            storedMove = myMoveSetScript.GetNextMove(currentMove);

        if (storedMove != null && (currentMove == null || myMoveSetScript.SearchMove(storedMove.moveName, currentMove.frameLinks))) {
			bool confirmQueue = false;
			bool ignoreConditions = false;
            if (currentMove != null && UFE.config.executionBufferType == ExecutionBufferType.OnlyMoveLinks) {
                foreach (FrameLink frameLink in currentMove.frameLinks) {
                    if (frameLink.cancelable) {
                        confirmQueue = true;
                    }

                    if (frameLink.ignorePlayerConditions) {
                        ignoreConditions = true;
                    }

                    if (confirmQueue) {
                        foreach (MoveInfo move in frameLink.linkableMoves) {
                            if (storedMove.name == move.name) {
                                storedMove.overrideStartupFrame = frameLink.nextMoveStartupFrame - 1;
                            }
                        }
                    }
                }
            } else if (UFE.config.executionBufferType == ExecutionBufferType.AnyMove
                      || (currentMove == null
                          && storedMoveTime >= ((Fix64)(UFE.config.executionBufferTime - 2) / (Fix64)UFE.config.fps))) {
				confirmQueue = true;
			}
			
			if (confirmQueue && (ignoreConditions || myMoveSetScript.ValidateMoveStances(storedMove.selfConditions, this))) {
				KillCurrentMove();
				this.SetMove(storedMove);

                storedMove = null;
                storedMoveTime = 0;
			}
		}
	}

    /*private ButtonPress characterInputOverride(ButtonPress buttonPress) {
        if (buttonPress == ButtonPress.Forward) return myInfo.customControls.walkForward;
        if (buttonPress == ButtonPress.Back) return myInfo.customControls.walkBack;
        if (buttonPress == ButtonPress.Up) return myInfo.customControls.jump;
        if (buttonPress == ButtonPress.Down) return myInfo.customControls.crouch;
        if (buttonPress == ButtonPress.Button1) return myInfo.customControls.button1;
        if (buttonPress == ButtonPress.Button2) return myInfo.customControls.button2;
        if (buttonPress == ButtonPress.Button3) return myInfo.customControls.button3;
        if (buttonPress == ButtonPress.Button4) return myInfo.customControls.button4;
        if (buttonPress == ButtonPress.Button5) return myInfo.customControls.button5;
        if (buttonPress == ButtonPress.Button6) return myInfo.customControls.button6;
        if (buttonPress == ButtonPress.Button7) return myInfo.customControls.button7;
        if (buttonPress == ButtonPress.Button8) return myInfo.customControls.button8;
        if (buttonPress == ButtonPress.Button9) return myInfo.customControls.button9;
        if (buttonPress == ButtonPress.Button10) return myInfo.customControls.button10;
        if (buttonPress == ButtonPress.Button11) return myInfo.customControls.button11;
        if (buttonPress == ButtonPress.Button12) return myInfo.customControls.button12;
        return ButtonPress.Button1;
    }*/

	private void translateInputs(
		IDictionary<InputReferences, InputEvents> previousInputs,
		IDictionary<InputReferences, InputEvents> currentInputs
	){
		if (!introPlayed || !opControlsScript.introPlayed) return;
		if (UFE.config.lockInputs && !UFE.config.roundOptions.allowMovementStart) return;
		if (UFE.config.lockMovements) return;
		
		foreach (InputReferences inputRef in currentInputs.Keys) {
			InputEvents ev = currentInputs[inputRef];
            //if (myInfo.customControls.enabled && myInfo.customControls.overrideInputs) inputRef.engineRelatedButton = characterInputOverride(inputRef.engineRelatedButton);

			if (((inputRef.engineRelatedButton == ButtonPress.Down && ev.axisRaw >= 0)
				|| (inputRef.engineRelatedButton == ButtonPress.Up && ev.axisRaw <= 0))
			    && myPhysicsScript.IsGrounded() 
			    && !myHitBoxesScript.isHit 
			    && currentSubState != SubStates.Stunned){
				currentState = PossibleStates.Stand;
			}
			
            // On Axis Release
			if (inputRef.inputType != InputType.Button && inputHeldDown[inputRef.engineRelatedButton] > 0 && ev.axisRaw == 0) {
				if ((inputRef.engineRelatedButton == ButtonPress.Back && UFE.config.blockOptions.blockType == BlockType.HoldBack)){
					potentialBlock = false;
				}

                // Pressure Sensitive Jump
                if (myInfo.physics.pressureSensitiveJump 
                    && myPhysicsScript.IsGrounded()
                    && myPhysicsScript.isTakingOff
                    && !myPhysicsScript.IsJumping()
                    && inputRef.engineRelatedButton == ButtonPress.Up) {
                    UFE.FindAndRemoveDelaySynchronizedAction(myPhysicsScript.Jump);
                    
                    Fix64 jumpDelaySeconds = (Fix64)myInfo.physics.jumpDelay / (Fix64)UFE.config.fps;
                    Fix64 pressurePercentage = FPMath.Min(inputHeldDown[inputRef.engineRelatedButton] / jumpDelaySeconds, 1);
                    Fix64 newJumpForce = FPMath.Max((myInfo.physics._jumpForce * pressurePercentage), myInfo.physics._minJumpForce);
                    if (newJumpForce < myInfo.physics.minJumpDelay) newJumpForce = myInfo.physics.minJumpDelay;

                    myPhysicsScript.Jump(newJumpForce);

                    //Debug.Log((inputHeldDown[inputRef.engineRelatedButton] * UFE.config.fps) + " - " + pressurePercentage + "% (" + (UFE.ToDouble(myInfo.physics.jumpForce) * pressurePercentage) + ")");
                }

                // Move Execution
                MoveInfo tempMove = myMoveSetScript.GetMove(new ButtonPress[] { inputRef.engineRelatedButton }, inputHeldDown[inputRef.engineRelatedButton], currentMove, true);
				inputHeldDown[inputRef.engineRelatedButton] = 0;
                if (tempMove != null) {
                    storedMove = tempMove;
					storedMoveTime = ((Fix64)UFE.config.executionBufferTime / (Fix64)UFE.config.fps);
					return;
				}
			}

			if (inputHeldDown[inputRef.engineRelatedButton] == 0 && inputRef.inputType != InputType.Button) {
				inputRef.activeIcon = ev.axisRaw > 0? inputRef.inputViewerIcon1 : inputRef.inputViewerIcon2;
			}

			/*if (inputController.GetButtonUp(inputRef)) {
				storedMove = myMoveSetScript.GetMove(new ButtonPress[]{inputRef.engineRelatedButton}, inputHeldDown[inputRef.engineRelatedButton], currentMove, true);
				inputHeldDown[inputRef.engineRelatedButton] = 0;
				if (storedMove != null){
					storedMoveTime = ((float)UFE.config.executionBufferTime / UFE.config.fps);
					return;
				}
			}*/
			
			// On Axis Press
			if (inputRef.inputType != InputType.Button && ev.axisRaw != 0) {
				if (inputRef.inputType == InputType.HorizontalAxis) {
					// Horizontal Movements
					if (ev.axisRaw > 0) {
						if (mirror == 1){
                            inputHeldDown[ButtonPress.Forward] = 0;
                            inputRef.engineRelatedButton = ButtonPress.Back;
                        } else {
                            inputHeldDown[ButtonPress.Back] = 0;
                            inputRef.engineRelatedButton = ButtonPress.Forward;
                        }

						inputHeldDown[inputRef.engineRelatedButton] += UFE.fixedDeltaTime;
						if (inputHeldDown[inputRef.engineRelatedButton] == UFE.fixedDeltaTime && testMoveExecution(inputRef.engineRelatedButton, false)) return;
						
						if (currentState == PossibleStates.Stand 
						    && !isBlocking 
						    && !myPhysicsScript.isTakingOff
						    && !myPhysicsScript.isLanding
						    && currentSubState != SubStates.Stunned
                            && !blockStunned
                            && currentMove == null
                            && myMoveSetScript.basicMoves.moveEnabled) {
							myPhysicsScript.Move(-mirror, ev.axisRaw);
						}
					}
					
					if (ev.axisRaw < 0) {
                        if (mirror == 1) {
                            inputHeldDown[ButtonPress.Back] = 0;
                            inputRef.engineRelatedButton = ButtonPress.Forward;
                        } else {
                            inputHeldDown[ButtonPress.Forward] = 0;
                            inputRef.engineRelatedButton = ButtonPress.Back;
                        }
						//inputRef.engineRelatedButton = mirror == 1? ButtonPress.Foward : ButtonPress.Back;
						inputHeldDown[inputRef.engineRelatedButton] += UFE.fixedDeltaTime;
						if (inputHeldDown[inputRef.engineRelatedButton] == UFE.fixedDeltaTime && testMoveExecution(inputRef.engineRelatedButton, false)) return;
						
						if (currentState == PossibleStates.Stand 
						    && !isBlocking 
						    && !myPhysicsScript.isTakingOff
						    && !myPhysicsScript.isLanding
						    && currentSubState != SubStates.Stunned
                            && !blockStunned
                            && currentMove == null
                            && myMoveSetScript.basicMoves.moveEnabled) {
							myPhysicsScript.Move(mirror, ev.axisRaw);
						}
					}

					// Check for potential blocking
					if (inputRef.engineRelatedButton == ButtonPress.Back 
					    && UFE.config.blockOptions.blockType == BlockType.HoldBack
					    && !myPhysicsScript.isTakingOff
                        && myMoveSetScript.basicMoves.blockEnabled) {
						potentialBlock = true;
					}
					
					// Check for potential parry
					if (((inputRef.engineRelatedButton == ButtonPress.Back && UFE.config.blockOptions.parryType == ParryType.TapBack) ||
					     (inputRef.engineRelatedButton == ButtonPress.Forward && UFE.config.blockOptions.parryType == ParryType.TapForward))
					    && (potentialParry == 0 || UFE.config.blockOptions.easyParry)
					    && inputHeldDown[inputRef.engineRelatedButton] == UFE.fixedDeltaTime
					    && currentMove == null
					    && !isBlocking 
					    && !myPhysicsScript.isTakingOff
					    && currentSubState != SubStates.Stunned
                        && !blockStunned
                        && myMoveSetScript.basicMoves.parryEnabled) {
						potentialParry = UFE.config.blockOptions._parryTiming;
					}

					
				}else {
					// Vertical Movements
					if (ev.axisRaw > 0) {
						inputRef.engineRelatedButton = ButtonPress.Up;
						if (!myPhysicsScript.isTakingOff && !myPhysicsScript.isLanding) {
							if (inputHeldDown[inputRef.engineRelatedButton] == 0) {
								if (!myPhysicsScript.IsGrounded() && myInfo.physics.canJump && myInfo.physics.multiJumps > 1){
									myPhysicsScript.Jump();
								}
								if (testMoveExecution(inputRef.engineRelatedButton, false)) return;
							}

                            if (!myPhysicsScript.freeze
                                && !myPhysicsScript.IsJumping()
                                && storedMove == null
                                && currentMove == null
                                && currentState == PossibleStates.Stand
                                && currentSubState != SubStates.Stunned
                                && !isBlocking
                                && myInfo.physics.canJump
                                && !blockStunned
                                && myMoveSetScript.basicMoves.jumpEnabled) {

                                myPhysicsScript.isTakingOff = true;
                                potentialBlock = false;
                                potentialParry = 0;

                                Fix64 jumpDelaySeconds = (Fix64)myInfo.physics.jumpDelay / (Fix64)UFE.config.fps;
                                UFE.DelaySynchronizedAction(myPhysicsScript.Jump, jumpDelaySeconds);

                                if (myMoveSetScript.AnimationExists(myMoveSetScript.basicMoves.takeOff.name)) {
                                    myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.takeOff);

                                    if (myMoveSetScript.basicMoves.takeOff.autoSpeed) {
                                        myMoveSetScript.SetAnimationSpeed(
                                            myMoveSetScript.basicMoves.takeOff.name,
                                            myMoveSetScript.GetAnimationLength(myMoveSetScript.basicMoves.takeOff.name) / jumpDelaySeconds);
                                    }

                                }
                            }
						}
						inputHeldDown[inputRef.engineRelatedButton] += UFE.fixedDeltaTime;
						
					} else if (ev.axisRaw < 0) {
                        inputRef.engineRelatedButton = ButtonPress.Down;
                        inputHeldDown[inputRef.engineRelatedButton] += UFE.fixedDeltaTime;
                        if (inputHeldDown[inputRef.engineRelatedButton] == UFE.fixedDeltaTime && testMoveExecution(inputRef.engineRelatedButton, false)) return;
                        
						if (!myPhysicsScript.freeze 
						    && myPhysicsScript.IsGrounded() 
						    && currentMove == null 
						    && currentSubState != SubStates.Stunned 
						    && !myPhysicsScript.isTakingOff
                            && !blockStunned
                            && myMoveSetScript.basicMoves.crouchEnabled) {

							currentState = PossibleStates.Crouch;
                            if (!isBlocking) {
								myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.crouching, false);
                            }else {
                                myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.blockingCrouchingPose, false);
                            }
						}
					}
				}

                // Axis + Button Execution
				foreach (InputReferences inputRef2 in currentInputs.Keys) {
					InputEvents ev2 = currentInputs[inputRef2];
					InputEvents p2;
					if (!previousInputs.TryGetValue(inputRef2, out p2)){
						p2 = InputEvents.Default;
					}
					bool button2Down = ev2.button && !p2.button;

                    if (button2Down)
                    {
                        // If its an axis, attempt diagonal input injection
                        if (inputRef2.inputType != InputType.Button)
                        {
                            ButtonPress newInputRefValue = inputRef.engineRelatedButton;
                            if (inputRef2 != inputRef && inputRef2.inputType == InputType.HorizontalAxis)
                            {
                                ButtonPress b2Press = ButtonPress.Back;
                                if ((ev2.axisRaw > 0 && mirror == -1) || (ev2.axisRaw < 0 && mirror == 1))
                                {
                                    b2Press = ButtonPress.Forward;
                                }
                                else if ((ev2.axisRaw < 0 && mirror == -1) || (ev2.axisRaw > 0 && mirror == 1))
                                {
                                    b2Press = ButtonPress.Back;
                                }

                                if (inputRef.engineRelatedButton == ButtonPress.Down && b2Press == ButtonPress.Back)
                                {
                                    newInputRefValue = ButtonPress.DownBack;
                                }
                                else if (inputRef.engineRelatedButton == ButtonPress.Up && b2Press == ButtonPress.Back)
                                {
                                    newInputRefValue = ButtonPress.UpBack;
                                }
                                else if (inputRef.engineRelatedButton == ButtonPress.Down && b2Press == ButtonPress.Forward)
                                {
                                    newInputRefValue = ButtonPress.DownForward;
                                }
                                else if (inputRef.engineRelatedButton == ButtonPress.Up && b2Press == ButtonPress.Forward)
                                {
                                    newInputRefValue = ButtonPress.UpForward;
                                }
                            }
                            else if (inputRef2 != inputRef && inputRef2.inputType == InputType.VerticalAxis)
                            {
                                ButtonPress b2Press = ev2.axisRaw > 0 ? ButtonPress.Up : ButtonPress.Down;

                                if (inputRef.engineRelatedButton == ButtonPress.Back && b2Press == ButtonPress.Down)
                                {
                                    newInputRefValue = ButtonPress.DownBack;
                                }
                                else if (inputRef.engineRelatedButton == ButtonPress.Forward && b2Press == ButtonPress.Down)
                                {
                                    newInputRefValue = ButtonPress.DownForward;
                                }
                                else if (inputRef.engineRelatedButton == ButtonPress.Back && b2Press == ButtonPress.Up)
                                {
                                    newInputRefValue = ButtonPress.UpBack;
                                }
                                else if (inputRef.engineRelatedButton == ButtonPress.Forward && b2Press == ButtonPress.Up)
                                {
                                    newInputRefValue = ButtonPress.UpForward;
                                }
                            }

                            // If the value has changed, send the new axis input
                            if (newInputRefValue != inputRef.engineRelatedButton)
                            {
                                MoveInfo tempMove = myMoveSetScript.GetMove(
                                    new ButtonPress[] { newInputRefValue }, 0, currentMove, false, false);

                                if (tempMove != null)
                                {
                                    storedMove = tempMove;
                                    storedMoveTime = ((Fix64)UFE.config.executionBufferTime / (Fix64)UFE.config.fps);
                                    return;
                                }
                            }
                        }
                        // If its a button, send both axis and button to attempt double input execution
                        else
                        {
                            MoveInfo tempMove = myMoveSetScript.GetMove(
                                new ButtonPress[] { inputRef.engineRelatedButton, inputRef2.engineRelatedButton }, 0, currentMove, false, false);

                            if (tempMove != null)
                            {
                                storedMove = tempMove;
                                storedMoveTime = ((Fix64)UFE.config.executionBufferTime / (Fix64)UFE.config.fps);
                                return;
                            }
                        }
                    }
				}
			}
			
			// Button Press
			if (inputRef.inputType == InputType.Button && !UFE.config.lockInputs){
				InputEvents p;
				if (!previousInputs.TryGetValue(inputRef, out p)){
					p = InputEvents.Default;
				}
				bool buttonDown = ev.button && !p.button;
				bool buttonUp = !ev.button && p.button;


				if (ev.button) {
					if (myMoveSetScript.CompareBlockButtons(inputRef.engineRelatedButton) 
					    && currentSubState != SubStates.Stunned 
					    && !myPhysicsScript.isTakingOff
					    && !blockStunned
                        && myMoveSetScript.basicMoves.blockEnabled) {
						potentialBlock = true;
						CheckBlocking(true);
					}

					if (myMoveSetScript.CompareParryButtons(inputRef.engineRelatedButton) 
					    && inputHeldDown[inputRef.engineRelatedButton] == 0 
					    && potentialParry == 0 
					    && currentMove == null 
					    && !isBlocking 
					    && currentSubState != SubStates.Stunned 
					    && !myPhysicsScript.isTakingOff
					    && !blockStunned
                        && myMoveSetScript.basicMoves.parryEnabled) {
						potentialParry = UFE.config.blockOptions._parryTiming;
					}
					
					inputHeldDown[inputRef.engineRelatedButton] += UFE.fixedDeltaTime;

                    // Plinking
					if (inputHeldDown[inputRef.engineRelatedButton] <= ((Fix64)UFE.config.plinkingDelay/(Fix64)UFE.config.fps)) {
						foreach (InputReferences inputRef2 in currentInputs.Keys) {
							InputEvents ev2 = currentInputs[inputRef2];
							InputEvents p2;
							if (!previousInputs.TryGetValue(inputRef2, out p2)){
								p2 = InputEvents.Default;
							}
							bool button2Down = ev2.button && !p2.button;

							if (inputRef2 != inputRef && inputRef2.inputType == InputType.Button && button2Down) {
                                inputHeldDown[inputRef2.engineRelatedButton] += UFE.fixedDeltaTime;
                                MoveInfo tempMove = myMoveSetScript.GetMove(
									new ButtonPress[]{inputRef.engineRelatedButton, inputRef2.engineRelatedButton}, 0, currentMove, false, true);

                                if (tempMove != null){
                                    if (currentMove != null && currentMove.currentFrame <= UFE.config.plinkingDelay) KillCurrentMove();
                                    storedMove = tempMove;
									storedMoveTime = ((Fix64)UFE.config.executionBufferTime / (Fix64)UFE.config.fps);
									return;
								}
							}
						}
					}
				}
				
				
				if (buttonDown) {
                    MoveInfo tempMove = myMoveSetScript.GetMove(new ButtonPress[] { inputRef.engineRelatedButton }, 0, currentMove, false);
                    if (tempMove != null) {
                        storedMove = tempMove;
						storedMoveTime = ((Fix64)UFE.config.executionBufferTime / (Fix64)UFE.config.fps);
						return;
					}
				}

                if (buttonUp) {
                    inputHeldDown[inputRef.engineRelatedButton] = 0;
                    MoveInfo tempMove = myMoveSetScript.GetMove(new ButtonPress[] { inputRef.engineRelatedButton }, inputHeldDown[inputRef.engineRelatedButton], currentMove, true);
                    if (tempMove != null) {
                        storedMove = tempMove;
						storedMoveTime = ((Fix64)UFE.config.executionBufferTime / (Fix64)UFE.config.fps);
						return;
					}

					if (myMoveSetScript.CompareBlockButtons(inputRef.engineRelatedButton) 
					    && !myPhysicsScript.isTakingOff) {
						potentialBlock = false;
						CheckBlocking(false);
					}
				}
			}
		}
	}

    public void ResetDrainStatus(bool clearGauge) {
        myMoveSetScript.ChangeMoveStances(DCStance);
        if (DCMove != null) CastMove(DCMove, true);

        inhibitGainWhileDraining = false;
        if (gaugeDPS > 0 && (myInfo.currentGaugePoints < 0 || clearGauge)) myInfo.currentGaugePoints = 0;
        gaugeDPS = 0;
        currentDrained = 0;
        totalDrain = 0;
        DCMove = null;
    }
	
	public void ApplyStun(
		IDictionary<InputReferences, InputEvents> previousInputs,
		IDictionary<InputReferences, InputEvents> currentInputs
	){

        if (airRecoveryType == AirRecoveryType.DontRecover 
            && !myPhysicsScript.IsGrounded() 
            && currentSubState == SubStates.Stunned 
            && currentState != PossibleStates.Down) {
			stunTime = 1;
		}else{
			stunTime -= UFE.fixedDeltaTime;
		}

		string standUpAnimation = null;
		Fix64 standUpTime = UFE.config.knockDownOptions.air._standUpTime;
        SubKnockdownOptions knockdownOption = null;

        if (!isDead && currentMove == null && myPhysicsScript.IsGrounded()) {
            // Hit Stun deceleration and knock down algorithms
            if (hitStunDeceleration > -(hitAnimationSpeed / 3) && currentMove == null) {
                hitStunDeceleration -= UFE.fixedDeltaTime;
                myMoveSetScript.SetAnimationSpeed(currentHitAnimation, hitAnimationSpeed + hitStunDeceleration);
            }

			if (currentState == PossibleStates.Down){
                if (myMoveSetScript.basicMoves.standUpFromAirHit.animMap[0].clip != null &&
                    (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitAir, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.fallingFromAirHit, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.fallingFromAirHit, 2)
                    || standUpOverride == StandUpOptions.AirJuggleClip)) {
                    if (stunTime <= UFE.config.knockDownOptions.air._standUpTime) {
                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromAirHit, 1);
                        standUpTime = UFE.config.knockDownOptions.air._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.air;
                    }
                } else if (myMoveSetScript.basicMoves.standUpFromKnockBack.animMap[0].clip != null && 
                    (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitKnockBack, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitKnockBack, 2)
                    || standUpOverride == StandUpOptions.KnockBackClip)) {
                    if (stunTime <= UFE.config.knockDownOptions.air._standUpTime) {
                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromKnockBack, 1);
                        standUpTime = UFE.config.knockDownOptions.air._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.air;
                    }
                } else if (myMoveSetScript.basicMoves.standUpFromStandingHighHit.animMap[0].clip != null && 
                    (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitHighKnockdown, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitHighKnockdown, 2)
                    || standUpOverride == StandUpOptions.HighKnockdownClip)){
					if (stunTime <= UFE.config.knockDownOptions.high._standUpTime){
						standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromStandingHighHit, 1);
                        standUpTime = UFE.config.knockDownOptions.high._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.high;
					}
                } else if (myMoveSetScript.basicMoves.standUpFromStandingMidHit.animMap[0].clip != null && 
                    (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitMidKnockdown, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitMidKnockdown, 2)
                    || standUpOverride == StandUpOptions.LowKnockdownClip)){
					if (stunTime <= UFE.config.knockDownOptions.highLow._standUpTime){
						standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromStandingMidHit, 1);
                        standUpTime = UFE.config.knockDownOptions.highLow._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.highLow;
					}
                } else if (myMoveSetScript.basicMoves.standUpFromSweep.animMap[0].clip != null && 
                    (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitSweep, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitSweep, 2)
                    || standUpOverride == StandUpOptions.SweepClip)){
					if (stunTime <= UFE.config.knockDownOptions.sweep._standUpTime){
						standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromSweep, 1);
                        standUpTime = UFE.config.knockDownOptions.sweep._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.sweep;
                    }
                } else if (myMoveSetScript.basicMoves.standUpFromAirWallBounce.animMap[0].clip != null && 
                    (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.airWallBounce, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.airWallBounce, 2)
                    || standUpOverride == StandUpOptions.AirWallBounceClip)) {
                    if (stunTime <= UFE.config.knockDownOptions.wallbounce._standUpTime) {
                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromAirWallBounce, 1);
                        standUpTime = UFE.config.knockDownOptions.wallbounce._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.wallbounce;
                    }
                } else if (myMoveSetScript.basicMoves.standUpFromGroundBounce.animMap[0].clip != null && 
                    (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.fallingFromGroundBounce, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.groundBounce, 1)
                    || currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.groundBounce, 2)
                    || standUpOverride == StandUpOptions.GroundBounceClip)) {
                    if (stunTime <= UFE.config.knockDownOptions.air._standUpTime) {
                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromGroundBounce, 1);
                        standUpTime = UFE.config.knockDownOptions.air._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.air;
                    }
				} else {
					if (myMoveSetScript.basicMoves.standUp.animMap[0].clip == null)
						Debug.LogError("Stand Up animation not found! Make sure you have it set on Character -> Basic Moves -> Stand Up");
					
					if (stunTime <= UFE.config.knockDownOptions.air._standUpTime){
						standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUp, 1);
                        standUpTime = UFE.config.knockDownOptions.air._standUpTime;
                        knockdownOption = UFE.config.knockDownOptions.air;
					}
				}
            } else if (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.getHitCrumple, 1)
                || standUpOverride == StandUpOptions.CrumpleClip){
				if (stunTime <= UFE.config.knockDownOptions.crumple._standUpTime){
                    if (myMoveSetScript.basicMoves.standUpFromCrumple.animMap[0].clip != null) {
                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromCrumple, 1);
                    } else {
                        if (myMoveSetScript.basicMoves.standUp.animMap[0].clip == null)
                            Debug.LogError("Stand Up animation not found! Make sure you have it set on Character -> Basic Moves -> Stand Up");

                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUp, 1);
                    }
                    standUpTime = UFE.config.knockDownOptions.crumple._standUpTime;
                    knockdownOption = UFE.config.knockDownOptions.crumple;
				}
            } else if (currentHitAnimation == myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standingWallBounceKnockdown, 1)
                || standUpOverride == StandUpOptions.StandingWallBounceClip) {
                if (stunTime <= UFE.config.knockDownOptions.wallbounce._standUpTime) {
                    if (myMoveSetScript.basicMoves.standUpFromStandingWallBounce.animMap[0].clip != null) {
                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUpFromStandingWallBounce, 1);
                    } else {
                        if (myMoveSetScript.basicMoves.standUp.animMap[0].clip == null)
                            Debug.LogError("Stand Up animation not found! Make sure you have it set on Character -> Basic Moves -> Stand Up");

                        standUpAnimation = myMoveSetScript.GetAnimationString(myMoveSetScript.basicMoves.standUp, 1);
                    }
                    standUpTime = UFE.config.knockDownOptions.wallbounce._standUpTime;
                    knockdownOption = UFE.config.knockDownOptions.wallbounce;
                }
            }
		}
		
		if (standUpAnimation != null && !myMoveSetScript.IsAnimationPlaying(standUpAnimation)){
			myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.standUp, standUpAnimation);
            if (myMoveSetScript.basicMoves.standUp.autoSpeed) {
                myMoveSetScript.SetAnimationSpeed(standUpAnimation, myMoveSetScript.GetAnimationLength(standUpAnimation) / standUpTime);
            }
            if (knockdownOption != null && knockdownOption.hideHitBoxes) myHitBoxesScript.HideHitBoxes(true);
		}
		
		if (stunTime <= 0) {
			//if (currentState == PossibleStates.Stand) myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.idle);
			ReleaseStun(previousInputs, currentInputs);
		}
	}
    
    public void CastMove(MoveInfo move, bool overrideCurrentMove = false, bool forceGrounded = false, bool castWarning = false) {
		if (move == null) return;
		if (castWarning && !myMoveSetScript.HasMove(move.moveName)) 
            Debug.LogError("Move '"+ move.name +"' could not be found under this character's move set.");

		if (overrideCurrentMove) {
			KillCurrentMove();
            MoveInfo newMove = myMoveSetScript.InstantiateMove(move);
			this.SetMove(newMove);
			currentMove.currentFrame = 0;
			currentMove.currentTick = 0;
        } else {
            storedMove = myMoveSetScript.InstantiateMove(move);
		}
        if (forceGrounded) myPhysicsScript.ForceGrounded();
	}

	public void SetMove(MoveInfo move){
        if (blockStunned) return;

		currentMove = move;

		foreach (HitBox hitBox in myHitBoxesScript.hitBoxes){
			if (hitBox != null && hitBox.bodyPart != BodyPart.none && hitBox.position != null){
				bool visible = hitBox.defaultVisibility;

				if (move != null && move.bodyPartVisibilityChanges != null){
					foreach (BodyPartVisibilityChange visibilityChange in move.bodyPartVisibilityChanges){
						if (visibilityChange.castingFrame == 0 && visibilityChange.bodyPart == hitBox.bodyPart){
							visible = visibilityChange.visible;
							visibilityChange.casted = true;
						}
					}
				}

				hitBox.position.gameObject.SetActive(visible);
			}
		}

        UFE.FireMove(currentMove, myInfo);
	}

	public void ReadMove(MoveInfo move){
		if (move == null) return;

		potentialParry = 0;
		potentialBlock = false;
        CheckBlocking(false);

		if (move.currentTick == 0) {
			if (!myMoveSetScript.AnimationExists(move.name)) 
				Debug.LogError("Animation for move '"+ move.name +"' not found!");
			
			
			if (move.disableHeadLook) ToggleHeadLook(false);

            if (myPhysicsScript.IsGrounded()) {
                myPhysicsScript.isTakingOff = false;
                myPhysicsScript.isLanding = false;
            }
			
			if (currentState == PossibleStates.NeutralJump ||
			    currentState == PossibleStates.ForwardJump ||
			    currentState == PossibleStates.BackJump){
				myMoveSetScript.totalAirMoves ++;
			}

			Fix64 normalizedTimeConv = myMoveSetScript.GetAnimationNormalizedTime(move.overrideStartupFrame, move);
			
			if (move.overrideBlendingIn) {
				myMoveSetScript.PlayAnimation(move.name, move._blendingIn, normalizedTimeConv);
			}else{
				myMoveSetScript.PlayAnimation(move.name, myInfo._blendingTime, normalizedTimeConv);
			}
            myHitBoxesScript.bakeSpeed = move.animMap.bakeSpeed;
            myHitBoxesScript.animationMaps = move.animMap.animationMaps;
            myHitBoxesScript.UpdateMap(0);

            if (currentMove.invertRotationLeft && mirror == -1) InvertRotation();
			if (currentMove.forceMirrorLeft && mirror == -1) ForceMirror(true);
			
			if (currentMove.invertRotationRight && mirror == 1) InvertRotation();
			if (currentMove.forceMirrorRight && mirror == 1) ForceMirror(false);


			move.currentTick = move.overrideStartupFrame;
			move.currentFrame = move.overrideStartupFrame;
			move.animationSpeedTemp = move._animationSpeed;
			
			myMoveSetScript.SetAnimationSpeed(move.name, move._animationSpeed);
			if (move.overrideBlendingOut) myMoveSetScript.overrideNextBlendingValue = move._blendingOut;
			
			AddGauge(move._gaugeGainOnMiss);
			RemoveGauge(move._gaugeUsage);
            if (move.startDrainingGauge) {
                gaugeDPS = move._gaugeDPS;
                totalDrain = move._totalDrain;
                DCMove = move.DCMove;
                DCStance = move.DCStance;
                inhibitGainWhileDraining = move.inhibitGainWhileDraining;
            }
            if (move.stopDrainingGauge) {
                gaugeDPS = 0;
                inhibitGainWhileDraining = false;
            }
		}

        // Next Tick
        if (myMoveSetScript.animationPaused) {
			move.currentTick += UFE.fixedDeltaTime * UFE.config.fps * myMoveSetScript.GetAnimationSpeed();
		}else{
			move.currentTick += UFE.fixedDeltaTime * UFE.config.fps;
		}
        
		
		// Assign Current Frame Data Description
		if (move.currentFrame <= move.startUpFrames) {
			move.currentFrameData = CurrentFrameData.StartupFrames;
		}else if (move.currentFrame > move.startUpFrames && move.currentFrame <= move.startUpFrames + move.activeFrames) {
			move.currentFrameData = CurrentFrameData.ActiveFrames;
		}else{
			move.currentFrameData = CurrentFrameData.RecoveryFrames;
		}

        // Check Speed Key Frames
        if (!move.fixedSpeed) {
            foreach (AnimSpeedKeyFrame speedKeyFrame in move.animSpeedKeyFrame) {
                if (move.currentFrame >= speedKeyFrame.castingFrame
                    && !myPhysicsScript.freeze) {
                    myMoveSetScript.SetAnimationSpeed(move.name, speedKeyFrame._speed * move._animationSpeed);
                }
            }
        }
		
		// Check Projectiles
		foreach (Projectile projectile in move.projectiles){
			if(
				!projectile.casted && 
				projectile.projectilePrefab != null &&
				move.currentFrame >= projectile.castingFrame
			){
				projectile.casted = true;
				projectile.gaugeGainOnHit = move._gaugeGainOnHit;
				projectile.gaugeGainOnBlock = move._gaugeGainOnBlock;
				projectile.opGaugeGainOnHit = move._opGaugeGainOnHit;
				projectile.opGaugeGainOnBlock = move._opGaugeGainOnBlock;
				projectile.opGaugeGainOnParry = move._opGaugeGainOnParry;
				
				FPVector newPos = myHitBoxesScript.GetPosition(projectile.bodyPart);
				if (projectile.fixedZAxis) newPos.z = 0;
                long durationFrames = Mathf.RoundToInt(projectile.duration * UFE.config.fps);
                GameObject pTemp = UFE.SpawnGameObject(projectile.projectilePrefab, newPos.ToVector(), Quaternion.identity, durationFrames);
                Vector3 newRotation = projectile.projectilePrefab.transform.rotation.eulerAngles;
                newRotation.z = projectile.directionAngle;
                pTemp.transform.rotation = Quaternion.Euler(newRotation);

                ProjectileMoveScript projectileMoveScript = pTemp.AddComponent<ProjectileMoveScript>();
                projectileMoveScript.data = projectile;
				projectileMoveScript.myControlsScript = this;
				projectileMoveScript.mirror = mirror;

                projectileMoveScript.fpTransform = pTemp.AddComponent<FPTransform>();
                projectileMoveScript.fpTransform.position = newPos;

                projectileMoveScript.transform.parent = UFE.gameEngine.transform;
				projectiles.Add(projectileMoveScript);
			}
		}
		
		// Check Particle Effects
		foreach (MoveParticleEffect particleEffect in move.particleEffects){
			if (
				!particleEffect.casted
                && particleEffect.particleEffect.prefab != null 
                && move.currentFrame >=  particleEffect.castingFrame
			){
				particleEffect.casted = true;
                UFE.FireParticleEffects(currentMove, myInfo, particleEffect);
                
                long frames = particleEffect.particleEffect.destroyOnMoveOver? (move.totalFrames - move.currentFrame) : Mathf.RoundToInt(particleEffect.particleEffect.duration * UFE.config.fps);
                Quaternion newRotation = particleEffect.particleEffect.initialRotation != Vector3.zero ? Quaternion.Euler(particleEffect.particleEffect.initialRotation) : Quaternion.identity;
                GameObject pTemp = UFE.SpawnGameObject(particleEffect.particleEffect.prefab, Vector3.zero, newRotation, frames);
                pTemp.transform.rotation = particleEffect.particleEffect.prefab.transform.rotation;

                if (particleEffect.particleEffect.stick) {
                    Transform targetTransform = myHitBoxesScript.GetTransform(particleEffect.particleEffect.bodyPart);
                    pTemp.transform.SetParent(targetTransform);
                    pTemp.transform.position = Vector3.zero;
                    if (particleEffect.particleEffect.followRotation) pTemp.AddComponent<StickyGameObject>();

                } else {
                    pTemp.transform.SetParent(UFE.gameEngine.transform);
                    pTemp.transform.position = myHitBoxesScript.GetPosition(particleEffect.particleEffect.bodyPart).ToVector();
                }

                if (particleEffect.particleEffect.lockLocalPosition) pTemp.transform.localPosition = Vector3.zero;

                Vector3 newPosition = Vector3.zero;
                newPosition.x = particleEffect.particleEffect.positionOffSet.x * -mirror;
                newPosition.y = particleEffect.particleEffect.positionOffSet.y;
                newPosition.z = particleEffect.particleEffect.positionOffSet.z;
                pTemp.transform.localPosition += newPosition;
			}
		}
		
		// Check Applied Forces
		foreach (AppliedForce addedForce in move.appliedForces){
			if (!addedForce.casted && move.currentFrame >= addedForce.castingFrame){
				myPhysicsScript.ResetForces(addedForce.resetPreviousHorizontal, addedForce.resetPreviousVertical);
				myPhysicsScript.AddForce(new FPVector(addedForce._force.x, addedForce._force.y, 0), -mirror);
				addedForce.casted = true;
			}
		}

        // Check Body Part Visibility Changes
        foreach (BodyPartVisibilityChange visibilityChange in move.bodyPartVisibilityChanges) {
            if (!visibilityChange.casted && move.currentFrame >= visibilityChange.castingFrame) {
				foreach (HitBox hitBox in myHitBoxesScript.hitBoxes) {
					if (visibilityChange.bodyPart == hitBox.bodyPart && 
                        ((mirror == - 1 && visibilityChange.left) || (mirror == 1 && visibilityChange.right))) {

                        UFE.FireBodyVisibilityChange(currentMove, myInfo, visibilityChange, hitBox);
						hitBox.position.gameObject.SetActive(visibilityChange.visible);
						visibilityChange.casted = true;
					}
				}
            }
        }

		// Check SlowMo Effects
		foreach (SlowMoEffect slowMoEffect in move.slowMoEffects){
			if (!slowMoEffect.casted && move.currentFrame >= slowMoEffect.castingFrame){
				UFE.timeScale = (slowMoEffect._percentage / 100) * UFE.config._gameSpeed;
				UFE.DelaySynchronizedAction(UFE.fluxCapacitor.ReturnTimeScale, slowMoEffect._duration);
				slowMoEffect.casted = true;
			}
		}
		
		// Check Sound Effects
		foreach (SoundEffect soundEffect in move.soundEffects){
			if (!soundEffect.casted && move.currentFrame >= soundEffect.castingFrame){
				UFE.PlaySound(soundEffect.sounds);
				soundEffect.casted = true;
			}
		}
		
		// Check In Game Alert
		foreach (InGameAlert inGameAlert in move.inGameAlert){
			if (!inGameAlert.casted && move.currentFrame >= inGameAlert.castingFrame){
				UFE.FireAlert(inGameAlert.alert, myInfo);
				inGameAlert.casted = true;
			}
		}
		
		// Change Stances
		foreach (StanceChange stanceChange in move.stanceChanges){
			if (!stanceChange.casted && move.currentFrame >= stanceChange.castingFrame){
				myMoveSetScript.ChangeMoveStances(stanceChange.newStance);
				stanceChange.casted = true;
			}
        }

        // Check Opponent Override
		foreach (OpponentOverride opponentOverride in move.opponentOverride){
			if (!opponentOverride.casted && move.currentFrame >= opponentOverride.castingFrame){
				if (opponentOverride.stun){
					opControlsScript.stunTime = opponentOverride._stunTime/(Fix64)UFE.config.fps;
					if (opControlsScript.stunTime > 0) opControlsScript.currentSubState = SubStates.Stunned;
				}
				
				opControlsScript.KillCurrentMove();
				foreach(CharacterSpecificMoves csMove in opponentOverride.characterSpecificMoves){
					if (opInfo.characterName == csMove.characterName) {
						opControlsScript.CastMove(csMove.move, true);
						if (opponentOverride.stun) opControlsScript.currentMove.standUpOptions = opponentOverride.standUpOptions;
						opControlsScript.currentMove.hitAnimationOverride = opponentOverride.overrideHitAnimations;
					}
				}
				if (opControlsScript.currentMove == null && opponentOverride.move != null){
					opControlsScript.CastMove(opponentOverride.move, true);
					if (opponentOverride.stun) opControlsScript.currentMove.standUpOptions = opponentOverride.standUpOptions;
					opControlsScript.currentMove.hitAnimationOverride = opponentOverride.overrideHitAnimations;
				}
				
				opControlsScript.activePullIn = new PullIn();
				FPVector newPos = opponentOverride._position;
				newPos.x *= -mirror;
				opControlsScript.activePullIn.position = worldTransform.position + newPos;
				opControlsScript.activePullIn.speed = opponentOverride.blendSpeed;
				
				if (opponentOverride.resetAppliedForces){
					opPhysicsScript.ResetForces(true, true);
					myPhysicsScript.ResetForces(true, true);
				}
				
				opponentOverride.casted = true;
			}
		}
		
		// Check Camera Movements (cinematics)
		foreach (CameraMovement cameraMovement in move.cameraMovements){
			if (cameraMovement.over) continue;
			if (cameraMovement.casted && !cameraMovement.over && cameraMovement.time >= cameraMovement._duration && UFE.freeCamera){
				cameraMovement.over = true;
				ReleaseCam();
			}
			if (move.currentFrame >= cameraMovement.castingFrame){
				cameraMovement.time += UFE.fixedDeltaTime;
				if (cameraMovement.casted) continue;
				cameraMovement.casted = true;
				
				PausePlayAnimation(true, cameraMovement._myAnimationSpeed * .01);
				opControlsScript.PausePlayAnimation(true, cameraMovement._opAnimationSpeed * .01);
                UFE.freezePhysics = cameraMovement.freezePhysics;
				myPhysicsScript.freeze = cameraMovement.freezePhysics;
				opPhysicsScript.freeze = cameraMovement.freezePhysics;
				cameraScript.cinematicFreeze = cameraMovement.freezePhysics;
				
				if (cameraMovement.cinematicType == CinematicType.CameraEditor){
					cameraMovement.position.x *= -mirror;
					Vector3 targetPosition = transform.TransformPoint(cameraMovement.position);
					Vector3 targetRotation = cameraMovement.rotation;
					targetRotation.y *= -mirror;
					targetRotation.z *= -mirror;
					cameraScript.MoveCameraToLocation(targetPosition,
					                                  targetRotation,
					                                  cameraMovement.fieldOfView,
					                                  cameraMovement.camSpeed, gameObject.name);
					
				}else if (cameraMovement.cinematicType == CinematicType.Prefab){
					cameraScript.SetCameraOwner(gameObject.name);
                    emulatedCam = UFE.SpawnGameObject(cameraMovement.prefab, transform.position, Quaternion.identity);
					
				}else if (cameraMovement.cinematicType == CinematicType.AnimationFile){
					emulatedCam = new GameObject();
					emulatedCam.name = "Camera Parent";
					emulatedCam.transform.parent = transform;
					emulatedCam.transform.localPosition = cameraMovement.gameObjectPosition;
					emulatedCam.AddComponent(typeof(Animation));
					emulatedCam.GetComponent<Animation>().AddClip(cameraMovement.animationClip, "cam");
					emulatedCam.GetComponent<Animation>()["cam"].speed = cameraMovement.camAnimationSpeed;
					emulatedCam.GetComponent<Animation>().Play("cam");
					
					Camera.main.transform.parent = emulatedCam.transform;
					cameraScript.MoveCameraToLocation(cameraMovement.position,
					                                  cameraMovement.rotation,
					                                  cameraMovement.fieldOfView,
					                                  cameraMovement.blendSpeed, gameObject.name);
					
				}
			}
		}
		
		// Check Invincible Body Parts
		if (move.invincibleBodyParts.Length > 0) {
			foreach (InvincibleBodyParts invBodyPart in move.invincibleBodyParts){
				if (move.currentFrame >= invBodyPart.activeFramesBegin &&
				    move.currentFrame < invBodyPart.activeFramesEnds) {
					if (invBodyPart.completelyInvincible){
						myHitBoxesScript.HideHitBoxes(true);
					}else{
						myHitBoxesScript.HideHitBoxes(invBodyPart.hitBoxes, true);
					}
					ignoreCollisionMass = invBodyPart.ignoreBodyColliders;
				}
				if (move.currentFrame >= invBodyPart.activeFramesEnds) {
					if (invBodyPart.completelyInvincible){
						myHitBoxesScript.HideHitBoxes(false);
					}else{
						myHitBoxesScript.HideHitBoxes(invBodyPart.hitBoxes, false);
					}
					ignoreCollisionMass = false;
				}
			}
		}
		
		// Check Blockable Area
		if (move.blockableArea.bodyPart != BodyPart.none){
			if (move.currentFrame >= move.blockableArea.activeFramesBegin &&
			    move.currentFrame < move.blockableArea.activeFramesEnds) {
				myHitBoxesScript.blockableArea = move.blockableArea;
				myHitBoxesScript.blockableArea.position = myHitBoxesScript.GetPosition(myHitBoxesScript.blockableArea.bodyPart);

				if (!opControlsScript.isBlocking
				    && !opControlsScript.blockStunned
				    && opControlsScript.currentSubState != SubStates.Stunned
				    && opHitBoxesScript.TestCollision(myHitBoxesScript.blockableArea).Length > 0) {
					opControlsScript.CheckBlocking(true);
				}
			}else if (move.currentFrame >= move.blockableArea.activeFramesEnds){
				if (UFE.config.blockOptions.blockType == BlockType.HoldBack ||
				    UFE.config.blockOptions.blockType == BlockType.AutoBlock) opControlsScript.CheckBlocking(false);
			}
		}
		
		// Check Frame Links
		foreach (FrameLink frameLink in move.frameLinks){
            if (move.currentFrame >= frameLink.activeFramesBegins &&
                move.currentFrame <= frameLink.activeFramesEnds) {
                if (frameLink.linkType == LinkType.NoConditions ||
                    (frameLink.linkType == LinkType.HitConfirm &&
                    ((currentMove.hitConfirmOnStrike && frameLink.onStrike) ||
                    (currentMove.hitConfirmOnBlock && frameLink.onBlock) ||
                    (currentMove.hitConfirmOnParry && frameLink.onParry)))) {
                        frameLink.cancelable = true;
                }
            } else {
                frameLink.cancelable = false;
            }
		}

        // Check Hits
        CheckHits(move);


        // Next Frame
        //if (move.currentTick >= move.currentFrame) move.currentFrame++;
        move.currentFrame = (int)FPMath.Floor(move.currentTick);

        // Kill Move
        if (move.currentFrame >= move.totalFrames) {
			if (move.name == "Intro") {
				introPlayed = true;
				UFE.CastNewRound();
			}
            if (move.armorOptions.hitsTaken > 0) comboHits = 0;

			KillCurrentMove();
		}
	}

    public void CheckHits(MoveInfo move) {
        HurtBox[] activeHurtBoxes = null;
        foreach (Hit hit in move.hits)
        {
            if (move.currentFrame >= hit.activeFramesBegin &&
                move.currentFrame <= hit.activeFramesEnds)
            {
                if (hit.hurtBoxes.Length > 0)
                {
                    activeHurtBoxes = hit.hurtBoxes;
                    if ((hit.disabled && !hit.continuousHit) || (hit.continuousHit && move.currentTick < move.currentFrame)) continue;
                    if (!opControlsScript.ValidateHit(hit)) continue;

                    foreach (HurtBox hurtBox in activeHurtBoxes)
                    {
                        hurtBox.position = myHitBoxesScript.GetPosition(hurtBox.bodyPart);
                        hurtBox.rendererBounds = myHitBoxesScript.GetBounds();
                    }

                    FPVector[] collisionVectors = opHitBoxesScript.TestCollision(activeHurtBoxes, hit.hitConfirmType);
                    if (collisionVectors.Length > 0)
                    { // HURTBOX TEST
                        Fix64 newAnimSpeed = GetHitAnimationSpeed(hit.hitStrength);
                        Fix64 freezingTime = GetHitFreezingTime(hit.hitStrength);

                        // Tech Throw
                        if (hit.hitConfirmType == HitConfirmType.Throw
                            && hit.techable
                            && opControlsScript.currentMove != null
                            && opControlsScript.currentMove.IsThrow(true)
                            )
                        {
                            CastMove(hit.techMove, true);
                            opControlsScript.CastMove(opControlsScript.currentMove.GetTechMove(), true);
                            return;

                        // Throw
                        }
                        else if (hit.hitConfirmType == HitConfirmType.Throw)
                        {
                            CastMove(hit.throwMove, true);
                            return;

                        // Parry
                        }
                        else if (opControlsScript.potentialParry > 0
                                 && opControlsScript.currentMove == null
                                 && hit.hitConfirmType != HitConfirmType.Throw
                                 && opControlsScript.TestParryStances(hit.hitType)
                                 )
                        {
                            opControlsScript.GetHitParry(hit, move.totalFrames - move.currentFrame, collisionVectors);
                            opControlsScript.AddGauge(move._opGaugeGainOnParry);
                            move.hitConfirmOnParry = true;

                        // Block
                        }
                        else if (opControlsScript.currentSubState != SubStates.Stunned
                                 && opControlsScript.currentMove == null
                                 && opControlsScript.isBlocking
                                 && opControlsScript.TestBlockStances(hit.hitType)
                                 && !hit.unblockable
                                 )
                        {
                            opControlsScript.GetHitBlocking(hit, move.totalFrames - move.currentFrame, collisionVectors);
                            AddGauge(move._gaugeGainOnBlock);
                            opControlsScript.AddGauge(move._opGaugeGainOnBlock);
                            move.hitConfirmOnBlock = true;

                            if (hit.overrideHitEffectsBlock)
                            {
                                newAnimSpeed = hit.hitEffectsBlock._animationSpeed;
                                freezingTime = hit.hitEffectsBlock._freezingTime;
                            }

                        // Hit
                        }
                        else
                        {
                            opControlsScript.GetHit(hit, move.totalFrames - move.currentFrame, collisionVectors);
                            AddGauge(move._gaugeGainOnHit);
                            opControlsScript.AddGauge(move._opGaugeGainOnHit);

                            if (hit.pullSelfIn.enemyBodyPart != BodyPart.none && hit.pullSelfIn.characterBodyPart != BodyPart.none)
                            {
                                FPVector newPos = opHitBoxesScript.GetPosition(hit.pullSelfIn.enemyBodyPart);
                                if (newPos != FPVector.zero)
                                {
                                    activePullIn = new PullIn();
                                    activePullIn.position = worldTransform.position + (newPos - myHitBoxesScript.GetPosition(hit.pullSelfIn.characterBodyPart));
                                    activePullIn.speed = hit.pullSelfIn.speed;
                                    activePullIn.forceStand = hit.pullEnemyIn.forceStand;
                                    activePullIn.position.z = 0;
                                    if (hit.pullEnemyIn.forceStand)
                                    {
                                        activePullIn.position.y = 0;
                                        myPhysicsScript.ForceGrounded();
                                    }
                                }
                            }
                            move.hitConfirmOnStrike = true;

                            if (hit.overrideHitEffects)
                            {
                                newAnimSpeed = hit.hitEffects._animationSpeed;
                                freezingTime = hit.hitEffects._freezingTime;
                            }
                        }
                        myPhysicsScript.ResetForces(hit.resetPreviousHorizontal, hit.resetPreviousVertical);
                        myPhysicsScript.AddForce(hit._appliedForce, -mirror);

                        // Test position boundaries
                        if ((opControlsScript.worldTransform.position.x >= UFE.config.selectedStage._rightBoundary - 2 ||
                             opControlsScript.worldTransform.position.x <= UFE.config.selectedStage._leftBoundary + 2)
                            && myPhysicsScript.IsGrounded()
                            && !UFE.config.comboOptions.neverCornerPush && hit.cornerPush
                            )
                        {

                            myPhysicsScript.ResetForces(hit.resetPreviousHorizontalPush, false);
                            myPhysicsScript.AddForce(
                                new FPVector(hit._pushForce.x + ((Fix64)opPhysicsScript.airTime * opInfo.physics._friction), 0, 0), mirror);
                        }

                        // Apply freezing effect
                        if (opPhysicsScript.freeze)
                        {
                            HitPause(newAnimSpeed * .01);
                            UFE.DelaySynchronizedAction(this.HitUnpause, freezingTime);
                        }

                        hit.disabled = true;
                    };
                }
            }
            myHitBoxesScript.activeHurtBoxes = activeHurtBoxes;
        }

    }


    // Imediately cancels any move being executed
    public void KillCurrentMove(){
		if (currentMove == null) return;
		currentMove.currentFrame = 0;
		currentMove.currentTick = 0;

		myHitBoxesScript.activeHurtBoxes = null;
		myHitBoxesScript.blockableArea = null;
		//myHitBoxesScript.HideHitBoxes(false);
		ignoreCollisionMass = false;
		if (UFE.config.blockOptions.blockType == BlockType.HoldBack ||
		    UFE.config.blockOptions.blockType == BlockType.AutoBlock) opControlsScript.CheckBlocking(false);
        
		if (currentMove.disableHeadLook) ToggleHeadLook(true);

		if (currentMove.invertRotationLeft && mirror == -1) InvertRotation();
		if (currentMove.forceMirrorLeft && mirror == -1) ForceMirror(false);

		if (currentMove.invertRotationRight && mirror == 1) InvertRotation();
		if (currentMove.forceMirrorRight && mirror == 1) ForceMirror(true);
		
		testCharacterRotation(100);

		if (stunTime > 0){
			standUpOverride = currentMove.standUpOptions;
			if (standUpOverride != StandUpOptions.None) currentState = PossibleStates.Down;
		}

		this.SetMove(null);
		ReleaseCam();
	}

	// Release character to be playable again
	private void ReleaseStun(
		IDictionary<InputReferences, InputEvents> previousInputs,
		IDictionary<InputReferences, InputEvents> currentInputs
	){
		if (currentSubState != SubStates.Stunned && !blockStunned) return;
		if (!isBlocking && comboHits > 1 && UFE.config.comboOptions.comboDisplayMode == ComboDisplayMode.ShowAfterComboExecution){
			UFE.FireAlert(UFE.config.selectedLanguage.combo, opInfo);
		}
        currentHit = null;
		currentSubState = SubStates.Resting;
		blockStunned = false;
		stunTime = 0;
		comboHits = 0;
		comboDamage = 0;
		comboHitDamage = 0;
		airJuggleHits = 0;
        consecutiveCrumple = 0;
        CheckBlocking(false);

        standUpOverride = StandUpOptions.None;

        myPhysicsScript.ResetWeight();
        myPhysicsScript.isWallBouncing = false;
        myPhysicsScript.wallBounceTimes = 0;
        myPhysicsScript.overrideStunAnimation = null;
        myPhysicsScript.overrideAirAnimation = false;

        if (!myPhysicsScript.IsGrounded()) isAirRecovering = true;

		if (!isDead) ToggleHeadLook(true);

		if (myPhysicsScript.IsGrounded()) currentState = PossibleStates.Stand;
		translateInputs(previousInputs, currentInputs);
	}

	private void ReleaseCam(){
		if (cameraScript.GetCameraOwner() != gameObject.name) return;
		if (outroPlayed && UFE.config.roundOptions.freezeCamAfterOutro) return;
		Camera.main.transform.parent = null;

		if (emulatedCam != null){
			UFE.DestroyGameObject(emulatedCam);
		}

		opControlsScript.PausePlayAnimation(false);
		PausePlayAnimation(false);
		cameraScript.ReleaseCam();
        UFE.freezePhysics = false;
		myPhysicsScript.freeze = false;
		opPhysicsScript.freeze = false;
	}

	public bool TestBlockStances(HitType hitType){
		if (UFE.config.blockOptions.blockType == BlockType.None) return false;
		if ((hitType == HitType.Mid || hitType == HitType.MidKnockdown || hitType == HitType.Launcher) && myPhysicsScript.IsGrounded()) return true;
		if ((hitType == HitType.Overhead || hitType == HitType.HighKnockdown) && currentState == PossibleStates.Crouch) return false;
		if ((hitType == HitType.Sweep || hitType == HitType.Low) && currentState != PossibleStates.Crouch) return false;
		if (!UFE.config.blockOptions.allowAirBlock && !myPhysicsScript.IsGrounded()) return false;
		return true;
	}
	
	public bool TestParryStances(HitType hitType){
		if (UFE.config.blockOptions.parryType == ParryType.None) return false;
		if ((hitType == HitType.Mid || hitType == HitType.MidKnockdown || hitType == HitType.Launcher) && myPhysicsScript.IsGrounded()) return true;
		if ((hitType == HitType.Overhead || hitType == HitType.HighKnockdown) && currentState == PossibleStates.Crouch) return false;
		if ((hitType == HitType.Sweep || hitType == HitType.Low) && currentState != PossibleStates.Crouch) return false;
		if (!UFE.config.blockOptions.allowAirParry && !myPhysicsScript.IsGrounded()) return false;
		return true;
	}
	
	public void CheckBlocking(bool flag){
		if (myPhysicsScript.freeze) return;
		if (myPhysicsScript.isTakingOff) return;
		if (flag){
			if (potentialBlock){
				if (currentMove != null) {
					potentialBlock = false;
					return;
				}
				if (currentState == PossibleStates.Crouch) {
					if (myMoveSetScript.basicMoves.blockingCrouchingPose.animMap[0].clip == null)
						Debug.LogError("Blocking Crouching Pose animation not found! Make sure you have it set on Character -> Basic Moves -> Blocking Crouching Pose");
					myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.blockingCrouchingPose, false);
					isBlocking = true;
				}else if (currentState == PossibleStates.Stand) {
					if (myMoveSetScript.basicMoves.blockingHighPose.animMap[0].clip == null)
						Debug.LogError("Blocking High Pose animation not found! Make sure you have it set on Character -> Basic Moves -> Blocking High Pose");
					myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.blockingHighPose, false);
					isBlocking = true;
				}else if (!myPhysicsScript.IsGrounded() && UFE.config.blockOptions.allowAirBlock) {
					if (myMoveSetScript.basicMoves.blockingAirPose.animMap[0].clip == null)
						Debug.LogError("Blocking Air Pose animation not found! Make sure you have it set on Character -> Basic Moves -> Blocking Air Pose");
					myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.blockingAirPose, false);
					isBlocking = true;
				}
			}
		}else if (!blockStunned){
			isBlocking = false;
		}
	}
	
	private void HighlightOn(GameObject target, bool flag){
		Renderer[] charRenders = target.GetComponentsInChildren<Renderer>();
		if (flag && !lit){
			lit = true;
			foreach(Renderer charRender in charRenders){
				charRender.material.shader = Shader.Find("VertexLit");
				charRender.material.color = UFE.config.blockOptions.parryColor;
			}
		}else if (lit){
			lit = false;
			for(int i = 0; i < charRenders.Length; i ++){
				charRenders[i].material.shader = normalShaders[i];
				charRenders[i].material.color = normalColors[i];
			}
		}
	}
	
	private void HighlightOff(){
		HighlightOn(character, false);
	}

	public bool ValidateHit(Hit hit){
		if (comboHits >= UFE.config.comboOptions.maxCombo) return false;
		if (!hit.groundHit && myPhysicsScript.IsGrounded()) return false;
        if (!hit.crouchingHit && currentState == PossibleStates.Crouch) return false;
        if (!hit.airHit && currentState != PossibleStates.Stand && currentState != PossibleStates.Crouch && !myPhysicsScript.IsGrounded()) return false;
		if (!hit.stunHit && currentSubState == SubStates.Stunned) return false;
		if (!hit.downHit && currentState == PossibleStates.Down) return false;
        if (!myMoveSetScript.ValidadeBasicMove(hit.opponentConditions, this)) return false;
        if (!myMoveSetScript.ValidateMoveStances(hit.opponentConditions, this)) return false;

		return true;
	}

	public void GetHitParry(Hit hit, int remainingFrames, FPVector[] location){
		UFE.FireAlert(UFE.config.selectedLanguage.parry, myInfo);

		BasicMoveInfo currentHitInfo = myMoveSetScript.basicMoves.parryHigh;
		blockStunned = true;
		currentSubState = SubStates.Blocking;

		myHitBoxesScript.isHit = true;

        if (!UFE.config.blockOptions.easyParry) {
            potentialParry = 0;
        }

		if (UFE.config.blockOptions.resetButtonSequence){
			myMoveSetScript.ClearLastButtonSequence();
		}

		if (UFE.config.blockOptions.parryStunType == ParryStunType.Fixed){
			stunTime = (Fix64)UFE.config.blockOptions.parryStunFrames/(Fix64)UFE.config.fps;
		}else{
			int stunFrames = 0;
			if (hit.hitStunType == HitStunType.FrameAdvantage) {
				stunFrames = hit.frameAdvantageOnBlock + remainingFrames;
				stunFrames *= (UFE.config.blockOptions.parryStunFrames/100);
				if (stunFrames < 1) stunFrames = 1;
				stunTime = (Fix64)stunFrames/(Fix64)UFE.config.fps;
			}else if (hit.hitStunType == HitStunType.Frames) {
				stunFrames = (int) hit._hitStunOnBlock;
				stunFrames = (int)FPMath.Round(((Fix64)(stunFrames * UFE.config.blockOptions.parryStunFrames)/(Fix64)100));
				if (stunFrames < 1) stunFrames = 1;
				stunTime = (Fix64)stunFrames/(Fix64)UFE.config.fps;
			}else{
				stunTime = hit._hitStunOnBlock * ((Fix64)UFE.config.blockOptions.parryStunFrames/ (Fix64)100);
			}
		}

        UFE.FireParry(myHitBoxesScript.GetStrokeHitBox(), opControlsScript.currentMove, myInfo);

		// Create hit parry effect
		GameObject particle = UFE.config.blockOptions.parryHitEffects.hitParticle;
        Fix64 killTime = UFE.config.blockOptions.parryHitEffects.killTime;
		AudioClip soundEffect = UFE.config.blockOptions.parryHitEffects.hitSound;
		if (location.Length > 0 && particle != null){
            HitEffectSpawnPoint spawnPoint = UFE.config.blockOptions.parryHitEffects.spawnPoint;
            if (hit.overrideEffectSpawnPoint) spawnPoint = hit.spawnPoint;

            long frames = (long)FPMath.Round(killTime * UFE.config.fps);
            GameObject pTemp = UFE.SpawnGameObject(particle, GetParticleSpawnPoint(spawnPoint, location), Quaternion.identity, frames);
            pTemp.transform.rotation = particle.transform.rotation;

            if (UFE.config.blockOptions.parryHitEffects.mirrorOn2PSide && mirror > 0) {
                pTemp.transform.localEulerAngles = new Vector3(pTemp.transform.localEulerAngles.x, pTemp.transform.localEulerAngles.y + 180, pTemp.transform.localEulerAngles.z);
            }

			//pTemp.transform.localScale = new Vector3(-mirror, 1, 1);
            pTemp.transform.parent = UFE.gameEngine.transform;
		}
		UFE.PlaySound(soundEffect);
		
		// Shake Options
		shakeCamera = UFE.config.blockOptions.parryHitEffects.shakeCameraOnHit;
		shakeCharacter = UFE.config.blockOptions.parryHitEffects.shakeCharacterOnHit;
		shakeDensity = UFE.config.blockOptions.parryHitEffects._shakeDensity;
        shakeCameraDensity = UFE.config.blockOptions.parryHitEffects._shakeCameraDensity;


		
		// Get correct animation according to stance
        if (currentState == PossibleStates.Crouch) {
            currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.parryCrouching, hit);
			currentHitInfo = myMoveSetScript.basicMoves.parryCrouching;
			if (!myMoveSetScript.AnimationExists(currentHitAnimation))
                Debug.LogError("Parry Crouching animation not found! Make sure you have it set on Character -> Basic Moves -> Parry Animations -> Crouching");
		}else if (currentState == PossibleStates.Stand){
            HitBox strokeHit = myHitBoxesScript.GetStrokeHitBox();
            if (strokeHit.type == HitBoxType.low && myMoveSetScript.basicMoves.parryLow.animMap[0].clip != null) {
                currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.parryLow, hit);
                currentHitInfo = myMoveSetScript.basicMoves.parryLow;

            } else {
                currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.parryHigh, hit);
                currentHitInfo = myMoveSetScript.basicMoves.parryHigh;
                if (!myMoveSetScript.AnimationExists(currentHitAnimation))
                    Debug.LogError("Parry High animation not found! Make sure you have it set on Character -> Basic Moves -> Parry Animations -> Standing");

            }
        } else if (!myPhysicsScript.IsGrounded()) {
            currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.parryAir, hit);
			currentHitInfo = myMoveSetScript.basicMoves.parryAir;
			if (!myMoveSetScript.AnimationExists(currentHitAnimation))
                Debug.LogError("Parry Air animation not found! Make sure you have it set on Character -> Basic Moves -> Parry Animations -> Air");
		}

		myMoveSetScript.PlayBasicMove(currentHitInfo, currentHitAnimation);
        if (currentHitInfo.autoSpeed) {
            myMoveSetScript.SetAnimationSpeed(currentHitAnimation, (myMoveSetScript.GetAnimationLength(currentHitAnimation) / stunTime));
        }
		
		// Highlight effect when parry
		if (UFE.config.blockOptions.highlightWhenParry){
			HighlightOn(gameObject, true);
			UFE.DelaySynchronizedAction(this.HighlightOff, 0.2);
		}
		
		// Freeze screen depending on how strong the hit was
		HitPause(GetHitAnimationSpeed(hit.hitStrength) * .01);
		UFE.DelaySynchronizedAction(this.HitUnpause, GetHitFreezingTime(hit.hitStrength));

        // Reset hit to allow for another hit while the character is still stunned
        Fix64 spaceBetweenHits = 1;
		if (hit.spaceBetweenHits == Sizes.Small){
			spaceBetweenHits = 1.1;
		}else if (hit.spaceBetweenHits == Sizes.Medium){
			spaceBetweenHits = 1.3;
		}else if (hit.spaceBetweenHits == Sizes.High){
			spaceBetweenHits = 1.7;
		}

        if (UFE.config.blockOptions.parryHitEffects.autoHitStop) {
            UFE.DelaySynchronizedAction(myHitBoxesScript.ResetHit, GetHitFreezingTime(hit.hitStrength) * spaceBetweenHits);
        } else {
            UFE.DelaySynchronizedAction(myHitBoxesScript.ResetHit, UFE.config.blockOptions.parryHitEffects._hitStop * spaceBetweenHits);
        }
		
		// Add force to the move
		myPhysicsScript.ResetForces(hit.resetPreviousHorizontalPush, hit.resetPreviousVerticalPush);

		if (!UFE.config.blockOptions.ignoreAppliedForceParry)
			myPhysicsScript.AddForce(new FPVector(hit._pushForce.x, 0, 0), -opControlsScript.mirror);

	}

	public void GetHitBlocking(Hit hit, int remainingFrames, FPVector[] location, bool ignoreDirection = false){
		// Lose life
		if (hit._damageOnBlock >= myInfo.currentLifePoints){
			GetHit(hit, remainingFrames, location);
			return;
		}else{
			DamageMe(hit._damageOnBlock);
		}

		blockStunned = true;
		currentSubState = SubStates.Blocking;
        myHitBoxesScript.isHit = true;
        hitStunDeceleration = -9999;

		int stunFrames = 0;
		BasicMoveInfo currentHitInfo = myMoveSetScript.basicMoves.blockingHighHit;

		if (hit.hitStunType == HitStunType.FrameAdvantage) {
			stunFrames = hit.frameAdvantageOnBlock + remainingFrames;
			if (stunFrames < 1) stunFrames = 1;
			stunTime = (Fix64)stunFrames/(Fix64)UFE.config.fps;
		}else if (hit.hitStunType == HitStunType.Frames) {
			stunFrames = (int) hit._hitStunOnBlock;
			if (stunFrames < 1) stunFrames = 1;
			stunTime = (Fix64)stunFrames/(Fix64)UFE.config.fps;
		}else{
			stunTime = hit._hitStunOnBlock;
		}

        UFE.FireBlock(myHitBoxesScript.GetStrokeHitBox(), opControlsScript.currentMove, myInfo);

        HitTypeOptions hitEffects = UFE.config.blockOptions.blockHitEffects;
        Fix64 freezingTime = GetHitFreezingTime(hit.hitStrength);
        if (hit.overrideHitEffectsBlock) {
            hitEffects = hit.hitEffectsBlock;
            freezingTime = hitEffects._freezingTime;
        }

        // Create hit effect
        GameObject particle = hitEffects.hitParticle;
        Fix64 killTime = hitEffects.killTime;
		AudioClip soundEffect = hitEffects.hitSound;
		if (location.Length > 0 && particle != null){
            HitEffectSpawnPoint spawnPoint = hitEffects.spawnPoint;
            if (hit.overrideEffectSpawnPoint) spawnPoint = hit.spawnPoint;
            
            long frames = (long)FPMath.Round(killTime * UFE.config.fps);
            GameObject pTemp = UFE.SpawnGameObject(particle, GetParticleSpawnPoint(spawnPoint, location), Quaternion.identity, frames);
            pTemp.transform.rotation = particle.transform.rotation;

            if (hitEffects.mirrorOn2PSide && mirror > 0) {
                pTemp.transform.localEulerAngles = new Vector3(pTemp.transform.localEulerAngles.x, pTemp.transform.localEulerAngles.y + 180, pTemp.transform.localEulerAngles.z);
            }

			//pTemp.transform.localScale = new Vector3(-mirror, 1, 1);
		}
		UFE.PlaySound(soundEffect);

		// Shake Options
		shakeCamera = hitEffects.shakeCameraOnHit;
		shakeCharacter = hitEffects.shakeCharacterOnHit;
		shakeDensity = hitEffects._shakeDensity;
        shakeCameraDensity = hitEffects._shakeCameraDensity;


        if (currentState == PossibleStates.Crouch){
			currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.blockingCrouchingHit, hit);
			currentHitInfo = myMoveSetScript.basicMoves.blockingCrouchingHit;

			if (!myMoveSetScript.AnimationExists(currentHitAnimation))
                Debug.LogError("Blocking Crouching Hit animation not found! Make sure you have it set on Character -> Basic Moves -> Blocking Animations");
		}else if (currentState == PossibleStates.Stand){
			HitBox strokeHit = myHitBoxesScript.GetStrokeHitBox();
			if (strokeHit.type == HitBoxType.low && myMoveSetScript.basicMoves.blockingLowHit.animMap[0].clip != null){
				currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.blockingLowHit, hit);
				currentHitInfo = myMoveSetScript.basicMoves.blockingLowHit;

			}else{
				currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.blockingHighHit, hit);
				currentHitInfo = myMoveSetScript.basicMoves.blockingHighHit;
				if (!myMoveSetScript.AnimationExists(currentHitAnimation))
                    Debug.LogError("Blocking High Hit animation not found! Make sure you have it set on Character -> Basic Moves -> Blocking Animations");

			}

		}else if (!myPhysicsScript.IsGrounded()){
			currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.blockingAirHit, hit);
			currentHitInfo = myMoveSetScript.basicMoves.blockingAirHit;
			if (!myMoveSetScript.AnimationExists(currentHitAnimation))
				Debug.LogError("Blocking Air Hit animation not found! Make sure you have it set on Character -> Basic Moves -> Blocking Animations");
		}


        myMoveSetScript.PlayBasicMove(currentHitInfo, currentHitAnimation);
        hitAnimationSpeed = myMoveSetScript.GetAnimationLength(currentHitAnimation) / stunTime;

        if (currentHitInfo.autoSpeed) {
            myMoveSetScript.SetAnimationSpeed(currentHitAnimation, hitAnimationSpeed);
        }
        // deprecated
        /*if (hit.overrideHitAcceleration) {
            hitStunDeceleration = hitAnimationSpeed / 3;
        }*/

        // Freeze screen depending on how strong the hit was
        HitPause(GetHitAnimationSpeed(hit.hitStrength) * .01);
        UFE.DelaySynchronizedAction(this.HitUnpause, freezingTime);

        // Reset hit to allow for another hit while the character is still stunned
        Fix64 spaceBetweenHits = 1;
		if (hit.spaceBetweenHits == Sizes.Small){
			spaceBetweenHits = 1.1;
		}else if (hit.spaceBetweenHits == Sizes.Medium){
			spaceBetweenHits = 1.3;
		}else if (hit.spaceBetweenHits == Sizes.High){
			spaceBetweenHits = 1.7;
		}

        if (hitEffects.autoHitStop) {
            UFE.DelaySynchronizedAction(myHitBoxesScript.ResetHit, freezingTime * spaceBetweenHits);
        } else {
            UFE.DelaySynchronizedAction(myHitBoxesScript.ResetHit, hitEffects._hitStop * spaceBetweenHits);
        }
		
		// Add force to the move
		myPhysicsScript.ResetForces(hit.resetPreviousHorizontalPush, hit.resetPreviousVerticalPush);

        if (!UFE.config.blockOptions.ignoreAppliedForceBlock)
            if (hit.applyDifferentBlockForce) {
                myPhysicsScript.AddForce(new FPVector(hit._pushForceBlock.x, hit._pushForceBlock.y, 0), ignoreDirection ? mirror : -opControlsScript.mirror);
            } else {
                myPhysicsScript.AddForce(new FPVector(hit._pushForce.x, 0, 0), ignoreDirection ? mirror : -opControlsScript.mirror);
            }
	}
	
	public void GetHit(Hit hit, int remainingFrames, FPVector[] location, bool ignoreDirection = false){
		// Get what animation should be played depending on the character's state
		bool airHit = false;
		bool armored = false;
		bool isKnockDown = false;
        Fix64 damageModifier = 1;
        Fix64 hitStunModifier = 1;
		BasicMoveInfo currentHitInfo;
		hitStunDeceleration =  -9999;

        currentHit = hit;

        myHitBoxesScript.isHit = true;

		if (myInfo.headLook.disableOnHit) ToggleHeadLook(false);

		if (currentMove != null && currentMove.frameLinks.Length > 0){
			foreach (FrameLink frameLink in currentMove.frameLinks){
				if (currentMove.currentFrame >= frameLink.activeFramesBegins &&
				    currentMove.currentFrame <= frameLink.activeFramesEnds) {
					if (frameLink.linkType == LinkType.CounterMove){
						bool cancelable = false;
						if (frameLink.counterMoveType == CounterMoveType.SpecificMove){
							if (frameLink.counterMoveFilter == currentMove) cancelable = true;
						}else{
							HitBox strokeHitBox = myHitBoxesScript.GetStrokeHitBox();
							if ((frameLink.anyHitStrength || frameLink.hitStrength == hit.hitStrength) &&
							    (frameLink.anyStrokeHitBox || frameLink.hitBoxType == strokeHitBox.type) &&
							    (frameLink.anyHitType || frameLink.hitType == hit.hitType)){
								cancelable = true;
							}
						}

                        if (cancelable) {
                            frameLink.cancelable = true;
							//currentMove.cancelable = true;
							
							if (frameLink.disableHitImpact) {
                                Fix64 timeLeft = (Fix64)(currentMove.totalFrames - currentMove.currentFrame)/(Fix64)UFE.config.fps;

								myHitBoxesScript.ResetHit();
								UFE.DelaySynchronizedAction(myHitBoxesScript.ResetHit, timeLeft);
								return;
							}
						}
					}
				}
			}
		}
		
		// Set position in case of pull enemy in
		activePullIn = null;
		if (hit.pullEnemyIn.enemyBodyPart != BodyPart.none && hit.pullEnemyIn.characterBodyPart != BodyPart.none){
			FPVector newPos = myHitBoxesScript.GetPosition(hit.pullEnemyIn.enemyBodyPart);
			if (newPos != FPVector.zero){
				activePullIn = new PullIn();
				activePullIn.position = worldTransform.position + (opHitBoxesScript.GetPosition(hit.pullEnemyIn.characterBodyPart) - newPos);
				activePullIn.speed = hit.pullEnemyIn.speed;
				activePullIn.forceStand = hit.pullEnemyIn.forceStand;
				activePullIn.position.z = 0;
				if (hit.pullEnemyIn.forceStand) {
					activePullIn.position.y = 0;
					myPhysicsScript.ForceGrounded();
				}
			}
		}

        if (hit.resetCrumples) consecutiveCrumple = 0;

        // Obtain animation depending on HitType
		if (myPhysicsScript.IsGrounded()) {
            if (hit.hitStrength == HitStrengh.Crumple && hit.hitType != HitType.Launcher) {
				if (myMoveSetScript.basicMoves.getHitCrumple.animMap[0].clip == null)
					Debug.LogError("("+ myInfo.characterName +") Crumple animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
				currentHitAnimation = myMoveSetScript.basicMoves.getHitCrumple.name;
				currentHitInfo = myMoveSetScript.basicMoves.getHitCrumple;
                consecutiveCrumple ++;
				//if (myMoveSetScript.basicMoves.getHitCrumple.invincible) myHitBoxesScript.HideHitBoxes(true);
			}else if (hit.hitType == HitType.Launcher){
				if (myMoveSetScript.basicMoves.getHitAir.animMap[0].clip == null)
                    Debug.LogError("(" + myInfo.characterName + ") Air Juggle animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
				currentHitAnimation = myMoveSetScript.basicMoves.getHitAir.name;
				currentHitInfo = myMoveSetScript.basicMoves.getHitAir;
				//if (myMoveSetScript.basicMoves.getHitAir.invincible) myHitBoxesScript.HideHitBoxes(true);
				airHit = true;
			}else if (hit.hitType == HitType.KnockBack){
                if (myMoveSetScript.basicMoves.getHitKnockBack.animMap[0].clip == null) {
                    if (myMoveSetScript.basicMoves.getHitAir.animMap[0].clip == null)
                        Debug.LogError("(" + myInfo.characterName + ") Air Juggle & Knock Back animations not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
                    currentHitAnimation = myMoveSetScript.basicMoves.getHitAir.name;
                    currentHitInfo = myMoveSetScript.basicMoves.getHitAir;
                } else {
                    currentHitAnimation = myMoveSetScript.basicMoves.getHitKnockBack.name;
                    currentHitInfo = myMoveSetScript.basicMoves.getHitKnockBack;
                }
				//if (myMoveSetScript.basicMoves.getHitKnockBack.invincible) myHitBoxesScript.HideHitBoxes(true);
				airHit = true;
			}else if (hit.hitType == HitType.HighKnockdown){
				if (myMoveSetScript.basicMoves.getHitHighKnockdown.animMap[0].clip == null)
                    Debug.LogError("(" + myInfo.characterName + ") Standing High Hit [Knockdown] animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
				currentHitAnimation = myMoveSetScript.basicMoves.getHitHighKnockdown.name;
				currentHitInfo = myMoveSetScript.basicMoves.getHitHighKnockdown;
				//if (myMoveSetScript.basicMoves.getHitHighKnockdown.invincible) myHitBoxesScript.HideHitBoxes(true);
				isKnockDown = true;
			}else if (hit.hitType == HitType.MidKnockdown){
				if (myMoveSetScript.basicMoves.getHitMidKnockdown.animMap[0].clip == null)
                    Debug.LogError("(" + myInfo.characterName + ") Standing Mid Hit [Knockdown] animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
				currentHitAnimation = myMoveSetScript.basicMoves.getHitMidKnockdown.name;
				currentHitInfo = myMoveSetScript.basicMoves.getHitMidKnockdown;
				//if (myMoveSetScript.basicMoves.getHitMidKnockdown.invincible) myHitBoxesScript.HideHitBoxes(true);
				isKnockDown = true;
			}else if (hit.hitType == HitType.Sweep){
				if (myMoveSetScript.basicMoves.getHitSweep.animMap[0].clip == null)
                    Debug.LogError("(" + myInfo.characterName + ") Sweep [Knockdown] animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
				currentHitAnimation = myMoveSetScript.basicMoves.getHitSweep.name;
				currentHitInfo = myMoveSetScript.basicMoves.getHitSweep;
				//if (myMoveSetScript.basicMoves.getHitSweep.invincible) myHitBoxesScript.HideHitBoxes(true);
				isKnockDown = true;
			}else if (currentState == PossibleStates.Crouch && !hit.forceStand){
				if (myMoveSetScript.basicMoves.getHitCrouching.animMap[0].clip == null)
                    Debug.LogError("(" + myInfo.characterName + ") Crouching Hit animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
				currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.getHitCrouching, hit);
				currentHitInfo = myMoveSetScript.basicMoves.getHitCrouching;
				//if (myMoveSetScript.basicMoves.getHitCrouching.invincible) myHitBoxesScript.HideHitBoxes(true);
			}else{
				HitBox strokeHit = myHitBoxesScript.GetStrokeHitBox();
                if (strokeHit.type == HitBoxType.low && myMoveSetScript.basicMoves.getHitLow.animMap[0].clip != null) {
                    if (myMoveSetScript.basicMoves.getHitHigh.animMap[0].clip == null)
                        Debug.LogError("(" + myInfo.characterName + ") Standing Low Hit animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
					currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.getHitLow, hit);
					currentHitInfo = myMoveSetScript.basicMoves.getHitLow;
					//if (myMoveSetScript.basicMoves.getHitLow.invincible) myHitBoxesScript.HideHitBoxes(true);
				}else{
					if (myMoveSetScript.basicMoves.getHitHigh.animMap[0].clip == null)
                        Debug.LogError("(" + myInfo.characterName + ") Standing High Hit animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
					currentHitAnimation = GetHitAnimation(myMoveSetScript.basicMoves.getHitHigh, hit);
					currentHitInfo = myMoveSetScript.basicMoves.getHitHigh;
					//if (myMoveSetScript.basicMoves.getHitHigh.invincible) myHitBoxesScript.HideHitBoxes(true);
				}
			}
		}else{
			if (hit.hitStrength == HitStrengh.Crumple && myMoveSetScript.basicMoves.getHitKnockBack.animMap[0].clip != null){
				currentHitAnimation = myMoveSetScript.basicMoves.getHitKnockBack.name;
				currentHitInfo = myMoveSetScript.basicMoves.getHitKnockBack;
			}else{
				if (myMoveSetScript.basicMoves.getHitAir.animMap[0].clip == null)
                    Debug.LogError("(" + myInfo.characterName + ") Air Juggle animation not found! Make sure you have it set on Character -> Basic Moves -> Hit Reactions");
				currentHitAnimation = myMoveSetScript.basicMoves.getHitAir.name;
				currentHitInfo = myMoveSetScript.basicMoves.getHitAir;
			}
			airHit = true;
		}
        
        // Override Hit Animation
        myPhysicsScript.overrideStunAnimation = null;
        if (hit.overrideHitAnimation) {
            BasicMoveInfo basicMoveOverride = myMoveSetScript.GetBasicAnimationInfo(hit.newHitAnimation);
            if (basicMoveOverride != null) {
                currentHitInfo = basicMoveOverride;
                currentHitAnimation = currentHitInfo.name;
                myPhysicsScript.overrideStunAnimation = currentHitInfo;
            } else {
                Debug.LogWarning("(" + myInfo.characterName + ") " + currentHitAnimation + " animation not found! Override not applied.");
            }
        }
		
		// Obtain hit effects
		HitTypeOptions hitEffects = hit.hitEffects;
		if (!hit.overrideHitEffects) {
			if (hit.hitStrength == HitStrengh.Weak) hitEffects = UFE.config.hitOptions.weakHit;
			if (hit.hitStrength == HitStrengh.Medium) hitEffects = UFE.config.hitOptions.mediumHit;
			if (hit.hitStrength == HitStrengh.Heavy) hitEffects = UFE.config.hitOptions.heavyHit;
			if (hit.hitStrength == HitStrengh.Crumple) hitEffects = UFE.config.hitOptions.crumpleHit;
			if (hit.hitStrength == HitStrengh.Custom1) hitEffects = UFE.config.hitOptions.customHit1;
			if (hit.hitStrength == HitStrengh.Custom2) hitEffects = UFE.config.hitOptions.customHit2;
			if (hit.hitStrength == HitStrengh.Custom3) hitEffects = UFE.config.hitOptions.customHit3;
		}

		// Cancel current move if any
        if (!hit.armorBreaker && currentMove != null &&
            currentMove.armorOptions.hitsTaken < currentMove.armorOptions.hitAbsorption &&
		    currentMove.currentFrame >= currentMove.armorOptions.activeFramesBegin && 
		    currentMove.currentFrame <= currentMove.armorOptions.activeFramesEnds){
			armored = true;
			currentMove.armorOptions.hitsTaken ++;
			damageModifier -= currentMove.armorOptions.damageAbsorption * .01;
			if (currentMove.armorOptions.overrideHitEffects) 
				hitEffects = currentMove.armorOptions.hitEffects;

		}else if (currentMove != null && !currentMove.hitAnimationOverride){
			if ((UFE.config.counterHitOptions.startUpFrames && currentMove.currentFrameData == CurrentFrameData.StartupFrames) ||
			    (UFE.config.counterHitOptions.activeFrames && currentMove.currentFrameData == CurrentFrameData.ActiveFrames) ||
			    (UFE.config.counterHitOptions.recoveryFrames && currentMove.currentFrameData == CurrentFrameData.RecoveryFrames)){
				UFE.FireAlert(UFE.config.selectedLanguage.counterHit, opInfo);
				damageModifier += UFE.config.counterHitOptions._damageIncrease * .01;
				hitStunModifier += UFE.config.counterHitOptions._hitStunIncrease * .01;
			}

            CheckHits(currentMove);
            storedMove = null;

			KillCurrentMove();
		}
		
		// Create hit effect
		if (location.Length > 0 && hitEffects.hitParticle != null){
            HitEffectSpawnPoint spawnPoint = hitEffects.spawnPoint;
            if (hit.overrideEffectSpawnPoint) spawnPoint = hit.spawnPoint;
            Vector3 newLocation = GetParticleSpawnPoint(spawnPoint, location);

            long frames = Mathf.RoundToInt(hitEffects.killTime * UFE.config.fps);
            GameObject pTemp = UFE.SpawnGameObject(hitEffects.hitParticle, newLocation, Quaternion.identity, frames);
            
            if (hitEffects.mirrorOn2PSide && mirror > 0) {
                pTemp.transform.localEulerAngles = new Vector3(pTemp.transform.localEulerAngles.x, pTemp.transform.localEulerAngles.y + 180, pTemp.transform.localEulerAngles.z);
            }
		}

		// Play sound
		UFE.PlaySound(hitEffects.hitSound);

		// Shake Options
		shakeCamera = hitEffects.shakeCameraOnHit;
		shakeCharacter = hitEffects.shakeCharacterOnHit;
		shakeDensity = hitEffects._shakeDensity;
        shakeCameraDensity = hitEffects._shakeCameraDensity;

        // Cast First Hit if true
        if (!firstHit && !opControlsScript.firstHit){
			opControlsScript.firstHit = true;
			UFE.FireAlert(UFE.config.selectedLanguage.firstHit, opInfo);
		}
		UFE.FireHit(myHitBoxesScript.GetStrokeHitBox(), opControlsScript.currentMove, opInfo);

		// Convert to percentage in case of DamageType
		if (hit.damageType == DamageType.Percentage) hit._damageOnHit = myInfo.lifePoints * (hit._damageOnHit/100);

        // Damage deterioration
        Fix64 damage = 0;
		if (!hit.damageScaling || UFE.config.comboOptions.damageDeterioration == Sizes.None){
			damage = hit._damageOnHit;
		}else if (UFE.config.comboOptions.damageDeterioration == Sizes.Small){
			damage = hit._damageOnHit - (hit._damageOnHit * (Fix64)comboHits * .1);
		}else if (UFE.config.comboOptions.damageDeterioration == Sizes.Medium){
			damage = hit._damageOnHit - (hit._damageOnHit * (Fix64)comboHits * .2);
		}else if (UFE.config.comboOptions.damageDeterioration == Sizes.High){
			damage = hit._damageOnHit - (hit._damageOnHit * (Fix64)comboHits * .4);
		}
		if (damage < UFE.config.comboOptions._minDamage) damage = UFE.config.comboOptions._minDamage;
		damage *= damageModifier;
        comboHitDamage = damage;
        comboDamage += damage;
		comboHits ++;

		if (comboHits > 1 && UFE.config.comboOptions.comboDisplayMode == ComboDisplayMode.ShowDuringComboExecution){
			UFE.FireAlert(UFE.config.selectedLanguage.combo, opInfo);
		}

		// Lose life
		isDead = DamageMe(damage, hit.doesntKill);

        // Reset hit to allow for another hit while the character is still stunned
        Fix64 spaceBetweenHits = 1;
		if (hit.spaceBetweenHits == Sizes.Small){
			spaceBetweenHits = 1.1;
		}else if (hit.spaceBetweenHits == Sizes.Medium){
			spaceBetweenHits = 1.3;
		}else if (hit.spaceBetweenHits == Sizes.High){
			spaceBetweenHits = 1.7;
		}

        if (hitEffects.autoHitStop) {
            UFE.DelaySynchronizedAction(myHitBoxesScript.ResetHit, hitEffects._freezingTime * spaceBetweenHits);
        } else {
            UFE.DelaySynchronizedAction(myHitBoxesScript.ResetHit, hitEffects._hitStop * spaceBetweenHits);
        }


        // Override Camera Speed
        if (hit.overrideCameraSpeed) {
            cameraScript.OverrideSpeed((float)hit._newMovementSpeed, (float)hit._newRotationSpeed);
            UFE.DelaySynchronizedAction(cameraScript.RestoreSpeed, hit._cameraSpeedDuration);
        }


        // Stun
        int stunFrames = 0;
		if ((currentMove == null || !currentMove.hitAnimationOverride) && (!armored || isDead)) {
			// Hit stun deterioration (the longer the combo gets, the harder it is to combo)
			currentSubState = SubStates.Stunned;
			if (hit.hitStunType == HitStunType.FrameAdvantage) {
				stunFrames = hit.frameAdvantageOnHit + remainingFrames;
				if (stunFrames < 1) stunFrames = 1;
				if (stunFrames < UFE.config.comboOptions._minHitStun) stunFrames = UFE.config.comboOptions._minHitStun;
				stunTime = (Fix64)stunFrames/(Fix64)UFE.config.fps;
			}else if (hit.hitStunType == HitStunType.Frames) {
				stunFrames = (int) hit._hitStunOnHit;
				if (stunFrames < 1) stunFrames = 1;
				if (stunFrames < UFE.config.comboOptions._minHitStun) stunFrames = UFE.config.comboOptions._minHitStun;
                stunTime = (Fix64)stunFrames/(Fix64)UFE.config.fps;
            } else {
                stunFrames = (int)FPMath.Round(hit._hitStunOnHit * UFE.config.fps);
				stunTime = hit._hitStunOnHit;
			}

            if (UFE.config.characterRotationOptions.fixRotationOnHit) testCharacterRotation(100);

			if (!hit.resetPreviousHitStun){
				if (UFE.config.comboOptions.hitStunDeterioration == Sizes.Small){
					stunTime -= (Fix64)comboHits * .01;
				}else if (UFE.config.comboOptions.hitStunDeterioration == Sizes.Medium){
					stunTime -= (Fix64)comboHits * .02;
				}else if (UFE.config.comboOptions.hitStunDeterioration == Sizes.High){
					stunTime -= (Fix64)comboHits * .04;
				}
			}
			stunTime *= hitStunModifier;

            FPVector pushForce = new FPVector();
            if (!myPhysicsScript.IsGrounded() && hit.applyDifferentAirForce) {
                pushForce.x = hit._pushForceAir.x;
                pushForce.y = hit._pushForceAir.y;
            } else {
                pushForce.x = hit._pushForce.x;
                pushForce.y = hit._pushForce.y;
            }

            if (consecutiveCrumple > UFE.config.comboOptions.maxConsecutiveCrumple) {
                isKnockDown = true;
                airHit = true;
                pushForce.y = 1;
            }

            if (hit.overrideAirRecoveryType) {
                airRecoveryType = hit.newAirRecoveryType;
            } else {
                airRecoveryType = UFE.config.comboOptions.airRecoveryType;
            }

            // Add force to the move		
            // Air juggle deterioration (the longer the combo, the harder it is to push the opponent higher)
            if (pushForce.y > 0 || (isDead && !isKnockDown)) {

				if (UFE.config.comboOptions.airJuggleDeteriorationType == AirJuggleDeteriorationType.ComboHits){
					airJuggleHits = comboHits - 1;
				}
                if (UFE.config.comboOptions.airJuggleDeterioration == Sizes.Small){
                    pushForce.y -= (pushForce.y * (Fix64)airJuggleHits * .04);
				}else if (UFE.config.comboOptions.airJuggleDeterioration == Sizes.Medium){
                    pushForce.y -= (pushForce.y * (Fix64)airJuggleHits * .1);
				}else if (UFE.config.comboOptions.airJuggleDeterioration == Sizes.High){
                    pushForce.y -= (pushForce.y * (Fix64)airJuggleHits * .3);
				}
                if (pushForce.y < UFE.config.comboOptions._minPushForce) pushForce.y = UFE.config.comboOptions._minPushForce;
				airJuggleHits ++;
			}

            // Force a standard weight so the same air combo works on all characters
			if (UFE.config.comboOptions.fixJuggleWeight){
				myPhysicsScript.ApplyNewWeight(UFE.config.comboOptions._juggleWeight);
			}
            if (hit.overrideJuggleWeight) {
                myPhysicsScript.ApplyNewWeight(hit._newJuggleWeight);
            }
			
            // Restand the opponent (or juggle) if its an OTG
            if (currentState == PossibleStates.Down) {
                if (pushForce.y > 0) {
                    currentState = PossibleStates.NeutralJump;
                } else {
                    currentState = PossibleStates.Stand;
                }
            }

            if (airHit && airRecoveryType == AirRecoveryType.CantMove && hit.instantAirRecovery) 
                stunTime = 0.001;

            if (isDead) stunTime = 99999;

            if ((airHit || (!myPhysicsScript.IsGrounded() && airRecoveryType == AirRecoveryType.DontRecover))
                && pushForce.y > 0) {
				
				if (myMoveSetScript.basicMoves.getHitAir.animMap[0].clip == null)
					Debug.LogError("Get Hit Air animation not found! Make sure you have it set on Character -> Basic Moves -> Get Hit Air");
				//if (myMoveSetScript.basicMoves.getHitAir.invincible) myHitBoxesScript.HideHitBoxes(true);

				myPhysicsScript.ResetForces(hit.resetPreviousHorizontalPush, hit.resetPreviousVerticalPush);
                myPhysicsScript.AddForce(new FPVector(pushForce.x, pushForce.y, 0), ignoreDirection? mirror : -opControlsScript.mirror);
                if (myMoveSetScript.basicMoves.getHitKnockBack.animMap[0].clip != null && 
                    pushForce.x > UFE.config.comboOptions._knockBackMinForce) {
                    currentHitAnimation = myMoveSetScript.basicMoves.getHitKnockBack.name;
                    currentHitInfo = myMoveSetScript.basicMoves.getHitKnockBack;
				}else{
					currentHitAnimation = myMoveSetScript.basicMoves.getHitAir.name;
					currentHitInfo = myMoveSetScript.basicMoves.getHitAir;
				}

                if (hit.overrideHitAnimationBlend) {
                    myMoveSetScript.PlayBasicMove(currentHitInfo, currentHitAnimation, hit._newHitBlendingIn, hit.resetHitAnimations);
                } else {
                    myMoveSetScript.PlayBasicMove(currentHitInfo, currentHitAnimation, hit.resetHitAnimations);
                }

                if (currentHitInfo.autoSpeed) {
                    // if the hit was in the air, calculate the time it will take for the character to hit the ground
                    Fix64 airTime = myPhysicsScript.GetPossibleAirTime(pushForce.y);

                    if (myMoveSetScript.basicMoves.fallingFromAirHit.animMap[0].clip == null) airTime *= 2;

                    if (stunTime > airTime || airRecoveryType == AirRecoveryType.DontRecover) { 
                        stunTime = airTime;
                    }

                    myMoveSetScript.SetAnimationNormalizedSpeed(currentHitAnimation, (myMoveSetScript.GetAnimationLength(currentHitAnimation) / stunTime));
                }

			}else{

                hitAnimationSpeed = 0;

				if (hit.hitType == HitType.HighKnockdown){
                    applyKnockdownForces(UFE.config.knockDownOptions.high);
                    myPhysicsScript.overrideAirAnimation = true;
                    airRecoveryType = AirRecoveryType.DontRecover;
                    if (!hit.customStunValues) stunTime =
                        UFE.config.knockDownOptions.high._knockedOutTime + UFE.config.knockDownOptions.high._standUpTime;

				}else if (hit.hitType == HitType.MidKnockdown){
                    applyKnockdownForces(UFE.config.knockDownOptions.highLow);
                    myPhysicsScript.overrideAirAnimation = true;
                    airRecoveryType = AirRecoveryType.DontRecover;
                    if (!hit.customStunValues) stunTime =
                        UFE.config.knockDownOptions.highLow._knockedOutTime + UFE.config.knockDownOptions.highLow._standUpTime;

				}else if (hit.hitType == HitType.Sweep){
                    applyKnockdownForces(UFE.config.knockDownOptions.sweep);
                    myPhysicsScript.overrideAirAnimation = true;
                    airRecoveryType = AirRecoveryType.DontRecover;
                    if (!hit.customStunValues) stunTime =
                        UFE.config.knockDownOptions.sweep._knockedOutTime + UFE.config.knockDownOptions.sweep._standUpTime;

				}

				hitAnimationSpeed = myMoveSetScript.GetAnimationLength(currentHitAnimation)/stunTime;

                if (hit.hitStrength == HitStrengh.Crumple) {
                    stunTime += UFE.config.knockDownOptions.crumple._knockedOutTime;
                }

                if (!myPhysicsScript.overrideAirAnimation) {
                    myPhysicsScript.ResetForces(hit.resetPreviousHorizontalPush, hit.resetPreviousVerticalPush);
                    myPhysicsScript.AddForce(pushForce, ignoreDirection ? mirror : -opControlsScript.mirror);
                }

                // Set deceleration of hit stun animation so it can look more natural (deprecated)
                /*if (hit.overrideHitAcceleration) {
                    hitStunDeceleration = hitAnimationSpeed / 3;
                }*/

                if (hit.overrideHitAnimationBlend){
                    myMoveSetScript.PlayBasicMove(currentHitInfo, currentHitAnimation, hit._newHitBlendingIn, hit.resetHitAnimations);
                }else{
                    myMoveSetScript.PlayBasicMove(currentHitInfo, currentHitAnimation, hit.resetHitAnimations);
                }

                if (currentHitInfo.autoSpeed && hitAnimationSpeed > 0) {
                    myMoveSetScript.SetAnimationSpeed(currentHitAnimation, hitAnimationSpeed);
                }

			}
		}
		
		// Freeze screen depending on how strong the hit was
		HitPause(GetHitAnimationSpeed(hit.hitStrength) * .01);
		UFE.DelaySynchronizedAction(this.HitUnpause, hitEffects._freezingTime);
    }

    private Vector3 GetParticleSpawnPoint(HitEffectSpawnPoint spawnPoint, FPVector[] locations) {
        if (spawnPoint == HitEffectSpawnPoint.StrikingHurtBox) {
            return locations[0].ToVector();
        } else if (spawnPoint == HitEffectSpawnPoint.StrokeHitBox) {
            return locations[1].ToVector();
        } else {
            return locations[2].ToVector();
        }
    }

	private void applyKnockdownForces(SubKnockdownOptions knockdownOptions){
		myPhysicsScript.ResetForces(true, true);
		myPhysicsScript.AddForce(knockdownOptions._predefinedPushForce, -opControlsScript.mirror);
	}

	private string GetHitAnimation(BasicMoveInfo hitMove, Hit hit){
		if (hit.hitStrength == HitStrengh.Weak) return hitMove.name;
		if (hitMove.animMap[1].clip != null && hit.hitStrength == HitStrengh.Medium) return myMoveSetScript.GetAnimationString(hitMove, 2);
		if (hitMove.animMap[2].clip != null && hit.hitStrength == HitStrengh.Heavy) return myMoveSetScript.GetAnimationString(hitMove, 3);
		if (hitMove.animMap[3].clip != null && hit.hitStrength == HitStrengh.Custom1) return myMoveSetScript.GetAnimationString(hitMove, 4);
		if (hitMove.animMap[4].clip != null && hit.hitStrength == HitStrengh.Custom2) return myMoveSetScript.GetAnimationString(hitMove, 5);
		if (hitMove.animMap[5].clip != null && hit.hitStrength == HitStrengh.Custom3) return myMoveSetScript.GetAnimationString(hitMove, 6);
		return hitMove.name;
	}

	public void ToggleHeadLook(bool flag){
		if (headLookScript != null && myInfo.headLook.enabled) headLookScript.enabled = flag;
	}

	// Pause animations and physics to create a sense of impact
	public void HitPause(){
		HitPause(0);
	}

	public void HitPause(Fix64 animSpeed){
		if (shakeCamera) Camera.main.transform.position += Vector3.forward/2;
		myPhysicsScript.freeze = true;
		
		PausePlayAnimation(true, animSpeed);
	}
	
	// Unpauses the pause
	public void HitUnpause(){
        if (cameraScript.cinematicFreeze) return;
        myPhysicsScript.freeze = false;

		PausePlayAnimation(false);
	}

	// Method to pause animations and return them to their prior speed accordly
	
	private void PausePlayAnimation(bool pause){
		PausePlayAnimation(pause, 0);
	}

	private void PausePlayAnimation(bool pause, Fix64 animSpeed){
		if (animSpeed < 0) animSpeed = 0;
		if (pause){
			myMoveSetScript.SetAnimationSpeed(animSpeed);
		}else {
			myMoveSetScript.RestoreAnimationSpeed();
		}
	}

    public void AddGauge(Fix64 gaugeGain) {
        if ((isDead || opControlsScript.isDead) && UFE.config.roundOptions.inhibitGaugeGain) return;
		if (!UFE.config.gameGUI.hasGauge) return;
        if (inhibitGainWhileDraining) return;
		myInfo.currentGaugePoints += (myInfo.maxGaugePoints * (gaugeGain/100));
		if (myInfo.currentGaugePoints > myInfo.maxGaugePoints) myInfo.currentGaugePoints = myInfo.maxGaugePoints;
	}

    private void RemoveGauge(Fix64 gaugeLoss) {
        if ((isDead || opControlsScript.isDead) && UFE.config.roundOptions.inhibitGaugeGain) return;
		if (!UFE.config.gameGUI.hasGauge) return;
        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && playerNum == 1 && UFE.config.trainingModeOptions.p1Gauge == LifeBarTrainingMode.Infinite) return;
        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && playerNum == 2 && UFE.config.trainingModeOptions.p2Gauge == LifeBarTrainingMode.Infinite) return;
        myInfo.currentGaugePoints -= (myInfo.maxGaugePoints * (gaugeLoss / 100));
		if (myInfo.currentGaugePoints < 0) myInfo.currentGaugePoints = 0;

        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && ((playerNum == 1 && UFE.config.trainingModeOptions.p1Gauge == LifeBarTrainingMode.Refill)
            || (playerNum == 2 && UFE.config.trainingModeOptions.p2Gauge == LifeBarTrainingMode.Refill))) {
                if (!UFE.FindAndUpdateDelaySynchronizedAction(this.RefillGauge, UFE.config.trainingModeOptions.refillTime)) 
				UFE.DelaySynchronizedAction(this.RefillGauge, UFE.config.trainingModeOptions.refillTime);
		}
	}
	
	public bool DamageMe(Fix64 damage, bool doesntKill){
		if (doesntKill && damage >= myInfo.currentLifePoints) damage = myInfo.currentLifePoints - 1;
		return DamageMe(damage);
	}
	
	private void RefillLife(){
		myInfo.currentLifePoints = myInfo.lifePoints;
		UFE.SetLifePoints(myInfo.lifePoints, myInfo);
	}
	
	private void RefillGauge(){
		AddGauge(myInfo.maxGaugePoints);
	}

	private bool DamageMe(Fix64 damage){
        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && playerNum == 1 && UFE.config.trainingModeOptions.p1Life == LifeBarTrainingMode.Infinite) return false;
        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && playerNum == 2 && UFE.config.trainingModeOptions.p2Life == LifeBarTrainingMode.Infinite) return false;
		if (myInfo.currentLifePoints <= 0) return true;
		if (UFE.GetTimer() <= 0 && UFE.config.roundOptions.hasTimer) return true;

		myInfo.currentLifePoints -= damage;
		if (myInfo.currentLifePoints < 0) myInfo.currentLifePoints = 0;
		UFE.SetLifePoints(myInfo.currentLifePoints, myInfo);

        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && ((playerNum == 1 && UFE.config.trainingModeOptions.p1Life == LifeBarTrainingMode.Refill) 
            || (playerNum == 2 && UFE.config.trainingModeOptions.p2Life == LifeBarTrainingMode.Refill))) {
                if (myInfo.currentLifePoints == 0) myInfo.currentLifePoints = myInfo.lifePoints;
                if (!UFE.FindAndUpdateDelaySynchronizedAction(this.RefillLife, UFE.config.trainingModeOptions.refillTime)) {
                    UFE.DelaySynchronizedAction(this.RefillLife, UFE.config.trainingModeOptions.refillTime);
                }
		}

        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && playerNum == 1 && UFE.config.trainingModeOptions.p1Life != LifeBarTrainingMode.Normal) return false;
        if ((UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)
            && playerNum == 2 && UFE.config.trainingModeOptions.p2Life != LifeBarTrainingMode.Normal) return false;

		if (myInfo.currentLifePoints == 0) return true;
		return false;
	}

    private void StartNextChallenge() {
        UFE.config.lockInputs = true;
        UFE.config.lockMovements = true;
        UFE.DelaySynchronizedAction(UFE.StartFight, (Fix64)2);

        if (challengeMode.resetRound) {
            UFE.ResetTimer();

            ResetData(true);
            opControlsScript.ResetData(false);
        }

        challengeMode.Run();
    }

	public void SetMoveToOutro(){
		this.SetMove(myMoveSetScript.GetOutro());
		if (currentMove != null) {
			currentMove.currentFrame = 0;
			currentMove.currentTick = 0;
		}
		outroPlayed = true;
	}

	public void ResetData(bool resetLife){
		if (UFE.config.roundOptions.resetPositions){
			if (playerNum == 1){
				worldTransform.position = new FPVector(UFE.config.roundOptions._p1XPosition, .009, worldTransform.position.z);
			}else{
                worldTransform.position = new FPVector(UFE.config.roundOptions._p2XPosition, .009, worldTransform.position.z);
			}
			myMoveSetScript.PlayBasicMove(myMoveSetScript.basicMoves.idle, myMoveSetScript.basicMoves.idle.name, 0);
			myPhysicsScript.ForceGrounded();

		}else if (currentState == PossibleStates.Down && myPhysicsScript.IsGrounded()){
			myMoveSetScript.PlayAnimation("standUp", 0);
		}

		if (resetLife || UFE.config.roundOptions.resetLifePoints){
            if (playerNum == 1 && (UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)) {
                myInfo.currentLifePoints = (Fix64)myInfo.lifePoints * (UFE.config.trainingModeOptions.p1StartingLife / 100);
            } else if (playerNum == 2 && (UFE.gameMode == GameMode.TrainingRoom || UFE.gameMode == GameMode.ChallengeMode)) {
                myInfo.currentLifePoints = (Fix64)myInfo.lifePoints * (UFE.config.trainingModeOptions.p2StartingLife / 100);
            } else {
                myInfo.currentLifePoints = (Fix64)myInfo.lifePoints;
            }
		}
		blockStunned = false;
		stunTime = 0;
		comboHits = 0;
		comboDamage = 0;
		comboHitDamage = 0;
		airJuggleHits = 0;
		CheckBlocking(false);
		isDead = false;
		myPhysicsScript.isTakingOff = false;
		myPhysicsScript.isLanding = false;
		
		//myHitBoxesScript.HideHitBoxes(false);
		myPhysicsScript.ResetWeight();
		ToggleHeadLook(true);

		currentState = PossibleStates.Stand;
		currentSubState = SubStates.Resting;
	}

	// Get amount of freezing time depending on the Strengtht of the move
	public Fix64 GetHitAnimationSpeed(HitStrengh hitStrength){
		if (hitStrength == HitStrengh.Weak){
			return UFE.config.hitOptions.weakHit._animationSpeed;
		} else if (hitStrength == HitStrengh.Medium){
			return UFE.config.hitOptions.mediumHit._animationSpeed;
		}else if (hitStrength == HitStrengh.Heavy){
			return UFE.config.hitOptions.heavyHit._animationSpeed;
		}else if (hitStrength == HitStrengh.Crumple){
			return UFE.config.hitOptions.crumpleHit._animationSpeed;
		}
		return 0;
	}

	// Get amount of freezing time depending on the Strengtht of the move
	public Fix64 GetHitFreezingTime(HitStrengh hitStrength){
		if (hitStrength == HitStrengh.Weak){
			return UFE.config.hitOptions.weakHit._freezingTime;
		} else if (hitStrength == HitStrengh.Medium){
			return UFE.config.hitOptions.mediumHit._freezingTime;
		}else if (hitStrength == HitStrengh.Heavy){
			return UFE.config.hitOptions.heavyHit._freezingTime;
		}else if (hitStrength == HitStrengh.Crumple){
            return UFE.config.hitOptions.crumpleHit._freezingTime;
        } else if (hitStrength == HitStrengh.Custom1) {
            return UFE.config.hitOptions.customHit1._freezingTime;
        } else if (hitStrength == HitStrengh.Custom2) {
            return UFE.config.hitOptions.customHit2._freezingTime;
        } else if (hitStrength == HitStrengh.Custom3) {
            return UFE.config.hitOptions.customHit3._freezingTime;
		}
		return 0;
	}
	
	// Shake character while being hit and in freezing mode
	
	void shakeCam(){
        //System.Random random = new System.Random(Random.seed);
        //float rnd = (float)(random.NextDouble() * (shakeDensity * .34d));
        //float rnd = Random.Range(-.2f * (float)shakeDensity, .2f * (float)shakeDensity);
        //float rnd = Random.Range((float)shakeDensity * -.3f, (float)shakeDensity * .3f);
        float rnd = Random.Range((float)shakeCameraDensity * -.1f, (float)shakeCameraDensity * .1f);
        Camera.main.transform.position += new Vector3(rnd, rnd, 0);
	}

    void shake() {
        //float rnd = Random.Range(-.1f * (float)shakeDensity, .2f * (float)shakeDensity);
        //character.transform.localPosition = new Vector3(rnd, 0, 0);

        Fix64 rnd = FPRandom.Range((float)shakeDensity * -.1f, (float)shakeDensity * .1f);
        localTransform.position = new FPVector(localTransform.position.x + rnd, localTransform.position.y, localTransform.position.z);
    }
}
