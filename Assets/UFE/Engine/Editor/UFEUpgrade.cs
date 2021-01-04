using UnityEngine;
using UnityEditor;
using FPLibrary;
using UFE3D;

public class UFEUpgrade : EditorWindow {

    private static GlobalInfo globalInfo;
    private static UFE3D.CharacterInfo characterInfo;
    private static MoveInfo moveInfo;

    private static SubKnockdownOptions UpgradeKnockdownOptions(SubKnockdownOptions knockDown) {
        knockDown._knockedOutTime = knockDown.knockedOutTime;
        knockDown._standUpTime = knockDown.standUpTime;
        knockDown._predefinedPushForce = FPVector.ToFPVector(knockDown.predefinedPushForce);
        return knockDown;
    }
    private static HitTypeOptions UpgradeHitOptions(HitTypeOptions hitType) {
        hitType._freezingTime = hitType.freezingTime;
        hitType._animationSpeed = hitType.animationSpeed;
        hitType._hitStop = hitType.hitStop;
        hitType._shakeDensity = hitType.shakeDensity;
        return hitType;
    }

    private static bool RetrieveSelection()
    {
        globalInfo = null;
        characterInfo = null;
        moveInfo = null;
        UnityEngine.Object[] selection = Selection.GetFiltered(typeof(GlobalInfo), SelectionMode.Assets);
        if (selection.Length > 0)
        {
            if (selection[0] == null) return false;
            globalInfo = (GlobalInfo)selection[0];

        }
        selection = Selection.GetFiltered(typeof(UFE3D.CharacterInfo), SelectionMode.Assets);
        if (selection.Length > 0)
        {
            if (selection[0] == null) return false;
            characterInfo = (UFE3D.CharacterInfo)selection[0];

        }
        selection = Selection.GetFiltered(typeof(MoveInfo), SelectionMode.Assets);
        if (selection.Length > 0)
        {
            if (selection[0] == null) return false;
            moveInfo = (MoveInfo)selection[0];

        }

        if (globalInfo == null && characterInfo == null && moveInfo == null)
        {
            EditorUtility.DisplayDialog("UFE Upgrade", "Must be a valid UFE file", "Ok");
            return false;
        }

        return true;
    }

    [MenuItem("Assets/UFE 2.0/Update All Definitions")]
    public static void UpdateAll()
    {
        UpdateInputs();
        UpdateVariables();
    }

    [MenuItem("Assets/UFE 2.0/Update Input Definitions")]
    public static void UpdateInputs()
    {
        if (!RetrieveSelection()) return;

        bool dontAskAgain = false;
        if (globalInfo != null)
        {
            foreach (UFE3D.CharacterInfo character in globalInfo.characters)
            {
                if (character == null) continue;
                foreach (MoveSetData moveSet in character.moves)
                {
                    foreach (MoveInfo move in moveSet.attackMoves)
                    {
                        MoveInputUpdate(move, ref dontAskAgain);
                    }
                }
            }
        }
        else if (characterInfo != null)
        {
            foreach (MoveSetData moveSet in characterInfo.moves)
            {
                foreach (MoveInfo move in moveSet.attackMoves)
                {
                    MoveInputUpdate(move, ref dontAskAgain);
                }
            }
        }
        else if (moveInfo != null)
        {
            MoveInputUpdate(moveInfo, ref dontAskAgain);
        }
    }

    private static void MoveInputUpdate(MoveInfo move, ref bool dontAskAgain)
    {
        int updateConfirm = dontAskAgain ? 0 : 1;
        if (updateConfirm == 1)
            updateConfirm = EditorUtility.DisplayDialogComplex("Override Input", "Move " + move.name + " already have a default input definition. Override anyway?", "Yes", "No", "Yes to All");

        if (updateConfirm == 0 || updateConfirm == 2)
        {
            if (updateConfirm == 2) dontAskAgain = true;
            move.defaultInputs.chargeMove = move.chargeMove;
            move.defaultInputs._chargeTiming = move._chargeTiming;
            move.defaultInputs.allowInputLeniency = move.allowInputLeniency;
            move.defaultInputs.allowNegativeEdge = move.allowNegativeEdge;
            move.defaultInputs.leniencyBuffer = move.leniencyBuffer;
            move.defaultInputs.onReleaseExecution = move.onReleaseExecution;
            move.defaultInputs.requireButtonPress = move.requireButtonPress;
            move.defaultInputs.onPressExecution = move.onPressExecution;
            move.defaultInputs.buttonSequence = (ButtonPress[])move.buttonSequence.Clone();
            move.defaultInputs.buttonExecution = (ButtonPress[])move.buttonExecution.Clone();

            EditorUtility.SetDirty(move);
            Debug.Log("Move " + move.name + " updated.");
        }
    }

