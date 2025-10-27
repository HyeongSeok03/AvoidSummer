using System.Collections;
using UnityEngine;

public abstract class PooledObject : MonoBehaviour
{
    [HideInInspector] public ObjectPool pool;
    [SerializeField] float lifeTime = 8f;   // 자동 반환까지 시간(초)
    Coroutine lifeCo = null;

    void OnEnable()
    {
        lifeCo = StartCoroutine(LifeRoutine());
    }

    void OnDisable()
    {
        if (lifeCo != null)
        {
            StopCoroutine(lifeCo);
            lifeCo = null;
        }
    }

    IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        Despawn();
    }

    public abstract void Collect();

    /// <summary> 외부에서 호출해 Despawn </summary>
    public void Despawn()
    {
        if (pool) pool.Return(gameObject);
        else gameObject.SetActive(false);
    }
}
