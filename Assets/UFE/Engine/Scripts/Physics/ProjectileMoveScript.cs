using UnityEngine;
using System.Collections;
using UFENetcode;
using FPLibrary;

public class ProjectileMoveScript : UFEBehaviour, UFEInterface {
	public ControlsScript opControlsScript {
		get{
			return this.myControlsScript.opControlsScript;
		}
	}

	public HitBoxesScript opHitBoxesScript{
		get{
			return this.opControlsScript.HitBoxes;
		}
	}
    
	public Projectile data;
	public int mirror = -1;
	public ControlsScript myControlsScript;
	public HurtBox hurtBox;
	public HitBox hitBox;
	public BlockArea blockableArea;


    [RecordVar] public Renderer projectileRenderer;
    [RecordVar] public Fix64 isHit = 0;
    [RecordVar] public int totalHits = int.MinValue;
    [RecordVar] public bool destroyMe;
    [RecordVar] public FPVector fpPos { get { return fpTransform.position; } set { fpTransform.position = value; } }


    // Runtime properties which are only modified when the projectile is instantiated.
    [HideInInspector] public FPVector directionVector = new FPVector(1, 0, 0);
	[HideInInspector] public FPVector movement;
	[HideInInspector] public Fix64 spaceBetweenHits = .1;
	[HideInInspector] public Hit hit;
	[HideInInspector] public FPTransform fpTransform;

	// Runtime Properties Required for instantiating a destroyed projectile (load/save state)

    
	//private int opProjectileLayer;
	//private int opProjectileMask;
	
