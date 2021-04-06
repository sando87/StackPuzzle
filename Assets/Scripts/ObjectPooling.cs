using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
    private const int ReserveSize = 20;
    private static ObjectPooling _Instance = null;
    public static ObjectPooling Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = FindObjectOfType<ObjectPooling>();
            return _Instance;
        }
    }

    private Dictionary<int, Queue<ObjectPoolingable>> ObjectPool = new Dictionary<int, Queue<ObjectPoolingable>>();

    public GameObject Instantiate(GameObject prefab)
    {
        if(!IsPrefab(prefab))
        {
            LOG.error();
            return null;
        }

        int instID = prefab.GetInstanceID();
        if(!ObjectPool.ContainsKey(instID) || ObjectPool[instID].Count <= 0)
            CreateNewObjects(prefab);

        ObjectPoolingable popObj = ObjectPool[instID].Dequeue();
        popObj.gameObject.SetActive(true);
        popObj.IsDead = false;
        return popObj.gameObject;
    }
    public GameObject Instantiate(GameObject prefab, Transform parent)
    {
        if (!IsPrefab(prefab))
        {
            LOG.error();
            return null;
        }

        int instID = prefab.GetInstanceID();
        if (!ObjectPool.ContainsKey(instID) || ObjectPool[instID].Count <= 0)
            CreateNewObjects(prefab);

        ObjectPoolingable popObj = ObjectPool[instID].Dequeue();
        popObj.gameObject.SetActive(true);
        popObj.IsDead = false;
        popObj.transform.SetParent(parent);
        return popObj.gameObject;
    }

    public void Destroy(GameObject obj)
    {
        ObjectPoolingable poolObj = obj.GetComponent<ObjectPoolingable>();
        poolObj.IsDead = true;
        int instID = poolObj.OriginPrefabInstanceID;
        obj.transform.SetParent(transform);
        obj.SetActive(false);
        ObjectPool[instID].Enqueue(poolObj);
    }

    private void CreateNewObjects(GameObject prefab)
    {
        int instID = prefab.GetInstanceID();
        if (!ObjectPool.ContainsKey(instID))
            ObjectPool[instID] = new Queue<ObjectPoolingable>();

        for (int i = 0; i < ReserveSize; ++i)
        {
            ObjectPoolingable newObj = Instantiate(prefab, transform).AddComponent<ObjectPoolingable>();
            newObj.OriginPrefabInstanceID = instID;
            newObj.IsDead = true;
            newObj.gameObject.SetActive(false);
            ObjectPool[instID].Enqueue(newObj);
        }
    }
    private bool IsPrefab(GameObject obj)
    {
        return obj.scene.rootCount == 0;
    }
}
