using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : MonoBehaviour
{
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

    // 요청한 객체가 부족할때 추가 할당해주는 객체의 개수
    private const int AllocCount = 10;
    // 나중에 객체가 반환될때 해당 객체가 어느 풀링그룹인지 확인할때 사용
    private const string PoolingGroup = "PoolingGroup";

    private Dictionary<string, Transform> mObjectPool = new Dictionary<string, Transform>();

    public GameObject AssignObject(GameObject prefab)
    {
        // 본체가 씬상에 이미 생성된 상태일때는 그냥 이름으로 key설정하고
        // Asset상태로 있는 경우에는 AssetID를 추가하여 key로 설정한다
        string key = prefab.name;
        if (prefab.scene.rootCount <= 0)
        {
            key += prefab.GetInstanceID();
        }

        // 요청한 객체가 할당 가능하지 확인하고 불가능 하면 추가로 객체 생성한다.
        if (!IsAllocable(key))
        {
            if (!AllocObjects(key, prefab))
                return null;
        }

        // Pool에서 실제 요청한 객체 하나를 빼와서 반환해준다
        Transform obj = mObjectPool[key].GetChild(0);
        return obj.gameObject;
    }
    public GameObject Instantiate(GameObject prefab)
    {
        GameObject obj = AssignObject(prefab);
        if (obj == null)
        {
            return null;
        }

        obj.transform.SetParent(null);
        obj.SetActive(true);
        return obj;
    }
    public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject obj = AssignObject(prefab);
        if (obj == null)
        {
            return null;
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.transform.SetParent(parent);
        obj.SetActive(true);
        return obj;
    }

    public GameObject AssignObject(string resourcesPath)
    {
        // 요청한 객체가 할당 가능하지 확인하고 불가능 하면 추가로 객체 생성한다.
        if (!IsAllocable(resourcesPath))
        {
            if (!AllocObjects(resourcesPath))
                return null;
        }

        // Pool에서 실제 요청한 객체 하나를 빼와서 반환해준다
        Transform obj = mObjectPool[resourcesPath].GetChild(0);
        return obj.gameObject;
    }
    public GameObject Instantiate(string resourcesPath)
    {
        GameObject obj = AssignObject(resourcesPath);
        if (obj == null)
        {
            return null;
        }

        obj.transform.SetParent(null);
        obj.SetActive(true);
        return obj;
    }

    public GameObject Instantiate(string resourcesPath, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject obj = AssignObject(resourcesPath);
        if(obj == null)
        {
            return null;
        }
        
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.transform.SetParent(parent);
        obj.SetActive(true);
        return obj;
    }
    
    public GameObject Instantiate(string resourcesPath, Transform parent = null)
    {
        GameObject obj = AssignObject(resourcesPath);
        if(obj == null)
        {
            return null;
        }
        
        if(parent != null)
            obj.transform.SetParent(parent);

        obj.SetActive(true);
        return obj;
    }

    // 요청한 객체가 할당 가능한지 여부 확인
    private bool IsAllocable(string path)
    {
        if(mObjectPool.ContainsKey(path))
        {
            return mObjectPool[path].childCount > 0;
        }
        return false;
    }
    // 요청한 객체가 Pool에 없을경우 10개의 객체를 미리 생성
    private bool AllocObjects(string resourcesPath, GameObject prefab = null)
    {
        // 프리팹 로딩
        if(prefab == null)
        {
            prefab = Resources.Load<GameObject>(resourcesPath);
            if (prefab == null)
            {
                return false;
            }
        }

        if(prefab.GetComponent<ObjectPoolable>() == null)
        {
            return false;
        }

        // 최초 요청시에는 그룹 관리를 위한 부모 객체 생성
        if(!mObjectPool.ContainsKey(resourcesPath))
        {
            GameObject newParent = new GameObject();
            newParent.transform.SetParent(transform);
            newParent.name = resourcesPath;
            mObjectPool[resourcesPath] = newParent.transform;
        }

        // 그룹 즉 부모 객체 하위에 풀링할 실제 객체를 10개 미리 생성해 놓는다
        Transform parentTr = mObjectPool[resourcesPath];
        for (int i = 0; i < AllocCount; ++i)
        {
            GameObject newObj = GameObject.Instantiate(prefab, parentTr);
            newObj.gameObject.SetActive(false);
            newObj.GetComponent<ObjectPoolable>().PoolingParent = parentTr;
        }
        return true;
    }

    public void DestroyReturn(GameObject obj)
    {
        // 어느 그룹쪽으로 반환해야할지 정보를 가져옴
        ObjectPoolable poolingObj = obj.GetComponent<ObjectPoolable>();
        if (poolingObj == null)
        {
            //풀링 대상이 아니므로 그냥 삭제
            Destroy(obj);
            return;
        }

        // 대상객체가 이미 풀링 안에 있는 상태이면 그냥 반환
        if(IsAlreadyInPoolingGroup(obj))
        {
            return;
        }

        // 다 사용한 객체는 재활용을 위해 다시 Pool에 넣어준다
        Transform parentTr = poolingObj.PoolingParent;
        obj.transform.SetParent(parentTr);
        obj.SetActive(false);
    }

    public void DestroyReturnAfter(GameObject obj, float delay)
    {
        if(delay <= 0)
        {
            DestroyReturn(obj);
        }
        else
        {
            this.ExDelayedCoroutine(delay, () => 
            {
                if(obj != null)
                    DestroyReturn(obj);
            });
        }
    }

    // 인자로 받은 객체가 프리팹상태인지 아니면 게임상에 존재하는 object인지 확인
    private bool IsInstantiated(GameObject obj)
    {
        return obj.scene.rootCount > 0;
    }

    public bool IsAlreadyInPoolingGroup(GameObject obj)
    {
        if(obj.activeSelf)
            return false;

        ObjectPooling poolRoot = obj.GetComponentInParent<ObjectPooling>();
        if(poolRoot == null)
            return false;

        return true;
    }

    public static bool IsPoolingable(GameObject obj)
    {
        return obj.GetComponent<ObjectPoolable>() != null && obj.GetComponent<ObjectPoolable>().PoolingParent != null;
    }

}
public static class ObjectPoolingHelper
{
    public static GameObject ReturnAfter(this GameObject obj, float delay = 0)
    {
        if(obj == null) return null;
        ObjectPooling.Instance.DestroyReturnAfter(obj, delay);
        return obj;
    }
}