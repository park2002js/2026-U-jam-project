using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyAnimationViewer : MonoBehaviour
{
    [SerializeField] private GameObject model;
    [SerializeField, HideInInspector] private AnimationClip[] clips;

    private PlayableGraph graph;
    private Vector2 scrollPosition;

    private void OnGUI()
    {
        if (model == null || clips == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 180, Screen.height - 20));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        foreach (AnimationClip clip in clips)
        {
            if (clip != null && GUILayout.Button(clip.name, GUILayout.Height(32)))
                Play(clip);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void Play(AnimationClip clip)
    {
        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();

        if (graph.IsValid()) graph.Destroy();
        graph = PlayableGraph.Create("Enemy Animation Viewer");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        output.SetSourcePlayable(AnimationClipPlayable.Create(graph, clip));
        graph.Play();
    }

    private void OnDisable()
    {
        if (graph.IsValid()) graph.Destroy();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EnemyAnimationViewer))]
    private class ViewerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty modelProperty = serializedObject.FindProperty("model");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(modelProperty);
            if (EditorGUI.EndChangeCheck())
            {
                GameObject selected = modelProperty.objectReferenceValue as GameObject;
                Object source = selected != null ? PrefabUtility.GetCorrespondingObjectFromSource(selected) : null;
                string path = AssetDatabase.GetAssetPath(source);
                AnimationClip[] found = string.IsNullOrEmpty(path) ? new AnimationClip[0] :
                    AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                        .Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
                SerializedProperty clipsProperty = serializedObject.FindProperty("clips");
                clipsProperty.arraySize = found.Length;
                for (int i = 0; i < found.Length; i++)
                    clipsProperty.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
                if (selected != null && found.Length == 0)
                    Debug.LogWarning("선택한 오브젝트의 FBX에서 애니메이션을 찾을 수 없습니다.", selected);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