	void Start () {
		gameObject.AddComponent<SphereCollider>();


        if (mirror == 1) directionVector.x = -1;

		if (totalHits == int.MinValue){
			totalHits = data.totalHits;
		}
        
		Fix64 angleRad = ((Fix64)data.directionAngle/180) * FPMath.Pi;
		movement = ((FPMath.Sin(angleRad) * FPVector.up) + (FPMath.Cos(angleRad) * directionVector)) * data.speed;
        fpTransform.Translate(new FPVector(data._castingOffSet.x * -mirror, data._castingOffSet.y, data._castingOffSet.z));

		// Create Blockable Area
		blockableArea = new BlockArea();
		blockableArea = data.blockableArea;

		// Create Hurtbox
		hurtBox = new HurtBox();
		hurtBox = data.hurtBox;

		// Create Hitbox
		hitBox = new HitBox();
		hitBox.shape = hurtBox.shape;
		hitBox._rect = hurtBox._rect;
		hitBox.followXBounds = hurtBox.followXBounds;
		hitBox.followYBounds = hurtBox.followYBounds;
		hitBox._radius = hurtBox._radius;
		hitBox._offSet = hurtBox._offSet;
		hitBox.position = gameObject.transform;

		UpdateRenderer();

		if (data.spaceBetweenHits == Sizes.Small){
			spaceBetweenHits = .15;
		}else if (data.spaceBetweenHits == Sizes.Medium){
			spaceBetweenHits = .2;
		}else if (data.spaceBetweenHits == Sizes.High){
			spaceBetweenHits = .3;
		}

		
		// Create Hit data
		hit = new Hit();
		hit.hitType = data.hitType;
		hit.spaceBetweenHits = data.spaceBetweenHits;
		hit.hitStrength = data.hitStrength;
		hit.hitStunType = HitStunType.Frames;
		hit._hitStunOnHit = data.hitStunOnHit;
		hit._hitStunOnBlock = data.hitStunOnBlock;
		hit._damageOnHit = data._damageOnHit;
		hit._damageOnBlock = data._damageOnBlock;
		hit.damageScaling = data.damageScaling;
		hit.damageType = data.damageType;
		hit.groundHit = data.groundHit;
		hit.airHit = data.airHit;
		hit.downHit = data.downHit;
        hit.overrideHitEffects = data.overrideHitEffects;
        hit.armorBreaker = data.armorBreaker;
		hit.hitEffects = data.hitEffects;
		hit.resetPreviousHorizontalPush = data.resetPreviousHorizontalPush;
		hit.resetPreviousVerticalPush = data.resetPreviousVerticalPush;
		hit.applyDifferentAirForce = data.applyDifferentAirForce;
		hit.applyDifferentBlockForce = data.applyDifferentBlockForce;
        hit._pushForce = data._pushForce;
        hit._pushForceAir = data._pushForceAir;
		hit._pushForceBlock = data._pushForceBlock;
		hit.pullEnemyIn = new PullIn();
		hit.pullEnemyIn.enemyBodyPart = BodyPart.none;

        if (data.mirrorOn2PSide && mirror > 0) {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y + 180, transform.localEulerAngles.z);
        }
	}

	public void UpdateRenderer(){
		if (hurtBox.followXBounds || hurtBox.followYBounds){
			Renderer[] rendererList = GetComponentsInChildren<Renderer>();
			foreach(Renderer childRenderer in rendererList){
				projectileRenderer = childRenderer;
			}
			if (projectileRenderer == null) 
				Debug.LogWarning("Warning: You are trying to access the projectile's bounds, but it does not have a renderer.");

		}
	}
	
	public bool IsDestroyed () {
		if (this == null) return true; 
		if (destroyMe){
            UFE.DestroyGameObject(gameObject);
		}
		return destroyMe;
	}

	public override void UFEFixedUpdate () {
		if (!this.gameObject.activeInHierarchy || destroyMe){
			return;
		}

		if (isHit > 0) {
            isHit -= UFE.fixedDeltaTime;
			return;
		}

		// Check if both controllers are ready
		if (UFE.freezePhysics) return;


        // Update Fixed Point Transform
        fpTransform.position += (movement * UFE.fixedDeltaTime);


        // Test Outbounds
        if (fpTransform.position.x > UFE.config.selectedStage._rightBoundary + 5
            || fpTransform.position.x < UFE.config.selectedStage._leftBoundary - 5)
        {
            destroyMe = true;
            return;
        }


        // Get Auto Bounds
		hurtBox.position = fpTransform.position;
		if (projectileRenderer != null && (hurtBox.followXBounds || hurtBox.followYBounds)) {
			hurtBox.rendererBounds = GetBounds();
			hitBox.rendererBounds = GetBounds();
		}


        // Check Block Area Contact
		blockableArea.position = fpTransform.position;
		if (!opControlsScript.isBlocking
		    && !opControlsScript.blockStunned
		    && opControlsScript.currentSubState != SubStates.Stunned
		    && opHitBoxesScript.TestCollision(blockableArea).Length > 0) {
			opControlsScript.CheckBlocking(true);
        }


        // Test Collision with Opponent's Projectiles
        if (data.projectileCollision){
			if (opControlsScript.projectiles.Count > 0){
				foreach(ProjectileMoveScript projectile in opControlsScript.projectiles){
					if (projectile == null) continue;
					if (projectile.hitBox == null) continue;
					if (projectile.hurtBox == null) continue;
                    
                    if (HitBoxesScript.TestCollision(projectile.fpTransform.position, new HitBox[]{projectile.hitBox}, new HurtBox[]{hurtBox}, HitConfirmType.Hit, mirror).Length > 0){
                        ProjectileHit();
                        projectile.ProjectileHit();
						break;
					}
				}
			}
		}


        // Test Collision with Opponent
        FPVector[] collisionVectors = (opHitBoxesScript.TestCollision(new HurtBox[]{hurtBox}, HitConfirmType.Hit));
		if (collisionVectors.Length > 0 && opControlsScript.ValidateHit(hit)) {
            ProjectileHit();

            //if (data.impactPrefab != null){
            //   GameObject hitEffect = UFE.SpawnGameObject(data.impactPrefab, fpTransform.position.ToVector(), Quaternion.Euler(0, 0, data.directionAngle), Mathf.RoundToInt(data.impactDuration * UFE.config.fps));
            //}
            //totalHits --;
            //if (totalHits <= 0){
            //	this.destroyMe = true;
            //}
            //isHit = opControlsScript.GetHitFreezingTime(data.hitStrength) * 1.2f;

            if (opControlsScript.currentSubState != SubStates.Stunned && opControlsScript.isBlocking && opControlsScript.TestBlockStances(hit.hitType)){
				myControlsScript.AddGauge(data.gaugeGainOnBlock);
				opControlsScript.AddGauge(data.opGaugeGainOnBlock);
				opControlsScript.GetHitBlocking(hit, 20, collisionVectors);

                if (data.moveLinkOnBlock != null)
                    myControlsScript.CastMove(data.moveLinkOnBlock, true, data.forceGrounded);

			}else if (opControlsScript.potentialParry > 0 && opControlsScript.TestParryStances(hit.hitType)){
				opControlsScript.AddGauge(data.opGaugeGainOnParry);
				opControlsScript.GetHitParry(hit, 20, collisionVectors);

                if (data.moveLinkOnParry != null)
                    myControlsScript.CastMove(data.moveLinkOnParry, true, data.forceGrounded);

			}else{
				myControlsScript.AddGauge(data.gaugeGainOnHit);
				opControlsScript.AddGauge(data.opGaugeGainOnHit);

				/*if (data.obeyDirectionalHit){
					hit._pushForce.x *= directionVector.x;
                }*/

                if (data.hitEffectsOnHit) {
                    opControlsScript.GetHit(hit, 30, collisionVectors, data.obeyDirectionalHit);
                } else {
                    opControlsScript.GetHit(hit, 30, new FPVector[0], data.obeyDirectionalHit);
                }

                if (data.moveLinkOnStrike != null)
                    myControlsScript.CastMove(data.moveLinkOnStrike, true, data.forceGrounded);

			}

			opControlsScript.CheckBlocking(false);
		}
        

        // Update Unity Transform
        transform.position = fpTransform.position.ToVector();
    }

    public void ProjectileHit() {
        if (data.impactPrefab != null) {
            UFE.SpawnGameObject(data.impactPrefab, fpTransform.position.ToVector(), Quaternion.Euler(0, 0, data.directionAngle), Mathf.RoundToInt(data.impactDuration * UFE.config.fps));
        }
        totalHits--;
        if (totalHits <= 0) destroyMe = true;

        isHit = spaceBetweenHits;
        fpTransform.Translate(movement * -1 * UFE.fixedDeltaTime);
    }

	public Rect GetBounds(){
		if (projectileRenderer != null){
			return new Rect(projectileRenderer.bounds.min.x, 
			                projectileRenderer.bounds.min.y, 
			                projectileRenderer.bounds.max.x,
			                projectileRenderer.bounds.max.y);
		}else{
			// alternative bounds
		}
		
		return new Rect();
	}

	private void GizmosDrawRectangle(Vector3 topLeft, Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight){
		Gizmos.DrawLine(topLeft, bottomLeft);
		Gizmos.DrawLine(bottomLeft, bottomRight);
		Gizmos.DrawLine(bottomRight, topRight);
		Gizmos.DrawLine(topRight, topLeft);
	}

	void OnDrawGizmos() {
		// COLLISION BOX SIZE
		// HURTBOXES
		if (hurtBox != null) {
			Gizmos.color = Color.cyan;

			Vector3 hurtBoxPosition = transform.position;
			if (UFE.config == null || !UFE.config.detect3D_Hits) hurtBoxPosition.z = -1;

			if (hurtBox.shape == HitBoxShape.circle){
				hurtBoxPosition += new Vector3((float)hurtBox._offSet.x * -mirror, (float)hurtBox._offSet.y, 0);
				Gizmos.DrawWireSphere(hurtBoxPosition, (float)hurtBox._radius);
			}else{
				Vector3 topLeft = new Vector3(hurtBox.rect.x * -mirror, hurtBox.rect.y) + hurtBoxPosition;
				Vector3 topRight = new Vector3((hurtBox.rect.x + hurtBox.rect.width) * -mirror, hurtBox.rect.y) + hurtBoxPosition;
				Vector3 bottomLeft = new Vector3(hurtBox.rect.x * -mirror, hurtBox.rect.y + hurtBox.rect.height) + hurtBoxPosition;
				Vector3 bottomRight = new Vector3((hurtBox.rect.x + hurtBox.rect.width) * -mirror, hurtBox.rect.y + hurtBox.rect.height) + hurtBoxPosition;

				if (hurtBox.followXBounds){
					hurtBox.rect.x = 0;
					topLeft.x = GetBounds().x - (hurtBox.rect.width/2);
					topRight.x = GetBounds().width + (hurtBox.rect.width/2);
					bottomLeft.x = GetBounds().x - (hurtBox.rect.width/2);
					bottomRight.x = GetBounds().width + (hurtBox.rect.width/2);
				}
				
				if (hurtBox.followYBounds){
					hurtBox.rect.y = 0;
					topLeft.y = GetBounds().height + (hurtBox.rect.height/2);
					topRight.y = GetBounds().height + (hurtBox.rect.height/2);
					bottomLeft.y = GetBounds().y - (hurtBox.rect.height/2);
					bottomRight.y = GetBounds().y - (hurtBox.rect.height/2);
				}
				GizmosDrawRectangle(topLeft, bottomLeft, bottomRight, topRight);
			}
		}

		// BLOCKBOXES
		if (blockableArea != null){
			Gizmos.color = Color.blue;
			
			if (!data.unblockable){
				Vector3 blockableAreaPosition;
				blockableAreaPosition = transform.position;
				if (UFE.config == null || !UFE.config.detect3D_Hits) blockableAreaPosition.z = -1;
				if (blockableArea.shape == HitBoxShape.circle){
					blockableAreaPosition += new Vector3((float)blockableArea._offSet.x * -mirror, (float)blockableArea._offSet.y, 0);
					Gizmos.DrawWireSphere(blockableAreaPosition, (float)blockableArea._radius);
				}else{
					Vector3 topLeft = new Vector3(blockableArea.rect.x * -mirror, blockableArea.rect.y) + blockableAreaPosition;
					Vector3 topRight = new Vector3((blockableArea.rect.x + blockableArea.rect.width) * -mirror, blockableArea.rect.y) + blockableAreaPosition;
					Vector3 bottomLeft = new Vector3(blockableArea.rect.x * -mirror, blockableArea.rect.y + blockableArea.rect.height) + blockableAreaPosition;
					Vector3 bottomRight = new Vector3((blockableArea.rect.x + blockableArea.rect.width) * -mirror, blockableArea.rect.y + blockableArea.rect.height) + blockableAreaPosition;
					GizmosDrawRectangle(topLeft, bottomLeft, bottomRight, topRight);
				}
			}
		}
    }
}
