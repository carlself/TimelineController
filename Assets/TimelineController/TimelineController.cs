using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class TrackBinding
{
    public int trackIndex;
    public string id;
}

[Serializable]
public class NestedTimlineBinding
{
    public int trackIndex;
    public int clipIndex;
    public string id;
    public List<TrackBinding> nestedTimelineTrackBindings;
    public PlayableAsset timelineAsset;
}

[RequireComponent(typeof(PlayableDirector))]
[ExecuteInEditMode]
public class TimelineController : MonoBehaviour
{
    [SerializeField]
    List<TrackBinding> trackBindings = new List<TrackBinding>();
    [SerializeField]
    List<NestedTimlineBinding> nestedTimelineBindings = new List<NestedTimlineBinding>();
    PlayableDirector playableDirector;
    Action onComplete;
    // 运行时绑定对象
    Dictionary<string, GameObject> runtimeObjMap = new Dictionary<string, GameObject>();
    List<TimelineReference> timelineReferences = new List<TimelineReference>(10);


#if UNITY_EDITOR

    [NonSerialized]
    public bool ActiveInScene;
    public List<NestedTimlineBinding> NestedTimelineBindings
    {
        get { return nestedTimelineBindings; }
    }

    public static TimelineController LastActiveTimeline;

    void OnValidate()
    {
        playableDirector = GetComponent<PlayableDirector>();
    }

    private void Awake()
    {
        if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(this))
        {
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            ActiveInScene = true;
            if (LastActiveTimeline != null)
            {
                LastActiveTimeline.ActiveInScene = false;
                LastActiveTimeline.gameObject.SetActive(false);
                LastActiveTimeline = null;
            }
            LastActiveTimeline = this;

            playableDirector = GetComponent<PlayableDirector>();
            Debug.Log("Editor causes this Awake");
            InstallRuntimeBindings();
            enabled = true;
        }
        else
        {
            playableDirector = GetComponent<PlayableDirector>();
            InstallRuntimeBindings();
            //playableDirector.RebuildGraph();
            //playableDirector.playOnAwake = false;
        }
    }

    //public TimelineAsset GetNestedTimelineAsset(TrackAsset trackAsset, int trackIndex, out float clipStartTime)
    //{
    //    foreach (var entry in nestedTimelineBindings)
    //    {
    //        if (entry.trackIndex == trackIndex)
    //        {
    //            int clipIndex = -1;
    //            TimelineClip nestTimelineClip = null;
    //            foreach (var clip in trackAsset.GetClips())
    //            {
    //                clipIndex++;
    //                if (clipIndex == entry.clipIndex)
    //                {
    //                    nestTimelineClip = clip;
    //                    break;
    //                }
    //            }

    //            clipStartTime = (float)nestTimelineClip.start;
    //            return entry.timelineAsset as TimelineAsset;
    //        }
    //    }
    //    clipStartTime = 0;
    //    return null;
    //}
#endif

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;
        playableDirector = GetComponent<PlayableDirector>();
        playableDirector.stopped += OnPlayableDirectorStopped;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        runtimeObjMap.Clear();
        playableDirector.stopped -= OnPlayableDirectorStopped;
        onComplete = null;
    }


    public void Play(Action onComplete)
    {
        InstallRuntimeBindings();
        playableDirector.Play();
        this.onComplete = onComplete;
    }

    // 添加动态对象
    public void AddRuntimeObject(GameObject bindingObject)
    {
        timelineReferences.Clear();
        bindingObject.GetComponentsInChildren(true, timelineReferences);
        if (timelineReferences.Count == 0)
            return;

        foreach (var timelineRef in timelineReferences)
        {
            runtimeObjMap.Add(timelineRef.Id, timelineRef.gameObject);
        }
    }

    void OnPlayableDirectorStopped(PlayableDirector playableDirector)
    {
        if (onComplete != null)
        {
            onComplete();
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Application.isPlaying)
            return;

        if (!ActiveInScene)
            return;

        UpdateBindingList(playableDirector, trackBindings, false);
        UpdateNestedTimelineBindingList(playableDirector, nestedTimelineBindings);
    }
#endif