    [MenuItem("Assets/UFE 2.0/Update Variable Definitions")]
    public static void UpdateVariables()
    {
        if (!RetrieveSelection()) return;

        bool updateConfirm = true;
        string warningText = "This file seems to be already converted to 2.0. Converting the data again will revert your project to when you originally imported. Continue?";
        if (globalInfo != null)
        {
            if (globalInfo.version >= 2f)
            {
                updateConfirm = false;
                updateConfirm = EditorUtility.DisplayDialog("Global Asset Update", warningText, "Yes", "No");
            }

            if (updateConfirm)
            {
                GlobalUpdate(globalInfo);
            }
        }
        else if (characterInfo != null)
        {
            if (characterInfo.version >= 2f)
            {
                updateConfirm = false;
                updateConfirm = EditorUtility.DisplayDialog("Character Asset Update", warningText, "Yes", "No");
            }

            if (updateConfirm)
            {
                CharacterUpdate(characterInfo);
            }
        }
        else if (moveInfo != null)
        {
            if (moveInfo.version >= 2f)
            {
                updateConfirm = false;
                updateConfirm = EditorUtility.DisplayDialog("Move Asset Update", warningText, "Yes", "No");
            }

            if (updateConfirm)
            {
                SpecialMoveUpdate(moveInfo);
            }
        }
        // End of Update
    }

    private static void GlobalUpdate(GlobalInfo global)
    {
        global.version = 2f;
        // Camera Options
        global.cameraOptions._maxDistance = global.cameraOptions.maxDistance;
        // Character Rotation Options
        global.characterRotationOptions._rotationSpeed = global.characterRotationOptions.rotationSpeed;
        global.characterRotationOptions._mirrorBlending = global.characterRotationOptions.mirrorBlending;
        // Combo Options
        global.comboOptions._minHitStun = Mathf.RoundToInt(global.comboOptions.minHitStun);
        global.comboOptions._minDamage = global.comboOptions.minDamage;
        global.comboOptions._minPushForce = global.comboOptions.minPushForce;
        global.comboOptions._knockBackMinForce = global.comboOptions.knockBackMinForce;
        global.comboOptions._juggleWeight = global.comboOptions.juggleWeight;
        // Ground Bounce Options
        global.groundBounceOptions._minimumBounceForce = global.groundBounceOptions.minimumBounceForce;
        global.groundBounceOptions._maximumBounces = global.groundBounceOptions.maximumBounces;
        global.groundBounceOptions._shakeDensity = global.groundBounceOptions.shakeDensity;
        // Wall Bounce Options
        global.wallBounceOptions._minimumBounceForce = global.wallBounceOptions.minimumBounceForce;
        global.wallBounceOptions._maximumBounces = global.wallBounceOptions.maximumBounces;
        global.wallBounceOptions._shakeDensity = global.wallBounceOptions.shakeDensity;
        // Block Options
        global.blockOptions._parryTiming = global.blockOptions.parryTiming;
        // Knockdown Options
        global.knockDownOptions.air = UpgradeKnockdownOptions(global.knockDownOptions.air);
        global.knockDownOptions.high = UpgradeKnockdownOptions(global.knockDownOptions.high);
        global.knockDownOptions.highLow = UpgradeKnockdownOptions(global.knockDownOptions.highLow);
        global.knockDownOptions.sweep = UpgradeKnockdownOptions(global.knockDownOptions.sweep);
        global.knockDownOptions.crumple = UpgradeKnockdownOptions(global.knockDownOptions.crumple);
        global.knockDownOptions.wallbounce = UpgradeKnockdownOptions(global.knockDownOptions.wallbounce);
        // Hit Options
        global.hitOptions.weakHit = UpgradeHitOptions(global.hitOptions.weakHit);
        global.hitOptions.mediumHit = UpgradeHitOptions(global.hitOptions.mediumHit);
        global.hitOptions.heavyHit = UpgradeHitOptions(global.hitOptions.heavyHit);
        global.hitOptions.crumpleHit = UpgradeHitOptions(global.hitOptions.crumpleHit);
        global.hitOptions.customHit1 = UpgradeHitOptions(global.hitOptions.customHit1);
        global.hitOptions.customHit2 = UpgradeHitOptions(global.hitOptions.customHit2);
        global.hitOptions.customHit3 = UpgradeHitOptions(global.hitOptions.customHit3);
        // Stage Options
        foreach (StageOptions stage in global.stages)
        {
            stage._groundFriction = stage.groundFriction;
            stage._leftBoundary = stage.leftBoundary;
            stage._rightBoundary = stage.rightBoundary;
        }
        // Counter Hit Options
        global.counterHitOptions._damageIncrease = global.counterHitOptions.damageIncrease;
        global.counterHitOptions._hitStunIncrease = global.counterHitOptions.hitStunIncrease;
        // Round Options
        global.roundOptions._timer = global.roundOptions.timer;
        global.roundOptions._timerSpeed = global.roundOptions.timerSpeed;
        global.roundOptions._p1XPosition = global.roundOptions.p1XPosition;
        global.roundOptions._p2XPosition = global.roundOptions.p2XPosition;
        global.roundOptions._endGameDelay = global.roundOptions.endGameDelay;
        global.roundOptions._newRoundDelay = global.roundOptions.newRoundDelay;
        global.roundOptions._slowMoTimer = global.roundOptions.slowMoTimer;
        global.roundOptions._slowMoSpeed = global.roundOptions.slowMoSpeed;
        // Global Options
        global._gameSpeed = global.gameSpeed;
        global._preloadingTime = global.preloadingTime;
        global._gravity = global.gravity;


        // Character Update
        foreach (UFE3D.CharacterInfo character in global.characters)
        {
            if (character == null) continue;
            CharacterUpdate(character);
        }

        EditorUtility.SetDirty(global);
        Debug.Log("Global Options updated.");
    }

