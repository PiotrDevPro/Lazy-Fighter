using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using FPLibrary;
using UFE3D;

[System.Serializable]
public class HitBox: ICloneable {
	public bool defaultVisibility = true;
	public BodyPart bodyPart;
	public HitBoxType type;
	public HitBoxShape shape;
	public Rect rect = new Rect(0, 0, 4, 4);
	public FPRect _rect = new FPRect();
	public bool followXBounds;
	public bool followYBounds;
	public float radius = .5f;
	public Fix64 _radius = .5;
	public Vector2 offSet;
	public FPVector _offSet;

	public CollisionType collisionType;
	public Transform position;
    public FPVector mappedPosition;

    #region trackable definitions
    public int state{get;set;}
    public Rect rendererBounds{get;set;}
    public bool hide{get;set;}          // Whether the hit box collisions will be detected
    public bool visibility{get;set;}    // Whether the GameObject will be active in the hierarchy
    #endregion

    public object Clone() {
		return CloneObject.Clone(this);
	}
}

[System.Serializable]
public class HurtBox: ICloneable {
	public BodyPart bodyPart;
	public HitBoxShape shape;
	public Rect rect = new Rect(0, 0, 4, 4);
	public FPRect _rect = new FPRect();
	public bool followXBounds;
	public bool followYBounds;
	public float radius = .5f;
	public Fix64 _radius = .5;
    public Vector2 offSet;
    public FPVector _offSet;

    #region trackable definitions
    public bool isBlock{get; set;}
    public FPVector position{get;set;}
    public Rect rendererBounds{get;set;}
    #endregion

    public object Clone() {
		return CloneObject.Clone(this);
	}
}

[System.Serializable]
public class BlockArea {
	public int activeFramesBegin;
	public int activeFramesEnds;

	public BodyPart bodyPart;
	public HitBoxShape shape;
	public Rect rect = new Rect(0, 0, 4, 4);
	public FPRect _rect = new FPRect();
	public bool followXBounds;
	public bool followYBounds;
	public float radius = .5f;
	public Fix64 _radius = .5;
	public Vector2 offSet;
	public FPVector _offSet;

	[HideInInspector] public FPVector position;
}

[System.Serializable]
public class PullIn: ICloneable {
	public int speed = 50;
	public bool forceStand = true;
	public BodyPart characterBodyPart;
	public BodyPart enemyBodyPart;
	public float targetDistance = .5f;
	public Fix64 _targetDistance = .5;

    public FPVector position;
	
	public object Clone() {
		return CloneObject.Clone(this);
	}
}

public class HitBoxesScript : MonoBehaviour {
    
    #region trackable definitions
    public bool isHit;
    public HitBox[] hitBoxes;
    public HurtBox[] activeHurtBoxes;
    public BlockArea blockableArea;
    public HitConfirmType hitConfirmType;
    public Fix64 collisionBoxSize;
    public bool currentMirror;
    public bool bakeSpeed;
    public AnimationMap[] animationMaps = new AnimationMap[0];
    #endregion

	[HideInInspector] public ControlsScript controlsScript;
	[HideInInspector] public bool previewInvertRotation;
    [HideInInspector] public bool previewMirror;
	public bool rectangleHitBoxLocationTest;
	public Texture2D rectTexture;

	public MoveSetScript moveSetScript;
    private Renderer characterRenderer;
    private FPVector deltaPosition;

    private FPTransform worldTransform { get { return controlsScript.worldTransform; } set { controlsScript.worldTransform = value; } }
    private FPTransform opWorldTransform { get { return controlsScript.opControlsScript.worldTransform; } set { controlsScript.opControlsScript.worldTransform = value; } }

    //[HideInInspector] public Rect characterBounds = new Rect(0,0,0,0);

