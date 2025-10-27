using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MosquitoScheduler : MonoBehaviour
{
    [Header("Mosquito Prefab")]
    [SerializeField] private Mosquito mosquitoPrefab;

    [Header("Anchors")]
    [SerializeField] private Transform leftEdge;
    [SerializeField] private Transform rightEdge;
    [SerializeField] private float minHeight = -1f;
    [SerializeField] private float maxHeight = 3f;

    [Header("Timing")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(3f, 7f);

    [Header("Speed")]
    [SerializeField] private float baseSpeed = 4f;   // 기본 속도
    [SerializeField] private float speedIncreaseRate = 0.1f; // 페이즈당 +10%

    [Header("Behaviour")]
    [SerializeField] private bool autoStart = true;

    private bool running;
    private Coroutine schedulerCo;

    void Start()
    {
        if (autoStart) StartScheduler();
    }

    public void StartScheduler()
    {
        if (running) return;
        running = true;
        schedulerCo = StartCoroutine(MosquitoLoop());
    }

    public void StopScheduler()
    {
        if (!running) return;
        running = false;

        if (schedulerCo != null)
        {
            StopCoroutine(schedulerCo);
            schedulerCo = null;
        }
    }

    public void ToggleScheduler(bool on)
    {
        if (on) StartScheduler();
        else StopScheduler();
    }

    IEnumerator MosquitoLoop()
    {
        while (running)
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                yield return null;
                continue;
            }

            // ☁️ 2페이즈 이상일 때만 등장
            if (GameManager.Instance.phaseIndex >= 1 && gm.weather_rain == false)
            {
                // 🔸 페이즈가 높을수록 출현 개수 증가
                int minSpawn = Mathf.Max(1, GameManager.Instance.phaseIndex - 1);
                int maxSpawn = Mathf.Min(8, minSpawn + 2);
                int spawnCount = Random.Range(minSpawn, maxSpawn + 1);

                // 🔸 속도 증가 (페이즈당 +10%)
                float mosquitoSpeed = baseSpeed * (1f + (GameManager.Instance.phaseIndex - 1) * speedIncreaseRate);
                mosquitoSpeed = Mathf.Min(mosquitoSpeed, baseSpeed * 2f); // 최대 2배 제한

                for (int i = 0; i < spawnCount; i++)
                {
                    float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
                    yield return new WaitForSeconds(wait);
                    SpawnMosquito(mosquitoSpeed);
                }

                // CloudPhase 끝나면 ThunderPhase까지 대기
                yield return new WaitUntil(() => gm.weather_rain == true);
            }
            else
            {
                yield return null;
            }
        }
    }

    void SpawnMosquito(float speed)
    {
        if (mosquitoPrefab == null) return;

        bool fromLeft = Random.value < 0.5f;
        float randomY = Random.Range(minHeight, maxHeight);

        Vector3 startPos = fromLeft
            ? new Vector3(leftEdge.position.x, randomY, 0)
            : new Vector3(rightEdge.position.x, randomY, 0);

        Mosquito m = Instantiate(mosquitoPrefab, startPos, Quaternion.identity, transform);

        float dir = fromLeft ? +1f : -1f;
        m.FlyAcross(dir, speed, fromLeft ? rightEdge.position.x : leftEdge.position.x);
    }
}
