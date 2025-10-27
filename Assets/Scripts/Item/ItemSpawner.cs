using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ItemSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ObjectPool pool;

    [Header("Spawn Settings")]
    [SerializeField] private float interval = 1.5f;
    [SerializeField] private Vector2 areaSize = new Vector2(8f, 4f); // 스폰 박스 크기
    [SerializeField] private bool autoStart = true;

    Coroutine co;

    void OnEnable()
    {
        if (autoStart) StartSpawn();
    }

    void OnDisable()
    {
        StopSpawn();
    }

    public void StartSpawn()
    {
        if (co == null) co = StartCoroutine(SpawnLoop());
    }

    public void StopSpawn()
    {
        if (co != null) { StopCoroutine(co); co = null; }
    }

    IEnumerator SpawnLoop()
    {
        var half = areaSize * 0.5f;

        while (true)
        {
            // 랜덤 위치(Spawner 중심 기준 box 내부)
            Vector3 local = new Vector3(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y),
                0f
            );
            Vector3 pos = transform.TransformPoint(local);

            var go = pool.Get(pos, Quaternion.identity);
            if (!go)
            {
                // 풀 고갈 & expandable=false인 경우
                // 필요하면 여기서 skip 로그
            }

            yield return new WaitForSeconds(interval);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.1f));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.1f));
    }
#endif
}
