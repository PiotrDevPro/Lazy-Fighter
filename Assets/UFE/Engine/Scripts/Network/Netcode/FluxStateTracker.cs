using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UFENetcode;
using FPLibrary;
using UFE3D;

public class FluxStateTracker {

    /*public static void LoadBattleGUI(FluxBattleGUIState state) {
        DefaultBattleGUI battleGUI = UFE.battleGUI as DefaultBattleGUI;
        if (state != null && battleGUI != null) {
            if (battleGUI.player1ButtonPresses != null) {
                foreach (List<Image> images in battleGUI.player1ButtonPresses) {
                    if (images != null) {
                        foreach (Image image in images) {
                            if (image != null) {
                                GameObject.Destroy(image.gameObject);
                            }
                        }
                    }
                }
                battleGUI.player1ButtonPresses.Clear();
            }

            if (battleGUI.player2ButtonPresses != null) {
                foreach (List<Image> images in battleGUI.player2ButtonPresses) {
                    if (images != null) {
                        foreach (Image image in images) {
                            if (image != null) {
                                GameObject.Destroy(image.gameObject);
                            }
                        }
                    }
                }
                battleGUI.player2ButtonPresses.Clear();
            }



            battleGUI.player1InputReferences.Clear();
            if (state.player1InputReferences != null) {
                for (int i = 0; i < state.player1InputReferences.Count; ++i) {
                    if (state.player1InputReferences[i] != null) {
                        battleGUI.AddInput(state.player1InputReferences[i], 1);
                    }
                }
            }

            battleGUI.player2InputReferences.Clear();
            if (state.player2InputReferences != null) {
                for (int i = 0; i < state.player2InputReferences.Count; ++i) {
                    if (state.player2InputReferences[i] != null) {
                        battleGUI.AddInput(state.player2InputReferences[i], 2);
                    }
                }
            }
        }
    }*/

    public static FluxGameHistory LoadGameState(FluxGameHistory history, long frame) {
        FluxStates gameState;

        if (history.TryGetState(frame, out gameState)) {
            LoadGameState(gameState);
        } else {
            throw new ArgumentOutOfRangeException(
                "frame"
                ,
                frame
                ,
                string.Format(
                    "The frame value should be between {0} and {1}.",
                    history.FirstStoredFrame,
                    history.LastStoredFrame
                )
            );
        }

        return history;
    }

    /*public static FluxBattleGUIState SaveBattleGUIState() {
        FluxBattleGUIState state = new FluxBattleGUIState();
        state.player1InputReferences = new List<InputReferences[]>();
        state.player2InputReferences = new List<InputReferences[]>();

        DefaultBattleGUI battleGUI = UFE.battleGUI as DefaultBattleGUI;
        if (battleGUI != null) {
            if (battleGUI.player1InputReferences != null) {
                for (int i = 0; i < battleGUI.player1InputReferences.Count; ++i) {
                    if (battleGUI.player1InputReferences[i] != null) {
                        InputReferences[] references = new InputReferences[battleGUI.player1InputReferences[i].Length];

                        for (int j = 0; j < battleGUI.player1InputReferences[i].Length; ++j) {
                            if (battleGUI.player1InputReferences[i][j] != null) {
                                references[j] = battleGUI.player1InputReferences[i][j];
                            }
                        }

                        state.player1InputReferences.Add(references);
                    }
                }
            }

            if (battleGUI.player2InputReferences != null) {
                for (int i = 0; i < battleGUI.player2InputReferences.Count; ++i) {
                    if (battleGUI.player2InputReferences[i] != null) {
                        InputReferences[] references = new InputReferences[battleGUI.player2InputReferences[i].Length];

                        for (int j = 0; j < battleGUI.player2InputReferences[i].Length; ++j) {
                            if (battleGUI.player2InputReferences[i][j] != null) {
                                references[j] = battleGUI.player2InputReferences[i][j];
                            }
                        }

                        state.player2InputReferences.Add(references);
                    }
                }
            }
        }

        return state;
    }*/

    // TODO FIX THIS
    public static void LoadGameState(FluxStates gameState) {
        LoadGameState(gameState, UFE.config.networkOptions.ufeTrackers);
    }

    // TODO FIX THIS
    public static void LoadGameState(FluxStates gameState, bool loadTrackers) {
        // Static Variables
        UFE.currentFrame = gameState.networkFrame;
        UFE.freeCamera = gameState.global.freeCamera;
        UFE.freezePhysics = gameState.global.freezePhysics;
        UFE.newRoundCasted = gameState.global.newRoundCasted;
        UFE.normalizedCam = gameState.global.normalizedCam;
        UFE.pauseTimer = gameState.global.pauseTimer;
        UFE.timer = gameState.global.timer;
        UFE.timeScale = gameState.global.timeScale;


        // Delayed Synchornized Actions
        UFE.delayedSynchronizedActions = new List<DelayedAction>();
        foreach (DelayedAction dAction in gameState.global.delayedActions) {
            UFE.delayedSynchronizedActions.Add(new DelayedAction(dAction.action, dAction.steps));
        }


        // Instantiated Objects
        List<InstantiatedGameObject> newInstantiatedObjects = new List<InstantiatedGameObject>();
        foreach (FluxStates.InstantiatedGameObjectState state in gameState.global.instantiatedObjects)
        {
            if (state.gameObject == null) continue;
            InstantiatedGameObject newIGameObject = new InstantiatedGameObject(state.gameObject, state.mrFusion, state.creationFrame, state.destructionFrame);
            newIGameObject.gameObject.transform.localPosition = state.transformState.localPosition;
            newIGameObject.gameObject.transform.localRotation = state.transformState.localRotation;
            newIGameObject.gameObject.transform.localScale = state.transformState.localScale;
            newIGameObject.gameObject.transform.position = state.transformState.position;
            newIGameObject.gameObject.transform.rotation = state.transformState.rotation;
            newIGameObject.gameObject.SetActive(state.transformState.active);
            if (newIGameObject.mrFusion != null && newIGameObject.gameObject.activeSelf) newIGameObject.mrFusion.LoadState(UFE.currentFrame);

            newInstantiatedObjects.Add(newIGameObject);
        }

        // Clean old references
        foreach (InstantiatedGameObject iGameObject in UFE.instantiatedObjects)
        {
            bool objFound = false;
            foreach (InstantiatedGameObject newIGameObject in newInstantiatedObjects) {
                if (iGameObject.gameObject == newIGameObject.gameObject) {
                    objFound = true;
                    break;
                }
            }
            if (!objFound) UnityEngine.Object.Destroy(iGameObject.gameObject);
        }
        UFE.instantiatedObjects = newInstantiatedObjects;


        // UFE Config Instance
        UFE.config.currentRound = gameState.global.currentRound;
        UFE.config.lockInputs = gameState.global.lockInputs;
        UFE.config.lockMovements = gameState.global.lockMovements;


        // Camera
        Camera.main.enabled = gameState.camera.enabled;
        Camera.main.fieldOfView = gameState.camera.fieldOfView;
        Camera.main.transform.localPosition = gameState.camera.localPosition;
        Camera.main.transform.localRotation = gameState.camera.localRotation;
        Camera.main.transform.position = gameState.camera.position;
        Camera.main.transform.rotation = gameState.camera.rotation;

        if (gameState.camera.cameraScript && UFE.cameraScript == null && UFE.gameEngine != null) {
            UFE.cameraScript = UFE.gameEngine.AddComponent<CameraScript>();
        }

        if (UFE.cameraScript != null) {
            if (gameState.camera.cameraScript) {
                UFE.cameraScript.cinematicFreeze = gameState.camera.cinematicFreeze;
                UFE.cameraScript.currentLookAtPosition = gameState.camera.currentLookAtPosition;
                UFE.cameraScript.freeCameraSpeed = gameState.camera.freeCameraSpeed;
                UFE.cameraScript.lastOwner = gameState.camera.lastOwner;
                UFE.cameraScript.killCamMove = gameState.camera.killCamMove;
                UFE.cameraScript.movementSpeed = gameState.camera.movementSpeed;
                UFE.cameraScript.rotationSpeed = gameState.camera.rotationSpeed;
                UFE.cameraScript.standardDistance = gameState.camera.standardDistance;
                UFE.cameraScript.standardGroundHeight = gameState.camera.standardGroundHeight;
                UFE.cameraScript.targetPosition = gameState.camera.targetPosition;
                UFE.cameraScript.targetRotation = gameState.camera.targetRotation;
                UFE.cameraScript.targetFieldOfView = gameState.camera.targetFieldOfView;
            } else {
                GameObject.Destroy(UFE.cameraScript);
            }
            // Camera Fade
            //CameraFade.instance.enabled = gameState.camera.cameraFade;
            //CameraFade.instance.m_CurrentScreenOverlayColor = gameState.camera.currentScreenOverlayColor;
        }
        

        // Characters
        LoadCharacterState(gameState.player1, 1);
        LoadCharacterState(gameState.player2, 2);


        // Load every variable through the auto tracker
        if (loadTrackers) UFE.UFEInstance = RecordVar.LoadStateTrackers(UFE.UFEInstance, gameState.tracker) as UFE;
    }

