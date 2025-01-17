﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;

namespace UnityEventReferenceViewer
{
    public class UnityEventReferenceViewerWindow : EditorWindow
    {
        private int mainSpacing = 20;
        private int tabulation = 30;
        private float leftColoumnRelativeWidth = 0.6f;

        private static List<EventReferenceInfo> dependencies;
        private string searchString = "";

        private Rect drawableRect;

        Vector2 scroll = Vector2.zero;

        private bool lastHideNoEventAssignment;
        private bool lastHideNullEventAssignment;
        private bool lastHideUnityEngineEventAssignment;

        [MenuItem("Window/UnityEvent Reference Viewer")]
        public static void OpenWindow()
        {
            UnityEventReferenceViewerWindow window = CreateWindow<UnityEventReferenceViewerWindow>();
            window.titleContent = new GUIContent("UnityEvent Reference Viewer");
            window.minSize = new Vector2(250, 100);
            window.Show();
        }

        [DidReloadScripts]
        private static void RefreshDependencies()
        {
            FindDependencies();
        }

        private void OnFocus()
        {
            RefreshDependencies();
        }

        private void OnEnable()
        {
            FindDependencies();
        }

        private void OnDisable()
        {
            dependencies = null;
        }

        private void OnGUI()
        {
            DrawWindow();
        }

        private void DrawWindow()
        {
            var drawnVertically = 0;
            drawableRect = GetDrawableRect();

            GUILayout.BeginHorizontal();

            // Show the toggles
            UnityEventReferenceFinder.HideNoEventAssignment = GUILayout.Toggle(UnityEventReferenceFinder.HideNoEventAssignment, "HideNoEventAssignment");
            UnityEventReferenceFinder.HideNullEventAssignment = GUILayout.Toggle(UnityEventReferenceFinder.HideNullEventAssignment, "HideNullEventAssignment");
            UnityEventReferenceFinder.HideUnityEngineEventAssignment = GUILayout.Toggle(UnityEventReferenceFinder.HideUnityEngineEventAssignment, "HideUnityEngineEventAssignment");

            // Search bar
            EditorGUI.LabelField(new Rect(drawableRect.position, new Vector2(150f, 16f)), "Search dependences");
            var newString = EditorGUI.TextField(new Rect(drawableRect.position + Vector2.right * 150f, new Vector2(drawableRect.width - 150, 16f)), searchString);
            GUILayout.EndHorizontal();

            // Search elaboration
            if (newString != searchString ||
                UnityEventReferenceFinder.HideNoEventAssignment != lastHideNoEventAssignment ||
                UnityEventReferenceFinder.HideNullEventAssignment != lastHideNullEventAssignment ||
                UnityEventReferenceFinder.HideUnityEngineEventAssignment != lastHideUnityEngineEventAssignment)
            {
                searchString = newString;

                if (searchString != "")
                {
                    FindDependencies(searchString);
                }
                else
                {

                    lastHideNoEventAssignment = UnityEventReferenceFinder.HideNoEventAssignment;
                    lastHideNullEventAssignment = UnityEventReferenceFinder.HideNullEventAssignment;
                    lastHideUnityEngineEventAssignment = UnityEventReferenceFinder.HideUnityEngineEventAssignment;

                    FindDependencies();
                }
            }

            // Data list showed to the user
            if (dependencies != null)
            {
                GUILayout.Space(50);
                GUILayout.BeginHorizontal();

                GUILayout.Label($"Entries found: {dependencies.Count}");

                GUILayout.EndHorizontal();

                GUILayout.Space(30);
                scroll = GUILayout.BeginScrollView(scroll, false, false);

                int i = 0;
                foreach (var d in dependencies)
                {
                    var verticalSpace = (d.Listeners.Count + 1) * 16 + mainSpacing;
                    var position = new Vector2(drawableRect.position.x, 0f) + Vector2.up * drawnVertically;

                    DrawDependencies(d, position);

                    drawnVertically += verticalSpace;
                    i++;
                }

                GUILayout.Space(drawnVertically + 20);

                GUILayout.EndScrollView();
            }
        }

        private void DrawDependencies(EventReferenceInfo dependency, Vector2 position)
        {
            float width = drawableRect.width * leftColoumnRelativeWidth;

            //EditorGUI.ObjectField(new Rect(position, new Vector2(width - tabulation, 16f)), dependency.OwnerTransform, typeof(Transform), true);      // You can use this instead if you want the transform of the owner
            EditorGUI.ObjectField(new Rect(position, new Vector2(width - tabulation, 16f)), dependency.Owner, typeof(MonoBehaviour), true);

            for (int i = 0; i < dependency.Listeners.Count; i++)
            {
                Vector2 positionRoot = position + Vector2.up * 16 + Vector2.up * 16 * i;
                EditorGUI.ObjectField(new Rect(positionRoot + Vector2.right * tabulation, new Vector2(width - tabulation, 16f)), dependency.Listeners[i], typeof(MonoBehaviour), true);

                Vector2 labelPosition = new Vector2(drawableRect.width * leftColoumnRelativeWidth + tabulation * 1.5f, positionRoot.y);
                EditorGUI.LabelField(new Rect(labelPosition, new Vector2(drawableRect.width * (1 - leftColoumnRelativeWidth) - tabulation / 1.5f, 16f)), dependency.MethodNames[i]);
            }
        }

        /// <summary>
        /// Filter the references found.
        /// </summary>
        /// <param name="methodName">The keyword of the reference.</param>
        private static void FindDependencies(string methodName)
        {
            var depens = UnityEventReferenceFinder.FindAllUnityEventsReferences();
            var onlyWithName = new List<EventReferenceInfo>();

            foreach (var d in depens)
            {
                if (d.MethodNames.Where(m => m.ToLower().Contains(methodName.ToLower())).Count() > 0)
                {
                    var indexes = d.MethodNames.Where(n => n.ToLower().Contains(methodName.ToLower())).Select(n => d.MethodNames.IndexOf(n)).ToArray();

                    var info = new EventReferenceInfo();
                    info.Owner = d.Owner;

                    foreach (var i in indexes)
                    {
                        info.Listeners.Add(d.Listeners[i]);
                        info.MethodNames.Add(d.MethodNames[i]);
                    }

                    onlyWithName.Add(info);
                }
            }

            dependencies = onlyWithName.Count > 0 ? onlyWithName : depens;
        }

        private static void FindDependencies()
        {
            dependencies = UnityEventReferenceFinder.FindAllUnityEventsReferences();
        }

        private Rect GetDrawableRect()
        {
            return new Rect(Vector2.one * 30f, position.size - Vector2.one * 60f);
        }
    }
}
