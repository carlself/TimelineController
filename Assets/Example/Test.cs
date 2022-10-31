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
        Instantiate(sphereHandle.Result);

        timelineHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Example/Timeline/Timeline.prefab");
        yield return timelineHandle;
        var timelineObj = Instantiate(timelineHandle.Result);
        var timelineController = timelineObj.GetComponent<TimelineController>();
        timelineController.Play(null);

    }

    private void OnDestroy()
    {

        Addressables.Release(timelineHandle);
        Addressables.Release(sphereHandle);
    }
}