    // TODO FIX THIS
    public static FluxStates SaveGameState(long frame) {
        return SaveGameState(frame, false);
    }

    // TODO FIX THIS
    public static FluxStates SaveGameState(long frame, bool saveTrackers) {
        FluxStates gameState = new FluxStates();
        gameState.networkFrame = frame;

        // Global
        gameState.global.freeCamera = UFE.freeCamera;
        gameState.global.freezePhysics = UFE.freezePhysics;
        gameState.global.newRoundCasted = UFE.newRoundCasted;
        gameState.global.normalizedCam = UFE.normalizedCam;
        gameState.global.pauseTimer = UFE.pauseTimer;
        gameState.global.timer = UFE.timer;
        gameState.global.timeScale = UFE.timeScale;

        gameState.global.delayedActions = new List<DelayedAction>();
        foreach (DelayedAction dAction in UFE.delayedSynchronizedActions) {
            gameState.global.delayedActions.Add(new DelayedAction(dAction.action, dAction.steps));
        }

        gameState.global.instantiatedObjects = new List<FluxStates.InstantiatedGameObjectState>();
        foreach (InstantiatedGameObject entry in UFE.instantiatedObjects) {
            FluxStates.InstantiatedGameObjectState goState = new FluxStates.InstantiatedGameObjectState();
            if (entry.gameObject == null) continue;
            goState.gameObject = entry.gameObject;
            goState.mrFusion = entry.mrFusion;
            
            goState.creationFrame = entry.creationFrame;
            goState.destructionFrame = entry.destructionFrame;
            goState.transformState = new FluxStates.TransformState();
            goState.transformState.localScale = entry.gameObject.transform.localScale;
            goState.transformState.position = entry.gameObject.transform.position;
            goState.transformState.rotation = entry.gameObject.transform.rotation;
            goState.transformState.active = entry.gameObject.activeSelf;
            if (goState.mrFusion != null && goState.transformState.active) goState.mrFusion.SaveState(frame);

            gameState.global.instantiatedObjects.Add(goState);
        }

        gameState.global.currentRound = UFE.config.currentRound;
        gameState.global.lockInputs = UFE.config.lockInputs;
        gameState.global.lockMovements = UFE.config.lockMovements;

        
        // Camera
        if (Camera.main != null) {
            gameState.camera.enabled = Camera.main.enabled;
            gameState.camera.fieldOfView = Camera.main.fieldOfView;
            gameState.camera.localPosition = Camera.main.transform.localPosition;
            gameState.camera.localRotation = Camera.main.transform.localRotation;
            gameState.camera.position = Camera.main.transform.position;
            gameState.camera.rotation = Camera.main.transform.rotation;

            gameState.camera.cameraScript = (UFE.cameraScript != null);
            if (gameState.camera.cameraScript) {
                gameState.camera.cinematicFreeze = UFE.cameraScript.cinematicFreeze;
                gameState.camera.currentLookAtPosition = UFE.cameraScript.currentLookAtPosition;
                gameState.camera.freeCameraSpeed = UFE.cameraScript.freeCameraSpeed;
                gameState.camera.lastOwner = UFE.cameraScript.lastOwner;
                gameState.camera.killCamMove = UFE.cameraScript.killCamMove;
                gameState.camera.movementSpeed = UFE.cameraScript.movementSpeed;
                gameState.camera.rotationSpeed = UFE.cameraScript.rotationSpeed;
                gameState.camera.standardDistance = UFE.cameraScript.standardDistance;
                gameState.camera.standardGroundHeight = UFE.cameraScript.standardGroundHeight;
                gameState.camera.targetPosition = UFE.cameraScript.targetPosition;
                gameState.camera.targetRotation = UFE.cameraScript.targetRotation;
                gameState.camera.targetFieldOfView = UFE.cameraScript.targetFieldOfView;
            }
            // Camera Fade
            //gameState.camera.cameraFade = CameraFade.instance.enabled;
            //gameState.camera.currentScreenOverlayColor = CameraFade.instance.m_CurrentScreenOverlayColor;
        }

        
        // Characters
        gameState.player1 = SaveCharacterState(1);
        gameState.player2 = SaveCharacterState(2);
        

        // Save every RecordVar attribute under UFEInterfaces to be used on auto tracker
        if (saveTrackers) gameState.tracker = RecordVar.SaveStateTrackers(UFE.UFEInstance, new Dictionary<System.Reflection.MemberInfo, object>());


        return gameState;
    }