    void Start(){
		if (transform.parent != null){
			controlsScript = transform.parent.gameObject.GetComponent<ControlsScript>();
		}
		moveSetScript = GetComponent<MoveSetScript>();
		UpdateRenderer();

		if (moveSetScript != null){
			foreach(MoveInfo move in moveSetScript.moves){
                if (move == null) {
                    Debug.LogWarning("You have empty entries in your move list. Check your special moves under Character Editor.");
                    continue;
                }
				foreach(InvincibleBodyParts invBodyPart in move.invincibleBodyParts){
					List<HitBox> invHitBoxes = new List<HitBox>();
					foreach(BodyPart bodyPart in invBodyPart.bodyParts){
						foreach(HitBox hitBox in hitBoxes){
							if (bodyPart == hitBox.bodyPart) {
								invHitBoxes.Add(hitBox);
								break;
							}
						}
					}
					invBodyPart.hitBoxes = invHitBoxes.ToArray();
				}
			}
		}

        rectangleHitBoxLocationTest = false;
		rectTexture = new Texture2D(1,1);
        rectTexture.SetPixel(0, 0, Color.red);
        rectTexture.Apply();
	}
	
	public static FPVector[] TestCollision(FPVector rootPosition, HitBox[] hitBoxes, HurtBox[] hurtBoxes, HitConfirmType hitConfirmType, int mirror) {
		foreach (HitBox hitBox in hitBoxes) {
			if (hitBox.hide) continue;
			if (hitBox.collisionType == CollisionType.noCollider) continue;
			if (hitConfirmType == HitConfirmType.Throw && hitBox.collisionType != CollisionType.throwCollider) continue;
			if (hitConfirmType == HitConfirmType.Hit && hitBox.collisionType == CollisionType.throwCollider) continue;

            hitBox.state = 0;
            //drawRect.Clear();
			foreach (HurtBox hurtBox in hurtBoxes) {
                FPVector hurtBoxPosition = hurtBox.position;
                FPVector hitBoxPosition = hitBox.mappedPosition + rootPosition;

				Fix64 dist = 0;
				bool collisionConfirm = false;
				
				if (!UFE.config.detect3D_Hits){
					hurtBoxPosition.z = 0;
					hitBoxPosition.z = 0;
				}
				
				if (hurtBox.shape == HitBoxShape.circle) {
					if (hitBox.shape == HitBoxShape.circle) {
						dist = FPVector.Distance(hurtBoxPosition, hitBoxPosition);
						if (dist <= hurtBox._radius + hitBox._radius) collisionConfirm = true;
						
					}else if (hitBox.shape == HitBoxShape.rectangle){
                        FPRect hitBoxRectanglePosition = hitBox._rect;
                        hitBoxRectanglePosition.x += hitBoxPosition.x;
                        hitBoxRectanglePosition.y += hitBoxPosition.y;
                        hitBoxRectanglePosition.RefreshPoints();

                        if (hitBox.followXBounds) {
                            //hitBoxRectanglePosition.x = hitBox.rendererBounds.x - (hitBox.rect.width / 2);
                            //hitBoxRectanglePosition.width = (hitBox.rendererBounds.width + hitBox.rect.width) - hitBox.rendererBounds.x;
                        }
                        if (hitBox.followYBounds) {
                            //hitBoxRectanglePosition.y = hitBox.rendererBounds.y - (hitBox.rect.height / 2);
                            //hitBoxRectanglePosition.height = (hitBox.rendererBounds.height + hitBox.rect.height) - hitBox.rendererBounds.y;
                        }
                        
                        dist = hitBoxRectanglePosition.DistanceToPoint(hurtBoxPosition);
                        if (hurtBox._radius >= dist) collisionConfirm = true;
                        
                        /*if (collisionConfirm && !hurtBox.isBlock) {
                            Debug.Log("------------------");
                            Debug.Log(hurtBoxPosition);
                            Debug.Log(hitBox.bodyPart + " - " + hitBoxRectanglePosition);
                            Debug.Log("xMin/xMax,yMin/yMax : " + hitBoxRectanglePosition.xMin + "/" + hitBoxRectanglePosition.xMax + ", " + hitBoxRectanglePosition.yMin + "/" + hitBoxRectanglePosition.yMax);
                            Debug.Log(hurtBox.radius + " >= " + dist + " = " + collisionConfirm);
                        }*/
					}
				}else if (hurtBox.shape == HitBoxShape.rectangle) {
                    FPRect hurtBoxRectanglePosition = hurtBox._rect;
                    if (mirror < 0) hurtBoxRectanglePosition.x += hurtBoxRectanglePosition.width;
                    hurtBoxRectanglePosition.x *= mirror;
                    hurtBoxRectanglePosition.x += hurtBoxPosition.x;
                    hurtBoxRectanglePosition.y += hurtBoxPosition.y;
                    hurtBoxRectanglePosition.RefreshPoints();
                    
                    if (hitBox.shape == HitBoxShape.circle){

						if (hurtBox.followXBounds){
							//hurtBoxRectanglePosition.x = hurtBox.rendererBounds.x - (hurtBox.rect.width/2);
							//hurtBoxRectanglePosition.width = (hurtBox.rendererBounds.width + hurtBox.rect.width) - hurtBox.rendererBounds.x;
						}
						if (hurtBox.followYBounds){
							//hurtBoxRectanglePosition.y = hurtBox.rendererBounds.y - (hurtBox.rect.height/2);
							//hurtBoxRectanglePosition.height = (hurtBox.rendererBounds.height + hurtBox.rect.height) - hurtBox.rendererBounds.y;
						}

                        dist = hurtBoxRectanglePosition.DistanceToPoint(hitBoxPosition);
						if (dist <= hitBox._radius) collisionConfirm = true;
						
					}else if (hitBox.shape == HitBoxShape.rectangle){
                        FPRect hitBoxRectanglePosition = hitBox._rect;
                        //if (mirror > 0) hitBoxRectanglePosition.x += hitBoxRectanglePosition.width;
                        //hitBoxRectanglePosition.x *= -mirror;
                        hitBoxRectanglePosition.x += hitBoxPosition.x;
                        hitBoxRectanglePosition.y += hitBoxPosition.y;
                        hitBoxRectanglePosition.RefreshPoints();


						if (hitBox.followXBounds){
							//hitBoxRectanglePosition.x = hitBox.rendererBounds.x - (hitBox.rect.width/2);
							//hitBoxRectanglePosition.width = (hitBox.rendererBounds.width + hitBox.rect.width) - hitBox.rendererBounds.x;
						}
						if (hitBox.followYBounds){
							//hitBoxRectanglePosition.y = hitBox.rendererBounds.y - (hitBox.rect.height/2);
							//hitBoxRectanglePosition.height = (hitBox.rendererBounds.height + hitBox.rect.height) - hitBox.rendererBounds.y;
						}

						if (hurtBox.followXBounds){
							//hurtBoxRectanglePosition.x = hurtBox.rendererBounds.x - (hurtBox.rect.width/2);
							//hurtBoxRectanglePosition.width = (hurtBox.rendererBounds.width + hurtBox.rect.width) - hurtBox.rendererBounds.x;
						}
						if (hurtBox.followYBounds){
							//hurtBoxRectanglePosition.y = hurtBox.rendererBounds.y - (hurtBox.rect.height/2);
							//hurtBoxRectanglePosition.height = (hurtBox.rendererBounds.height + hurtBox.rect.height) - hurtBox.rendererBounds.y;
						}
						
						if (hurtBoxRectanglePosition.Intersects(hitBoxRectanglePosition)) collisionConfirm = true;
					}
				}

				if (collisionConfirm) {
					if (hitConfirmType == HitConfirmType.Hit) {
						hitBox.state = 1;
					}
					return new FPVector[]{hurtBoxPosition, hitBoxPosition, (hurtBoxPosition + hitBoxPosition)/2};
				}
			}
		}

		foreach (HitBox hitBox in hitBoxes) {
			if (hitBox.state == 1) hitBox.state = 0;
		}
		return new FPVector[0];
	}

