using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    [Header("Pool Target")]
    [SerializeField] private GameObject prefab;

    [Header("Pool Settings")]
    [SerializeField] private int initialSize = 10;
    [SerializeField] private bool expandable = true;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        Prewarm();
    }

    private void Prewarm()
    {
        for (int i = 0; i < Mathf.Max(1, initialSize); i++)
            CreateInstance();
    }

    private GameObject CreateInstance()
    {
        var go = Instantiate(prefab, transform);
        go.SetActive(false);

        // 인스턴스에 PooledObject 보장
        var po = go.GetComponent<PooledObject>();
        if (!po) po = go.AddComponent<PooledObject>();
        po.pool = this;

        pool.Enqueue(go);
        return go;
    }

    /// <summary> 풀에서 하나 꺼내서 position/rotation으로 활성화 </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        if (pool.Count == 0)
        {
            if (!expandable) return null;
            CreateInstance();
        }

        var go = pool.Dequeue();
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    /// <summary> 사용 완료된 오브젝트를 풀에 반환 </summary>
    public void Return(GameObject go)
    {
        if (!go) return;
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        pool.Enqueue(go);
    }
}