    private static void CharacterUpdate(UFE3D.CharacterInfo character) {
        character.version = 2f;
        character._executionTiming = character.executionTiming;
        character._blendingTime = character.blendingTime;

        // Character Physics
        character.physics._moveForwardSpeed = character.physics.moveForwardSpeed;
        character.physics._moveBackSpeed = character.physics.moveBackSpeed;
        character.physics._friction = character.physics.friction;
        character.physics._minJumpForce = character.physics.minJumpForce;
        character.physics._jumpDistance = character.physics.jumpDistance;
        character.physics._weight = character.physics.weight;
        character.physics._groundCollisionMass = character.physics.groundCollisionMass;

        // Move Set
        if (character.moves != null && character.moves.Length > 0)
        {
            foreach (MoveSetData moveSetData in character.moves)
            {
                // Basic Moves
                BasicMoveUpdate(moveSetData.basicMoves.idle);
                BasicMoveUpdate(moveSetData.basicMoves.moveForward);
                BasicMoveUpdate(moveSetData.basicMoves.moveBack);
                BasicMoveUpdate(moveSetData.basicMoves.crouching);

                BasicMoveUpdate(moveSetData.basicMoves.takeOff);
                BasicMoveUpdate(moveSetData.basicMoves.jumpStraight);
                BasicMoveUpdate(moveSetData.basicMoves.jumpBack);
                BasicMoveUpdate(moveSetData.basicMoves.jumpForward);
                BasicMoveUpdate(moveSetData.basicMoves.fallStraight);
                BasicMoveUpdate(moveSetData.basicMoves.fallBack);
                BasicMoveUpdate(moveSetData.basicMoves.fallForward);
                BasicMoveUpdate(moveSetData.basicMoves.landing);

                BasicMoveUpdate(moveSetData.basicMoves.blockingHighPose);
                BasicMoveUpdate(moveSetData.basicMoves.blockingHighHit);
                BasicMoveUpdate(moveSetData.basicMoves.blockingLowHit);
                BasicMoveUpdate(moveSetData.basicMoves.blockingCrouchingPose);
                BasicMoveUpdate(moveSetData.basicMoves.blockingCrouchingHit);
                BasicMoveUpdate(moveSetData.basicMoves.blockingAirPose);
                BasicMoveUpdate(moveSetData.basicMoves.blockingAirHit);

                BasicMoveUpdate(moveSetData.basicMoves.parryHigh);
                BasicMoveUpdate(moveSetData.basicMoves.parryLow);
                BasicMoveUpdate(moveSetData.basicMoves.parryCrouching);
                BasicMoveUpdate(moveSetData.basicMoves.parryAir);

                BasicMoveUpdate(moveSetData.basicMoves.getHitHigh);
                BasicMoveUpdate(moveSetData.basicMoves.getHitLow);
                BasicMoveUpdate(moveSetData.basicMoves.getHitCrouching);
                BasicMoveUpdate(moveSetData.basicMoves.getHitAir);
                BasicMoveUpdate(moveSetData.basicMoves.getHitKnockBack);
                BasicMoveUpdate(moveSetData.basicMoves.getHitHighKnockdown);
                BasicMoveUpdate(moveSetData.basicMoves.getHitMidKnockdown);
                BasicMoveUpdate(moveSetData.basicMoves.getHitSweep);
                BasicMoveUpdate(moveSetData.basicMoves.getHitCrumple);

                BasicMoveUpdate(moveSetData.basicMoves.fallDown);
                BasicMoveUpdate(moveSetData.basicMoves.groundBounce);
                BasicMoveUpdate(moveSetData.basicMoves.airWallBounce);
                BasicMoveUpdate(moveSetData.basicMoves.fallingFromGroundBounce);
                BasicMoveUpdate(moveSetData.basicMoves.standUp);

                // Special Moves
                foreach (MoveInfo moveInfo in moveSetData.attackMoves)
                {
                    SpecialMoveUpdate(moveInfo);
                }
                SpecialMoveUpdate(moveSetData.cinematicIntro);
                SpecialMoveUpdate(moveSetData.cinematicOutro);
            }
        }

        EditorUtility.SetDirty(character);
        Debug.Log("Character " + character.characterName + " updated.");
    }

