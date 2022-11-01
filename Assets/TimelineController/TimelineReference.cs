using System;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[DisallowMultipleComponent]
public class TimelineReference : MonoBehaviour
{
    public static readonly Dictionary<string, List<GameObject>> IdMap = new Dictionary<string, List<GameObject>>();

    [SerializeField, ShowAsReadOnly]
    public string Id = Guid.NewGuid().ToString();

    void Awake()
    {
        Register();
    }


    void Register()
    {
        List<GameObject> instances;
        if (!IdMap.TryGetValue(Id, out instances))
        {
            instances = new List<GameObject>();
            IdMap.Add(Id, instances);
        }

        instances.Add(gameObject);
    }

    void OnDestroy()
    {
        if(IdMap.TryGetValue(Id, out var instances))
        {
            instances.Remove(gameObject);
            if (instances.Count == 0)
            {
                IdMap.Remove(Id);
            }
        }
    }
}