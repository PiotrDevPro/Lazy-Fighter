using UnityEngine;
using System.Collections;

public class CameraScript : MonoBehaviour {

    #region trackable definitions
    public bool cinematicFreeze;
    public Vector3 currentLookAtPosition;
    public float freeCameraSpeed;
    public string lastOwner;
    public bool killCamMove;
    public float movementSpeed;
    public float rotationSpeed;
    public float standardDistance;
    public float standardGroundHeight;
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public float targetFieldOfView;
    #endregion


    public GameObject playerLight;
    public Transform player1;
	public Transform player2;
	
    
	void Start(){
		playerLight = GameObject.Find("Player Light");
		player1 = GameObject.Find("Player1").transform;
		player2 = GameObject.Find("Player2").transform;

		ResetCam();
		//standardZoom = UFE.config.cameraOptions.initialDistance.z;
        standardDistance = Vector3.Distance(player1.position, player2.position);
        movementSpeed = UFE.config.cameraOptions.movementSpeed;
        rotationSpeed = UFE.config.cameraOptions.rotationSpeed;
        UFE.freeCamera = false;

	}

	public void ResetCam(){
		Camera.main.transform.localPosition = UFE.config.cameraOptions.initialDistance;
		Camera.main.transform.position = UFE.config.cameraOptions.initialDistance;
		Camera.main.transform.localRotation = Quaternion.Euler(UFE.config.cameraOptions.initialRotation);
		Camera.main.fieldOfView = UFE.config.cameraOptions.initialFieldOfView;
		//standardGroundHeight = Camera.main.transform.position.y;

	}

	public Vector3 LerpByDistance(Vector3 A, Vector3 B, float speed){
		Vector3 P = speed * (float)UFE.fixedDeltaTime * Vector3.Normalize(B - A) + A;
		return P;
	}

