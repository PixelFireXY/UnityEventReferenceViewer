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

        [ContextMenu("FindReferences")]
        public void FindReferences()
        {
            FindAllUnityEventsReferences();
        }

        public static List<EventReferenceInfo> FindAllUnityEventsReferences()
        {
            var behaviours = FindAssetsByMonoBehaviour();

            var monobehaviourList = new List<MonoBehaviour>();
            var unityEventList = new List<UnityEventBase>();

            foreach (var b in behaviours)
            {
                if (b == null)
                    continue;

                TypeInfo info = b.GetType().GetTypeInfo();
                List<FieldInfo> evnts = info.DeclaredFields.Where(f => f.FieldType.IsSubclassOf(typeof(UnityEventBase))).ToList();
                foreach (var e in evnts)
                {
                    var eventField = e.GetValue(b) as UnityEventBase;

                    var eventCount = eventField.GetPersistentEventCount();
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

                if (monobehaviourList[i].name.Contains("OnButton"))
                    Debug.Log($"");

                var info = new EventReferenceInfo();
                info.Owner = monobehaviourList[i];
                info.OwnerTransform = monobehaviourList[i].transform;

                bool hasMoreThanOneElementValid = !HideNullEventAssignment || !HideNoEventAssignment;

                for (int ii = 0; ii < count; ii++)
                {
                    var obj = currUnityEvent.GetPersistentTarget(ii);

                    if (HideUnityEngineEventAssignment)
                    {
                        if (obj != null &&
                            obj.GetType().Namespace == "UnityEngine")
                        {
                            continue;
                        }
                    }

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

                if (hasMoreThanOneElementValid)
                    infos.Add(info);
            }

            return infos;
        }

        public static List<MonoScript> FindAssetsByMonoScript()
        {
            List<MonoScript> assets = new List<MonoScript>();

            string[] guids = AssetDatabase.FindAssets(string.Format("t:script"));
            //string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).ToString().Replace("UnityEngine.", "")));
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
                    var monoChildren = assetMono.GetComponentsInChildren<MonoBehaviour>(true);
                    //assets.Add(assetMono);
                    assets.AddRange(monoChildren);
                }
            }
            return assets;
        }
    }
}