    private static FluxStates.CharacterState.MoveState CopyMove(FluxStates.CharacterState.MoveState moveState, MoveInfo targetMove) {
        moveState.move = targetMove;
        if (targetMove == null) return moveState;

        //moveState.cancelable = targetMove.cancelable;
        moveState.kill = targetMove.kill;
        moveState.armorHits = targetMove.armorOptions.hitsTaken;
        moveState.currentFrame = targetMove.currentFrame;
        moveState.overrideStartupFrame = targetMove.overrideStartupFrame;
        moveState.animationSpeedTemp = targetMove.animationSpeedTemp;
        moveState.currentTick = targetMove.currentTick;
        moveState.hitConfirmOnBlock = targetMove.hitConfirmOnBlock;
        moveState.hitConfirmOnParry = targetMove.hitConfirmOnParry;
        moveState.hitConfirmOnStrike = targetMove.hitConfirmOnStrike;
        moveState.hitAnimationOverride = targetMove.hitAnimationOverride;
        moveState.standUpOptions = targetMove.standUpOptions;
        moveState.currentFrameData = targetMove.currentFrameData;


        moveState.hitStates = new bool[targetMove.hits.Length];
        for (int i = 0; i < targetMove.hits.Length; ++i) {
            moveState.hitStates[i] = targetMove.hits[i].disabled;
        }
        moveState.frameLinkStates = new bool[targetMove.frameLinks.Length];
        for (int i = 0; i < targetMove.frameLinks.Length; ++i) {
            moveState.frameLinkStates[i] = targetMove.frameLinks[i].cancelable;
        }
        moveState.castedBodyPartVisibilityChange = new bool[targetMove.bodyPartVisibilityChanges.Length];
        for (int i = 0; i < targetMove.bodyPartVisibilityChanges.Length; ++i) {
            moveState.castedBodyPartVisibilityChange[i] = targetMove.bodyPartVisibilityChanges[i].casted;
        }
        moveState.castedProjectile = new bool[targetMove.projectiles.Length];
        for (int i = 0; i < targetMove.projectiles.Length; ++i) {
            moveState.castedProjectile[i] = targetMove.projectiles[i].casted;
        }
        moveState.castedAppliedForce = new bool[targetMove.appliedForces.Length];
        for (int i = 0; i < targetMove.appliedForces.Length; ++i) {
            moveState.castedAppliedForce[i] = targetMove.appliedForces[i].casted;
        }
        moveState.castedMoveParticleEffect = new bool[targetMove.particleEffects.Length];
        for (int i = 0; i < targetMove.particleEffects.Length; ++i) {
            moveState.castedMoveParticleEffect[i] = targetMove.particleEffects[i].casted;
        }
        moveState.castedSlowMoEffect = new bool[targetMove.slowMoEffects.Length];
        for (int i = 0; i < targetMove.slowMoEffects.Length; ++i) {
            moveState.castedSlowMoEffect[i] = targetMove.slowMoEffects[i].casted;
        }
        moveState.castedSoundEffect = new bool[targetMove.soundEffects.Length];
        for (int i = 0; i < targetMove.soundEffects.Length; ++i) {
            moveState.castedSoundEffect[i] = targetMove.soundEffects[i].casted;
        }
        moveState.castedInGameAlert = new bool[targetMove.inGameAlert.Length];
        for (int i = 0; i < targetMove.inGameAlert.Length; ++i) {
            moveState.castedInGameAlert[i] = targetMove.inGameAlert[i].casted;
        }
        moveState.castedStanceChange = new bool[targetMove.stanceChanges.Length];
        for (int i = 0; i < targetMove.stanceChanges.Length; ++i) {
            moveState.castedStanceChange[i] = targetMove.stanceChanges[i].casted;
        }
        moveState.castedCameraMovement = new bool[targetMove.cameraMovements.Length];
        for (int i = 0; i < targetMove.cameraMovements.Length; ++i) {
            moveState.castedCameraMovement[i] = targetMove.cameraMovements[i].casted;
        }
        moveState.cameraOver = new bool[targetMove.cameraMovements.Length];
        for (int i = 0; i < targetMove.cameraMovements.Length; ++i) {
            moveState.cameraOver[i] = targetMove.cameraMovements[i].over;
        }
        moveState.cameraTime = new FPLibrary.Fix64[targetMove.cameraMovements.Length];
        for (int i = 0; i < targetMove.cameraMovements.Length; ++i) {
            moveState.cameraTime[i] = targetMove.cameraMovements[i].time;
        }
        moveState.castedOpponentOverride = new bool[targetMove.opponentOverride.Length];
        for (int i = 0; i < targetMove.opponentOverride.Length; ++i) {
            moveState.castedOpponentOverride[i] = targetMove.opponentOverride[i].casted;
        }

        return moveState;
    }

    private static void CopyMove(ref MoveInfo targetMove, FluxStates.CharacterState.MoveState moveState) {
        targetMove = moveState.move;
        if (targetMove == null) return;

        //targetMove.cancelable = moveState.cancelable;
        targetMove.kill = moveState.move.kill;
        targetMove.armorOptions.hitsTaken = moveState.armorHits;
        targetMove.currentFrame = moveState.currentFrame;
        targetMove.overrideStartupFrame = moveState.overrideStartupFrame;
        targetMove.animationSpeedTemp = moveState.animationSpeedTemp;
        targetMove.currentTick = moveState.currentTick;
        targetMove.hitConfirmOnBlock = moveState.hitConfirmOnBlock;
        targetMove.hitConfirmOnParry = moveState.hitConfirmOnParry;
        targetMove.hitConfirmOnStrike = moveState.hitConfirmOnStrike;
        targetMove.hitAnimationOverride = moveState.hitAnimationOverride;
        targetMove.standUpOptions = moveState.standUpOptions;
        targetMove.currentFrameData = moveState.currentFrameData;

        for (int i = 0; i < moveState.hitStates.Length; ++i) {
            targetMove.hits[i].disabled = moveState.hitStates[i];
        }
        for (int i = 0; i < moveState.frameLinkStates.Length; ++i) {
            targetMove.frameLinks[i].cancelable = moveState.frameLinkStates[i];
        }
        for (int i = 0; i < moveState.castedBodyPartVisibilityChange.Length; ++i) {
            targetMove.bodyPartVisibilityChanges[i].casted = moveState.castedBodyPartVisibilityChange[i];
        }
        for (int i = 0; i < moveState.castedProjectile.Length; ++i) {
            targetMove.projectiles[i].casted = moveState.castedProjectile[i];
        }
        for (int i = 0; i < moveState.castedAppliedForce.Length; ++i) {
            targetMove.appliedForces[i].casted = moveState.castedAppliedForce[i];
        }
        for (int i = 0; i < moveState.castedMoveParticleEffect.Length; ++i) {
            targetMove.particleEffects[i].casted = moveState.castedMoveParticleEffect[i];
        }
        for (int i = 0; i < moveState.castedSlowMoEffect.Length; ++i) {
            targetMove.slowMoEffects[i].casted = moveState.castedSlowMoEffect[i];
        }
        for (int i = 0; i < moveState.castedSoundEffect.Length; ++i) {
            targetMove.soundEffects[i].casted = moveState.castedSoundEffect[i];
        }
        for (int i = 0; i < moveState.castedInGameAlert.Length; ++i) {
            targetMove.inGameAlert[i].casted = moveState.castedInGameAlert[i];
        }
        for (int i = 0; i < moveState.castedStanceChange.Length; ++i) {
            targetMove.stanceChanges[i].casted = moveState.castedStanceChange[i];
        }
        for (int i = 0; i < moveState.castedCameraMovement.Length; ++i) {
            targetMove.cameraMovements[i].casted = moveState.castedCameraMovement[i];
        }
        for (int i = 0; i < moveState.cameraOver.Length; ++i) {
            targetMove.cameraMovements[i].over = moveState.cameraOver[i];
        }
        for (int i = 0; i < moveState.cameraTime.Length; ++i) {
            targetMove.cameraMovements[i].time = moveState.cameraTime[i];
        }
        for (int i = 0; i < moveState.castedOpponentOverride.Length; ++i) {
            targetMove.opponentOverride[i].casted = moveState.castedOpponentOverride[i];
        }
    }

