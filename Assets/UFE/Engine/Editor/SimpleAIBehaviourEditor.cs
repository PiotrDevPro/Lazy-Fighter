using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SimpleAIBehaviour))]
public class SimpleAIBehaviourEditor : Editor{
	public override void OnInspectorGUI(){
		if (GUILayout.Button("Open Simple AI Editor")){
			SimpleAIBehaviourEditorWindow.Init();
		}
	}
}