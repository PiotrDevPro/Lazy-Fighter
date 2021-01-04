#if UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
#define UNITY_PRE_5_0
#endif

#if UNITY_PRE_5_0 || UNITY_5_0
#define UNITY_PRE_5_1
#endif

#if UNITY_PRE_5_1 || UNITY_5_1
#define UNITY_PRE_5_2
#endif

#if UNITY_PRE_5_2 || UNITY_5_2
#define UNITY_PRE_5_3
#endif

#if UNITY_PRE_5_3 || UNITY_5_3
#define UNITY_PRE_5_4
#endif


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using FPLibrary;
using UFENetcode;
using UFE3D;

[RequireComponent(typeof(EventSystem))]
public class UFE : MonoBehaviour, UFEInterface
{
	#region public instance enum
	public enum MultiplayerMode{
		Lan,
		Online,
		Bluetooth,
	}
	#endregion

	#region public instance properties
    public GlobalInfo UFE_Config;
	#endregion

	#region public event definitions
	public delegate void MeterHandler(float newFloat, UFE3D.CharacterInfo player);
	public static event MeterHandler OnLifePointsChange;

	public delegate void IntHandler(int newInt);
	public static event IntHandler OnRoundBegins;

	public delegate void StringHandler(string newString, UFE3D.CharacterInfo player);
	public static event StringHandler OnNewAlert;
	
	public delegate void HitHandler(HitBox strokeHitBox, MoveInfo move, UFE3D.CharacterInfo player);
    public static event HitHandler OnHit;
    public static event HitHandler OnBlock;
    public static event HitHandler OnParry;

	public delegate void MoveHandler(MoveInfo move, UFE3D.CharacterInfo player);
	public static event MoveHandler OnMove;

    public delegate void ButtonHandler(ButtonPress button, UFE3D.CharacterInfo player);
    public static event ButtonHandler OnButton;

    public delegate void BasicMoveHandler(BasicMoveReference basicMove, UFE3D.CharacterInfo player);
    public static event BasicMoveHandler OnBasicMove;

    public delegate void BodyVisibilityHandler(MoveInfo move, UFE3D.CharacterInfo player, BodyPartVisibilityChange bodyPartVisibilityChange, HitBox hitBox);
    public static event BodyVisibilityHandler OnBodyVisibilityChange;

    public delegate void ParticleEffectsHandler(MoveInfo move, UFE3D.CharacterInfo player, MoveParticleEffect particleEffects);
    public static event ParticleEffectsHandler OnParticleEffects;

    public delegate void SideSwitchHandler(int side, UFE3D.CharacterInfo player);
    public static event SideSwitchHandler OnSideSwitch;

	public delegate void GameBeginHandler(UFE3D.CharacterInfo player1, UFE3D.CharacterInfo player2, StageOptions stage);
	public static event GameBeginHandler OnGameBegin;

	public delegate void GameEndsHandler(UFE3D.CharacterInfo winner, UFE3D.CharacterInfo loser);
	public static event GameEndsHandler OnGameEnds;
	public static event GameEndsHandler OnRoundEnds;

	public delegate void GamePausedHandler(bool isPaused);
	public static event GamePausedHandler OnGamePaused;

	public delegate void ScreenChangedHandler(UFEScreen previousScreen, UFEScreen newScreen);
	public static event ScreenChangedHandler OnScreenChanged;

	public delegate void StoryModeHandler(UFE3D.CharacterInfo character);
	public static event StoryModeHandler OnStoryModeStarted;
	public static event StoryModeHandler OnStoryModeCompleted;

	public delegate void TimerHandler(Fix64 time);
	public static event TimerHandler OnTimer;

	public delegate void TimeOverHandler();
	public static event TimeOverHandler OnTimeOver;

	public delegate void InputHandler(InputReferences[] inputReferences, int player);
	public static event InputHandler OnInput;
	#endregion

	#region network definitions
    //-----------------------------------------------------------------------------------------------------------------
	// Network
	//-----------------------------------------------------------------------------------------------------------------
    public static MultiplayerAPI multiplayerAPI{
		get{
			if (UFE.multiplayerMode == MultiplayerMode.Bluetooth){
				return UFE.bluetoothMultiplayerAPI;
			}else if (UFE.multiplayerMode == MultiplayerMode.Lan){
				return UFE.lanMultiplayerAPI;
			}else{
				return UFE.onlineMultiplayerAPI;
			}
		}
	}

	public static MultiplayerMode multiplayerMode{
		get{
			return UFE._multiplayerMode;
		}
		set{
			UFE._multiplayerMode = value;

			if (value == MultiplayerMode.Bluetooth){
				UFE.bluetoothMultiplayerAPI.enabled = true;
				UFE.lanMultiplayerAPI.enabled = false;
				UFE.onlineMultiplayerAPI.enabled = false;
			}else if (value == MultiplayerMode.Lan){
				UFE.bluetoothMultiplayerAPI.enabled = false;
				UFE.lanMultiplayerAPI.enabled = true;
				UFE.onlineMultiplayerAPI.enabled = false;
			}else{
				UFE.bluetoothMultiplayerAPI.enabled = false;
				UFE.lanMultiplayerAPI.enabled = false;
				UFE.onlineMultiplayerAPI.enabled = true;
			}
		}
	}

	private static MultiplayerAPI bluetoothMultiplayerAPI;
	private static MultiplayerAPI lanMultiplayerAPI;
	private static MultiplayerAPI onlineMultiplayerAPI;

	private static MultiplayerMode _multiplayerMode = MultiplayerMode.Lan;
    #endregion
    
	#region gui definitions
    //-----------------------------------------------------------------------------------------------------------------
	// GUI
	//-----------------------------------------------------------------------------------------------------------------
	public static Canvas canvas{get; protected set;}
	public static CanvasGroup canvasGroup{get; protected set;}
	public static EventSystem eventSystem{get; protected set;}
	public static GraphicRaycaster graphicRaycaster{get; protected set;}
	public static StandaloneInputModule standaloneInputModule{get; protected set;}
	//-----------------------------------------------------------------------------------------------------------------
	protected static readonly string MusicEnabledKey = "MusicEnabled";
	protected static readonly string MusicVolumeKey = "MusicVolume";
	protected static readonly string SoundsEnabledKey = "SoundsEnabled";
	protected static readonly string SoundsVolumeKey = "SoundsVolume";
	protected static readonly string DifficultyLevelKey = "DifficultyLevel";
	protected static readonly string DebugModeKey = "DebugMode";
	//-----------------------------------------------------------------------------------------------------------------

	public static CameraScript cameraScript{get; set;}
    public static FluxCapacitor fluxCapacitor;
	public static GameMode gameMode = GameMode.None;
	public static GlobalInfo config;
	public static UFE UFEInstance;
	public static bool debug = true;
	public static Text debugger1;
	public static Text debugger2;
    #endregion

    #region addons definitions
    public static bool isAiAddonInstalled {get; set;}
    public static bool isCInputInstalled { get; set; }
    public static bool isControlFreakInstalled { get; set; }
    public static bool isControlFreak1Installed { get; set; }
    public static bool isControlFreak2Installed { get; set; }
    public static bool isRewiredInstalled { get; set; }
    public static bool isNetworkAddonInstalled {get; set; }
    public static bool isPhotonInstalled { get; set; }
    public static bool isBluetoothAddonInstalled { get; set; }
    public static GameObject controlFreakPrefab;
    public static InputTouchControllerBridge touchControllerBridge;
    #endregion
    
    #region screen definitions
    public static UFEScreen currentScreen{get; protected set;}
	public static UFEScreen battleGUI{get; protected set;}
	public static GameObject gameEngine{get; protected set;}
    #endregion

    #region trackable definitions
    public static bool freeCamera;
    public static bool freezePhysics;
    public static bool newRoundCasted;
    public static bool normalizedCam = true;
    public static bool pauseTimer;
    public static Fix64 timer;
    public static Fix64 timeScale;
    public static ControlsScript p1ControlsScript;
    public static ControlsScript p2ControlsScript;
    public static List<DelayedAction> delayedLocalActions = new List<DelayedAction>();
    public static List<DelayedAction> delayedSynchronizedActions = new List<DelayedAction>();
    public static List<InstantiatedGameObject> instantiatedObjects = new List<InstantiatedGameObject>();
    #endregion

    #region story mode definitions
    //-----------------------------------------------------------------------------------------------------------------
    // Required for the Story Mode: if the player lost its previous battle, 
    // he needs to fight the same opponent again, not the next opponent.
    //-----------------------------------------------------------------------------------------------------------------
    private static StoryModeInfo storyMode = new StoryModeInfo();
    private static List<string> unlockedCharactersInStoryMode = new List<string>();
    private static List<string> unlockedCharactersInVersusMode = new List<string>();
    private static bool player1WonLastBattle;
    private static int lastStageIndex;
    #endregion

    #region public definitions
    public static Fix64 fixedDeltaTime { get { return _fixedDeltaTime * timeScale; } set { _fixedDeltaTime = value; } }
    public static int intTimer;
    public static int fps = 60;
    public static long currentFrame { get; set; }
    public static bool gameRunning { get; protected set; }

    public static UFEController localPlayerController;
    public static UFEController remotePlayerController;
    #endregion

    #region private definitions
    private static Fix64 _fixedDeltaTime;
    private static AudioSource musicAudioSource;
	private static AudioSource soundsAudioSource;

	private static UFEController p1Controller;
	private static UFEController p2Controller;

	private static RandomAI p1RandomAI;
	private static RandomAI p2RandomAI;
	private static AbstractInputController p1FuzzyAI;
	private static AbstractInputController p2FuzzyAI;
	private static SimpleAI p1SimpleAI;
	private static SimpleAI p2SimpleAI;
    
    private static bool closing = false;
	private static bool disconnecting = false;
    private static List<object> memoryDump = new List<object>();
    #endregion


    #region public class methods: Delay the execution of a method maintaining synchronization between clients
    public static void DelayLocalAction(Action action, Fix64 seconds) {
        if (UFE.fixedDeltaTime > 0) {
            UFE.DelayLocalAction(action, (int)FPMath.Floor(seconds * config.fps));
		}else{
			UFE.DelayLocalAction(action, 1);
		}
	}

	public static void DelayLocalAction(Action action, int steps){
		UFE.DelayLocalAction(new DelayedAction(action, steps));
	}

	public static void DelayLocalAction(DelayedAction delayedAction){
		UFE.delayedLocalActions.Add(delayedAction);
	}

	public static void DelaySynchronizedAction(Action action, Fix64 seconds){
        if (UFE.fixedDeltaTime > 0) {
            UFE.DelaySynchronizedAction(action, (int)FPMath.Floor(seconds * config.fps));
		}else{
			UFE.DelaySynchronizedAction(action, 1);
		}
	}

	public static void DelaySynchronizedAction(Action action, int steps){
		UFE.DelaySynchronizedAction(new DelayedAction(action, steps));
	}

	public static void DelaySynchronizedAction(DelayedAction delayedAction){
		UFE.delayedSynchronizedActions.Add(delayedAction);
	}
	
	
	public static bool FindDelaySynchronizedAction(Action action){
		foreach (DelayedAction delayedAction in UFE.delayedSynchronizedActions){
			if (action == delayedAction.action) return true;
		}
		return false;
	}

    public static bool FindAndUpdateDelaySynchronizedAction(Action action, Fix64 seconds) {
		foreach (DelayedAction delayedAction in UFE.delayedSynchronizedActions){
			if (action == delayedAction.action) {
				delayedAction.steps = (int)FPMath.Floor(seconds * config.fps);
				return true;
			}
		}
		return false;
	}

    public static void FindAndRemoveDelaySynchronizedAction(Action action) {
        foreach (DelayedAction delayedAction in UFE.delayedSynchronizedActions) {
            if (action == delayedAction.action) {
                UFE.delayedSynchronizedActions.Remove(delayedAction);
                return;
            }
        }
    }

    public static void FindAndRemoveDelayLocalAction(Action action) {
        foreach (DelayedAction delayedAction in UFE.delayedLocalActions) {
            if (action == delayedAction.action) {
                UFE.delayedLocalActions.Remove(delayedAction);
                return;
            }
        }
    }

    public static GameObject SpawnGameObject(GameObject gameObject, Vector3 position, Quaternion rotation, long? destroyTimer = null) {
        if (gameObject == null) return null;

        GameObject goInstance = UnityEngine.Object.Instantiate(gameObject, position, rotation);
        goInstance.transform.SetParent(UFE.gameEngine.transform);
        MrFusion mrFusion = (MrFusion) goInstance.GetComponent(typeof(MrFusion));
        if (mrFusion == null) mrFusion = goInstance.AddComponent<MrFusion>();

        UFE.instantiatedObjects.Add(new InstantiatedGameObject(goInstance, mrFusion, UFE.currentFrame, UFE.currentFrame + destroyTimer));

        return goInstance;
    }

