using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


public class Test : MonoBehaviour
{
    private AsyncOperationHandle<GameObject> sphereHandle;
    private AsyncOperationHandle<GameObject> timelineHandle;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        sphereHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Example/Sphere.prefab");
        yield return sphereHandle;
        GameObject sphere1 = Instantiate(sphereHandle.Result);
        sphere1.name = "Sphere1";
        GameObject sphere2 = Instantiate(sphereHandle.Result);
        sphere2.name = "Sphere2";

        timelineHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Example/Timeline/Timeline.prefab");
        yield return timelineHandle;
        var timelineObj = Instantiate(timelineHandle.Result);
        var timelineController = timelineObj.GetComponent<TimelineController>();
        timelineController.AddRuntimeObject(sphere2);
        timelineController.Play(()=>Debug.Log("TimelineController Complete"));
    }

    private void OnDestroy()
    {
        Addressables.Release(timelineHandle);
        Addressables.Release(sphereHandle);
    }
}
