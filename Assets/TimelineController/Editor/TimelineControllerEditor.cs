using UnityEngine;
using UnityEditor;


    [CustomEditor(typeof(TimelineController))]
    public class TimelineControllerEditor : Editor
    {
        TimelineController timelineController;
        private void OnEnable()
        {
            timelineController = serializedObject.targetObject as TimelineController;
        }


        public override void OnInspectorGUI()
        {
            if (timelineController.gameObject.scene != null && timelineController.gameObject.scene.isLoaded)
            {
                EditorGUI.BeginChangeCheck();
                timelineController.ActiveInScene = EditorGUILayout.Toggle("Activate", timelineController.ActiveInScene, GUILayout.MinWidth(50f));
                if (EditorGUI.EndChangeCheck())
                {
                    timelineController.gameObject.SetActive(timelineController.ActiveInScene);
                    if (timelineController.ActiveInScene)
                    {
                        if (TimelineController.LastActiveTimeline != null && TimelineController.LastActiveTimeline != timelineController)
                        {
                            TimelineController.LastActiveTimeline.ActiveInScene = false;
                            TimelineController.LastActiveTimeline.gameObject.SetActive(false);
                            TimelineController.LastActiveTimeline = null;
                        }
                        TimelineController.LastActiveTimeline = timelineController;
                        timelineController.InstallRuntimeBindings();
                    }
                }

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(timelineController.gameObject);
                if (prefab && PrefabUtility.HasPrefabInstanceAnyOverrides(timelineController.gameObject, false))
                {
                    if (GUILayout.Button("Save"))
                    {
                        PrefabUtility.ApplyPrefabInstance(timelineController.gameObject, InteractionMode.AutomatedAction);
                    }
                }
            }
            using (new EditorGUI.DisabledGroupScope(true))
                base.OnInspectorGUI();
        }
    }