    private static void BasicMoveUpdate(BasicMoveInfo basicMove) {
        if (basicMove.clip1 != null) {
            basicMove.animMap[0].clip = basicMove.clip1;
            basicMove.animMap[0].length = basicMove.clip1.length;
        }
        if (basicMove.clip2 != null) {
            basicMove.animMap[1].clip = basicMove.clip2;
            basicMove.animMap[1].length = basicMove.clip2.length;
        }
        if (basicMove.clip3 != null) {
            basicMove.animMap[2].clip = basicMove.clip3;
            basicMove.animMap[2].length = basicMove.clip3.length;
        }
        if (basicMove.clip4 != null) {
            basicMove.animMap[3].clip = basicMove.clip4;
            basicMove.animMap[3].length = basicMove.clip4.length;
        }
        if (basicMove.clip5 != null) {
            basicMove.animMap[4].clip = basicMove.clip5;
            basicMove.animMap[4].length = basicMove.clip5.length;
        }
        if (basicMove.clip6 != null) {
            basicMove.animMap[5].clip = basicMove.clip6;
            basicMove.animMap[5].length = basicMove.clip6.length;
        }

        basicMove._animationSpeed = basicMove.animationSpeed;
        basicMove._restingClipInterval = basicMove.restingClipInterval;
        basicMove._blendingIn = basicMove.blendingIn;
        basicMove._blendingOut = basicMove.blendingOut;
    }