    public static void DestroyGameObject(GameObject gameObject, long? destroyTimer = null) {
        for (int i = 0; i < UFE.instantiatedObjects.Count; ++i) {
            if (UFE.instantiatedObjects[i].gameObject == gameObject) {
                UFE.instantiatedObjects[i].destructionFrame = destroyTimer == null? UFE.currentFrame : destroyTimer;
                break;
            }
        }
    }

	#endregion

	#region public class methods: Audio related methods
	public static bool GetMusic(){
		return config.music;
	}

	public static AudioClip GetMusicClip(){
		return UFE.musicAudioSource.clip;
	}
	
	public static bool GetSoundFX(){
		return config.soundfx;
	}

	public static float GetMusicVolume(){
		if (UFE.config != null) return config.musicVolume;
		return 1f;
	}

	public static float GetSoundFXVolume(){
		if (UFE.config != null) return UFE.config.soundfxVolume;
		return 1f;
	}

	public static void InitializeAudioSystem(){
		Camera cam = Camera.main;

		// Create the AudioSources required for the music and sound effects
		UFE.musicAudioSource = cam.GetComponent<AudioSource>();
		if (UFE.musicAudioSource == null){
			UFE.musicAudioSource = cam.gameObject.AddComponent<AudioSource>();
		}

		UFE.musicAudioSource.loop = true;
		UFE.musicAudioSource.playOnAwake = false;
		UFE.musicAudioSource.volume = config.musicVolume;


		UFE.soundsAudioSource = cam.gameObject.AddComponent<AudioSource>();
		UFE.soundsAudioSource.loop = false;
		UFE.soundsAudioSource.playOnAwake = false;
		UFE.soundsAudioSource.volume = 1f;
	}

	public static bool IsPlayingMusic(){
		if (UFE.musicAudioSource.clip != null) return UFE.musicAudioSource.isPlaying;
		return false;
	}

	public static bool IsMusicLooped(){
		return UFE.musicAudioSource.loop;
	}

	public static bool IsPlayingSoundFX(){
		return false;
	}

	public static void LoopMusic(bool loop){
		UFE.musicAudioSource.loop = loop;
	}

	public static void PlayMusic(){
		if (config.music && !UFE.IsPlayingMusic() && UFE.musicAudioSource.clip != null){
			UFE.musicAudioSource.Play();
		}
	}

	public static void PlayMusic(AudioClip music){
		if (music != null){
			AudioClip oldMusic = UFE.GetMusicClip();

			if (music != oldMusic){
				UFE.musicAudioSource.clip = music;
			}

			if (config.music && (music != oldMusic || !UFE.IsPlayingMusic())){
				UFE.musicAudioSource.Play();
			}
		}
	}

	public static void PlaySound(IList<AudioClip> sounds){
		if (sounds.Count > 0){
			UFE.PlaySound(sounds[UnityEngine.Random.Range(0, sounds.Count)]);
		}
	}
	
	public static void PlaySound(AudioClip soundFX){
		UFE.PlaySound(soundFX, UFE.GetSoundFXVolume());
	}

	public static void PlaySound(AudioClip soundFX, float volume){
		if (config.soundfx && soundFX != null && UFE.soundsAudioSource != null){
			UFE.soundsAudioSource.PlayOneShot(soundFX, volume);
		}
	}
	
	public static void SetMusic(bool on){
		bool isPlayingMusic = UFE.IsPlayingMusic();
		UFE.config.music = on;

		if (on && !isPlayingMusic)		UFE.PlayMusic();
		else if (!on && isPlayingMusic)	UFE.StopMusic();

		PlayerPrefs.SetInt(UFE.MusicEnabledKey, on ? 1 : 0);
		PlayerPrefs.Save();
	}
	
	public static void SetSoundFX(bool on){
		UFE.config.soundfx = on;
		PlayerPrefs.SetInt(UFE.SoundsEnabledKey, on ? 1 : 0);
		PlayerPrefs.Save();
	}
	
	public static void SetMusicVolume(float volume){
		if (UFE.config != null) UFE.config.musicVolume = volume;
		if (UFE.musicAudioSource != null) UFE.musicAudioSource.volume = volume;

		PlayerPrefs.SetFloat(UFE.MusicVolumeKey, volume);
		PlayerPrefs.Save();
	}

	public static void SetSoundFXVolume(float volume){
		if (UFE.config != null) UFE.config.soundfxVolume = volume;
		PlayerPrefs.SetFloat(UFE.SoundsVolumeKey, volume);
		PlayerPrefs.Save();
	}
    
    public static void StopMusic()
    {
        if (UFE.musicAudioSource.clip != null) UFE.musicAudioSource.Stop();
    }

    public static void StopSounds()
    {
        UFE.soundsAudioSource.Stop();
    }
    #endregion

    #region public class methods: AI related methods
    public static void SetAIEngine(AIEngine engine){
		UFE.config.aiOptions.engine = engine;
	}
	
	public static AIEngine GetAIEngine(){
		return UFE.config.aiOptions.engine;
	}

    public static ChallengeModeOptions GetChallenge(int challengeNum) {
        return UFE.config.challengeModeOptions[challengeNum];
    }
	
	public static void SetDebugMode(bool flag){
		UFE.config.debugOptions.debugMode = flag;
		if (debugger1 != null) debugger1.enabled = flag;
        if (debugger2 != null) debugger2.enabled = flag;
	}

	public static void SetAIDifficulty(AIDifficultyLevel difficulty){
		foreach(AIDifficultySettings difficultySettings in UFE.config.aiOptions.difficultySettings){
			if (difficultySettings.difficultyLevel == difficulty) {
				UFE.SetAIDifficulty(difficultySettings);
				break;
			}
		}
	}

	public static void SetAIDifficulty(AIDifficultySettings difficulty){
		UFE.config.aiOptions.selectedDifficulty = difficulty;
		UFE.config.aiOptions.selectedDifficultyLevel = difficulty.difficultyLevel;

		for (int i = 0; i < UFE.config.aiOptions.difficultySettings.Length; ++i){
			if (difficulty == UFE.config.aiOptions.difficultySettings[i]){
				PlayerPrefs.SetInt(UFE.DifficultyLevelKey, i);
				PlayerPrefs.Save();
				break;
			}
		}
	}

	public static void SetSimpleAI(int player, SimpleAIBehaviour behaviour){
		if (player == 1){
			UFE.p1SimpleAI.behaviour = behaviour;
			UFE.p1Controller.cpuController = UFE.p1SimpleAI;
		}else if (player == 2){
			UFE.p2SimpleAI.behaviour = behaviour;
			UFE.p2Controller.cpuController = UFE.p2SimpleAI;
		}
	}

	public static void SetRandomAI(int player){
		if (player == 1){
			UFE.p1Controller.cpuController = UFE.p1RandomAI;
		}else if (player == 2){
			UFE.p2Controller.cpuController = UFE.p2RandomAI;
		}
	}

	public static void SetFuzzyAI(int player, UFE3D.CharacterInfo character){
		UFE.SetFuzzyAI(player, character, UFE.config.aiOptions.selectedDifficulty);
	}

	public static void SetFuzzyAI(int player, UFE3D.CharacterInfo character, AIDifficultySettings difficulty){
		if (UFE.isAiAddonInstalled){
			if (player == 1){
				UFE.p1Controller.cpuController = UFE.p1FuzzyAI;
			}else if (player == 2){
				UFE.p2Controller.cpuController = UFE.p2FuzzyAI;
			}

			UFEController controller = UFE.GetController(player);
			if (controller != null && controller.isCPU){
				AbstractInputController cpu = controller.cpuController;

				if (cpu != null){
					MethodInfo method = cpu.GetType().GetMethod(
						"SetAIInformation", 
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy,
						null,
						new Type[]{typeof(ScriptableObject)},
						null
					);

					if (method != null){
						if (character != null && character.aiInstructionsSet != null && character.aiInstructionsSet.Length > 0){
							if (difficulty.startupBehavior == AIBehavior.Any){
								method.Invoke(cpu, new object[]{character.aiInstructionsSet[0].aiInfo});
							}else{
								ScriptableObject selectedAIInfo = character.aiInstructionsSet[0].aiInfo;
								foreach(AIInstructionsSet instructionSet in character.aiInstructionsSet){
									if (instructionSet.behavior == difficulty.startupBehavior){
										selectedAIInfo = instructionSet.aiInfo;
										break;
									}
								}
								method.Invoke(cpu, new object[]{selectedAIInfo});
							}
						}else{
							method.Invoke(cpu, new object[]{null});
						}
					}
				}
			}
		}
	}
	#endregion

	#region public class methods: Story Mode related methods
	public static CharacterStory GetCharacterStory(UFE3D.CharacterInfo character){
		if (!UFE.config.storyMode.useSameStoryForAllCharacters){
			StoryMode storyMode = UFE.config.storyMode;

			for (int i = 0; i < UFE.config.characters.Length; ++i){
				if (UFE.config.characters[i] == character && storyMode.selectableCharactersInStoryMode.Contains(i)){
					CharacterStory characterStory = null;

					if (storyMode.characterStories.TryGetValue(i, out characterStory) && characterStory != null){
						return characterStory;
					}
				}
			}
		}
		
		return UFE.config.storyMode.defaultStory;
	}
	

	public static AIDifficultySettings GetAIDifficulty(){
		return UFE.config.aiOptions.selectedDifficulty;
	}
	#endregion

	#region public class methods: GUI Related methods
	public static BattleGUI GetBattleGUI(){
		return UFE.config.gameGUI.battleGUI;
	}

	public static BluetoothGameScreen GetBluetoothGameScreen(){
		return UFE.config.gameGUI.bluetoothGameScreen;
	}

	public static CharacterSelectionScreen GetCharacterSelectionScreen(){
		return UFE.config.gameGUI.characterSelectionScreen;
	}

	public static ConnectionLostScreen GetConnectionLostScreen(){
		return UFE.config.gameGUI.connectionLostScreen;
	}

	public static CreditsScreen GetCreditsScreen(){
		return UFE.config.gameGUI.creditsScreen;
	}

	public static HostGameScreen GetHostGameScreen(){
		return UFE.config.gameGUI.hostGameScreen;
	}

	public static IntroScreen GetIntroScreen(){
		return UFE.config.gameGUI.introScreen;
	}

	public static JoinGameScreen GetJoinGameScreen(){
		return UFE.config.gameGUI.joinGameScreen;
	}

	public static LoadingBattleScreen GetLoadingBattleScreen(){
		return UFE.config.gameGUI.loadingBattleScreen;
	}

	public static MainMenuScreen GetMainMenuScreen(){
		return UFE.config.gameGUI.mainMenuScreen;
	}

	public static NetworkGameScreen GetNetworkGameScreen(){
		return UFE.config.gameGUI.networkGameScreen;
	}

	public static OptionsScreen GetOptionsScreen(){
		return UFE.config.gameGUI.optionsScreen;
	}

	public static StageSelectionScreen GetStageSelectionScreen(){
		return UFE.config.gameGUI.stageSelectionScreen;
	}

	public static StoryModeScreen GetStoryModeCongratulationsScreen(){
		return UFE.config.gameGUI.storyModeCongratulationsScreen;
	}

	public static StoryModeContinueScreen GetStoryModeContinueScreen(){
		return UFE.config.gameGUI.storyModeContinueScreen;
	}

	public static StoryModeScreen GetStoryModeGameOverScreen(){
		return UFE.config.gameGUI.storyModeGameOverScreen;
	}

	public static VersusModeAfterBattleScreen GetVersusModeAfterBattleScreen(){
		return UFE.config.gameGUI.versusModeAfterBattleScreen;
	}

	public static VersusModeScreen GetVersusModeScreen(){
		return UFE.config.gameGUI.versusModeScreen;
	}

	public static void HideScreen(UFEScreen screen){
		if (screen != null){
			screen.OnHide();
			GameObject.Destroy(screen.gameObject);
            if (!gameRunning && gameEngine != null) UFE.EndGame();
		}
	}
	
	public static void ShowScreen(UFEScreen screen, Action nextScreenAction = null){
		if (screen != null){
			if (UFE.OnScreenChanged != null){
				UFE.OnScreenChanged(UFE.currentScreen, screen);
			}

			UFE.currentScreen = (UFEScreen) GameObject.Instantiate(screen);
			UFE.currentScreen.transform.SetParent(UFE.canvas != null ? UFE.canvas.transform : null, false);

			StoryModeScreen storyModeScreen = UFE.currentScreen as StoryModeScreen;
			if (storyModeScreen != null){
				storyModeScreen.nextScreenAction = nextScreenAction;
			}

			UFE.currentScreen.OnShow ();
		}
	}

	public static void Quit(){
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}