#if UNITY_EDITOR
    static bool IsChildOf(Transform child, Transform parent)
    {
        if (parent == null || child == null)
            return false;

        do
        {
            if (child == parent)
                return true;
        } while (child = child.parent);

        return false;
    }

    string GetTimelineId(GameObject owner)
    {
        var timelineRef = owner.GetComponent<TimelineReference>();
        if (timelineRef == null)
        {
            bool hasPrefab = false;

            GameObject ownerPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(owner);
            if (ownerPrefab != null && (ownerPrefab.hideFlags & HideFlags.NotEditable) == 0)
            {
                hasPrefab = true;
                timelineRef = ownerPrefab.AddComponent<TimelineReference>();
                PrefabUtility.SavePrefabAsset(ownerPrefab.transform.root.gameObject);
            }

            if (!hasPrefab)
            {
                timelineRef = owner.AddComponent<TimelineReference>();

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(owner.scene);
            }
        }
        return timelineRef.Id;
    }

    void UpdateBindingList(PlayableDirector playableDirector, List<TrackBinding> trackBindings, bool includeChildObject)
    {
        var timelineAsset = playableDirector.playableAsset as TimelineAsset;
        if (timelineAsset == null)
            return;

        PrefabUtility.RecordPrefabInstancePropertyModifications(this);

        trackBindings.Clear();
        for (int i = 0; i < timelineAsset.outputTrackCount; i++)
        {
            TrackAsset trackAsset = timelineAsset.GetOutputTrack(i);
            if (trackAsset == null)
                continue;

            var outputs = trackAsset.outputs;
            bool hasOutput = false;
            foreach (var output in outputs)
            {
                if (output.outputTargetType == null || !typeof(UnityEngine.Object).IsAssignableFrom(output.outputTargetType) || output.sourceObject == null)
                    continue;
                else
                {
                    hasOutput = true;
                    break;
                }
            }

            TrackBinding trackBinding = null;

            if (hasOutput)
            {
                var owner = playableDirector.GetGenericBinding(trackAsset) as GameObject;
                var comp = playableDirector.GetGenericBinding(trackAsset) as Component;
                if (comp != null)
                    owner = comp.gameObject;

                if (owner != null)
                {
                    // 如果binding的对象是子节点，不用动态绑定
                    if (!includeChildObject && IsChildOf(owner.transform, playableDirector.transform))
                        continue;

                    var guid = GetTimelineId(owner);
                    trackBinding = new TrackBinding() { trackIndex = i, id = guid };
                }
            }
            if (trackBinding != null)
            {
                trackBinding.trackIndex = i;
                trackBindings.Add(trackBinding);
            }
        }
    }

    void UpdateNestedTimelineBindingList(PlayableDirector playableDirector, List<NestedTimlineBinding> nestedTimelineBindings)
    {
        var timelineAsset = playableDirector.playableAsset as TimelineAsset;
        if (timelineAsset == null)
            return;

        nestedTimelineBindings.Clear();

        for (int trackIndex = 0; trackIndex < timelineAsset.outputTrackCount; trackIndex++)
        {
            TrackAsset trackAsset = timelineAsset.GetOutputTrack(trackIndex);
            ControlTrack controlTrack = trackAsset as ControlTrack;
            if (controlTrack == null)
                continue;

            int clipIndex = -1;
            foreach (TimelineClip clip in controlTrack.GetClips())
            {
                clipIndex++;
                ControlPlayableAsset playableAsset = (ControlPlayableAsset)clip.asset;
                GameObject resolvedObj = playableAsset.sourceGameObject.Resolve(playableDirector);
                if (resolvedObj == null)
                    continue;

                PlayableDirector resolvedDirector = resolvedObj.GetComponent<PlayableDirector>();
                if (resolvedDirector == null)
                    continue;

                // 如果binding的对象是子节点，不用动态绑定
                if (IsChildOf(resolvedObj.transform, transform))
                    continue;

                var timelineRef = resolvedObj.GetComponent<TimelineReference>();
                if (timelineRef == null)
                {
                    bool hasPrefab = false;

                    GameObject resovledObjInPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(resolvedObj);
                    if (resovledObjInPrefab != null)
                    {
                        hasPrefab = true;
                        timelineRef = resovledObjInPrefab.AddComponent<TimelineReference>();
                        PrefabUtility.SavePrefabAsset(resovledObjInPrefab.transform.root.gameObject);
                    }

                    if (!hasPrefab)
                    {
                        timelineRef = resolvedObj.AddComponent<TimelineReference>();
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(resolvedObj.scene);
                    }
                }

                List<TrackBinding> trackBindings = new List<TrackBinding>();
                UpdateBindingList(resolvedDirector, trackBindings, true);

                NestedTimlineBinding binding = new NestedTimlineBinding()
                {
                    trackIndex = trackIndex,
                    clipIndex = clipIndex,
                    id = timelineRef.Id,
                    timelineAsset = resolvedDirector.playableAsset,
                    nestedTimelineTrackBindings = trackBindings,
                };
                nestedTimelineBindings.Add(binding);
            }
        }
    }