	public FPVector[] TestCollision(HurtBox[] hurtBoxes, HitConfirmType hitConfirmType) {
        if (isHit && hitConfirmType == HitConfirmType.Hit) return new FPVector[0];
		foreach(HitBox hitbox in this.hitBoxes) if (hitbox.followXBounds || hitbox.followYBounds) hitbox.rendererBounds = GetBounds();
		
		return HitBoxesScript.TestCollision(worldTransform.position, this.hitBoxes, hurtBoxes, hitConfirmType, controlsScript.mirror);
	}

	public FPVector[] TestCollision(BlockArea blockableArea) {
		HurtBox hurtBox = new HurtBox();
		hurtBox.position = blockableArea.position;
		hurtBox.shape = blockableArea.shape;
		hurtBox._rect = blockableArea._rect;
		hurtBox.followXBounds = blockableArea.followXBounds;
		hurtBox.followYBounds = blockableArea.followYBounds;
		hurtBox._radius = blockableArea._radius;
		hurtBox._offSet = blockableArea._offSet;
        hurtBox.isBlock = true;

		// We use throw confirmation type so the engine doesn't register the state of the stroke hitbox as hit
		return HitBoxesScript.TestCollision(worldTransform.position, this.hitBoxes, new HurtBox[]{hurtBox}, HitConfirmType.Hit, controlsScript.mirror);
	}
	
