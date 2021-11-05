using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.Events;
using System.Linq;
using UnityEditor;

namespace UnityEventReferenceViewer
{
    public class EventReferenceInfo
    {
        public Transform OwnerTransform { get; set; }
        public MonoBehaviour Owner { get; set; }
        public List<MonoBehaviour> Listeners { get; set; } = new List<MonoBehaviour>();
        public List<string> MethodNames { get; set; } = new List<string>();
    }

    public class UnityEventReferenceFinder : MonoBehaviour
    {
        /// <summary>
        /// Hide all UnityEvent with no events assigned.
        /// </summary>
        public static bool HideNoEventAssignment { get; set; } = true;

        /// <summary>
        /// Hide all events of the UnityEvents that are null?
        /// </summary>
        public static bool HideNullEventAssignment { get; set; } = true;

        /// <summary>
        /// Hide all events of the UnityEngine?
        /// </summary>
        public static bool HideUnityEngineEventAssignment { get; set; } = true;

        public static List<EventReferenceInfo> FindAllUnityEventsReferences()
        {
            // Find all prefabs
            List<MonoBehaviour> behaviours = FindAssetsByMonoBehaviour();

            var monobehaviourList = new List<MonoBehaviour>();
            var unityEventList = new List<UnityEventBase>();

            foreach (var b in behaviours)
            {
                if (b == null)      // Prevent errors
                    continue;

                // Filter by UnityEvents
                TypeInfo info = b.GetType().GetTypeInfo();
                List<FieldInfo> evnts = info.DeclaredFields.Where(f => f.FieldType.IsSubclassOf(typeof(UnityEventBase))).ToList();

                foreach (var e in evnts)
                {
                    var eventField = e.GetValue(b) as UnityEventBase;

                    var eventCount = eventField.GetPersistentEventCount();

                    // Prevent to load the UnityEvents with no events in list
                    if (HideNoEventAssignment &&
                        eventField.GetPersistentEventCount() == 0)
                        continue;

                    monobehaviourList.Add(b);
                    unityEventList.Add(eventField);
                }
            }

            var infos = new List<EventReferenceInfo>();

            for (int i = 0; i < monobehaviourList.Count; i++)
            {
                var currUnityEvent = unityEventList[i];
                int count = currUnityEvent.GetPersistentEventCount();

                var info = new EventReferenceInfo();
                info.Owner = monobehaviourList[i];
                info.OwnerTransform = monobehaviourList[i].transform;

                // Used to decide if add the element or not to the list
                bool hasMoreThanOneElementValid = !HideNullEventAssignment || !HideNoEventAssignment;

                for (int ii = 0; ii < count; ii++)
                {
                    var obj = currUnityEvent.GetPersistentTarget(ii);

                    // Hide UnityEngine components
                    if (HideUnityEngineEventAssignment)
                    {
                        if (obj != null &&
                            obj.GetType().Namespace == "UnityEngine")
                        {
                            continue;
                        }
                    }

                    // Hide UnityEvents with events in list but with null reference
                    if (HideNullEventAssignment)
                    {
                        if (obj != null)
                        {
                            hasMoreThanOneElementValid = true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var method = currUnityEvent.GetPersistentMethodName(ii);

                    info.Listeners.Add(obj as MonoBehaviour);
                    info.MethodNames.Add($"{obj.GetType().Name}.{method}");
                }

                // Add the element elaborated to the list
                if (hasMoreThanOneElementValid)
                    infos.Add(info);
            }

            return infos;
        }

        /// <summary>
        /// Use this if you want only to find the scripts.
        /// </summary>
        /// <returns>The list of scripts found in the project.</returns>
        public static List<MonoScript> FindAssetsByMonoScript()
        {
            List<MonoScript> assets = new List<MonoScript>();

            string[] guids = AssetDatabase.FindAssets(string.Format("t:script"));

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var assetMono = (MonoScript)AssetDatabase.LoadAssetAtPath(assetPath, typeof(MonoScript));
                if (assetMono != null)
                {
                    assets.Add(assetMono);
                }
            }
            return assets;
        }

        /// <summary>
        /// Find all prefabs with a UnityEvent on it.
        /// </summary>
        /// <returns>The list of prefabs found in the project.</returns>
        public static List<MonoBehaviour> FindAssetsByMonoBehaviour()
        {
            List<MonoBehaviour> assets = new List<MonoBehaviour>();

            string[] guids = AssetDatabase.FindAssets(string.Format("t:prefab"));

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var assetMono = (MonoBehaviour)AssetDatabase.LoadAssetAtPath(assetPath, typeof(MonoBehaviour));
                if (assetMono != null)
                {
                    // Make sure to be recursive and not only for the parent prefab
                    var monoChildren = assetMono.GetComponentsInChildren<MonoBehaviour>(true);

                    assets.AddRange(monoChildren);
                }
            }
            return assets;
        }
    }
}
