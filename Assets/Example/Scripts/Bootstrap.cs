using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Bootstrap : MonoBehaviour
{
    private AsyncOperationHandle<GameObject> sphereHandle;
    private AsyncOperationHandle<GameObject> cubeHandle;
    private AsyncOperationHandle<GameObject> childTimelineHandle;
    private AsyncOperationHandle<GameObject> timelineHandle;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Load dependent GameObjects
        cubeHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Example/Prefabs/Cube.prefab");
        yield return cubeHandle;
        GameObject cube = Instantiate(cubeHandle.Result);

        sphereHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Example/Prefabs/Sphere.prefab");
        yield return sphereHandle;
        // There are multiple instances of the sphere prefab.
        // You need select which one to bind or it will bind the instance created first
        GameObject sphere1 = Instantiate(sphereHandle.Result);
        sphere1.name = "Sphere1";
        GameObject sphere2 = Instantiate(sphereHandle.Result);
        sphere2.name = "Sphere2";

        // Load child timeline
        childTimelineHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Example/Prefabs/ChildTimeline.prefab");
        yield return childTimelineHandle;
        GameObject childTimeline = Instantiate<GameObject>(childTimelineHandle.Result);

        // Load timeline GameObject
        timelineHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Example/Timeline/Timeline.prefab");
        yield return timelineHandle;
        var timelineObj = Instantiate(timelineHandle.Result);
        var timelineController = timelineObj.GetComponent<TimelineController>();
        // Select the instance to bind
        timelineController.AddRuntimeObject(sphere2);
        // Play the timeline
        timelineController.Play(() => Debug.Log("TimelineController Complete"));
    }

    private void OnDestroy()
    {
        Addressables.Release(timelineHandle);
        Addressables.Release(sphereHandle);
        Addressables.Release(cubeHandle);
        Addressables.Release(childTimelineHandle);
    }
}