	public Fix64 TestCollision(FPVector myRootPosition, FPVector opRootPosition, HitBox[] opHitBoxes) {
		Fix64 totalPushForce = 0;
		foreach (HitBox hitBox in hitBoxes) {
			if (hitBox.collisionType != CollisionType.bodyCollider) continue;
			foreach (HitBox opHitBox in opHitBoxes) {
				if (opHitBox.collisionType != CollisionType.bodyCollider) continue;
				FPVector opHitBoxPosition = opHitBox.mappedPosition + opRootPosition;
                FPVector hitBoxPosition = hitBox.mappedPosition + myRootPosition;

				if (!UFE.config.detect3D_Hits){
					opHitBoxPosition.z = 0;
					hitBoxPosition.z = 0;
				}
				Fix64 dist = FPVector.Distance(opHitBoxPosition, hitBoxPosition);
				if (dist <= opHitBox._radius + hitBox._radius) totalPushForce += (opHitBox._radius + hitBox._radius) - dist;
			}
		}
		return totalPushForce;
	}

	public bool GetDefaultVisibility(BodyPart bodyPart){
		foreach(HitBox hitBox in hitBoxes){
			if (bodyPart == hitBox.bodyPart && hitBox.defaultVisibility) return true;
		}

		return false;
	}


	public FPVector GetPosition(BodyPart bodyPart){
		foreach(HitBox hitBox in hitBoxes){
            if (bodyPart == hitBox.bodyPart) {
                FPVector newMap = new FPVector();
                if (controlsScript == null) { 
                    // If its running from the editor, load positions from transforms
                    newMap = FPVector.ToFPVector(hitBox.position.position);
                } else {
                    newMap = hitBox.mappedPosition + worldTransform.position;
                }
                return newMap;
            }
		}
		return FPVector.zero;
    }

    public FPVector GetDeltaPosition() {
        if (controlsScript.myInfo.useAnimationMaps)
        {
        return deltaPosition * -controlsScript.mirror;
    }
        else
        {
            return FPVector.ToFPVector(moveSetScript.GetDeltaPosition());
        }
    }

    public HitBoxMap[] GetAnimationMaps() {
        List<HitBoxMap> animMaps = new List<HitBoxMap>();
        foreach (HitBox hitBox in hitBoxes) {
            HitBoxMap animMap = new HitBoxMap();
            animMap.bodyPart = hitBox.bodyPart;
            animMap.mappedPosition = FPVector.ToFPVector(hitBox.position.position);
            animMaps.Add(animMap);
        }

        return animMaps.ToArray();
    }

    public Transform GetTransform(BodyPart bodyPart){
		foreach(HitBox hitBox in hitBoxes){
			if (bodyPart == hitBox.bodyPart) return hitBox.position;
		}
		return null;
	}

