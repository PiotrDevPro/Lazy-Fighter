using UnityEngine;
using UFENetcode;

public class InstantiatedGameObject {
	public GameObject gameObject;
	public MrFusion mrFusion;
	public long creationFrame;
	public long? destructionFrame;

    public bool destroyMe;

	public InstantiatedGameObject(
		GameObject gameObject = null,
        MrFusion mrFusion = null,
        long creationFrame = 0, 
		long? destructionFrame = null
	){
        this.gameObject = gameObject;
        this.mrFusion = mrFusion;
        this.creationFrame = creationFrame;
		this.destructionFrame = destructionFrame != null ? new long?(destructionFrame.Value) : null;
	}

	public InstantiatedGameObject(InstantiatedGameObject other) : this(
		other.gameObject,
		other.mrFusion,
        other.creationFrame,
		other.destructionFrame
	){}
    
    public bool IsDestroyed()
    {
        if (this == null) return true;
        return destroyMe;
    }
}