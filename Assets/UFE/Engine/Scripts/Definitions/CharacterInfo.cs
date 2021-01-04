using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using FPLibrary;
using UFE3D;

[System.Serializable]
public class PhysicsData {
	public float moveForwardSpeed = 4f; // How fast this character can move forward
	public Fix64 _moveForwardSpeed = 4;
	public float moveBackSpeed = 3.5f; // How fast this character can move backwards
	public Fix64 _moveBackSpeed = 3.5; // How fast this character can move backwards
	public bool highMovingFriction = true; // When releasing the horizontal controls character will stop imediatelly
	public float friction = 30f; // Friction used in case of highMovingFriction false. Also used when player is pushed
	public Fix64 _friction = 30; // Friction used in case of highMovingFriction false. Also used when player is pushed

    public bool canCrouch = true;
    public int crouchDelay = 2;
    public int standingDelay = 2;

	public bool canJump = true;
    public bool pressureSensitiveJump = false; // How high this character will jumps
    public bool overrideCrouch = false;
	public float jumpForce = 40f; // How high this character will jumps
	public Fix64 _jumpForce = 40; // How high this character will jumps
	public float minJumpForce = 30f;
	public Fix64 _minJumpForce = 30;
    public int minJumpDelay = 4;
	public float jumpDistance = 8f; // How far this character will move horizontally while jumping
	public Fix64 _jumpDistance = 8; // How far this character will move horizontally while jumping
	public bool cumulativeForce = true; // If this character is being juggled, should new forces add to or replace existing force?
	public int multiJumps = 1; // Can this character double or triple jump? Set how many times the character can jump here
	public float weight = 175;
	public Fix64 _weight = 175;
	public int jumpDelay = 8;
	public int landingDelay = 7;
	public float groundCollisionMass = 2;
	public Fix64 _groundCollisionMass = 2;
}

[System.Serializable]
public class HeadLook {
	public bool enabled = false;
	public BendingSegment[] segments = new BendingSegment[0];
	public NonAffectedJoints[] nonAffectedJoints = new NonAffectedJoints[0];
	public BodyPart target = BodyPart.head;
	public float effect = 1;
	public bool overrideAnimation = true;
	public bool disableOnHit = true;
}

[System.Serializable]
public class CustomControls {
    public bool enabled = false;
    public bool overrideInputs = false;
    public ButtonPress walkForward = ButtonPress.Forward;
    public ButtonPress walkBack = ButtonPress.Back;
    public ButtonPress crouch = ButtonPress.Down;
    public ButtonPress jump = ButtonPress.Up;
    public ButtonPress button1 = ButtonPress.Button1;
    public ButtonPress button2 = ButtonPress.Button2;
    public ButtonPress button3 = ButtonPress.Button3;
    public ButtonPress button4 = ButtonPress.Button4;
    public ButtonPress button5 = ButtonPress.Button5;
    public ButtonPress button6 = ButtonPress.Button6;
    public ButtonPress button7 = ButtonPress.Button7;
    public ButtonPress button8 = ButtonPress.Button8;
    public ButtonPress button9 = ButtonPress.Button9;
    public ButtonPress button10 = ButtonPress.Button10;
    public ButtonPress button11 = ButtonPress.Button11;
    public ButtonPress button12 = ButtonPress.Button12;
    public bool overrideControlFreak = false;
    public InputTouchControllerBridge controlFreak2Prefab = null;
}

[System.Serializable]
public class MoveSetData: ICloneable {
	public CombatStances combatStance = CombatStances.Stance1; // This move set combat stance
	public MoveInfo cinematicIntro;
	public MoveInfo cinematicOutro;

	public BasicMoves basicMoves = new BasicMoves(); // List of basic moves
	public MoveInfo[] attackMoves = new MoveInfo[0]; // List of attack moves
	
	[HideInInspector] public bool enabledBasicMovesToggle;
	[HideInInspector] public bool basicMovesToggle;
	[HideInInspector] public bool attackMovesToggle;


    public StanceInfo ConvertData() {
        StanceInfo stanceData = new StanceInfo();
        stanceData.combatStance = this.combatStance;
        stanceData.cinematicIntro = this.cinematicIntro;
        stanceData.cinematicOutro = this.cinematicOutro;
        stanceData.basicMoves = this.basicMoves;
        stanceData.attackMoves = this.attackMoves;

        return stanceData;
    }

    public object Clone() {
		return CloneObject.Clone(this);
	}
}

[System.Serializable]
public class AltCostume {
    public string name;
    public StorageMode characterPrefabStorage = StorageMode.Legacy;
    public GameObject prefab;
    public string prefabResourcePath;
    public bool enableColorMask;
    public Color colorMask;
}

namespace UFE3D
{
    [System.Serializable]
    public class CharacterInfo : ScriptableObject
    {
        public float version;
        public Texture2D profilePictureSmall;
        public Texture2D profilePictureBig;
        public string characterName;
        public Gender gender;
        public string characterDescription;
        public AnimationClip selectionAnimation;
        public AudioClip selectionSound;
        public AudioClip deathSound;
        public float height;
        public int age;
        public string bloodType;
        public int lifePoints = 1000;
        public int maxGaugePoints;
        public StorageMode characterPrefabStorage = StorageMode.Legacy;
        public GameObject characterPrefab; // The prefab representing the character (must have hitBoxScript attached to it)
        public string prefabResourcePath; // Resource Path alternative loading
        public AltCostume[] alternativeCostumes = new AltCostume[0];
        public FPVector initialPosition;
        public FPQuaternion initialRotation;

        public PhysicsData physics;
        public HeadLook headLook;
        public CustomControls customControls;

        public float executionTiming = .3f; // How fast the player needs to press each key during the execution of a special move
        public Fix64 _executionTiming = .3; // How fast the player needs to press each key during the execution of a special move
        public int possibleAirMoves = 1; // How many moves this character can perform while in the air
        public float blendingTime = .1f; // The speed of transiction between basic moves
        public Fix64 _blendingTime = .1; // The speed of transiction between basic moves

        public AnimationType animationType;
        public Avatar avatar; // Mecanim variable
        public bool applyRootMotion; // Mecanim variable
        public AnimationFlow animationFlow;
        public bool useAnimationMaps;

        public string[] stanceResourcePath = new string[0];
        public MoveSetData[] moves = new MoveSetData[0];
        public AIInstructionsSet[] aiInstructionsSet = new AIInstructionsSet[0];

        public int playerNum { get; set; }
        public bool isAlt { get; set; }
        public int selectedCostume { get; set; }
        public MoveSetData[] loadedMoves { get; set; }

        #region trackable definitions
        public CombatStances currentCombatStance { get; set; }
        public Fix64 currentLifePoints { get; set; }
        public Fix64 currentGaugePoints { get; set; }
        #endregion
    }
}