	public void SetTransform(BodyPart bodyPart, Transform transform){
		foreach(HitBox hitBox in hitBoxes){
			if (bodyPart == hitBox.bodyPart) {
				hitBox.position = transform;
				return;
			}
		}
	}

	public HitBox[] GetHitBoxes(BodyPart[] bodyParts){
		List<HitBox> hitBoxesList = new List<HitBox>();
		foreach(HitBox hitBox in hitBoxes){
			foreach(BodyPart bodyPart in bodyParts){
				if (bodyPart == hitBox.bodyPart) {
					hitBoxesList.Add(hitBox);
					break;
				}
			}
		}

		return hitBoxesList.ToArray();
	}
	
	public void ResetHit(){
		//if (!isHit) return;
		foreach (HitBox hitBox in hitBoxes) {
			if (hitBox.state == 1) hitBox.state = 0;
		}
		isHit = false;
	}

	public HitBox GetStrokeHitBox(){
		if (!isHit) return null;
		foreach (HitBox hitBox in hitBoxes) {
			if (hitBox.state == 1) return hitBox;
		}
		return null;
	}
	
	public void HideHitBoxes(HitBox[] invincibleHitBoxes, bool hide){
		foreach (HitBox invHitBox in invincibleHitBoxes)
        {
            foreach (HitBox hitBox in hitBoxes)
            {
                if (invHitBox.bodyPart == hitBox.bodyPart)
                {
                    hitBox.hide = hide;
                    break;
                }
            }
		}
	}
	
	public void HideHitBoxes(bool hide){
		foreach (HitBox hitBox in hitBoxes) {
			hitBox.hide = hide;
		}
	}

	public void InvertHitBoxes(bool mirror){
		if (currentMirror == mirror) return;
		currentMirror = mirror;

		foreach (HitBox hitBox in hitBoxes) {
			foreach (HitBox hitBox2 in hitBoxes) {
				if ((hitBox.bodyPart == BodyPart.leftCalf && hitBox2.bodyPart == BodyPart.rightCalf) ||
				    (hitBox.bodyPart == BodyPart.leftFoot && hitBox2.bodyPart == BodyPart.rightFoot) ||
				    (hitBox.bodyPart == BodyPart.leftForearm && hitBox2.bodyPart == BodyPart.rightForearm) ||
				    (hitBox.bodyPart == BodyPart.leftHand && hitBox2.bodyPart == BodyPart.rightHand) ||
				    (hitBox.bodyPart == BodyPart.leftThigh && hitBox2.bodyPart == BodyPart.rightThigh) ||
				    (hitBox.bodyPart == BodyPart.leftUpperArm && hitBox2.bodyPart == BodyPart.rightUpperArm)) 
					invertTransform(hitBox, hitBox2);
			}
		}
	}
	
	private void invertTransform(HitBox hb1, HitBox hb2){
		Transform hb2Transform = hb2.position;
		hb2.position = hb1.position;
		hb1.position = hb2Transform;
	}
	
	public Transform FindTransform(string searchString){
		Transform[] transformChildren = GetComponentsInChildren<Transform>();
		Transform found;
		foreach(Transform transformChild in transformChildren){
			found = transformChild.Find("mixamorig:"+ searchString);
			if (found == null) found = transformChild.Find(gameObject.name + ":" + searchString);
			if (found == null) found = transformChild.Find(searchString);
			if (found != null) return found;
		}
		return null;
	}

	
	public Rect GetBounds(){
		if (characterRenderer != null){
			return new Rect(characterRenderer.bounds.min.x, 
		    	            characterRenderer.bounds.min.y, 
		        	        characterRenderer.bounds.max.x,
		            	    characterRenderer.bounds.max.y);
		}else{
			// alternative bounds
		}

		return new Rect();
	}
	
	public void UpdateBounds(HurtBox[] hurtBoxes){
		foreach(HurtBox hurtBox in hurtBoxes) if (hurtBox.followXBounds || hurtBox.followYBounds) hurtBox.rendererBounds = GetBounds();
	}