	public void DoFixedUpdate() {
		if (killCamMove) return;
		if (UFE.freeCamera) {
			Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFieldOfView, (float)UFE.fixedDeltaTime * freeCameraSpeed * 1.8f);
			Camera.main.transform.localPosition = Vector3.Lerp(Camera.main.transform.localPosition, targetPosition, (float)UFE.fixedDeltaTime * freeCameraSpeed * 1.8f);
			Camera.main.transform.localRotation = Quaternion.Slerp(Camera.main.transform.localRotation, targetRotation, (float)UFE.fixedDeltaTime * freeCameraSpeed * 1.8f);

		}else{
			Vector3 newPosition = ((player1.position + player2.position)/2) + UFE.config.cameraOptions.initialDistance;
			if (UFE.config.cameraOptions.followJumpingCharacter) 
				newPosition.y += Mathf.Abs(player1.position.y - player2.position.y)/2;

			newPosition.x = Mathf.Clamp(newPosition.x, 
				(float)UFE.config.selectedStage._leftBoundary + 8,
                (float)UFE.config.selectedStage._rightBoundary - 8);

            newPosition.z = UFE.config.cameraOptions.initialDistance.z - Vector3.Distance(player1.position, player2.position) + standardDistance;
			newPosition.z = Mathf.Clamp(newPosition.z, -UFE.config.cameraOptions.maxZoom, -UFE.config.cameraOptions.minZoom);

            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, UFE.config.cameraOptions.initialFieldOfView, (float)UFE.fixedDeltaTime * movementSpeed);
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, newPosition, (float)UFE.fixedDeltaTime * movementSpeed);
			Camera.main.transform.localRotation = Quaternion.Slerp(Camera.main.transform.localRotation, Quaternion.Euler(UFE.config.cameraOptions.initialRotation), (float)UFE.fixedDeltaTime * UFE.config.cameraOptions.movementSpeed);

			if (Camera.main.transform.localRotation == Quaternion.Euler(UFE.config.cameraOptions.initialRotation))
				UFE.normalizedCam = true;

			if (playerLight != null) playerLight.GetComponent<Light>().enabled = false;

			if (UFE.config.cameraOptions.enableLookAt) {
                //Vector3 lookAtPosition = ((player1.position + player2.position)/2);
                //lookAtPosition.y += UFE.config.cameraOptions.heightOffSet;

                Vector3 newLookAtPosition = ((player1.position + player2.position) / 2) + UFE.config.cameraOptions.rotationOffSet;

                if (UFE.config.cameraOptions.motionSensor != MotionSensor.None) {
                    Vector3 acceleration = Input.acceleration;
                    if (UFE.config.cameraOptions.motionSensor == MotionSensor.Gyroscope && SystemInfo.supportsGyroscope) acceleration = Input.gyro.gravity;

#if UNITY_STANDALONE || UNITY_EDITOR
                    if (Input.mousePresent) {
                        Vector3 mouseXY = new Vector3(Input.mousePosition.x - Screen.width / 2, Input.mousePosition.y - Screen.height / 2, 0);
                        acceleration = mouseXY / 1000;
                    }
#endif
                    acceleration *= UFE.config.cameraOptions.motionSensibility;
                    newLookAtPosition -= acceleration;

                    newPosition.y += acceleration.y;
                    Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, newPosition, (float)UFE.fixedDeltaTime * movementSpeed);
                }

                currentLookAtPosition = Vector3.Lerp(currentLookAtPosition,
                    newLookAtPosition, 
                    (float)UFE.fixedDeltaTime * rotationSpeed);


                Camera.main.transform.LookAt(currentLookAtPosition, Vector3.up);
			}

		}
	}

	public void MoveCameraToLocation(Vector3 targetPos, Vector3 targetRot, float targetFOV, float speed, string owner){
		targetFieldOfView = targetFOV;
		targetPosition = targetPos;
		targetRotation = Quaternion.Euler(targetRot);
		freeCameraSpeed = speed;
		UFE.freeCamera = true;
		UFE.normalizedCam = false;
		lastOwner = owner;
		if (playerLight != null) playerLight.GetComponent<Light>().enabled = true;
	}
	
	public void DisableCam(){
		Camera.main.enabled = false;
	}

	public void ReleaseCam(){
		Camera.main.enabled = true;
		cinematicFreeze = false;
		UFE.freeCamera = false;
		lastOwner = "";
	}

    public void OverrideSpeed(float newMovement, float newRotation) {
        movementSpeed = newMovement;
        rotationSpeed = newRotation;
    }

    public void RestoreSpeed() {
        movementSpeed = UFE.config.cameraOptions.movementSpeed;
        rotationSpeed = UFE.config.cameraOptions.rotationSpeed;
    }

	public void SetCameraOwner(string owner){
		lastOwner = owner;
	}

	public string GetCameraOwner(){
		return lastOwner;
	}

	public Vector3 GetRelativePosition(Transform origin, Vector3 position) {
		Vector3 distance = position - origin.position;
		Vector3 relativePosition = Vector3.zero;
		relativePosition.x = Vector3.Dot(distance, origin.right.normalized);
		relativePosition.y = Vector3.Dot(distance, origin.up.normalized);
		relativePosition.z = Vector3.Dot(distance, origin.forward.normalized);
		
		return relativePosition;
	}

    void OnDrawGizmos() {
        Vector3 cameraLeftBounds = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, -Camera.main.transform.position.z));
        Vector3 cameraRightBounds = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, -Camera.main.transform.position.z));

        cameraLeftBounds.x = Camera.main.transform.position.x - ((float)UFE.config.cameraOptions._maxDistance / 2);
        cameraRightBounds.x = Camera.main.transform.position.x + ((float)UFE.config.cameraOptions._maxDistance / 2);

        Gizmos.DrawLine(cameraLeftBounds, cameraLeftBounds + new Vector3(0, 15, 0));
        Gizmos.DrawLine(cameraRightBounds, cameraRightBounds + new Vector3(0, 15, 0));
        //Gizmos.DrawWireSphere(cameraRightBounds, 1);
    }
}
