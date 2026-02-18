using UnityEditor;
using UnityEngine;

namespace Riomaster.Analytics.Editor
{
    public class RiomasterAnalyticsWindow : EditorWindow
    {
        [MenuItem("Riomaster/Analytics Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<RiomasterAnalyticsWindow>("Riomaster Analytics");
        }

        private void OnGUI()
        {
            GUILayout.Label("Riomaster Analytics", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live analytics data.", MessageType.Info);

                EditorGUILayout.Space(10);
                GUILayout.Label("Quick Setup", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                if (GUILayout.Button("Create Analytics Config Asset"))
                {
                    var config = ScriptableObject.CreateInstance<RiomasterAnalyticsConfig>();
                    AssetDatabase.CreateAsset(config, "Assets/RiomasterAnalyticsConfig.asset");
                    AssetDatabase.SaveAssets();
                    Selection.activeObject = config;
                    EditorUtility.FocusProjectWindow();
                }

                if (GUILayout.Button("Create Analytics GameObject in Scene"))
                {
                    var go = new GameObject("[RiomasterAnalytics]");
                    go.AddComponent<RiomasterAnalyticsAutoInit>();
                    Selection.activeGameObject = go;
                    Undo.RegisterCreatedObjectUndo(go, "Create Riomaster Analytics");
                }

                return;
            }

            // Play mode stats
            var instance = RiomasterAnalytics.Instance;
            if (instance == null || !instance.IsInitialized)
            {
                EditorGUILayout.HelpBox("Analytics is not initialized.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Status", "✅ Connected");
            EditorGUILayout.LabelField("Session ID", instance.SessionId);
            EditorGUILayout.LabelField("Events Sent", instance.EventsSent.ToString());
            EditorGUILayout.LabelField("Events Dropped", instance.EventsDropped.ToString());
            EditorGUILayout.LabelField("Queue Size", instance.QueueSize.ToString());

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Flush Now"))
            {
                instance.Flush();
            }

            if (GUILayout.Button("Send Test Event"))
            {
                instance.Track("editor_test", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "source", "editor_window" },
                    { "timestamp", System.DateTime.UtcNow.ToString("o") }
                });
            }

            Repaint();
        }
    }
}