    public void UpdateMap(int frame)
    {
        if (controlsScript == null) return;
        if (animationMaps == null && controlsScript.myInfo.useAnimationMaps){
            Debug.LogError("Animation '" + moveSetScript.GetCurrentClipName() + "' has no animation maps");
            return;
        }
        if (controlsScript.myInfo.useAnimationMaps)
        {
        HitBoxMap[] hitBoxMaps = new HitBoxMap[0];
        int highestFrame = 0;
        foreach (AnimationMap map in animationMaps) {
            if (map.frame > highestFrame) highestFrame = map.frame;
            if (map.frame == frame) {
                hitBoxMaps = map.hitBoxMaps;
                deltaPosition = map.deltaDisplacement;
                break;
            }
        }

        // If frame can't be found, cast the highest possible frame
        if (hitBoxMaps.Length == 0) {
            hitBoxMaps = animationMaps[highestFrame].hitBoxMaps;
            deltaPosition = animationMaps[highestFrame].deltaDisplacement;
        }
        

        foreach(HitBoxMap map in hitBoxMaps) {
            foreach (HitBox hitBox in hitBoxes) {
                if (hitBox.bodyPart == map.bodyPart) {
                    hitBox.mappedPosition = map.mappedPosition;
                        if (currentMirror) hitBox.mappedPosition.x += (hitBox.mappedPosition.x * -2);
                    }
                }
            }
        }
        else
        {
            foreach (HitBox hitBox in hitBoxes) {
                hitBox.mappedPosition = FPVector.ToFPVector(hitBox.position.position) - worldTransform.position;
            }
        }
    }

	public void UpdateRenderer(){
		bool confirmUpdate = false;
		foreach(HitBox hitBox in hitBoxes){
			if (hitBox.followXBounds || hitBox.followYBounds) confirmUpdate = true;
		}

		if (moveSetScript != null){
            foreach (MoveInfo move in moveSetScript.moves) {
                if (move == null) {
                    Debug.LogWarning("You have empty entries in your move list. Check your special moves under Character Editor.");
                    continue;
                }
				foreach(Hit hit in move.hits){
					foreach(HurtBox hurtbox in hit.hurtBoxes){
						if (hurtbox.followXBounds || hurtbox.followYBounds) confirmUpdate = true;
					}
				}

				if (move.blockableArea != null && (move.blockableArea.followXBounds || move.blockableArea.followYBounds))
					confirmUpdate = true;
			}
		}

		if (confirmUpdate){
			Renderer[] rendererList = GetComponentsInChildren<Renderer>();
			foreach(Renderer childRenderer in rendererList){
				characterRenderer = childRenderer;
				return;
			}
			Debug.LogWarning("Warning: You are trying to access the character's bounds, but it does not have a renderer.");
		}
	}

	private void GizmosDrawRectangle(Vector3 topLeft, Vector3 bottomLeft, Vector3 bottomRight, Vector3 topRight){
		Gizmos.DrawLine(topLeft, bottomLeft);
		Gizmos.DrawLine(bottomLeft, bottomRight);
		Gizmos.DrawLine(bottomRight, topRight);
		Gizmos.DrawLine(topRight, topLeft);
	}

