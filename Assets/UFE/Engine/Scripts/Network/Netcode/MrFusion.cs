using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFENetcode;

public class MrFusion : MonoBehaviour {
    public bool debugger = false;

    private struct TrackableInterface
    {
        public UFEInterface ufeInterface;
        public Dictionary<System.Reflection.MemberInfo, System.Object> tracker;
    }

    private Dictionary<long, TrackableInterface[]> gameHistory = new Dictionary<long, TrackableInterface[]>();
    private UFEInterface[] ufeInterfaces;
    private UFEBehaviour[] ufeBehaviours;


    void Start () {
        ufeInterfaces = GetComponentsInChildren<UFEInterface>();
        ufeBehaviours = GetComponentsInChildren<UFEBehaviour>();
    }

    public void UpdateBehaviours(){
        if (ufeBehaviours == null) return;
        foreach (UFEBehaviour ufeBehaviour in ufeBehaviours) {
            ufeBehaviour.UFEFixedUpdate();
        }
    }

    public void SaveState(long frame)
    {
        List<TrackableInterface> newTrackableList = new List<TrackableInterface>();
        foreach(UFEInterface ufeInterface in ufeInterfaces)
        {
            TrackableInterface newTrackableInterface;
            newTrackableInterface.ufeInterface = ufeInterface;
            newTrackableInterface.tracker = RecordVar.SaveStateTrackers(ufeInterface, new Dictionary<System.Reflection.MemberInfo, object>());
            newTrackableList.Add(newTrackableInterface);
        }

        if (gameHistory.ContainsKey(frame)) {
            gameHistory[frame] = newTrackableList.ToArray();
        } else {
            gameHistory.Add(frame, newTrackableList.ToArray());
        }
    }

    public void LoadState(long frame)
    {
        if (gameHistory.ContainsKey(frame)) {
            TrackableInterface[] loadedInterfaces = gameHistory[frame];
            foreach (TrackableInterface trackableInterface in loadedInterfaces)
            {
                UFEInterface reflectionTarget = trackableInterface.ufeInterface;
                reflectionTarget = RecordVar.LoadStateTrackers(trackableInterface.ufeInterface, trackableInterface.tracker);
                if (reflectionTarget == null && debugger) Debug.LogWarning("Empty interface found at '"+ trackableInterface.ToString() + "'");
            }
        } else {
            Debug.LogError("Frame data not found (" + frame + ")");
        }
    }

}
