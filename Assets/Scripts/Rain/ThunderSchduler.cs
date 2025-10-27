using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // Light2D

public class ThunderScheduler : MonoBehaviour
{
    [Header("Clouds Move In/Out")]
    public GameObject[] clouds;         // 씬의 클라우드 4개
    private Vector2[] start_poses;      // 시작 위치 (자동 기록)
    public Vector2[] end_poses;         // 클라우드의 비 모드 위치
    [SerializeField] float cloudFastSpeed = 5f;

    [Header("Rain & Lights")]
    [SerializeField] ParticleSystem rain;
    public Light2D sun;
    public Light2D global;
    [SerializeField] float sunFadeSeconds = 0.5f;

    [Header("Global Light Settings")]
    [SerializeField] float globalBrightIntensity = 1f; // 맑을 때
    [SerializeField] float globalDimIntensity = 0.1f;  // 비/번개 때

    [Header("Thunder Pool")]
    [SerializeField] Thunder thunderPrefab;
    [SerializeField] int initialPoolSize = 6;
    Queue<Thunder> pool = new Queue<Thunder>();
    readonly List<Thunder> actives = new List<Thunder>();

    [Header("Thunder Area & Timing")]
    [SerializeField] Transform areaCenter;
    [SerializeField] Vector2 areaSize = new Vector2(12f, 6f);

    // ↓↓↓ GameManager가 런타임에 조절할 값들
    [SerializeField] Vector2 intervalRange = new Vector2(0.8f, 2.0f);
    [SerializeField] int maxConcurrent = 3;
    [SerializeField] float warnDuration = 0.6f;
    [SerializeField] float warnBlinkHz = 8f;
    [SerializeField] float strikeDuration = 0.12f;

    bool running;
    Coroutine loopCo;

    void Awake()
    {
        // 자동 start_poses 기록
        if (clouds != null && clouds.Length > 0)
        {
            start_poses = new Vector2[clouds.Length];
            for (int i = 0; i < clouds.Length; i++)
                start_poses[i] = clouds[i] ? (Vector2)clouds[i].transform.position : Vector2.zero;
        }

        PrewarmPool();
    }

    void PrewarmPool()
    {
        int n = Mathf.Max(1, initialPoolSize);
        for (int i = 0; i < n; i++)
        {
            var th = Instantiate(thunderPrefab, transform);
            th.gameObject.SetActive(false);
            th.Setup(ReturnToPool);
            pool.Enqueue(th);
        }
    }

    Thunder GetThunder()
    {
        Thunder th = (pool.Count > 0) ? pool.Dequeue()
                                      : Instantiate(thunderPrefab, transform);
        th.Setup(ReturnToPool);
        th.gameObject.SetActive(true);
        actives.Add(th);
        return th;
    }

    void ReturnToPool(Thunder th)
    {
        if (!th) return;
        actives.Remove(th);
        th.gameObject.SetActive(false);
        pool.Enqueue(th);
    }

    // === 외부에서 호출: CloudScheduler.StopScheduler() 이후 ===
    public void StartRain()
    {
        // 1) 클라우드들을 end_poses로 이동
        int cnt = Mathf.Min(clouds?.Length ?? 0, end_poses?.Length ?? 0);
        for (int i = 0; i < cnt; i++)
            if (clouds[i]) StartCoroutine(MoveToXY(clouds[i].transform, end_poses[i], cloudFastSpeed));

        // 2) 비 시작
        if (rain)
        {
            rain.gameObject.SetActive(true);
            rain.Clear(true);
            rain.Play(true);
        }

        // 3) 해 끄고, global 어둡게
        StartCoroutine(FadeLights(sunTo: 0f, globalTo: globalDimIntensity, duration: sunFadeSeconds));

        // 4) 번개 스케줄 시작
        StartScheduler();
    }

    public void StartScheduler()
    {
        if (running) return;
        running = true;
        loopCo = StartCoroutine(Loop());
    }

    public void StopScheduler()
    {
        if (!running) return;
        running = false;

        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }

        // 진행 중 번개 모두 강제 종료 후 반납
        for (int i = actives.Count - 1; i >= 0; i--)
            if (actives[i]) actives[i].ForceStopAndReturn();
        actives.Clear();

        // 1) 클라우드들을 start_poses로 복귀
        int cnt = Mathf.Min(clouds?.Length ?? 0, start_poses?.Length ?? 0);
        for (int i = 0; i < cnt; i++)
            if (clouds[i]) StartCoroutine(MoveToXY(clouds[i].transform, start_poses[i], cloudFastSpeed));

        // 2) 비 멈춤
        if (rain) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 3) 해 켜고, global 밝게
        StartCoroutine(FadeLights(sunTo: 1f, globalTo: globalBrightIntensity, duration: sunFadeSeconds));
    }

    IEnumerator Loop()
    {
        while (running)
        {
            float wait = Random.Range(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(wait);
            if (!running) break;

            if (actives.Count >= maxConcurrent) continue;

            var th = GetThunder();
            th.PlayAt(RandomPoint(), warnDuration, warnBlinkHz, strikeDuration);
        }
    }

    Vector3 RandomPoint()
    {
        if (!areaCenter) return Vector3.zero;
        var half = areaSize * 0.5f;
        var local = new Vector3(Random.Range(-half.x, half.x), 0f, 0f);
        return areaCenter.TransformPoint(local);
    }

    IEnumerator MoveToXY(Transform tr, Vector2 targetXY, float speed)
    {
        Vector3 target = new Vector3(targetXY.x, targetXY.y, tr.position.z);
        while (Vector3.Distance(tr.position, target) > 0.01f)
        {
            tr.position = Vector3.MoveTowards(tr.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator FadeLights(float sunTo, float globalTo, float duration)
    {
        if (duration <= 0f)
        {
            if (sun) sun.intensity = sunTo;
            if (global) global.intensity = globalTo;
            yield break;
        }

        float t = 0f;
        float sunFrom = sun ? sun.intensity : 0f;
        float globalFrom = global ? global.intensity : 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            if (sun) sun.intensity = Mathf.Lerp(sunFrom, sunTo, k);
            if (global) global.intensity = Mathf.Lerp(globalFrom, globalTo, k);

            yield return null;
        }

        if (sun) sun.intensity = sunTo;
        if (global) global.intensity = globalTo;
    }

    // === Runtime Setters (GameManager가 호출) ===
    public void SetIntervals(Vector2 newRange)
    {
        float min = Mathf.Max(0.05f, newRange.x);
        float max = Mathf.Max(min + 0.05f, newRange.y);
        intervalRange = new Vector2(min, max);
    }

    public void SetMaxConcurrent(int count)
    {
        maxConcurrent = Mathf.Clamp(count, 1, 10);
    }

    public void SetWarnDuration(float seconds)
    {
        warnDuration = Mathf.Clamp(seconds, 0.05f, 2f);
    }

    public void SetStrikeDuration(float seconds)
    {
        strikeDuration = Mathf.Clamp(seconds, 0.05f, 0.6f);
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!areaCenter) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.matrix = areaCenter.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.1f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.1f));
    }
#endif
}