	void OnDrawGizmos() {
		// HITBOXES
		if (hitBoxes == null) return;
		int mirrorAdjust = controlsScript != null? controlsScript.mirror : -1;
        Vector3 rootPosition = controlsScript != null ? worldTransform.position.ToVector() : transform.position;


        foreach (HitBox hitBox in hitBoxes) {
			if (hitBox.position == null) continue;
			if (hitBox.hide) continue;
			if (hitBox.state == 1) {
				Gizmos.color = Color.red;
			} else if (isHit){
				Gizmos.color = Color.magenta;
			} else if (hitBox.collisionType == CollisionType.bodyCollider) {	
				Gizmos.color = Color.yellow;
			} else if (hitBox.collisionType == CollisionType.noCollider) {	
				Gizmos.color = Color.white;
			} else if (hitBox.collisionType == CollisionType.throwCollider) {	
				Gizmos.color = new Color(1f, 0, .5f);
			}else{
				Gizmos.color = Color.green;
			}
            
            Vector3 currentPosition = hitBox.mappedPosition.ToVector() + rootPosition;
            if (controlsScript == null) currentPosition = hitBox.position.position;

            if (hitBox.shape == HitBoxShape.rectangle && rectangleHitBoxLocationTest) {
                Rect hitBoxRectPos = new Rect(hitBox.rect);
                hitBoxRectPos.x *= -mirrorAdjust;
                hitBoxRectPos.width *= -mirrorAdjust;

                //Vector3 currentPosition = hitBox.position.position;
                //if (myMoveSetScript.GetCurrentClipName) currentAnimationMap.ContainKey(myMoveSetScript.GetCurrentClipFrame()) currentPosition = currentAnimationMap[myMoveSetScript.GetCurrentClipFrame()];

                hitBoxRectPos.x += currentPosition.x;
                hitBoxRectPos.y += currentPosition.y;
                Gizmos.DrawGUITexture(hitBoxRectPos, rectTexture);
            }


			Vector3 hitBoxPosition = currentPosition + new Vector3((float)hitBox._offSet.x, (float)hitBox._offSet.y, 0);
			if (UFE.config == null || !UFE.config.detect3D_Hits) hitBoxPosition.z = -1;
			if (hitBox.shape == HitBoxShape.circle && hitBox._radius > 0){
				Gizmos.DrawWireSphere(hitBoxPosition, (float)hitBox._radius);
			}else if (hitBox.shape == HitBoxShape.rectangle){

				/*hitBoxPosition = hitBox.position.position;
				Vector3 topLeft = new Vector3(hitBox.rect.x * -mirrorAdjust, hitBox.rect.y) + hitBoxPosition;
				Vector3 topRight = new Vector3((hitBox.rect.x + hitBox.rect.width) * -mirrorAdjust, hitBox.rect.y) + hitBoxPosition;
				Vector3 bottomLeft = new Vector3(hitBox.rect.x * -mirrorAdjust, hitBox.rect.y + hitBox.rect.height) + hitBoxPosition;
				Vector3 bottomRight = new Vector3((hitBox.rect.x + hitBox.rect.width) * -mirrorAdjust, hitBox.rect.y + hitBox.rect.height) + hitBoxPosition;

                Gizmos.color = Color.red;
                GizmosDrawRectangle(topLeft, bottomLeft, bottomRight, topRight);*/

                Rect hitBoxRectPosTemp = new Rect(hitBox.rect);
                hitBoxRectPosTemp.x *= -mirrorAdjust;
                hitBoxRectPosTemp.width *= -mirrorAdjust;
                hitBoxRectPosTemp.x += currentPosition.x;
                hitBoxRectPosTemp.y += currentPosition.y;
                Vector3 topLeft = new Vector3(hitBoxRectPosTemp.x, hitBoxRectPosTemp.y);
                Vector3 topRight = new Vector3((hitBoxRectPosTemp.xMax), hitBoxRectPosTemp.y);
                Vector3 bottomLeft = new Vector3(hitBoxRectPosTemp.x, hitBoxRectPosTemp.yMax);
                Vector3 bottomRight = new Vector3((hitBoxRectPosTemp.xMax), hitBoxRectPosTemp.yMax);

				if (hitBox.followXBounds){
					hitBox.rect.x = 0;
					topLeft.x = GetBounds().x - (hitBox.rect.width/2);
					topRight.x = GetBounds().width + (hitBox.rect.width/2);
					bottomLeft.x = GetBounds().x - (hitBox.rect.width/2);
					bottomRight.x = GetBounds().width + (hitBox.rect.width/2);
				}
				
				if (hitBox.followYBounds){
					hitBox.rect.y = 0;
					topLeft.y = GetBounds().height + (hitBox.rect.height/2);
					topRight.y = GetBounds().height + (hitBox.rect.height/2);
					bottomLeft.y = GetBounds().y - (hitBox.rect.height/2);
					bottomRight.y = GetBounds().y - (hitBox.rect.height/2);
				}

				GizmosDrawRectangle(topLeft, bottomLeft, bottomRight, topRight);
			}
			
			if (hitBox.collisionType != CollisionType.noCollider){
				if (hitBox.type == HitBoxType.low){
					Gizmos.color = Color.red;
				}else{
					Gizmos.color = Color.yellow;
				}
				Gizmos.DrawWireSphere(hitBoxPosition, .1f);
			}
        }

		// COLLISION BOX SIZE
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, (float)collisionBoxSize);