	public static void StartBluetoothGameScreen(){
		UFE.StartBluetoothGameScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}
	
	public static void StartBluetoothGameScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartBluetoothGameScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartBluetoothGameScreen(fadeTime / 2f);
        }
	}

	public static void StartCharacterSelectionScreen(){
		UFE.StartCharacterSelectionScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartCharacterSelectionScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartCharacterSelectionScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartCharacterSelectionScreen(fadeTime / 2f);
        }
	}

	public static void StartCpuVersusCpu(){
		UFE.StartCpuVersusCpu((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartCpuVersusCpu(float fadeTime){
		UFE.gameMode = GameMode.VersusMode;
		UFE.SetCPU(1, true);
		UFE.SetCPU(2, true);
		UFE.StartCharacterSelectionScreen(fadeTime);
	}

	public static void StartConnectionLostScreenIfMainMenuNotLoaded(){
		UFE.StartConnectionLostScreenIfMainMenuNotLoaded((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartConnectionLostScreenIfMainMenuNotLoaded(float fadeTime){
		if ((UFE.currentScreen as MainMenuScreen) == null){
			UFE.StartConnectionLostScreen(fadeTime);
		}
	}

	public static void StartConnectionLostScreen(){
		UFE.StartConnectionLostScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartConnectionLostScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartConnectionLostScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartConnectionLostScreen(fadeTime / 2f);
        }
	}

	public static void StartCreditsScreen(){
		UFE.StartCreditsScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartCreditsScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartCreditsScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartCreditsScreen(fadeTime / 2f);
        }
	}

	public static void StartGame(){
		UFE.StartGame((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartGame(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.gameFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartGame(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartGame(fadeTime / 2f);
        }
	}

	public static void StartHostGameScreen(){
		UFE.StartHostGameScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartHostGameScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartHostGameScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartHostGameScreen(fadeTime / 2f);
        }
	}

	public static void StartIntroScreen(){
		UFE.StartIntroScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartIntroScreen(float fadeTime){
        if (UFE.currentScreen != null && UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartIntroScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartIntroScreen(fadeTime / 2f);
        }
	}

	public static void StartJoinGameScreen(){
		UFE.StartJoinGameScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartJoinGameScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartJoinGameScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartJoinGameScreen(fadeTime / 2f);
        }
	}

	public static void StartLoadingBattleScreen(){
		UFE.StartLoadingBattleScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartLoadingBattleScreen(float fadeTime){
        if (UFE.currentScreen != null && UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartLoadingBattleScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartLoadingBattleScreen(fadeTime / 2f);
        }
	}

	public static void StartMainMenuScreen(){
		UFE.StartMainMenuScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartMainMenuScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartMainMenuScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartMainMenuScreen(fadeTime / 2f);
        }
	}

	public static void StartSearchMatchScreen(){
		UFE.StartSearchMatchScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartSearchMatchScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartSearchMatchScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartSearchMatchScreen(fadeTime / 2f);
        }
	}

	public static void StartNetworkGameScreen(){
		UFE.StartNetworkGameScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartNetworkGameScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartNetworkGameScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartNetworkGameScreen(fadeTime / 2f);
        }
	}

	public static void StartOptionsScreen(){
		UFE.StartOptionsScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartOptionsScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartOptionsScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartOptionsScreen(fadeTime / 2f);
        }
	}

	public static void StartPlayerVersusPlayer(){
		UFE.StartPlayerVersusPlayer((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartPlayerVersusPlayer(float fadeTime){
		UFE.gameMode = GameMode.VersusMode;
		UFE.SetCPU(1, false);
		UFE.SetCPU(2, false);
		UFE.StartCharacterSelectionScreen(fadeTime);
	}

	public static void StartPlayerVersusCpu(){
		UFE.StartPlayerVersusCpu((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartPlayerVersusCpu(float fadeTime){
		UFE.gameMode = GameMode.VersusMode;
		UFE.SetCPU(1, false);
		UFE.SetCPU(2, true);
		UFE.StartCharacterSelectionScreen(fadeTime);
	}

	public static void StartNetworkGame(float fadeTime, int localPlayer, bool startImmediately){
		UFE.disconnecting = false;
		Application.runInBackground = true;

		UFE.localPlayerController.Initialize(UFE.p1Controller.inputReferences);
		UFE.localPlayerController.humanController = UFE.p1Controller.humanController;
		UFE.localPlayerController.cpuController = UFE.p1Controller.cpuController;
		UFE.remotePlayerController.Initialize(UFE.p2Controller.inputReferences);

		if (localPlayer == 1){
			UFE.localPlayerController.player = 1;
			UFE.remotePlayerController.player = 2;
		}else{
			UFE.localPlayerController.player = 2;
			UFE.remotePlayerController.player = 1;
		}

		UFE.fluxCapacitor.Initialize();
		UFE.gameMode = GameMode.NetworkGame;
		UFE.SetCPU(1, false);
		UFE.SetCPU(2, false);

        if (startImmediately) {
            UFE.StartLoadingBattleScreen(fadeTime);
            //UFE.StartGame();
        } else {
            UFE.StartCharacterSelectionScreen(fadeTime);
        }
	}

	public static void StartStageSelectionScreen(){
		UFE.StartStageSelectionScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStageSelectionScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStageSelectionScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStageSelectionScreen(fadeTime / 2f);
        }
	}

	public static void StartStoryMode(){
		UFE.StartStoryMode((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryMode(float fadeTime){
		//-------------------------------------------------------------------------------------------------------------
		// Required for loading the first combat correctly.
		UFE.player1WonLastBattle = true; 
		//-------------------------------------------------------------------------------------------------------------
		UFE.gameMode = GameMode.StoryMode;
		UFE.SetCPU(1, false);
		UFE.SetCPU(2, true);
		UFE.storyMode.characterStory = null;
		UFE.storyMode.canFightAgainstHimself = UFE.config.storyMode.canCharactersFightAgainstThemselves;
		UFE.storyMode.currentGroup = -1;
		UFE.storyMode.currentBattle = -1;
		UFE.storyMode.currentBattleInformation = null;
		UFE.storyMode.defeatedOpponents.Clear();
		UFE.StartCharacterSelectionScreen(fadeTime);
	}

	public static void StartStoryModeBattle(){
		UFE.StartStoryModeBattle((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeBattle(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeBattle(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeBattle(fadeTime / 2f);
        }
	}

	public static void StartStoryModeCongratulationsScreen(){
		UFE.StartStoryModeCongratulationsScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeCongratulationsScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeCongratulationsScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeCongratulationsScreen(fadeTime / 2f);
        }
	}

	public static void StartStoryModeContinueScreen(){
		UFE.StartStoryModeContinueScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeContinueScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeContinueScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeContinueScreen(fadeTime / 2f);
        }
	}

	public static void StartStoryModeConversationAfterBattleScreen(UFEScreen conversationScreen){
		UFE.StartStoryModeConversationAfterBattleScreen(conversationScreen, (float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeConversationAfterBattleScreen(UFEScreen conversationScreen, float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeConversationAfterBattleScreen(conversationScreen, fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeConversationAfterBattleScreen(conversationScreen, fadeTime / 2f);
        }
	}

	public static void StartStoryModeConversationBeforeBattleScreen(UFEScreen conversationScreen){
		UFE.StartStoryModeConversationBeforeBattleScreen(conversationScreen, (float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeConversationBeforeBattleScreen(UFEScreen conversationScreen, float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeConversationBeforeBattleScreen(conversationScreen, fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeConversationBeforeBattleScreen(conversationScreen, fadeTime / 2f);
        }
	}

	public static void StartStoryModeEndingScreen(){
		UFE.StartStoryModeEndingScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeEndingScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeEndingScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeEndingScreen(fadeTime / 2f);
        }
	}

	public static void StartStoryModeGameOverScreen(){
		UFE.StartStoryModeGameOverScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeGameOverScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeGameOverScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeGameOverScreen(fadeTime / 2f);
        }
	}

	public static void StartStoryModeOpeningScreen(){
		UFE.StartStoryModeOpeningScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartStoryModeOpeningScreen(float fadeTime){
		// First, retrieve the character story, so we can find the opening associated to this player
		UFE.storyMode.characterStory = UFE.GetCharacterStory(UFE.GetPlayer1());

        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartStoryModeOpeningScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartStoryModeOpeningScreen(fadeTime / 2f);
        }
	}

	public static void StartTrainingMode(){
		UFE.StartTrainingMode((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartTrainingMode(float fadeTime){
		UFE.gameMode = GameMode.TrainingRoom;
		UFE.SetCPU(1, false);
		UFE.SetCPU(2, false);
		UFE.StartCharacterSelectionScreen(fadeTime);
	}

	public static void StartVersusModeAfterBattleScreen(){
		UFE.StartVersusModeAfterBattleScreen(0f);
	}

	public static void StartVersusModeAfterBattleScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartVersusModeAfterBattleScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartVersusModeAfterBattleScreen(fadeTime / 2f);
        }
	}

	public static void StartVersusModeScreen(){
		UFE.StartVersusModeScreen((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void StartVersusModeScreen(float fadeTime){
        if (UFE.currentScreen.hasFadeOut) {
            UFE.eventSystem.enabled = false;
            CameraFade.StartAlphaFade(
                UFE.config.gameGUI.screenFadeColor,
                false,
                fadeTime / 2f,
                0f
            );
            UFE.DelayLocalAction(() => { UFE.eventSystem.enabled = true; UFE._StartVersusModeScreen(fadeTime / 2f); }, (Fix64)fadeTime / 2);
        } else {
            UFE._StartVersusModeScreen(fadeTime / 2f);
        }
	}

	public static void WonStoryModeBattle(){
		UFE.WonStoryModeBattle((float)UFE.config.gameGUI.screenFadeDuration);
	}

	public static void WonStoryModeBattle(float fadeTime){
		UFE.storyMode.defeatedOpponents.Add(UFE.storyMode.currentBattleInformation.opponentCharacterIndex);
		UFE.StartStoryModeConversationAfterBattleScreen(UFE.storyMode.currentBattleInformation.conversationAfterBattle, fadeTime);
	}
	#endregion

	#region public class methods: Language
	public static void SetLanguage(){
		foreach(LanguageOptions languageOption in config.languages){
			if (languageOption.defaultSelection){
				config.selectedLanguage = languageOption;
				return;
			}
		}
	}

	public static void SetLanguage(string language){
		foreach(LanguageOptions languageOption in config.languages){
			if (language == languageOption.languageName){
				config.selectedLanguage = languageOption;
				return;
			}
		}
	}
	#endregion

	#region public class methods: Input Related methods
	public static bool GetCPU(int player){
		UFEController controller = UFE.GetController(player);
		if (controller != null){
			return controller.isCPU;
		}
		return false;
	}

	public static string GetInputReference(ButtonPress button, InputReferences[] inputReferences){
		foreach(InputReferences inputReference in inputReferences){
			if (inputReference.engineRelatedButton == button) return inputReference.inputButtonName;
		}
		return null;
	}
	
	public static string GetInputReference(InputType inputType, InputReferences[] inputReferences){
		foreach(InputReferences inputReference in inputReferences){
			if (inputReference.inputType == inputType) return inputReference.inputButtonName;
		}
		return null;
	}

	public static UFEController GetPlayer1Controller(){
		if (UFE.isNetworkAddonInstalled && UFE.isConnected){
			if (UFE.multiplayerAPI.IsServer()){
				return UFE.localPlayerController;
			}else{
				return UFE.remotePlayerController;
			}
		}
		return UFE.p1Controller;
	}
	
	public static UFEController GetPlayer2Controller(){
		if (UFE.isNetworkAddonInstalled && UFE.isConnected){
			if (UFE.multiplayerAPI.IsServer()){
				return UFE.remotePlayerController;
			}else{
				return UFE.localPlayerController;
			}
		}
		return UFE.p2Controller;
	}
	
	public static UFEController GetController(int player){
		if		(player == 1)	return UFE.GetPlayer1Controller();
		else if (player == 2)	return UFE.GetPlayer2Controller();
		else					return null;
	}
	
	public static int GetLocalPlayer(){
		if		(UFE.localPlayerController == UFE.GetPlayer1Controller())	return 1;
		else if	(UFE.localPlayerController == UFE.GetPlayer2Controller())	return 2;
		else																return -1;
	}
	
	public static int GetRemotePlayer(){
		if		(UFE.remotePlayerController == UFE.GetPlayer1Controller())	return 1;
		else if	(UFE.remotePlayerController == UFE.GetPlayer2Controller())	return 2;
		else																return -1;
	}

	public static void SetAI(int player, UFE3D.CharacterInfo character){
		if (UFE.isAiAddonInstalled){
			UFEController controller = UFE.GetController(player);
			
			if (controller != null && controller.isCPU){
				AbstractInputController cpu = controller.cpuController;
				
				if (cpu != null){
					MethodInfo method = cpu.GetType().GetMethod(
						"SetAIInformation", 
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy,
						null,
						new Type[]{typeof(ScriptableObject)},
					null
					);
					
					if (method != null){
						if (character != null && character.aiInstructionsSet != null && character.aiInstructionsSet.Length > 0){
							method.Invoke(cpu, new object[]{character.aiInstructionsSet[0].aiInfo});
						}else{
							method.Invoke(cpu, new object[]{null});
						}
					}
				}
			}
		}
	}

	public static void SetCPU(int player, bool cpuToggle){
		UFEController controller = UFE.GetController(player);
		if (controller != null){
			controller.isCPU = cpuToggle;
		}
	}
	#endregion

	#region public class methods: methods related to the character selection
	public static UFE3D.CharacterInfo GetPlayer(int player){
		if (player == 1){
			return UFE.GetPlayer1();
		}else if (player == 2){
			return UFE.GetPlayer2();
		}
		return null;
	}
	
	public static UFE3D.CharacterInfo GetPlayer1(){
		return config.player1Character;
	}
	
	public static UFE3D.CharacterInfo GetPlayer2(){
		return config.player2Character;
	}

	public static UFE3D.CharacterInfo[] GetStoryModeSelectableCharacters(){
		List<UFE3D.CharacterInfo> characters = new List<UFE3D.CharacterInfo>();

		for (int i = 0; i < UFE.config.characters.Length; ++i){
			if(
				UFE.config.characters[i] != null 
				&& 
				(
					UFE.config.storyMode.selectableCharactersInStoryMode.Contains(i) || 
					UFE.unlockedCharactersInStoryMode.Contains(UFE.config.characters[i].characterName)
				)
			){
				characters.Add(UFE.config.characters[i]);
			}
		}
		
		return characters.ToArray();
	}

	public static UFE3D.CharacterInfo[] GetTrainingRoomSelectableCharacters(){
		List<UFE3D.CharacterInfo> characters = new List<UFE3D.CharacterInfo>();
		
		for (int i = 0; i < UFE.config.characters.Length; ++i){
			// If the character is selectable on Story Mode or Versus Mode,
			// then the character should be selectable on Training Room...
			if(
				UFE.config.characters[i] != null 
				&& 
				(
					UFE.config.storyMode.selectableCharactersInStoryMode.Contains(i) || 
					UFE.config.storyMode.selectableCharactersInVersusMode.Contains(i) || 
					UFE.unlockedCharactersInStoryMode.Contains(UFE.config.characters[i].characterName) ||
					UFE.unlockedCharactersInVersusMode.Contains(UFE.config.characters[i].characterName)
				)
			){
				characters.Add(UFE.config.characters[i]);
			}
		}
		
		return characters.ToArray();
	}
	
	public static UFE3D.CharacterInfo[] GetVersusModeSelectableCharacters(){
		List<UFE3D.CharacterInfo> characters = new List<UFE3D.CharacterInfo>();
		
		for (int i = 0; i < UFE.config.characters.Length; ++i){
			if(
				UFE.config.characters[i] != null && 
				(
					UFE.config.storyMode.selectableCharactersInVersusMode.Contains(i) || 
					UFE.unlockedCharactersInVersusMode.Contains(UFE.config.characters[i].characterName)
				)
			){
				characters.Add(UFE.config.characters[i]);
			}
		}
		
		return characters.ToArray();
	}

	public static void SetPlayer(int player, UFE3D.CharacterInfo info){
		if (player == 1){
			config.player1Character = info;
		}else if (player == 2){
			config.player2Character = info;
		}
	}

	public static void SetPlayer1(UFE3D.CharacterInfo player1){
		config.player1Character = player1;
	}

	public static void SetPlayer2(UFE3D.CharacterInfo player2){
		config.player2Character = player2;
	}

	public static void LoadUnlockedCharacters(){
		UFE.unlockedCharactersInStoryMode.Clear();
		string value = PlayerPrefs.GetString("UCSM", null);

		if (!string.IsNullOrEmpty(value)){
			string[] characters = value.Split(new char[]{'\n'}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string character in characters){
				unlockedCharactersInStoryMode.Add(character);
			}
		}


		UFE.unlockedCharactersInVersusMode.Clear();
		value = PlayerPrefs.GetString("UCVM", null);
		
		if (!string.IsNullOrEmpty(value)){
			string[] characters = value.Split(new char[]{'\n'}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string character in characters){
				unlockedCharactersInVersusMode.Add(character);
			}
		}
	}

	public static void SaveUnlockedCharacters(){
		StringBuilder sb = new StringBuilder();
		foreach (string characterName in UFE.unlockedCharactersInStoryMode){
			if (!string.IsNullOrEmpty(characterName)){
				if (sb.Length > 0){
					sb.AppendLine();
				}
				sb.Append(characterName);
			}
		}
		PlayerPrefs.SetString("UCSM", sb.ToString());

		sb = new StringBuilder();
		foreach (string characterName in UFE.unlockedCharactersInVersusMode){
			if (!string.IsNullOrEmpty(characterName)){
				if (sb.Length > 0){
					sb.AppendLine();
				}
				sb.Append(characterName);
			}
		}
		PlayerPrefs.SetString("UCVM", sb.ToString());
		PlayerPrefs.Save();
	}

	public static void RemoveUnlockedCharacterInStoryMode(UFE3D.CharacterInfo character){
		if (character != null && !string.IsNullOrEmpty(character.characterName)){
			UFE.unlockedCharactersInStoryMode.Remove(character.characterName);
		}
		
		UFE.SaveUnlockedCharacters();
	}

	public static void RemoveUnlockedCharacterInVersusMode(UFE3D.CharacterInfo character){
		if (character != null && !string.IsNullOrEmpty(character.characterName)){
			UFE.unlockedCharactersInVersusMode.Remove(character.characterName);
		}
		
		UFE.SaveUnlockedCharacters();
	}

	public static void RemoveUnlockedCharactersInStoryMode(){
		UFE.unlockedCharactersInStoryMode.Clear();
		UFE.SaveUnlockedCharacters();
	}
	
	public static void RemoveUnlockedCharactersInVersusMode(){
		UFE.unlockedCharactersInVersusMode.Clear();
		UFE.SaveUnlockedCharacters();
	}

	public static void UnlockCharacterInStoryMode(UFE3D.CharacterInfo character){
		if(
			character != null && 
			!string.IsNullOrEmpty(character.characterName) &&
			!UFE.unlockedCharactersInStoryMode.Contains(character.characterName)
		){
			UFE.unlockedCharactersInStoryMode.Add(character.characterName);
		}

		UFE.SaveUnlockedCharacters();
	}

	public static void UnlockCharacterInVersusMode(UFE3D.CharacterInfo character){
		if(
			character != null && 
			!string.IsNullOrEmpty(character.characterName) &&
			!UFE.unlockedCharactersInVersusMode.Contains(character.characterName)
		){
			UFE.unlockedCharactersInVersusMode.Add(character.characterName);
		}

		UFE.SaveUnlockedCharacters();
	}
	#endregion

	#region public class methods: methods related to the stage selection
	public static void SetStage(StageOptions stage){
		config.selectedStage = stage;
	}

	public static void SetStage(string stageName){
		foreach(StageOptions stage in config.stages){
			if (stageName == stage.stageName){
				UFE.SetStage(stage);
				return;
			}
		}
	}
	
	public static StageOptions GetStage(){
		return config.selectedStage;
	}
	#endregion


	#region public class methods: methods related to the behaviour of the characters during the battle
	public static ControlsScript GetControlsScript(int player){
		if (player == 1){
			return UFE.GetPlayer1ControlsScript();
		}else if (player == 2){
			return UFE.GetPlayer2ControlsScript();
		}
		return null;
	}

	public static ControlsScript GetPlayer1ControlsScript(){
		return p1ControlsScript;
	}
	
	public static ControlsScript GetPlayer2ControlsScript(){
		return p2ControlsScript;
	}
	#endregion

	#region public class methods: methods that are used for raising events
	public static void SetLifePoints(Fix64 newValue, UFE3D.CharacterInfo player){
		if (UFE.OnLifePointsChange != null) UFE.OnLifePointsChange((float)newValue, player);
	}

	public static void FireAlert(string alertMessage, UFE3D.CharacterInfo player){
		if (UFE.OnNewAlert != null) UFE.OnNewAlert(alertMessage, player);
	}

	public static void FireHit(HitBox strokeHitBox, MoveInfo move, UFE3D.CharacterInfo player){
		if (UFE.OnHit != null) UFE.OnHit(strokeHitBox, move, player);
	}

    public static void FireBlock(HitBox strokeHitBox, MoveInfo move, UFE3D.CharacterInfo player) {
        if (UFE.OnBlock != null) UFE.OnBlock(strokeHitBox, move, player);
    }

    public static void FireParry(HitBox strokeHitBox, MoveInfo move, UFE3D.CharacterInfo player) {
        if (UFE.OnParry != null) UFE.OnParry(strokeHitBox, move, player);
    }
	
	public static void FireMove(MoveInfo move, UFE3D.CharacterInfo player){
		if (UFE.OnMove != null) UFE.OnMove(move, player);
	}

    public static void FireButton(ButtonPress button, UFE3D.CharacterInfo player) {
        if (UFE.OnButton != null) UFE.OnButton(button, player);
    }

    public static void FireBasicMove(BasicMoveReference basicMove, UFE3D.CharacterInfo player) {
        if (UFE.OnBasicMove != null) UFE.OnBasicMove(basicMove, player);
    }

    public static void FireBodyVisibilityChange(MoveInfo move, UFE3D.CharacterInfo player, BodyPartVisibilityChange bodyPartVisibilityChange, HitBox hitBox) {
        if (UFE.OnBodyVisibilityChange != null) UFE.OnBodyVisibilityChange(move, player, bodyPartVisibilityChange, hitBox);
    }

    public static void FireParticleEffects(MoveInfo move, UFE3D.CharacterInfo player, MoveParticleEffect particleEffects) {
        if (UFE.OnParticleEffects != null) UFE.OnParticleEffects(move, player, particleEffects);
    }

    public static void FireSideSwitch(int side, UFE3D.CharacterInfo player) {
        if (UFE.OnSideSwitch != null) UFE.OnSideSwitch(side, player);
    }

	public static void FireGameBegins(){
		if (UFE.OnGameBegin != null) {
			gameRunning = true;
			UFE.OnGameBegin(config.player1Character, config.player2Character, config.selectedStage);
		}
	}
	
	public static void FireGameEnds(UFE3D.CharacterInfo winner, UFE3D.CharacterInfo loser){
		// I've commented the next line because it worked with the old GUI, but not with the new one.
		//UFE.EndGame();

        UFE.timeScale = UFE.config._gameSpeed;
		UFE.gameRunning = false;
		UFE.newRoundCasted = false;
		UFE.player1WonLastBattle = (winner == UFE.GetPlayer1());

		/*if (UFE.fluxGameManager != null){
			UFE.fluxGameManager.Initialize();
		}*/

		if (UFE.OnGameEnds != null) {
			UFE.OnGameEnds(winner, loser);
		}
	}
	
	public static void FireRoundBegins(int currentRound){
		if (UFE.OnRoundBegins != null) UFE.OnRoundBegins(currentRound);
	}

	public static void FireRoundEnds(UFE3D.CharacterInfo winner, UFE3D.CharacterInfo loser){
		if (UFE.OnRoundEnds != null) UFE.OnRoundEnds(winner, loser);
	}

	public static void FireTimer(float timer){
		if (UFE.OnTimer != null) UFE.OnTimer(timer);
	}

	public static void FireTimeOver(){
		if (UFE.OnTimeOver != null) UFE.OnTimeOver();
	}
	#endregion

    
	#region public class methods: UFE CORE methods
	public static void PauseGame(bool pause){
        if (pause && UFE.timeScale == 0) return;

		if (pause){
            UFE.timeScale = 0;
		}else{
            UFE.timeScale = UFE.config._gameSpeed;
		}

		if (UFE.OnGamePaused != null){
			UFE.OnGamePaused(pause);
		}
	}

	public static bool IsInstalled(string theClass){
		return UFE.SearchClass(theClass) != null;
	}
	
	public static bool isPaused(){
        return UFE.timeScale <= 0;
	}
	
	public static Fix64 GetTimer(){
		return timer;
	}
	
	public static void ResetTimer(){
		timer = config.roundOptions._timer;
		intTimer = (int)FPMath.Round(config.roundOptions._timer);
		if (UFE.OnTimer != null) OnTimer((float)timer);
	}
	
	public static Type SearchClass(string theClass){
		Type type = null;
		
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()){
			type = assembly.GetType(theClass);
			if (type != null){break;}
		}
		
		return type;
	}
	
	public static void SetTimer(Fix64 time){
		timer = time;
		intTimer = (int)FPMath.Round(time);
		if (UFE.OnTimer != null) OnTimer(timer);
	}
	
	public static void PlayTimer(){
		pauseTimer = false;
	}
	
	public static void PauseTimer(){
		pauseTimer = true;
	}
	
	public static bool IsTimerPaused(){
		return pauseTimer;
	}

    public static void EndGame(){
        /*UFE.timeScale = UFE.ToDouble(UFE.config.gameSpeed);
		UFE.gameRunning = false;
		UFE.newRoundCasted = false;*/

		if (UFE.battleGUI != null){
			UFE.battleGUI.OnHide();
            GameObject.Destroy(UFE.battleGUI.gameObject);
            UFE.battleGUI = null;
		}
		
		if (gameEngine != null) {
			GameObject.Destroy(gameEngine);
			gameEngine = null;
		}
	}

	public static void ResetRoundCast(){
		newRoundCasted = false;
	}
	
	public static void CastNewRound(){
		if (newRoundCasted) return;
		if (p1ControlsScript.introPlayed && p2ControlsScript.introPlayed){
            UFE.FireRoundBegins(config.currentRound);
			UFE.DelaySynchronizedAction(StartFight, (Fix64)2);
			newRoundCasted = true;
		}
	}

    public static void StartFight() {
        if (UFE.gameMode != GameMode.ChallengeMode) 
            UFE.FireAlert(UFE.config.selectedLanguage.fight, null);
        UFE.config.lockInputs = false;
        UFE.config.lockMovements = false;
        UFE.PlayTimer();
    }

	public static void CastInput(InputReferences[] inputReferences, int player){
		if (UFE.OnInput != null) OnInput(inputReferences, player);
	}
	#endregion

	#region public class methods: Network Related methods
	public static void HostBluetoothGame(){
		if (UFE.isNetworkAddonInstalled){
			UFE.multiplayerMode = MultiplayerMode.Bluetooth;
			UFE.AddNetworkEventListeners();
			UFE.multiplayerAPI.CreateMatch(new MultiplayerAPI.MatchCreationRequest(UFE.config.networkOptions.port, null, 1, false, null));
		}
	}

	public static void HostGame(){
		if (UFE.isNetworkAddonInstalled){
			UFE.multiplayerMode = MultiplayerMode.Lan;

			UFE.AddNetworkEventListeners();
			UFE.multiplayerAPI.CreateMatch(new MultiplayerAPI.MatchCreationRequest(UFE.config.networkOptions.port, null, 1, false, null));
		}
	}

	public static void JoinBluetoothGame(){
		if (UFE.isNetworkAddonInstalled){
			UFE.multiplayerMode = MultiplayerMode.Bluetooth;

			UFE.multiplayerAPI.OnMatchesDiscovered += UFE.OnMatchesDiscovered;
			UFE.multiplayerAPI.OnMatchDiscoveryError += UFE.OnMatchDiscoveryError;
			UFE.multiplayerAPI.StartSearchingMatches();
		}
	}

	protected static void OnMatchesDiscovered(ReadOnlyCollection<MultiplayerAPI.MatchInformation> matches){
		UFE.multiplayerAPI.OnMatchesDiscovered -= UFE.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError -= UFE.OnMatchDiscoveryError;
		UFE.AddNetworkEventListeners();

		if (matches != null && matches.Count > 0){
			// TODO: let the player choose the desired game
			UFE.multiplayerAPI.JoinMatch(matches[0]);
		}else{
			UFE.StartConnectionLostScreen();
		}
    }
    
	protected static void OnMatchDiscoveryError(){
		UFE.multiplayerAPI.OnMatchesDiscovered -= UFE.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError -= UFE.OnMatchDiscoveryError;
		UFE.StartConnectionLostScreen();
    }

	public static void JoinGame(MultiplayerAPI.MatchInformation match){
		if (UFE.isNetworkAddonInstalled){
			UFE.multiplayerMode = MultiplayerMode.Lan;

			UFE.AddNetworkEventListeners();
			UFE.multiplayerAPI.JoinMatch(match);
		}
	}

	public static void DisconnectFromGame(){
		if (UFE.isNetworkAddonInstalled){
			NetworkState state = UFE.multiplayerAPI.GetConnectionState();
			if (state == NetworkState.Client){
				UFE.multiplayerAPI.DisconnectFromMatch();
			}else if (state == NetworkState.Server){
				UFE.multiplayerAPI.DestroyMatch();
			}
		}
	}
	#endregion
    

	#region protected instance methods: MonoBehaviour methods
	protected void Awake(){
        UFE.config = UFE_Config;
        UFE.UFEInstance = this;
        UFE.fixedDeltaTime = 1 / (Fix64)UFE.config.fps;

        FPRandom.Init();

        // Check which characters have been unlocked
        UFE.LoadUnlockedCharacters();

        // Check the installed Addons and supported 3rd party products
        UFE.isCInputInstalled = UFE.IsInstalled("cInput");
#if UFE_LITE
        UFE.isAiAddonInstalled = false;
#else
        UFE.isAiAddonInstalled = UFE.IsInstalled("RuleBasedAI");
#endif

#if UFE_LITE || UFE_BASIC
		UFE.isNetworkAddonInstalled = false;
		UFE.isPhotonInstalled = false;
        UFE.isBluetoothAddonInstalled = false;
#else
        UFE.isNetworkAddonInstalled = UFE.IsInstalled("UnetHighLevelMultiplayerAPI") && UFE.config.networkOptions.networkService != NetworkService.Disabled;
        UFE.isPhotonInstalled = UFE.IsInstalled("PhotonMultiplayerAPI") && UFE.config.networkOptions.networkService != NetworkService.Disabled;
        UFE.isBluetoothAddonInstalled = UFE.IsInstalled("BluetoothMultiplayerAPI") && UFE.config.networkOptions.networkService != NetworkService.Disabled;
#endif

        UFE.isControlFreak1Installed = UFE.IsInstalled("TouchController");				// [DGT]
        UFE.isControlFreak2Installed = UFE.IsInstalled("ControlFreak2.UFEBridge");
        UFE.isControlFreakInstalled = UFE.isControlFreak1Installed || UFE.isControlFreak2Installed;
        UFE.isRewiredInstalled = UFE.IsInstalled("Rewired.Integration.UniversalFightingEngine.RewiredUFEInputManager");

        // Check if we should run the application in background
        Application.runInBackground = UFE.config.runInBackground;

        // Check if cInput is installed and initialize the cInput GUI
		if (UFE.isCInputInstalled){
			Type t = UFE.SearchClass("cGUI");
			if (t != null) t.GetField("cSkin").SetValue(null, UFE.config.inputOptions.cInputSkin);
		}

        //-------------------------------------------------------------------------------------------------------------
        // Initialize the GUI
        //-------------------------------------------------------------------------------------------------------------
        GameObject goGroup = new GameObject("CanvasGroup");
        UFE.canvasGroup = goGroup.AddComponent<CanvasGroup>();


        GameObject go = new GameObject("Canvas");
        go.transform.SetParent(goGroup.transform);

        UFE.canvas = go.AddComponent<Canvas>();
        UFE.canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if(EventSystem.current != null) {
            // Use the current event system if one exists
            UFE.eventSystem = EventSystem.current;
        } else {
            UFE.eventSystem = go.AddComponent<EventSystem>();
        }
        //UFE.eventSystem = GameObject.FindObjectOfType<EventSystem>();
        //if (UFE.eventSystem == null) UFE.eventSystem = go.AddComponent<EventSystem>();

        UFE.graphicRaycaster = go.AddComponent<GraphicRaycaster>();

        UFE.standaloneInputModule = go.AddComponent<StandaloneInputModule>();
        UFE.standaloneInputModule.verticalAxis = "Mouse Wheel";
        UFE.standaloneInputModule.horizontalAxis = "Mouse Wheel";
        UFE.standaloneInputModule.forceModuleActive = true;

        if (UFE.config.gameGUI.useCanvasScaler) {
            CanvasScaler cs = go.AddComponent<CanvasScaler>();
            cs.defaultSpriteDPI = UFE.config.gameGUI.canvasScaler.defaultSpriteDPI;
            cs.fallbackScreenDPI = UFE.config.gameGUI.canvasScaler.fallbackScreenDPI;
            cs.matchWidthOrHeight = UFE.config.gameGUI.canvasScaler.matchWidthOrHeight;
            cs.physicalUnit = UFE.config.gameGUI.canvasScaler.physicalUnit;
            cs.referencePixelsPerUnit = UFE.config.gameGUI.canvasScaler.referencePixelsPerUnit;
            cs.referenceResolution = UFE.config.gameGUI.canvasScaler.referenceResolution;
            cs.scaleFactor = UFE.config.gameGUI.canvasScaler.scaleFactor;
            cs.screenMatchMode = UFE.config.gameGUI.canvasScaler.screenMatchMode;
            cs.uiScaleMode = UFE.config.gameGUI.canvasScaler.scaleMode;
            
            //Line commented because we use "Screen Space - Overlay" canvas and the "dynaicPixelsPerUnit" property is only used in "World Space" Canvas.
            //cs.dynamicPixelsPerUnit = UFE.config.gameGUI.canvasScaler.dynamicPixelsPerUnit; 
        }

        // Check if "Control Freak Virtual Controller" is installed and instantiate the prefab
        if (UFE.isControlFreakInstalled && UFE.config.inputOptions.inputManagerType == InputManagerType.ControlFreak)
        {
            if (UFE.isControlFreak2Installed && (UFE.config.inputOptions.controlFreak2Prefab != null)) {
                // Try to instantiate Control Freak 2 rig prefab...
                UFE.controlFreakPrefab = (GameObject)Instantiate(UFE.config.inputOptions.controlFreak2Prefab.gameObject);
                UFE.touchControllerBridge = (UFE.controlFreakPrefab != null) ? UFE.controlFreakPrefab.GetComponent<InputTouchControllerBridge>() : null;
                UFE.touchControllerBridge.Init();

            }
            else if (UFE.isControlFreak1Installed && (UFE.config.inputOptions.controlFreakPrefab != null)) {
                // ...or try to instantiate Control Freak 1.x controller prefab...
                UFE.controlFreakPrefab = (GameObject)Instantiate(UFE.config.inputOptions.controlFreakPrefab);
            }
        }

		// Check if the "network addon" is installed
		string uuid = (UFE.config.gameName ?? "UFE") /*+ "_" + Application.version*/;
        if (UFE.isNetworkAddonInstalled)
        {
            GameObject networkManager = new GameObject("Network Manager");
            networkManager.transform.SetParent(this.gameObject.transform);

            UFE.lanMultiplayerAPI = networkManager.AddComponent(UFE.SearchClass("UnetLanMultiplayerAPI")) as MultiplayerAPI;
			UFE.lanMultiplayerAPI.Initialize(uuid);

			if (UFE.config.networkOptions.networkService == NetworkService.Unity) {
				UFE.onlineMultiplayerAPI = networkManager.AddComponent(UFE.SearchClass("UnetOnlineMultiplayerAPI")) as MultiplayerAPI;
			} else if (UFE.config.networkOptions.networkService == NetworkService.Photon && UFE.isPhotonInstalled) {
				UFE.onlineMultiplayerAPI = networkManager.AddComponent(UFE.SearchClass("PhotonMultiplayerAPI")) as MultiplayerAPI;
			}else if (UFE.config.networkOptions.networkService == NetworkService.Photon && !UFE.isPhotonInstalled){
                Debug.LogError("You need 'Photon Unity Networking' installed in order to use Photon as a Network Service.");
            }
			UFE.onlineMultiplayerAPI.Initialize(uuid);

            if (Application.platform == RuntimePlatform.Android && UFE.isBluetoothAddonInstalled) {
                UFE.bluetoothMultiplayerAPI = networkManager.AddComponent(UFE.SearchClass("BluetoothMultiplayerAPI")) as MultiplayerAPI;
            } else {
                UFE.bluetoothMultiplayerAPI = networkManager.AddComponent<NullMultiplayerAPI>();
            }
            UFE.bluetoothMultiplayerAPI.Initialize(uuid);
			

			UFE.multiplayerAPI.SendRate = 1 / (float)UFE.config.fps;

			UFE.localPlayerController = gameObject.AddComponent<UFEController> ();
			UFE.remotePlayerController = gameObject.AddComponent<DummyInputController>();

			UFE.localPlayerController.isCPU = false;
			UFE.remotePlayerController.isCPU = false;

            // TODO deprecated
            //NetworkView network = this.gameObject.AddComponent<NetworkView>();
            //network.stateSynchronization = NetworkStateSynchronization.Off;
            //network.observed = UFE.remotePlayerController;
		}else{
			UFE.lanMultiplayerAPI = this.gameObject.AddComponent<NullMultiplayerAPI>();
			UFE.lanMultiplayerAPI.Initialize(uuid);

			UFE.onlineMultiplayerAPI = this.gameObject.AddComponent<NullMultiplayerAPI>();
			UFE.onlineMultiplayerAPI.Initialize(uuid);
			
			UFE.bluetoothMultiplayerAPI = this.gameObject.AddComponent<NullMultiplayerAPI>();
			UFE.bluetoothMultiplayerAPI.Initialize(uuid);
		}

		UFE.fluxCapacitor = new FluxCapacitor(UFE.currentFrame, UFE.config.networkOptions.maxBufferSize);
		UFE._multiplayerMode = MultiplayerMode.Lan;


		// Initialize the input systems
        p1Controller = gameObject.AddComponent<UFEController>();
        if (UFE.config.inputOptions.inputManagerType == InputManagerType.ControlFreak) {
            p1Controller.humanController = gameObject.AddComponent<InputTouchController>();
        } else if (UFE.config.inputOptions.inputManagerType == InputManagerType.Rewired) {
            p1Controller.humanController = gameObject.AddComponent<RewiredInputController>();
            (p1Controller.humanController as RewiredInputController).rewiredPlayerId = 0;
        } else {
			p1Controller.humanController = gameObject.AddComponent<InputController>();
		}

        // Initialize AI
        p1SimpleAI = gameObject.AddComponent<SimpleAI>();
		p1SimpleAI.player = 1;

		p1RandomAI = gameObject.AddComponent<RandomAI>();
		p1RandomAI.player = 1;

		p1FuzzyAI = null;
		if (UFE.isAiAddonInstalled && UFE.config.aiOptions.engine == AIEngine.FuzzyAI){
			p1FuzzyAI = gameObject.AddComponent(UFE.SearchClass("RuleBasedAI")) as AbstractInputController;
			p1FuzzyAI.player = 1;
			p1Controller.cpuController = p1FuzzyAI;
		}else{
			p1Controller.cpuController = p1RandomAI;
		}

		p1Controller.isCPU = config.p1CPUControl;
		p1Controller.player = 1;

		p2Controller = gameObject.AddComponent<UFEController> ();
		p2Controller.humanController = gameObject.AddComponent<InputController>();

		p2SimpleAI = gameObject.AddComponent<SimpleAI>();
		p2SimpleAI.player = 2;

		p2RandomAI = gameObject.AddComponent<RandomAI>();
		p2RandomAI.player = 2;

		p2FuzzyAI = null;
		if (UFE.isAiAddonInstalled && UFE.config.aiOptions.engine == AIEngine.FuzzyAI){
			p2FuzzyAI = gameObject.AddComponent(UFE.SearchClass("RuleBasedAI")) as AbstractInputController;
			p2FuzzyAI.player = 2;
			p2Controller.cpuController = p2FuzzyAI;
		}else{
			p2Controller.cpuController = p2RandomAI;
		}

		p2Controller.isCPU = config.p2CPUControl;
		p2Controller.player = 2;


		p1Controller.Initialize(config.player1_Inputs);
		p2Controller.Initialize(config.player2_Inputs);

		if (config.fps > 0) {
            UFE.timeScale = UFE.config._gameSpeed;
			Application.targetFrameRate = config.fps;
		}

        SetLanguage();
        UFE.InitializeAudioSystem();
        UFE.SetAIDifficulty(UFE.config.aiOptions.selectedDifficultyLevel);
        UFE.SetDebugMode(config.debugOptions.debugMode);

        // Load the player settings from disk
        UFE.SetMusic(PlayerPrefs.GetInt(UFE.MusicEnabledKey, 1) > 0);
		UFE.SetMusicVolume(PlayerPrefs.GetFloat(UFE.MusicVolumeKey, 1f));
		UFE.SetSoundFX(PlayerPrefs.GetInt(UFE.SoundsEnabledKey, 1) > 0);
		UFE.SetSoundFXVolume(PlayerPrefs.GetFloat(UFE.SoundsVolumeKey, 1f));
        
        // Load the intro screen or the combat, depending on the UFE Config settings
        if (UFE.config.debugOptions.startGameImmediately){
            if (UFE.config.debugOptions.matchType == MatchType.Training) {
                UFE.gameMode = GameMode.TrainingRoom;
            } else if (UFE.config.debugOptions.matchType == MatchType.Challenge) {
                UFE.gameMode = GameMode.ChallengeMode;
            } else {
                UFE.gameMode = GameMode.VersusMode;
            }
			UFE.config.player1Character = config.p1CharStorage;
			UFE.config.player2Character = config.p2CharStorage;
			UFE.SetCPU(1, config.p1CPUControl);
			UFE.SetCPU(2, config.p2CPUControl);

            if (UFE.config.debugOptions.skipLoadingScreen)
            {
                UFE._StartGame((float)UFE.config.gameGUI.gameFadeDuration);
            }
            else
            {
                UFE._StartLoadingBattleScreen((float)UFE.config.gameGUI.screenFadeDuration);
            }
		}else{
			UFE.StartIntroScreen(0f);
        }
    }

	protected void Update(){
		UFE.GetPlayer1Controller().DoUpdate();
		UFE.GetPlayer2Controller().DoUpdate();

#if UNITY_EDITOR
        // Save and Load State
        if (UFE.fluxCapacitor != null && UFE.config.debugOptions.stateTrackerTest) {
			if (Input.GetKeyDown(KeyCode.F2)) { // Save State
				Debug.Log("Save (" + UFE.currentFrame + ")");
                UFE.fluxCapacitor.savedState = FluxStateTracker.SaveGameState(UFE.currentFrame);

                //dictionaryList.Add(RecordVar.SaveStateTrackers(this, new Dictionary<MemberInfo, object>()));
                //testRecording = !testRecording;
            }
			if (UFE.fluxCapacitor.savedState != null && Input.GetKeyDown(KeyCode.F3)) { // Load State
				Debug.Log("Load (" + UFE.fluxCapacitor.savedState.Value.networkFrame + ")");
                FluxStateTracker.LoadGameState(UFE.fluxCapacitor.savedState.Value);
                UFE.fluxCapacitor.PlayerManager.Initialize(UFE.fluxCapacitor.savedState.Value.networkFrame);

                //UFE ufeInstance = this;
                //ufeInstance = RecordVar.LoadStateTrackers(ufeInstance, dictionaryList[dictionaryList.Count - 1]) as UFE;
                //p1ControlsScript.MoveSet.MecanimControl.Refresh();
                //p2ControlsScript.MoveSet.MecanimControl.Refresh();
            }
        }
#endif
    }

#if UNITY_EDITOR
    private void OnGUI() {
        if (UFE.config.debugOptions.stateTrackerTest && UFE.gameRunning)
        {
            if (GUI.Button(new Rect(10, 10, 160, 40), "Save State"))
            {
                Debug.Log("Save (" + UFE.currentFrame + ")");
                UFE.fluxCapacitor.savedState = FluxStateTracker.SaveGameState(UFE.currentFrame);

                //Debug.Log(UFE.GetPlayer1ControlsScript().MoveSet.GetCurrentClipFrame());
            }

            if (GUI.Button(new Rect(10, 60, 160, 40), "Load State"))
            {
                Debug.Log("Load (" + UFE.fluxCapacitor.savedState.Value.networkFrame + ")");
                FluxStateTracker.LoadGameState(UFE.fluxCapacitor.savedState.Value);
                UFE.fluxCapacitor.PlayerManager.Initialize(UFE.fluxCapacitor.savedState.Value.networkFrame);

                //Debug.Log(UFE.GetPlayer1ControlsScript().MoveSet.GetCurrentClipFrame());
            }
        }
    }
#endif

    //public List<Dictionary<System.Reflection.MemberInfo, System.Object>> dictionaryList = new List<Dictionary<System.Reflection.MemberInfo, System.Object>>();
    //public bool testRecording = false;

    protected void FixedUpdate(){
		if (UFE.fluxCapacitor != null){
			UFE.fluxCapacitor.DoFixedUpdate();

            /*if (testRecording)
            {
                dictionaryList.Add(RecordVar.SaveStateTrackers(this, new Dictionary<MemberInfo, object>()));
                if (dictionaryList.Count > 30) dictionaryList.RemoveAt(0);
            }*/
        }
	}
    
	protected void OnApplicationQuit(){
		UFE.closing = true;
		UFE.EnsureNetworkDisconnection();
	}
#endregion

#region protected instance methods: Network Events
	public static bool isConnected{
		get{
			return UFE.multiplayerAPI.IsConnected() && UFE.multiplayerAPI.Connections > 0;
		}
	}

	public static void EnsureNetworkDisconnection(){
		if (!UFE.disconnecting){
			NetworkState state = UFE.multiplayerAPI.GetConnectionState();

			if (state == NetworkState.Client){
				UFE.RemoveNetworkEventListeners();
				UFE.multiplayerAPI.DisconnectFromMatch();
			}else if (state == NetworkState.Server){
				UFE.RemoveNetworkEventListeners();
				UFE.multiplayerAPI.DestroyMatch();
			}
		}
    }

	protected static void AddNetworkEventListeners(){
		UFE.multiplayerAPI.OnDisconnection -= UFE.OnDisconnectedFromServer;
		UFE.multiplayerAPI.OnJoined -= UFE.OnJoined;
		UFE.multiplayerAPI.OnJoinError -= UFE.OnJoinError;
		UFE.multiplayerAPI.OnPlayerConnectedToMatch -= UFE.OnPlayerConnectedToMatch;
		UFE.multiplayerAPI.OnPlayerDisconnectedFromMatch -= UFE.OnPlayerDisconnectedFromMatch;
		UFE.multiplayerAPI.OnMatchesDiscovered -= UFE.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError -= UFE.OnMatchDiscoveryError;
		UFE.multiplayerAPI.OnMatchCreated -= UFE.OnMatchCreated;
		UFE.multiplayerAPI.OnMatchDestroyed -= UFE.OnMatchDestroyed;

		UFE.multiplayerAPI.OnDisconnection += UFE.OnDisconnectedFromServer;
		UFE.multiplayerAPI.OnJoined += UFE.OnJoined;
		UFE.multiplayerAPI.OnJoinError += UFE.OnJoinError;
		UFE.multiplayerAPI.OnPlayerConnectedToMatch += UFE.OnPlayerConnectedToMatch;
		UFE.multiplayerAPI.OnPlayerDisconnectedFromMatch += UFE.OnPlayerDisconnectedFromMatch;
		UFE.multiplayerAPI.OnMatchesDiscovered += UFE.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError += UFE.OnMatchDiscoveryError;
		UFE.multiplayerAPI.OnMatchCreated += UFE.OnMatchCreated;
		UFE.multiplayerAPI.OnMatchDestroyed += UFE.OnMatchDestroyed;
	}

	protected static void RemoveNetworkEventListeners(){
		UFE.multiplayerAPI.OnDisconnection -= UFE.OnDisconnectedFromServer;
		UFE.multiplayerAPI.OnJoined -= UFE.OnJoined;
		UFE.multiplayerAPI.OnJoinError -= UFE.OnJoinError;
		UFE.multiplayerAPI.OnPlayerConnectedToMatch -= UFE.OnPlayerConnectedToMatch;
		UFE.multiplayerAPI.OnPlayerDisconnectedFromMatch -= UFE.OnPlayerDisconnectedFromMatch;
		UFE.multiplayerAPI.OnMatchesDiscovered -= UFE.OnMatchesDiscovered;
		UFE.multiplayerAPI.OnMatchDiscoveryError -= UFE.OnMatchDiscoveryError;
		UFE.multiplayerAPI.OnMatchCreated -= UFE.OnMatchCreated;
		UFE.multiplayerAPI.OnMatchDestroyed -= UFE.OnMatchDestroyed;
	}

	protected static void OnJoined(MultiplayerAPI.JoinedMatchInformation match){
		if (UFE.config.debugOptions.connectionLog) Debug.Log("Connected to server");
		UFE.StartNetworkGame(0.5f, 2, false);
	}

	protected static void OnDisconnectedFromServer() {
        if (UFE.config.debugOptions.connectionLog) Debug.Log("Disconnected from server");
		UFE.fluxCapacitor.Initialize(); // Return to single player controls

		if (!UFE.closing){
			UFE.disconnecting = true;
			Application.runInBackground = UFE.config.runInBackground;

			if (UFE.config.lockInputs && UFE.currentScreen == null){
				UFE.DelayLocalAction(UFE.StartConnectionLostScreenIfMainMenuNotLoaded, 1f);
			}else{
				UFE.StartConnectionLostScreen();
			}
		}
	}

	protected static void OnJoinError() {
        if (UFE.config.debugOptions.connectionLog) Debug.Log("Could not connect to server");
		Application.runInBackground = UFE.config.runInBackground;
		UFE.StartConnectionLostScreen();
	}

	protected static void OnMatchCreated(MultiplayerAPI.CreatedMatchInformation match){}

	protected static void OnMatchDestroyed(){}

	protected static void OnMatchJoined(JoinMatchResponse response){}

	protected static void OnMatchDropped(){}

	protected static void OnPlayerConnectedToMatch(MultiplayerAPI.PlayerInformation player) {
		if (UFE.config.debugOptions.connectionLog){
			if (player.networkIdentity != null){
				Debug.Log("Connection: " + player.networkIdentity.connectionToClient);
			}else{
				Debug.Log("Player connected: " + player.photonPlayer);
			}
		}

		UFE.StartNetworkGame(0.5f, 1, false);
	}

	protected static void OnPlayerDisconnectedFromMatch(MultiplayerAPI.PlayerInformation player) {
        if (UFE.config.debugOptions.connectionLog) Debug.Log("Clean up after player " + player);
		UFE.fluxCapacitor.Initialize(); // Return to single player controls

		if (!UFE.closing){
			UFE.disconnecting = true;
			Application.runInBackground = UFE.config.runInBackground;

			if (UFE.config.lockInputs && UFE.currentScreen == null){
				UFE.DelayLocalAction(UFE.StartConnectionLostScreenIfMainMenuNotLoaded, 1f);
			}else{
				UFE.StartConnectionLostScreen();
			}
		}
	}

	protected static void OnServerInitialized() {
        if (UFE.config.debugOptions.connectionLog) Debug.Log("Server initialized and ready");
		Application.runInBackground = true;
		UFE.disconnecting = false;
	}
#endregion

#region private class methods: GUI Related methods
    public static Text DebuggerText(string dName, string dText, Vector2 position, TextAnchor alignment)
    {
        GameObject debugger = new GameObject(dName);
        debugger.transform.SetParent(UFE.canvas.transform);

        RectTransform trans = debugger.AddComponent<RectTransform>();
        trans.anchoredPosition = position;

        Text debuggerText = debugger.AddComponent<Text>();
        debuggerText.text = dText;
        debuggerText.alignment = alignment;
        debuggerText.color = Color.black;
        debuggerText.fontStyle = FontStyle.Bold;

        Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        debuggerText.font = ArialFont;
        debuggerText.fontSize = 24;
        debuggerText.verticalOverflow = VerticalWrapMode.Overflow;
        debuggerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        debuggerText.material = ArialFont.material;

        //Outline debuggerTextOutline = debugger.AddComponent<Outline>();
        //debuggerTextOutline.effectColor = Color.white;

        return debuggerText;
    }

    public static void GoToNetworkGameScreen(){
		if (UFE.multiplayerMode == MultiplayerMode.Bluetooth){
			UFE.StartBluetoothGameScreen();
		}else{
			UFE.StartNetworkGameScreen();
		}
	}

	public static void GoToNetworkGameScreen(float fadeTime){
		if (UFE.multiplayerMode == MultiplayerMode.Bluetooth){
			UFE.StartBluetoothGameScreen(fadeTime);
		}else{
			UFE.StartNetworkGameScreen(fadeTime);
		}
    }

	private static void _StartBluetoothGameScreen(float fadeTime){
		UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.bluetoothGameScreen == null){
			Debug.LogError("Bluetooth Game Screen not found! Make sure you have set the prefab correctly in the Global Editor");
		}else if (UFE.isNetworkAddonInstalled){
            UFE.ShowScreen(UFE.config.gameGUI.bluetoothGameScreen);
            if (!UFE.config.gameGUI.bluetoothGameScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			Debug.LogWarning("Network Addon not found!");
		}
	}

	private static void _StartCharacterSelectionScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.characterSelectionScreen == null){
			Debug.LogError("Character Selection Screen not found! Make sure you have set the prefab correctly in the Global Editor");
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.characterSelectionScreen);
            if (!UFE.config.gameGUI.characterSelectionScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartIntroScreen(float fadeTime){
		UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.introScreen == null){
			//Debug.Log("Intro Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.introScreen);
            if (!UFE.config.gameGUI.introScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
        }
	}

	private static void _StartMainMenuScreen(float fadeTime){
		UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.mainMenuScreen == null){
			Debug.LogError("Main Menu Screen not found! Make sure you have set the prefab correctly in the Global Editor");
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.mainMenuScreen);
            if (!UFE.config.gameGUI.mainMenuScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartStageSelectionScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.stageSelectionScreen == null){
			Debug.LogError("Stage Selection Screen not found! Make sure you have set the prefab correctly in the Global Editor");
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.stageSelectionScreen);
            if (!UFE.config.gameGUI.stageSelectionScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}
	
	private static void _StartCreditsScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.creditsScreen == null){
			Debug.Log("Credits screen not found! Make sure you have set the prefab correctly in the Global Editor");
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.creditsScreen);
            if (!UFE.config.gameGUI.creditsScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartConnectionLostScreen(float fadeTime){
		UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.connectionLostScreen == null){
			Debug.LogError("Connection Lost Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else if (UFE.isNetworkAddonInstalled){
            UFE.ShowScreen(UFE.config.gameGUI.connectionLostScreen);
            if (!UFE.config.gameGUI.connectionLostScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			Debug.LogWarning("Network Addon not found!");
			UFE._StartMainMenuScreen(fadeTime);
		}
	}

	private static void _StartGame(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.battleGUI == null){
			Debug.LogError("Battle GUI not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE.battleGUI = new GameObject("BattleGUI").AddComponent<UFEScreen>();
		}else{
			UFE.battleGUI = (UFEScreen) GameObject.Instantiate(UFE.config.gameGUI.battleGUI);
        }
        if (!UFE.battleGUI.hasFadeIn) fadeTime = 0;
        CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);

		UFE.battleGUI.transform.SetParent(UFE.canvas != null ? UFE.canvas.transform : null, false);
        UFE.battleGUI.OnShow();
        UFE.canvasGroup.alpha = 0;
		
		UFE.gameEngine = new GameObject("Game");
		UFE.cameraScript = UFE.gameEngine.AddComponent<CameraScript>();

		if (UFE.config.player1Character == null){
			Debug.LogError("No character selected for player 1.");
			return;
		}
		if (UFE.config.player2Character == null){
			Debug.LogError("No character selected for player 2.");
			return;
		}
		if (UFE.config.selectedStage == null){
			Debug.LogError("No stage selected.");
			return;
		}
		
		if (UFE.config.aiOptions.engine == AIEngine.FuzzyAI){
			UFE.SetFuzzyAI(1, UFE.config.player1Character);
            UFE.SetFuzzyAI(2, UFE.config.player2Character);
        } else {
            UFE.SetRandomAI(1);
            UFE.SetRandomAI(2);
        }
		
		UFE.config.player1Character.currentLifePoints = (Fix64)UFE.config.player1Character.lifePoints;
		UFE.config.player2Character.currentLifePoints = (Fix64)UFE.config.player2Character.lifePoints;
		UFE.config.player1Character.currentGaugePoints = 0;
		UFE.config.player2Character.currentGaugePoints = 0;

        GameObject stageInstance = null;
        if (UFE.config.stagePrefabStorage == StorageMode.Legacy) {
            if (UFE.config.selectedStage.prefab != null) {
                stageInstance = (GameObject)Instantiate(config.selectedStage.prefab);
                stageInstance.transform.parent = gameEngine.transform;
            } else {
                Debug.LogError("Stage prefab not found! Make sure you have set the prefab correctly in the Global Editor.");
            }
        } else {
            GameObject prefab = Resources.Load<GameObject>(config.selectedStage.stageResourcePath);

            if (prefab != null) {
                stageInstance = (GameObject)GameObject.Instantiate(prefab);
                stageInstance.transform.parent = gameEngine.transform;
            } else {
                Debug.LogError("Stage prefab not found! Make sure the prefab is correctly located under the Resources folder and the path is written correctly.");
            }
        }


        UFE.config.currentRound = 1;
		UFE.config.lockInputs = true;
		UFE.SetTimer(config.roundOptions._timer);
		UFE.PauseTimer();

        // Initialize Player 1 Character
		GameObject p1 = new GameObject("Player1");
		p1.transform.parent = gameEngine.transform;
        UFE.p1ControlsScript = p1.AddComponent<ControlsScript>();
        UFE.p1ControlsScript.Physics = p1.AddComponent<PhysicsScript>();
        UFE.p1ControlsScript.myInfo = (UFE3D.CharacterInfo)Instantiate(UFE.config.player1Character);

        UFE.config.player1Character = UFE.p1ControlsScript.myInfo;
        UFE.p1ControlsScript.myInfo.playerNum = 1;
        if (UFE.isControlFreak2Installed && UFE.p1ControlsScript.myInfo.customControls.overrideControlFreak && UFE.p1ControlsScript.myInfo.customControls.controlFreak2Prefab != null) {
            UFE.controlFreakPrefab = (GameObject)Instantiate(UFE.p1ControlsScript.myInfo.customControls.controlFreak2Prefab.gameObject);
            UFE.touchControllerBridge = (UFE.controlFreakPrefab != null) ? UFE.controlFreakPrefab.GetComponent<InputTouchControllerBridge>() : null;
            UFE.touchControllerBridge.Init();
        }

        // Initialize Player 2 Character
		GameObject p2 = new GameObject("Player2");
		p2.transform.parent = gameEngine.transform;
        UFE.p2ControlsScript = p2.AddComponent<ControlsScript>();
        UFE.p2ControlsScript.Physics = p2.AddComponent<PhysicsScript>();
        UFE.p2ControlsScript.myInfo = (UFE3D.CharacterInfo)Instantiate(UFE.config.player2Character);
        UFE.config.player2Character = UFE.p2ControlsScript.myInfo;
        UFE.p2ControlsScript.myInfo.playerNum = 2;


        // If the same character is selected, try loading the alt costume
        if (UFE.config.player1Character.name == UFE.config.player2Character.name) {
            if (UFE.config.player2Character.alternativeCostumes.Length > 0) {
                UFE.config.player2Character.isAlt = true;
                UFE.config.player2Character.selectedCostume = 0;
                
                if (UFE.config.player2Character.alternativeCostumes[0].characterPrefabStorage == StorageMode.Legacy) {
                    UFE.p2ControlsScript.myInfo.characterPrefab = UFE.config.player2Character.alternativeCostumes[0].prefab;
                } else {
                    UFE.p2ControlsScript.myInfo.characterPrefab = Resources.Load<GameObject>(UFE.config.player2Character.alternativeCostumes[0].prefabResourcePath);
                }
            }
        }

        // Initialize Debuggers
        UFE.debugger1 = UFE.DebuggerText("Debugger1", "", new Vector2(- Screen.width + 50, Screen.height - 180), TextAnchor.UpperLeft);
        UFE.debugger2 = UFE.DebuggerText("Debugger2", "", new Vector2(Screen.width - 50, Screen.height - 180), TextAnchor.UpperRight);
        UFE.p1ControlsScript.debugger = UFE.debugger1;
        UFE.p2ControlsScript.debugger = UFE.debugger2;
        UFE.debugger1.enabled = UFE.debugger2.enabled = config.debugOptions.debugMode;


        //UFE.fluxGameManager.Initialize(UFE.currentFrame);
        UFE.fluxCapacitor.savedState = null;
        UFE.PauseGame(false);
    }

    //Preloader
    public static void PreloadBattle() {
        PreloadBattle((float)UFE.config._preloadingTime);
    }

    public static void PreloadBattle(float warmTimer) {
        if (UFE.config.preloadHitEffects) {
            SearchAndCastGameObject(UFE.config.hitOptions, warmTimer);
            SearchAndCastGameObject(UFE.config.groundBounceOptions, warmTimer);
            SearchAndCastGameObject(UFE.config.wallBounceOptions, warmTimer);
            if (UFE.config.debugOptions.preloadedObjects) Debug.Log("Hit Effects Loaded");
        }
        if (UFE.config.preloadStage) {
            SearchAndCastGameObject(UFE.config.selectedStage, warmTimer);
            if (UFE.config.debugOptions.preloadedObjects) Debug.Log("Stage Loaded");
        }
        if (UFE.config.preloadCharacter1) {
            SearchAndCastGameObject(UFE.config.player1Character, warmTimer);
            if (UFE.config.debugOptions.preloadedObjects) Debug.Log("Character 1 Loaded");
        }
        if (UFE.config.preloadCharacter2) {
            SearchAndCastGameObject(UFE.config.player2Character, warmTimer);
            if (UFE.config.debugOptions.preloadedObjects) Debug.Log("Character 2 Loaded");
        }
        if (UFE.config.warmAllShaders) Shader.WarmupAllShaders();

        memoryDump.Clear();
    }

    public static void SearchAndCastGameObject(object target, float warmTimer) {
        if (target != null) {
            Type typeSource = target.GetType();
            FieldInfo[] fields = typeSource.GetFields();

            foreach (FieldInfo field in fields) {
                object fieldValue = field.GetValue(target);
                if (fieldValue == null || fieldValue.Equals(null)) continue;
                if (memoryDump.Contains(fieldValue)) continue;
                memoryDump.Add(fieldValue);

                if (field.FieldType.Equals(typeof(GameObject))) {
                    if (UFE.config.debugOptions.preloadedObjects) Debug.Log(fieldValue + " preloaded");
                    GameObject tempGO = (GameObject)Instantiate((GameObject)fieldValue);
                    tempGO.transform.position = new Vector2(-999, -999);

                    //Light lightComponent = tempGO.GetComponent<Light>();
                    //if (lightComponent != null) lightComponent.enabled = false;

                    Destroy(tempGO, warmTimer);

                } else if (field.FieldType.IsArray && !field.FieldType.GetElementType().IsEnum) {
                    object[] fieldValueArray = (object[])fieldValue;
                    foreach (object obj in fieldValueArray) {
                        SearchAndCastGameObject(obj, warmTimer);
                    }
                }
            }
        }
    }

	private static void _StartHostGameScreen(float fadeTime){
		UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.hostGameScreen == null){
			Debug.LogError("Host Game Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else if (UFE.isNetworkAddonInstalled){
            UFE.ShowScreen(UFE.config.gameGUI.hostGameScreen);
            if (!UFE.config.gameGUI.hostGameScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			Debug.LogWarning("Network Addon not found!");
			UFE._StartMainMenuScreen(fadeTime);
		}
	}

	private static void _StartJoinGameScreen(float fadeTime){
		UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.joinGameScreen == null){
			Debug.LogError("Join To Game Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else if (UFE.isNetworkAddonInstalled){
            UFE.ShowScreen(UFE.config.gameGUI.joinGameScreen);
            if (!UFE.config.gameGUI.joinGameScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			Debug.LogWarning("Network Addon not found!");
			UFE._StartMainMenuScreen(fadeTime);
		}
	}
	
	private static void _StartLoadingBattleScreen(float fadeTime){
        UFE.config.lockInputs = true;

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.loadingBattleScreen == null){
			Debug.Log("Loading Battle Screen not found! Make sure you have set the prefab correctly in the Global Editor");
            UFE._StartGame((float)UFE.config.gameGUI.gameFadeDuration);
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.loadingBattleScreen);
            if (!UFE.config.gameGUI.loadingBattleScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartSearchMatchScreen(float fadeTime){
		//UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.searchMatchScreen == null){
			Debug.LogError("Random Match Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else if (UFE.isNetworkAddonInstalled){
			//UFE.AddNetworkEventListeners();
            UFE.ShowScreen(UFE.config.gameGUI.searchMatchScreen);
            if (!UFE.config.gameGUI.searchMatchScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			Debug.LogWarning("Network Addon not found!");
			UFE._StartMainMenuScreen(fadeTime);
		}
	}

	private static void _StartNetworkGameScreen(float fadeTime){
		UFE.EnsureNetworkDisconnection();

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.networkGameScreen == null){
			Debug.LogError("Network Game Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else if (UFE.isNetworkAddonInstalled){
            UFE.ShowScreen(UFE.config.gameGUI.networkGameScreen);
            if (!UFE.config.gameGUI.networkGameScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			Debug.LogWarning("Network Addon not found!");
			UFE._StartMainMenuScreen(fadeTime);
		}
	}

	private static void _StartOptionsScreen(float fadeTime){

		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.optionsScreen == null){
			Debug.LogError("Options Screen not found! Make sure you have set the prefab correctly in the Global Editor");
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.optionsScreen);
            if (!UFE.config.gameGUI.optionsScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	public static void _StartStoryModeBattle(float fadeTime){
		// If the player 1 won the last battle, load the information of the next battle. 
		// Otherwise, repeat the last battle...
		UFE3D.CharacterInfo character = UFE.GetPlayer(1);

		if (UFE.player1WonLastBattle){
			// If the player 1 won the last battle...
			if (UFE.storyMode.currentGroup < 0){
				// If we haven't fought any battle, raise the "Story Mode Started" event...
				if (UFE.OnStoryModeStarted != null){
					UFE.OnStoryModeStarted(character);
				}

				// And start with the first battle of the first group
				UFE.storyMode.currentGroup = 0;
				UFE.storyMode.currentBattle = 0;
			}else if (UFE.storyMode.currentGroup >= 0 && UFE.storyMode.currentGroup < UFE.storyMode.characterStory.fightsGroups.Length){
				// Otherwise, check if there are more remaining battles in the current group
				FightsGroup currentGroup = UFE.storyMode.characterStory.fightsGroups[UFE.storyMode.currentGroup];
				int numberOfFights = currentGroup.maxFights;
				
				if (currentGroup.mode != FightsGroupMode.FightAgainstSeveralOpponentsInTheGroupInRandomOrder){
					numberOfFights = currentGroup.opponents.Length;
				}
				
				if (UFE.storyMode.currentBattle < numberOfFights - 1){
					// If there are more battles in the current group, go to the next battle...
					++UFE.storyMode.currentBattle;
				}else{
					// Otherwise, go to the next group of battles...
					++UFE.storyMode.currentGroup;
					UFE.storyMode.currentBattle = 0;
					UFE.storyMode.defeatedOpponents.Clear();
				}
			}

			// If the player hasn't finished the game...
			UFE.storyMode.currentBattleInformation = null;
			while (
				UFE.storyMode.currentBattleInformation == null &&
				UFE.storyMode.currentGroup >= 0 && 
				UFE.storyMode.currentGroup < UFE.storyMode.characterStory.fightsGroups.Length
			){
				// Try to retrieve the information of the next battle
				FightsGroup currentGroup = UFE.storyMode.characterStory.fightsGroups[UFE.storyMode.currentGroup];
				UFE.storyMode.currentBattleInformation = null;
				
				if (currentGroup.mode == FightsGroupMode.FightAgainstAllOpponentsInTheGroupInTheDefinedOrder){
					StoryModeBattle b = currentGroup.opponents[UFE.storyMode.currentBattle];
					UFE3D.CharacterInfo opponent = UFE.config.characters[b.opponentCharacterIndex];

					if (UFE.storyMode.canFightAgainstHimself || !character.characterName.Equals(opponent.characterName)){
						UFE.storyMode.currentBattleInformation = b;
					}else{
						// Otherwise, check if there are more remaining battles in the current group
						int numberOfFights = currentGroup.maxFights;
						
						if (currentGroup.mode != FightsGroupMode.FightAgainstSeveralOpponentsInTheGroupInRandomOrder){
							numberOfFights = currentGroup.opponents.Length;
						}
						
						if (UFE.storyMode.currentBattle < numberOfFights - 1){
							// If there are more battles in the current group, go to the next battle...
							++UFE.storyMode.currentBattle;
						}else{
							// Otherwise, go to the next group of battles...
							++UFE.storyMode.currentGroup;
							UFE.storyMode.currentBattle = 0;
							UFE.storyMode.defeatedOpponents.Clear();
						}
					}
				}else{
					List<StoryModeBattle> possibleBattles = new List<StoryModeBattle>();
					
					foreach (StoryModeBattle b in currentGroup.opponents){
						if (!UFE.storyMode.defeatedOpponents.Contains(b.opponentCharacterIndex)){
							UFE3D.CharacterInfo opponent = UFE.config.characters[b.opponentCharacterIndex];
							
							if (UFE.storyMode.canFightAgainstHimself || !character.characterName.Equals(opponent.characterName)){
								possibleBattles.Add(b);
							}
						}
					}
					
					if (possibleBattles.Count > 0){
						int index = UnityEngine.Random.Range(0, possibleBattles.Count);
						UFE.storyMode.currentBattleInformation = possibleBattles[index];
					}else{
						// If we can't find a valid battle in this group, try moving to the next group
						++UFE.storyMode.currentGroup;
					}
				}
			}
		}

		if (UFE.storyMode.currentBattleInformation != null){
			// If we could retrieve the battle information, load the opponent and the stage
			int characterIndex = UFE.storyMode.currentBattleInformation.opponentCharacterIndex;
			UFE.SetPlayer2(UFE.config.characters[characterIndex]);

			if (UFE.player1WonLastBattle){
				UFE.lastStageIndex = UnityEngine.Random.Range(0, UFE.storyMode.currentBattleInformation.possibleStagesIndexes.Count);
			}

			UFE.SetStage(UFE.config.stages[UFE.storyMode.currentBattleInformation.possibleStagesIndexes[UFE.lastStageIndex]]);
			
			// Finally, check if we should display any "Conversation Screen" before the battle
			UFE._StartStoryModeConversationBeforeBattleScreen(UFE.storyMode.currentBattleInformation.conversationBeforeBattle, fadeTime);
		}else{
			// Otherwise, show the "Congratulations" Screen
			if (UFE.OnStoryModeCompleted != null){
				UFE.OnStoryModeCompleted(character);
			}

			UFE._StartStoryModeCongratulationsScreen(fadeTime);
		}
	}

	private static void _StartStoryModeCongratulationsScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.storyModeCongratulationsScreen == null){
			Debug.Log("Congratulations Screen not found! Make sure you have set the prefab correctly in the Global Editor");
            UFE._StartStoryModeEndingScreen(fadeTime);
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.storyModeCongratulationsScreen, delegate() { UFE.StartStoryModeEndingScreen(fadeTime); });
            if (!UFE.config.gameGUI.storyModeCongratulationsScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartStoryModeContinueScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.storyModeContinueScreen == null){
			Debug.Log("Continue Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.storyModeContinueScreen);
            if (!UFE.config.gameGUI.storyModeContinueScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

    private static void _StartStoryModeConversationAfterBattleScreen(UFEScreen conversationScreen, float fadeTime) {
        UFE.HideScreen(UFE.currentScreen);
		if (conversationScreen != null){
            UFE.ShowScreen(conversationScreen, delegate() { UFE.StartStoryModeBattle(fadeTime); });
            if (!conversationScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			UFE._StartStoryModeBattle(fadeTime);
		}
	}

    private static void _StartStoryModeConversationBeforeBattleScreen(UFEScreen conversationScreen, float fadeTime) {
        UFE.HideScreen(UFE.currentScreen);
		if (conversationScreen != null){
            UFE.ShowScreen(conversationScreen, delegate() { UFE.StartLoadingBattleScreen(fadeTime); });
            if (!conversationScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}else{
			UFE._StartLoadingBattleScreen(fadeTime);
		}
	}

	private static void _StartStoryModeEndingScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.storyMode.characterStory.ending == null){
			Debug.Log("Ending Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartCreditsScreen(fadeTime);
		}else{
            UFE.ShowScreen(UFE.storyMode.characterStory.ending, delegate() { UFE.StartCreditsScreen(fadeTime); });
            if (!UFE.storyMode.characterStory.ending.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartStoryModeGameOverScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.storyModeGameOverScreen == null){
			Debug.Log("Game Over Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartMainMenuScreen(fadeTime);
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.storyModeGameOverScreen, delegate() { UFE.StartMainMenuScreen(fadeTime); });
            if (!UFE.config.gameGUI.storyModeGameOverScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartStoryModeOpeningScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.storyMode.characterStory.opening == null){
			Debug.Log("Opening Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE._StartStoryModeBattle(fadeTime);
		}else{
            UFE.ShowScreen(UFE.storyMode.characterStory.opening, delegate() { UFE.StartStoryModeBattle(fadeTime); });
            if (!UFE.storyMode.characterStory.opening.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartVersusModeScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.versusModeScreen == null){
			Debug.Log("Versus Mode Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			UFE.StartPlayerVersusPlayer(fadeTime);
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.versusModeScreen);
            if (!UFE.config.gameGUI.versusModeScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}

	private static void _StartVersusModeAfterBattleScreen(float fadeTime){
		UFE.HideScreen(UFE.currentScreen);
		if (UFE.config.gameGUI.versusModeAfterBattleScreen == null){
			Debug.Log("Versus Mode \"After Battle\" Screen not found! Make sure you have set the prefab correctly in the Global Editor");
			
			UFE._StartMainMenuScreen(fadeTime);
		}else{
            UFE.ShowScreen(UFE.config.gameGUI.versusModeAfterBattleScreen);
            if (!UFE.config.gameGUI.versusModeAfterBattleScreen.hasFadeIn) fadeTime = 0;
            CameraFade.StartAlphaFade(UFE.config.gameGUI.screenFadeColor, true, fadeTime);
		}
	}
#endregion
}