#endif

    GameObject GetBindTarget(string id)
    {
        // 首先获取动态对象
        if (runtimeObjMap.TryGetValue(id, out var bindTarget))
        {
            return bindTarget;
        }

        List<GameObject> instances;
        if (!TimelineReference.IdMap.TryGetValue(id, out instances))
            return null;

        if (instances.Count == 0)
            return null;

        return instances[0];
    }

    bool BindTrack(PlayableDirector playableDirector, TrackBinding binding)
    {
        TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;

        if (binding.trackIndex >= timelineAsset.outputTrackCount)
        {
            Debug.LogWarningFormat("{0}trackIndex {1}出界", timelineAsset.ToString(), binding.trackIndex);
            return false;
        }
        TrackAsset trackAsset = timelineAsset.GetOutputTrack(binding.trackIndex);

        Type outputType = null;
        var outputs = trackAsset.outputs;
        foreach (var output in outputs)
        {
            outputType = output.outputTargetType;
            break;
        }

        if (outputType == null)
            return false;

        bool isComponent = typeof(Component).IsAssignableFrom(outputType);
        bool isGameObject = typeof(GameObject).IsAssignableFrom(outputType);
        if (!isComponent && !isGameObject)
            return false;

        GameObject bindTarget = GetBindTarget(binding.id);

        if (bindTarget == null)
        {
            Debug.LogWarningFormat("{0} 绑定轨道{1}失败 找不到绑定对象{2}", timelineAsset.ToString(), trackAsset.ToString(), binding.id);
            return false;
        }

        UnityEngine.Object target = bindTarget;

        if (isComponent)
        {
            target = bindTarget.GetComponent(outputType);
        }

        var oldBinding = playableDirector.GetGenericBinding(trackAsset);
        if (oldBinding != target)
        {
            playableDirector.SetGenericBinding(trackAsset, target);
        }

        return true;
    }

    public void InstallRuntimeBindings()
    {
        // install main timeline bindings
        foreach (var entry in trackBindings)
        {
            if (string.IsNullOrEmpty(entry.id))
                continue;

            BindTrack(playableDirector, entry);
        }

        // install nested timeline binding
        for (int i = 0; i < nestedTimelineBindings.Count; i++)
        {
            NestedTimlineBinding entry = nestedTimelineBindings[i];
            if (entry.timelineAsset == null || string.IsNullOrEmpty(entry.id))
                continue;

            GameObject owner = GetBindTarget(entry.id);
            if (owner == null)
                continue;
            TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;

            int clipIndex = -1;
            if (entry.trackIndex >= timelineAsset.outputTrackCount)
            {
                Debug.LogWarningFormat("{0}trackIndex {1}出界", timelineAsset.ToString(), entry.trackIndex);
                continue;
            }
            TrackAsset trackAsset = timelineAsset.GetOutputTrack(entry.trackIndex);
            clipIndex = -1;
            ControlPlayableAsset clipAsset = null;
            foreach (var clip in trackAsset.GetClips())
            {
                clipIndex++;
                if (clipIndex == entry.clipIndex)
                {
                    clipAsset = clip.asset as ControlPlayableAsset;
                    break;
                }
            }
            playableDirector.SetReferenceValue(clipAsset.sourceGameObject.exposedName, owner);
            PlayableDirector nestedPlayableDirector = owner.GetComponent<PlayableDirector>();
            nestedPlayableDirector.playableAsset = entry.timelineAsset;

            // nested timeline
            foreach (var binding in entry.nestedTimelineTrackBindings)
            {
                if (string.IsNullOrEmpty(binding.id))
                {
                    Debug.LogWarningFormat("绑定子timeline出错 id为空子timeline {0} 主timeline {1}",
                        nestedPlayableDirector.playableAsset.ToString(), playableDirector.playableAsset.ToString());
                }
                else
                {
                    BindTrack(nestedPlayableDirector, binding);
                }
            }
            nestedPlayableDirector.RebuildGraph();
            //nestedPlayableDirector.RebindPlayableGraphOutputs();
        }
    }
}