    protected static void LoadCharacterState(FluxStates.CharacterState state, int player) {
        ControlsScript controlsScript = UFE.GetControlsScript(player);

        if (controlsScript != null) {
            if (state.controlsScript) {
                // Character Shell Transform (deprecated)
                controlsScript.transform.position = state.shellTransform.position;
                controlsScript.transform.rotation = state.shellTransform.rotation;

                // Character Transform (deprecated)
                controlsScript.character.transform.localPosition = state.characterTransform.localPosition;
                controlsScript.character.transform.rotation = state.characterTransform.rotation;
                //controlsScript.character.transform.localScale = state.characterTransform.localScale;


                controlsScript.worldTransform.position = state.shellTransform.fpPosition;
                controlsScript.localTransform.position = state.characterTransform.fpPosition;
                controlsScript.localTransform.rotation = state.characterTransform.fpRotation;
                // Update Unity Transform
                controlsScript.transform.position = controlsScript.worldTransform.position.ToVector();
                controlsScript.character.transform.localPosition = controlsScript.localTransform.position.ToVector();
                controlsScript.character.transform.rotation = controlsScript.localTransform.rotation.ToQuaternion();


                // Global
                controlsScript.myInfo.currentCombatStance = state.combatStance;
                controlsScript.myInfo.currentLifePoints = state.life;
                controlsScript.myInfo.currentGaugePoints = state.gauge;


                // Control
                controlsScript.afkTimer = state.afkTimer;
                controlsScript.airJuggleHits = state.airJuggleHits;
                controlsScript.airRecoveryType = state.airRecoveryType;
                controlsScript.applyRootMotion = state.applyRootMotion;
                controlsScript.blockStunned = state.blockStunned;
                controlsScript.comboDamage = state.comboDamage;
                controlsScript.comboHitDamage = state.comboHitDamage;
                controlsScript.comboHits = state.comboHits;
                controlsScript.consecutiveCrumple = state.consecutiveCrumple;
                controlsScript.currentBasicMove = state.currentBasicMove;
                controlsScript.currentDrained = state.currentDrained;
                controlsScript.currentHit = state.currentHit;
                controlsScript.currentHitAnimation = state.currentHitAnimation;
                controlsScript.currentState = state.currentState;
                controlsScript.currentSubState = state.currentSubState;
                controlsScript.DCMove = state.DCMove;
                controlsScript.DCStance = state.DCStance;
                controlsScript.firstHit = state.firstHit;
                controlsScript.gaugeDPS = state.gaugeDPS;
                controlsScript.hitDetected = state.hitDetected;
                controlsScript.hitAnimationSpeed = state.hitAnimationSpeed;
                controlsScript.hitStunDeceleration = state.hitStunDeceleration;
                controlsScript.inhibitGainWhileDraining = state.inhibitGainWhileDraining;
                controlsScript.isAirRecovering = state.isAirRecovering;
                controlsScript.isBlocking = state.isBlocking;
                controlsScript.isDead = state.isDead;
                controlsScript.ignoreCollisionMass = state.ignoreCollisionMass;
                controlsScript.introPlayed = state.introPlayed;
                controlsScript.lit = state.lit;
                controlsScript.mirror = state.mirror;
                controlsScript.normalizedDistance = state.normalizedDistance;
                controlsScript.normalizedJumpArc = state.normalizedJumpArc;
                controlsScript.outroPlayed = state.outroPlayed;
                controlsScript.potentialBlock = state.potentialBlock;
                controlsScript.potentialParry = state.potentialParry;
                controlsScript.roundMsgCasted = state.roundMsgCasted;
                controlsScript.roundsWon = state.roundsWon;
                controlsScript.shakeCamera = state.shakeCamera;
                controlsScript.shakeCharacter = state.shakeCharacter;
                controlsScript.shakeDensity = state.shakeDensity;
                controlsScript.shakeCameraDensity = state.shakeCameraDensity;
                controlsScript.standUpOverride = state.standUpOverride;
                controlsScript.standardYRotation = state.standardYRotation;
                controlsScript.storedMoveTime = state.storedMoveTime;
                controlsScript.stunTime = state.stunTime;
                controlsScript.totalDrain = state.totalDrain;


                // Active PullIn
                controlsScript.activePullIn = state.activePullIn.pullIn;
                if (controlsScript.activePullIn != null)
                    controlsScript.activePullIn.position = state.activePullIn.position;


                // Moves
                CopyMove(ref controlsScript.currentMove, state.currentMove);
                CopyMove(ref controlsScript.storedMove, state.storedMove);


                // Physics
                controlsScript.Physics.airTime = state.physics.airTime;
                controlsScript.Physics.appliedGravity = state.physics.appliedGravity;
                controlsScript.Physics.currentAirJumps = state.physics.currentAirJumps;
                controlsScript.Physics.freeze = state.physics.freeze;
                controlsScript.Physics.groundBounceTimes = state.physics.groundBounceTimes;
                controlsScript.Physics.horizontalForce = state.physics.horizontalForce;
                controlsScript.Physics.isGroundBouncing = state.physics.isGroundBouncing;
                controlsScript.Physics.isLanding = state.physics.isLanding;
                controlsScript.Physics.isTakingOff = state.physics.isTakingOff;
                controlsScript.Physics.isWallBouncing = state.physics.isWallBouncing;
                controlsScript.Physics.moveDirection = state.physics.moveDirection;
                controlsScript.Physics.overrideAirAnimation = state.physics.overrideAirAnimation;
                controlsScript.Physics.overrideStunAnimation = state.physics.overrideStunAnimation;
                controlsScript.Physics.verticalForce = state.physics.verticalForce;
                controlsScript.Physics.verticalTotalForce = state.physics.verticalTotalForce;
                controlsScript.Physics.wallBounceTimes = state.physics.wallBounceTimes;


                // Move Set
                controlsScript.MoveSet.totalAirMoves = state.moveSet.totalAirMoves;
                controlsScript.MoveSet.animationPaused = state.moveSet.animationPaused;
                controlsScript.MoveSet.overrideNextBlendingValue = state.moveSet.overrideNextBlendingValue;
                controlsScript.MoveSet.lastTimePress = state.moveSet.lastTimePress;

                //controlsScript.MoveSet.chargeValues = new Dictionary<ButtonPress, float>(state.moveSet.chargeValues);
                controlsScript.inputHeldDown = state.inputHeldDown.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);
                
                controlsScript.projectiles = new List<ProjectileMoveScript>();
                foreach (ProjectileMoveScript projectile in state.projectiles) {
                    if (projectile != null) controlsScript.projectiles.Add(projectile);
                }

                controlsScript.MoveSet.lastButtonPresses = new List<ButtonSequenceRecord>();
                foreach (ButtonSequenceRecord btnRecord in state.moveSet.lastButtonPresses) {
                    controlsScript.MoveSet.lastButtonPresses.Add(new ButtonSequenceRecord(btnRecord.buttonPress, btnRecord.chargeTime));
                }


                // Hit Boxes State - Hit Boxes
                controlsScript.HitBoxes.isHit = state.hitBoxes.isHit;
                controlsScript.HitBoxes.hitConfirmType = state.hitBoxes.hitConfirmType;
                controlsScript.HitBoxes.collisionBoxSize = state.hitBoxes.collisionBoxSize;

                //controlsScript.HitBoxes.InvertHitBoxes(state.hitBoxes.currentMirror);
                controlsScript.HitBoxes.currentMirror = state.hitBoxes.currentMirror;

                for (int i = 0; i < controlsScript.HitBoxes.hitBoxes.Length; ++i) {
                    controlsScript.HitBoxes.hitBoxes[i].rendererBounds = state.hitBoxes.hitBoxes[i].rendererBounds;
                    controlsScript.HitBoxes.hitBoxes[i].state = state.hitBoxes.hitBoxes[i].state;
                    controlsScript.HitBoxes.hitBoxes[i].hide = state.hitBoxes.hitBoxes[i].hide;
                    controlsScript.HitBoxes.hitBoxes[i].visibility = state.hitBoxes.hitBoxes[i].visibility;
                }


                // Hit Boxes State - Hurt Boxes
                if (state.hitBoxes.activeHurtBoxes != null) {
                    if (state.hitBoxes.activeHurtBoxes.Length > 0) {
                        controlsScript.HitBoxes.activeHurtBoxes = new HurtBox[state.hitBoxes.activeHurtBoxes.Length];
                        for (int i = 0; i < controlsScript.HitBoxes.activeHurtBoxes.Length; ++i) {
                            controlsScript.HitBoxes.activeHurtBoxes[i] = state.hitBoxes.activeHurtBoxes[i].hurtBox;
                            controlsScript.HitBoxes.activeHurtBoxes[i].rendererBounds = state.hitBoxes.activeHurtBoxes[i].rendererBounds;
                            controlsScript.HitBoxes.activeHurtBoxes[i].isBlock = state.hitBoxes.activeHurtBoxes[i].isBlock;
                            controlsScript.HitBoxes.activeHurtBoxes[i].position = state.hitBoxes.activeHurtBoxes[i].position;
                        }
                    } else {
                        controlsScript.HitBoxes.activeHurtBoxes = null;
                    }
                } else {
                    controlsScript.HitBoxes.activeHurtBoxes = null;
                }


                // Hit Boxes State - Animation Maps
                controlsScript.HitBoxes.bakeSpeed = state.hitBoxes.bakeSpeed;
                controlsScript.HitBoxes.animationMaps = new AnimationMap[state.hitBoxes.animationMaps.Length];
                for (int i = 0; i < controlsScript.HitBoxes.animationMaps.Length; ++i) {
                    controlsScript.HitBoxes.animationMaps[i] = state.hitBoxes.animationMaps[i];
                }
                //controlsScript.HitBoxes.animationMaps = state.hitBoxes.animationMaps;


                // Hit Boxes State - Block Area
                controlsScript.HitBoxes.blockableArea = state.hitBoxes.blockableArea.blockArea;
                if (controlsScript.HitBoxes.blockableArea != null)
                    controlsScript.HitBoxes.blockableArea.position = state.hitBoxes.blockableArea.position;
                

                // Animator
                if (controlsScript.myInfo.animationType == AnimationType.Mecanim) {
                    controlsScript.MoveSet.MecanimControl.currentMirror = state.moveSet.animator.currentMirror;

                    controlsScript.MoveSet.MecanimControl.currentAnimationData = state.moveSet.animator.currentAnimationData.mecanimAnimationData;
                    if (controlsScript.MoveSet.MecanimControl.currentAnimationData != null) {
                        controlsScript.MoveSet.MecanimControl.currentAnimationData.normalizedTime = state.moveSet.animator.currentAnimationData.normalizedTime;
                        controlsScript.MoveSet.MecanimControl.currentAnimationData.secondsPlayed = state.moveSet.animator.currentAnimationData.secondsPlayed;
                        controlsScript.MoveSet.MecanimControl.currentAnimationData.timesPlayed = state.moveSet.animator.currentAnimationData.timesPlayed;
                        controlsScript.MoveSet.MecanimControl.currentAnimationData.speed = state.moveSet.animator.currentAnimationData.speed;
                    }

                    // Mecanim Control 3.0 (MC3)
                    /*
                    controlsScript.MoveSet.MecanimControl.mc3Animator.currentInput = state.moveSet.animator.currentInput;
                    controlsScript.MoveSet.MecanimControl.mc3Animator.transitionDuration = state.moveSet.animator.transitionDuration;
                    controlsScript.MoveSet.MecanimControl.mc3Animator.transitionTime = state.moveSet.animator.transitionTime;

                    controlsScript.MoveSet.MecanimControl.mc3Animator.SetWeights(state.moveSet.animator.weightList);
                    controlsScript.MoveSet.MecanimControl.mc3Animator.speedArray = (float[]) state.moveSet.animator.speedList.Clone();
                    controlsScript.MoveSet.MecanimControl.mc3Animator.timeArray = (float[]) state.moveSet.animator.timeList.Clone();
                    controlsScript.MoveSet.MecanimControl.mc3Animator.SetController(state.moveSet.animator.currentInput);
                    controlsScript.MoveSet.MecanimControl.mc3Animator.Update(0);
                    */

                    // Mecanim Control 1.0
                    controlsScript.MoveSet.MecanimControl.currentNormalizedTime = state.moveSet.animator.currentNormalizedTime;
                    controlsScript.MoveSet.MecanimControl.currentState = state.moveSet.animator.currentState;
                    controlsScript.MoveSet.MecanimControl.currentSpeed = state.moveSet.animator.currentSpeed;

                    controlsScript.MoveSet.MecanimControl.animator.runtimeAnimatorController = state.moveSet.animator.overrideController;
                    controlsScript.MoveSet.MecanimControl.animator.Play(state.moveSet.animator.currentState, 0, (float)state.moveSet.animator.currentAnimationData.normalizedTime);
                    controlsScript.MoveSet.MecanimControl.animator.applyRootMotion = controlsScript.MoveSet.MecanimControl.currentAnimationData.applyRootMotion;
                    controlsScript.MoveSet.MecanimControl.animator.Update(0);
                    controlsScript.MoveSet.MecanimControl.SetSpeed(state.moveSet.animator.currentSpeed);

                } else {
                    controlsScript.MoveSet.LegacyControl.currentMirror = state.moveSet.animator.currentMirror;
                    controlsScript.MoveSet.LegacyControl.globalSpeed = state.moveSet.animator.globalSpeed;
                    controlsScript.MoveSet.LegacyControl.lastPosition = state.moveSet.animator.lastPosition;

                    controlsScript.MoveSet.LegacyControl.currentAnimationData = state.moveSet.animator.currentAnimationData.legacyAnimationData;
                    if (controlsScript.MoveSet.LegacyControl.currentAnimationData != null) {
                    controlsScript.MoveSet.LegacyControl.animator.Play(controlsScript.MoveSet.LegacyControl.currentAnimationData.clipName);
                        controlsScript.MoveSet.LegacyControl.currentAnimationData.animState = state.moveSet.animator.currentAnimationData.legacyAnimationData.animState;
                        controlsScript.MoveSet.LegacyControl.currentAnimationData.secondsPlayed = state.moveSet.animator.currentAnimationData.secondsPlayed;
                        controlsScript.MoveSet.LegacyControl.currentAnimationData.normalizedTime = state.moveSet.animator.currentAnimationData.normalizedTime;
                        controlsScript.MoveSet.LegacyControl.currentAnimationData.animState.normalizedTime = (float)state.moveSet.animator.currentAnimationData.normalizedTime;
                    }
                    controlsScript.MoveSet.LegacyControl.animator.Sample();
                }


                // HitBoxes & MoveSet - Update Animation Map
                controlsScript.HitBoxes.UpdateMap(controlsScript.MoveSet.GetCurrentClipFrame(controlsScript.HitBoxes.bakeSpeed));
            }
            else {
                Debug.LogWarning("We don't have the player state for this frame.");
            }
        }
    }

    protected static FluxStates.CharacterState SaveCharacterState(int player) {
        ControlsScript controlsScript = UFE.GetControlsScript(player);
        FluxStates.CharacterState state = new FluxStates.CharacterState();

        state.controlsScript = (controlsScript != null);
        if (state.controlsScript) {
            // Character Shell Transform
            state.shellTransform.position = controlsScript.transform.position;
            state.shellTransform.rotation = controlsScript.transform.rotation;

            state.shellTransform.fpPosition = controlsScript.worldTransform.position;

            // Character Transform
            state.characterTransform.localPosition = controlsScript.character.transform.localPosition;
            state.characterTransform.rotation = controlsScript.character.transform.rotation;
            state.characterTransform.localScale = controlsScript.character.transform.localScale;

            state.characterTransform.fpPosition = controlsScript.localTransform.position;
            state.characterTransform.fpRotation = controlsScript.localTransform.rotation;


            // Global
            state.combatStance = controlsScript.myInfo.currentCombatStance;
            state.life = controlsScript.myInfo.currentLifePoints;
            state.gauge = controlsScript.myInfo.currentGaugePoints;


            // Control
            state.afkTimer = controlsScript.afkTimer;
            state.airJuggleHits = controlsScript.airJuggleHits;
            state.airRecoveryType = controlsScript.airRecoveryType;
            state.applyRootMotion = controlsScript.applyRootMotion;
            state.blockStunned = controlsScript.blockStunned;
            state.comboDamage = controlsScript.comboDamage;
            state.comboHitDamage = controlsScript.comboHitDamage;
            state.comboHits = controlsScript.comboHits;
            state.consecutiveCrumple = controlsScript.consecutiveCrumple;
            state.currentBasicMove = controlsScript.currentBasicMove;
            state.currentDrained = controlsScript.currentDrained;
            state.currentHit = controlsScript.currentHit;
            state.currentHitAnimation = controlsScript.currentHitAnimation;
            state.currentState = controlsScript.currentState;
            state.currentSubState = controlsScript.currentSubState;
            state.DCMove = controlsScript.DCMove;
            state.DCStance = controlsScript.DCStance;
            state.firstHit = controlsScript.firstHit;
            state.gaugeDPS = controlsScript.gaugeDPS;
            state.hitDetected = controlsScript.hitDetected;
            state.hitAnimationSpeed = controlsScript.hitAnimationSpeed;
            state.hitStunDeceleration = controlsScript.hitStunDeceleration;
            state.inhibitGainWhileDraining = controlsScript.inhibitGainWhileDraining;
            state.isAirRecovering = controlsScript.isAirRecovering;
            state.isBlocking = controlsScript.isBlocking;
            state.isDead = controlsScript.isDead;
            state.ignoreCollisionMass = controlsScript.ignoreCollisionMass;
            state.introPlayed = controlsScript.introPlayed;
            state.lit = controlsScript.lit;
            state.mirror = controlsScript.mirror;
            state.normalizedDistance = controlsScript.normalizedDistance;
            state.normalizedJumpArc = controlsScript.normalizedJumpArc;
            state.outroPlayed = controlsScript.outroPlayed;
            state.potentialBlock = controlsScript.potentialBlock;
            state.potentialParry = controlsScript.potentialParry;
            state.roundMsgCasted = controlsScript.roundMsgCasted;
            state.roundsWon = controlsScript.roundsWon;
            state.shakeCamera = controlsScript.shakeCamera;
            state.shakeCharacter = controlsScript.shakeCharacter;
            state.shakeDensity = controlsScript.shakeDensity;
            state.shakeCameraDensity = controlsScript.shakeCameraDensity;
            state.standUpOverride = controlsScript.standUpOverride;
            state.standardYRotation = controlsScript.standardYRotation;
            state.storedMoveTime = controlsScript.storedMoveTime;
            state.stunTime = controlsScript.stunTime;
            state.totalDrain = controlsScript.totalDrain;


            // Active PullIn
            state.activePullIn.pullIn = controlsScript.activePullIn;
            if (controlsScript.activePullIn != null)
                state.activePullIn.position = controlsScript.activePullIn.position;


            // Moves
            state.currentMove = CopyMove(state.currentMove, controlsScript.currentMove);
            state.storedMove = CopyMove(state.storedMove, controlsScript.storedMove);


            // Physics
            state.physics.airTime = controlsScript.Physics.airTime;
            state.physics.appliedGravity = controlsScript.Physics.appliedGravity;
            state.physics.currentAirJumps = controlsScript.Physics.currentAirJumps;
            state.physics.freeze = controlsScript.Physics.freeze;
            state.physics.groundBounceTimes = controlsScript.Physics.groundBounceTimes;
            state.physics.horizontalForce = controlsScript.Physics.horizontalForce;
            state.physics.isGroundBouncing = controlsScript.Physics.isGroundBouncing;
            state.physics.isLanding = controlsScript.Physics.isLanding;
            state.physics.isTakingOff = controlsScript.Physics.isTakingOff;
            state.physics.isWallBouncing = controlsScript.Physics.isWallBouncing;
            state.physics.moveDirection = controlsScript.Physics.moveDirection;
            state.physics.overrideAirAnimation = controlsScript.Physics.overrideAirAnimation;
            state.physics.overrideStunAnimation = controlsScript.Physics.overrideStunAnimation;
            state.physics.verticalForce = controlsScript.Physics.verticalForce;
            state.physics.verticalTotalForce = controlsScript.Physics.verticalTotalForce;
            state.physics.wallBounceTimes = controlsScript.Physics.wallBounceTimes;


            // Move Set
            state.moveSet.totalAirMoves = controlsScript.MoveSet.totalAirMoves;
            state.moveSet.animationPaused = controlsScript.MoveSet.animationPaused;
            state.moveSet.overrideNextBlendingValue = controlsScript.MoveSet.overrideNextBlendingValue;
            state.moveSet.lastTimePress = controlsScript.MoveSet.lastTimePress;

            state.inputHeldDown = controlsScript.inputHeldDown.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);

            state.projectiles = new List<ProjectileMoveScript>();
            foreach (ProjectileMoveScript projectile in controlsScript.projectiles) {
                if (projectile != null) state.projectiles.Add(projectile);
            }

            state.moveSet.lastButtonPresses = new List<ButtonSequenceRecord>();
            foreach (ButtonSequenceRecord btnRecord in controlsScript.MoveSet.lastButtonPresses) {
                state.moveSet.lastButtonPresses.Add(new ButtonSequenceRecord(btnRecord.buttonPress, btnRecord.chargeTime));
            }


            // Hit Boxes State - Hit Boxes
            state.hitBoxes.isHit = controlsScript.HitBoxes.isHit;
            state.hitBoxes.hitConfirmType = controlsScript.HitBoxes.hitConfirmType;
            state.hitBoxes.collisionBoxSize = controlsScript.HitBoxes.collisionBoxSize;
            state.hitBoxes.currentMirror = controlsScript.HitBoxes.currentMirror;
            
            state.hitBoxes.hitBoxes = new FluxStates.CharacterState.HitBoxState[controlsScript.HitBoxes.hitBoxes.Length];
            for (int i = 0; i < state.hitBoxes.hitBoxes.Length; ++i) {
                state.hitBoxes.hitBoxes[i].hitBox = controlsScript.HitBoxes.hitBoxes[i];
                state.hitBoxes.hitBoxes[i].rendererBounds = controlsScript.HitBoxes.hitBoxes[i].rendererBounds;
                state.hitBoxes.hitBoxes[i].state = controlsScript.HitBoxes.hitBoxes[i].state;
                state.hitBoxes.hitBoxes[i].hide = controlsScript.HitBoxes.hitBoxes[i].hide;
                state.hitBoxes.hitBoxes[i].visibility = controlsScript.HitBoxes.hitBoxes[i].visibility;
            }


            // Hit Boxes State - Hurt Boxes
            if (controlsScript.HitBoxes.activeHurtBoxes != null) {
                state.hitBoxes.activeHurtBoxes = new FluxStates.CharacterState.HurtBoxState[controlsScript.HitBoxes.activeHurtBoxes.Length];
                for (int i = 0; i < state.hitBoxes.activeHurtBoxes.Length; ++i) {
                    state.hitBoxes.activeHurtBoxes[i].hurtBox = controlsScript.HitBoxes.activeHurtBoxes[i];
                    state.hitBoxes.activeHurtBoxes[i].rendererBounds = controlsScript.HitBoxes.activeHurtBoxes[i].rendererBounds;
                    state.hitBoxes.activeHurtBoxes[i].isBlock = controlsScript.HitBoxes.activeHurtBoxes[i].isBlock;
                    state.hitBoxes.activeHurtBoxes[i].position = controlsScript.HitBoxes.activeHurtBoxes[i].position;
                }
            } else {
                state.hitBoxes.activeHurtBoxes = null;
            }


            // Hit Boxes State - Block Area
            state.hitBoxes.blockableArea.blockArea = controlsScript.HitBoxes.blockableArea;
            if (controlsScript.HitBoxes.blockableArea != null)
                state.hitBoxes.blockableArea.position = controlsScript.HitBoxes.blockableArea.position;


            // Hit Boxes State - Animation Maps
            state.hitBoxes.bakeSpeed = controlsScript.HitBoxes.bakeSpeed;
            state.hitBoxes.animationMaps = new AnimationMap[controlsScript.HitBoxes.animationMaps.Length];
            for (int i = 0; i < state.hitBoxes.animationMaps.Length; ++i) {
                state.hitBoxes.animationMaps[i] = controlsScript.HitBoxes.animationMaps[i];
            }
            //state.hitBoxes.animationMaps = controlsScript.HitBoxes.animationMaps;


            // Animator
            if (controlsScript.myInfo.animationType == AnimationType.Mecanim) {
                state.moveSet.animator.currentMirror = controlsScript.MoveSet.MecanimControl.currentMirror;

                state.moveSet.animator.currentAnimationData.mecanimAnimationData = controlsScript.MoveSet.MecanimControl.currentAnimationData;
                if (controlsScript.MoveSet.MecanimControl.currentAnimationData != null) {
                    state.moveSet.animator.currentAnimationData.normalizedTime = controlsScript.MoveSet.MecanimControl.currentAnimationData.normalizedTime;
                    state.moveSet.animator.currentAnimationData.secondsPlayed = controlsScript.MoveSet.MecanimControl.currentAnimationData.secondsPlayed;
                    state.moveSet.animator.currentAnimationData.timesPlayed = controlsScript.MoveSet.MecanimControl.currentAnimationData.timesPlayed;
                    state.moveSet.animator.currentAnimationData.speed = controlsScript.MoveSet.MecanimControl.currentAnimationData.speed;
                }

                // Mecanim Control 3.0
                /*
	            state.moveSet.animator.currentInput = controlsScript.MoveSet.MecanimControl.mc3Animator.currentInput;
	            state.moveSet.animator.transitionDuration = controlsScript.MoveSet.MecanimControl.mc3Animator.transitionDuration;
	            state.moveSet.animator.transitionTime = controlsScript.MoveSet.MecanimControl.mc3Animator.transitionTime;

	            state.moveSet.animator.weightList = controlsScript.MoveSet.MecanimControl.mc3Animator.GetWeights();
                state.moveSet.animator.speedList = (float[]) controlsScript.MoveSet.MecanimControl.mc3Animator.speedArray.Clone();
                state.moveSet.animator.timeList = (float[])controlsScript.MoveSet.MecanimControl.mc3Animator.timeArray.Clone();
                */

                // Mecanim Control 1.0
                state.moveSet.animator.currentNormalizedTime = controlsScript.MoveSet.MecanimControl.currentNormalizedTime;
                state.moveSet.animator.currentState = controlsScript.MoveSet.MecanimControl.currentState;
                state.moveSet.animator.currentSpeed = controlsScript.MoveSet.MecanimControl.currentSpeed;
                state.moveSet.animator.overrideController = controlsScript.MoveSet.MecanimControl.animator.runtimeAnimatorController;

            } else {
                state.moveSet.animator.currentMirror = controlsScript.MoveSet.LegacyControl.currentMirror;
                state.moveSet.animator.globalSpeed = controlsScript.MoveSet.LegacyControl.globalSpeed;
                state.moveSet.animator.lastPosition = controlsScript.MoveSet.LegacyControl.lastPosition;

                state.moveSet.animator.currentAnimationData.legacyAnimationData = controlsScript.MoveSet.LegacyControl.currentAnimationData;
                if (controlsScript.MoveSet.LegacyControl.currentAnimationData != null) {
                    state.moveSet.animator.currentAnimationData.secondsPlayed = controlsScript.MoveSet.LegacyControl.currentAnimationData.secondsPlayed;
                    state.moveSet.animator.currentAnimationData.normalizedTime = controlsScript.MoveSet.LegacyControl.currentAnimationData.normalizedTime;
                    state.moveSet.animator.currentAnimationData.legacyAnimationData.animState = controlsScript.MoveSet.LegacyControl.currentAnimationData.animState;
                }
            }


            // Projectiles
            /*state.projectiles = new List<FluxProjectileState>();
            for (int i = 0; i < controlsScript.projectiles.Count; ++i) {
                ProjectileMoveScript projectile = controlsScript.projectiles[i];
                if (projectile != null) {
                    FluxProjectileState projectileState = new FluxProjectileState();
                    projectileState.airHit = projectile.data.airHit;
                    projectileState.armorBreaker = projectile.data.armorBreaker;
                    projectileState.blockableArea = new BlockArea(projectile.blockableArea);
                    projectileState.bodyPart = projectile.data.bodyPart;
                    projectileState.casted = projectile.data.casted;
                    projectileState.castingFrame = projectile.data.castingFrame;
                    projectileState.castingOffSet = projectile.data.castingOffSet;
                    projectileState.damageType = projectile.data.damageType;
                    projectileState.damageOnHit = projectile.data.damageOnHit;
                    projectileState.damageOnBlock = projectile.data.damageOnBlock;
                    projectileState.damageScaling = projectile.data.damageScaling;
                    projectileState.destroyMe = projectile.destroyMe;
                    projectileState.directionAngle = projectile.data.directionAngle;
                    projectileState.downHit = projectile.data.downHit;
                    projectileState.duration = projectile.data.duration;
                    projectileState.fixedZAxis = projectile.data.fixedZAxis;
                    projectileState.forceGrounded = projectile.data.forceGrounded;
                    projectileState.gaugeGainOnHit = projectile.data.gaugeGainOnHit;
                    projectileState.gaugeGainOnBlock = projectile.data.gaugeGainOnBlock;
                    projectileState.groundHit = projectile.data.groundHit;
                    projectileState.hitEffects = new HitTypeOptions(projectile.data.hitEffects);
                    projectileState.hitEffectsOnHit = projectile.data.hitEffectsOnHit;
                    projectileState.hitStrength = projectile.data.hitStrength;
                    projectileState.hitStunOnHit = projectile.data.hitStunOnHit;
                    projectileState.hitStunOnBlock = projectile.data.hitStunOnBlock;
                    projectileState.hitType = projectile.data.hitType;
                    projectileState.impactDuration = projectile.data.impactDuration;
                    projectileState.impactPrefab = projectile.data.impactPrefab;
                    projectileState.isHit = projectile.isHit;
                    projectileState.mirror = projectile.mirror;
                    projectileState.moveLinkOnStrike = projectile.data.moveLinkOnStrike;
                    projectileState.moveLinkOnBlock = projectile.data.moveLinkOnBlock;
                    projectileState.moveLinkOnParry = projectile.data.moveLinkOnParry;
                    projectileState.obeyDirectionalHit = projectile.data.obeyDirectionalHit;
                    projectileState.opGaugeGainOnHit = projectile.data.opGaugeGainOnHit;
                    projectileState.opGaugeGainOnBlock = projectile.data.opGaugeGainOnBlock;
                    projectileState.opGaugeGainOnParry = projectile.data.opGaugeGainOnParry;
                    projectileState.overrideHitEffects = projectile.data.overrideHitEffects;
                    projectileState.projectile = projectile;
                    projectileState.projectileCollision = projectile.data.projectileCollision;
                    projectileState.projectilePrefab = projectile.data.projectilePrefab;
                    projectileState.pushForce = projectile.data.pushForce;
                    projectileState.resetPreviousHitStun = projectile.data.resetPreviousHitStun;
                    projectileState.spaceBetweenHits = projectile.data.spaceBetweenHits;
                    projectileState.speed = projectile.data.speed;
                    projectileState.totalHits = projectile.totalHits;
                    projectileState.unblockable = projectile.data.unblockable;


                    if (projectile.currentPushForce != null) {
                        projectileState.currentPushForce = projectile.currentPushForce.Value;
                    } else {
                        projectileState.currentPushForce = null;
                    }


                    projectileState.creationFrame = projectile.creationFrame;
                    projectileState.destructionFrame = projectile.destructionFrame;


                    projectileState.hitBox = new FluxHitBoxState();
                    projectileState.hitBox.bodyPart = projectile.data.hitBox.bodyPart;
                    projectileState.hitBox.collisionType = projectile.data.hitBox.collisionType;
                    projectileState.hitBox.defaultVisibility = projectile.data.hitBox.defaultVisibility;
                    projectileState.hitBox.followXBounds = projectile.data.hitBox.followXBounds;
                    projectileState.hitBox.followYBounds = projectile.data.hitBox.followYBounds;
                    projectileState.hitBox.hide = projectile.data.hitBox.hide;
                    projectileState.hitBox.radius = projectile.data.hitBox.radius;
                    projectileState.hitBox.rect = new Rect(projectile.data.hitBox.rect);
                    projectileState.hitBox.rendererBounds = new Rect(projectile.data.hitBox.rendererBounds);
                    projectileState.hitBox.shape = projectile.data.hitBox.shape;
                    projectileState.hitBox.state = projectile.data.hitBox.state;
                    projectileState.hitBox.type = projectile.data.hitBox.type;
                    projectileState.hitBox.visibility = projectile.data.hitBox.visibility;

                    projectileState.hitBox.offSet = new Vector2(
                        projectileState.hitBox.offSet.x,
                        projectileState.hitBox.offSet.y
                    );

                    //					projectile.data.hitBox.position = projectileState.projectile.transform;
                    //					if (projectileState.projectile.transform != null){
                    //						state.hitBoxesState.hitBoxes[i].transformState = new FluxTransformState(
                    //							projectileState.projectile.transform.parent,
                    //							projectileState.projectile.transform.localPosition,
                    //							projectileState.projectile.transform.localRotation,
                    //							projectileState.projectile.transform.localScale,
                    //							projectileState.projectile.transform.gameObject.activeSelf
                    //						);
                    //					}


                    projectileState.hurtBox = new FluxHurtBoxState();
                    projectileState.hurtBox.bodyPart = projectile.data.hurtBox.bodyPart;
                    projectileState.hurtBox.followXBounds = projectile.data.hurtBox.followXBounds;
                    projectileState.hurtBox.followYBounds = projectile.data.hurtBox.followYBounds;
                    projectileState.hurtBox.isBlock = projectile.data.hurtBox.isBlock;
                    projectileState.hurtBox.offSet = projectile.data.hurtBox.offSet;
                    projectileState.hurtBox.position = projectile.data.hurtBox.position;
                    projectileState.hurtBox.radius = projectile.data.hurtBox.radius;
                    projectileState.hurtBox.rect = new Rect(projectile.data.hurtBox.rect);
                    projectileState.hurtBox.rendererBounds = new Rect(projectile.data.hurtBox.rendererBounds);
                    projectileState.hurtBox.shape = projectile.data.hurtBox.shape;


                    projectileState.transformState = new FluxTransformState(
                        projectile.transform.parent,
                        projectile.transform.localPosition,
                        projectile.transform.localRotation,
                        projectile.transform.localScale,
                        projectile.gameObject.activeSelf
                    );

                    state.projectiles.Add(projectileState);
                }
            }*/
        }

        return state;
    }
}