    private static void SpecialMoveUpdate(MoveInfo move) {
        if (move == null) return;
        move.version = 2f;
        move._gaugeDPS = move.gaugeDPS;
        move._totalDrain = move.totalDrain;
        move._gaugeRequired = move.gaugeRequired;
        move._gaugeUsage = move.gaugeUsage;
        move._gaugeGainOnMiss = move.gaugeGainOnMiss;
        move._gaugeGainOnHit = move.gaugeGainOnHit;
        move._gaugeGainOnBlock = move.gaugeGainOnBlock;
        move._opGaugeGainOnBlock = move.opGaugeGainOnBlock;
        move._opGaugeGainOnParry = move.opGaugeGainOnParry;
        move._opGaugeGainOnHit = move.opGaugeGainOnHit;
        move._blendingIn = move.blendingIn;
        move._blendingOut = move.blendingOut;
        move._chargeTiming = move.chargeTiming;
        move.blockableArea._radius = move.blockableArea.radius;
        move.blockableArea._offSet = FPVector.ToFPVector(move.blockableArea.offSet);
        move._animationSpeed = move.animationSpeed;

        if (move.animationClip == null) {
            Debug.LogWarning("Move " + move.name + " has no animation attached.");
        } else {
            move.animMap.clip = move.animationClip;
            move.animMap.length = move.animationClip.length;
        }

        foreach (Projectile projectile in move.projectiles) {
            projectile._damageOnHit = projectile.damageOnHit;
            projectile._damageOnBlock = projectile.damageOnBlock;
            projectile._castingOffSet = FPVector.ToFPVector(projectile.castingOffSet);
            projectile._pushForce = FPVector.ToFPVector(projectile.pushForce);
            projectile.hurtBox._radius = projectile.hurtBox.radius;
            projectile.hurtBox._offSet = FPVector.ToFPVector(projectile.hurtBox.offSet);
            projectile.hurtBox._rect = new FPRect(projectile.hurtBox.rect);
        }

        foreach (AppliedForce aForce in move.appliedForces) {
            aForce._force = FPVector.ToFPVector(aForce.force);
        }

        foreach (Hit hit in move.hits) {
            hit._newHitBlendingIn = hit.newHitBlendingIn;
            hit._newJuggleWeight = hit.newJuggleWeight;
            hit._hitStunOnHit = hit.hitStunOnHit;
            hit._hitStunOnBlock = hit.hitStunOnBlock;
            hit._damageOnHit = hit.damageOnHit;
            hit._damageOnBlock = hit.damageOnBlock;
            hit._newMovementSpeed = hit.newMovementSpeed;
            hit._newRotationSpeed = hit.newRotationSpeed;
            hit._cameraSpeedDuration = hit.cameraSpeedDuration;
            hit._pushForce = FPVector.ToFPVector(hit.pushForce);
            hit._pushForceAir = FPVector.ToFPVector(hit.pushForceAir);
            hit._appliedForce = FPVector.ToFPVector(hit.appliedForce);
            hit._groundBouncePushForce = FPVector.ToFPVector(hit.groundBouncePushForce);
            hit._wallBouncePushForce = FPVector.ToFPVector(hit.wallBouncePushForce);
            foreach (HurtBox hurtBox in hit.hurtBoxes) {
                hurtBox._radius = hurtBox.radius;
                hurtBox._offSet = FPVector.ToFPVector(hurtBox.offSet);
                hurtBox._rect = new FPRect(hurtBox.rect);
            }
            hit.pullEnemyIn._targetDistance = hit.pullEnemyIn.targetDistance;
            hit.pullSelfIn._targetDistance = hit.pullSelfIn.targetDistance;

            if (hit.overrideHitEffects) {
                hit.hitEffects = UpgradeHitOptions(hit.hitEffects);
            }
        }

        foreach (SlowMoEffect slowMo in move.slowMoEffects) {
            slowMo._duration = slowMo.duration;
            slowMo._percentage = slowMo.percentage;
        }

        foreach (CameraMovement camMove in move.cameraMovements) {
            camMove._duration = camMove.duration;
            camMove._myAnimationSpeed = camMove.myAnimationSpeed;
            camMove._opAnimationSpeed = camMove.opAnimationSpeed;
        }

        foreach (OpponentOverride opOvr in move.opponentOverride) {
            opOvr._stunTime = opOvr.stunTime;
            opOvr._position = FPVector.ToFPVector(opOvr.position);
        }

        foreach (AnimSpeedKeyFrame animKey in move.animSpeedKeyFrame) {
            animKey._speed = animKey.speed;
        }


        EditorUtility.SetDirty(move);
        Debug.Log("Move " + move.name + " updated.");
    }
}