        // HURTBOXES
		if (activeHurtBoxes != null) {
			if (hitConfirmType == HitConfirmType.Throw){
				Gizmos.color = new Color(1f, .5f, 0);
			}else{
				Gizmos.color = Color.cyan;
			}

			foreach (HurtBox hurtBox in activeHurtBoxes) {
				if (GetTransform(hurtBox.bodyPart) != null){
					Vector3 hurtBoxPosition;
					hurtBoxPosition = GetPosition(hurtBox.bodyPart).ToVector();
					if (UFE.config == null || !UFE.config.detect3D_Hits) hurtBoxPosition.z = -1;
					if (hurtBox.shape == HitBoxShape.circle){
						hurtBoxPosition += new Vector3((float)hurtBox._offSet.x * -mirrorAdjust, (float)hurtBox._offSet.y, 0);
						Gizmos.DrawWireSphere(hurtBoxPosition, (float)hurtBox._radius);
					}else{
						Vector3 topLeft = new Vector3(hurtBox.rect.x * -mirrorAdjust, hurtBox.rect.y) + hurtBoxPosition;
						Vector3 topRight = new Vector3((hurtBox.rect.x + hurtBox.rect.width) * -mirrorAdjust, hurtBox.rect.y) + hurtBoxPosition;
						Vector3 bottomLeft = new Vector3(hurtBox.rect.x * -mirrorAdjust, hurtBox.rect.y + hurtBox.rect.height) + hurtBoxPosition;
						Vector3 bottomRight = new Vector3((hurtBox.rect.x + hurtBox.rect.width) * -mirrorAdjust, hurtBox.rect.y + hurtBox.rect.height) + hurtBoxPosition;

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
			}
		}
		
		
		// BLOCKBOXES
		if (blockableArea != null){
			Gizmos.color = Color.blue;

			if (GetTransform(blockableArea.bodyPart) != null){
				Vector3 blockableAreaPosition;
				blockableAreaPosition = GetPosition(blockableArea.bodyPart).ToVector();
				if (UFE.config == null || !UFE.config.detect3D_Hits) blockableAreaPosition.z = -1;
				if (blockableArea.shape == HitBoxShape.circle){
					blockableAreaPosition += new Vector3((float)blockableArea._offSet.x * -mirrorAdjust, (float)blockableArea._offSet.y, 0);
					//blockableAreaPosition.x += (blockableArea.radius/2) * -mirrorAdjust;
					Gizmos.DrawWireSphere(blockableAreaPosition, (float)blockableArea._radius);
				}else{
					Vector3 topLeft = new Vector3(blockableArea.rect.x * -mirrorAdjust, blockableArea.rect.y) + blockableAreaPosition;
					Vector3 topRight = new Vector3((blockableArea.rect.x + blockableArea.rect.width) * -mirrorAdjust, blockableArea.rect.y) + blockableAreaPosition;
					Vector3 bottomLeft = new Vector3(blockableArea.rect.x * -mirrorAdjust, blockableArea.rect.y + blockableArea.rect.height) + blockableAreaPosition;
					Vector3 bottomRight = new Vector3((blockableArea.rect.x + blockableArea.rect.width) * -mirrorAdjust, blockableArea.rect.y + blockableArea.rect.height) + blockableAreaPosition;
					GizmosDrawRectangle(topLeft, bottomLeft, bottomRight, topRight);
				}
			}
		}
    }
}
