using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudScheduler : MonoBehaviour
{
    [Header("Targets (씬의 4개 Cloud)")]
    [SerializeField] private Cloud[] clouds = new Cloud[4];

    [Header("Anchors")]
    [SerializeField] private Transform leftPos;
    [SerializeField] private Transform rightPos;

    [Header("Timing")]
    [SerializeField] private Vector2 intervalRange = new Vector2(1f, 8f); // 출발 간 랜덤 대기시간

    [Header("Speed")]
    [SerializeField] public float cloudSpeed = 3f; // ✅ 전 구름 공통 속도
    [SerializeField] private float fadeOutSpeed = 20f;

    [Header("Behaviour")]
    [SerializeField] private bool autoStart = true;

    readonly List<Coroutine> schedulers = new();
    bool running;

    void Awake()
    {
        foreach (var c in clouds)
            if (c) c.ConfigureAnchors(leftPos, rightPos);
    }

    void Start()
    {
        if (autoStart) StartScheduler();
    }

    // === Public API ===
    public void StartScheduler()
    {
        if (running) return;
        running = true;

        for (int i = 0; i < clouds.Length; i++)
        {
            var c = clouds[i];
            if (!c) continue;

            bool firstLeftNow = (i == 0); // ★ clouds[0]만 처음에 가운데→왼쪽
            schedulers.Add(StartCoroutine(CloudLoop(c, firstLeftNow)));
        }
    }

    public void StopScheduler()
    {
        if (!running) return;
        running = false;

        foreach (var co in schedulers) if (co != null) StopCoroutine(co);
        schedulers.Clear();

        foreach (var c in clouds) if (c) c.StopAndFadeOut(fadeOutSpeed);
    }

    public void ToggleScheduler(bool on)
    {
        if (on) StartScheduler();
        else StopScheduler();
    }

    /// <summary> 런타임 속도 변경(이동 중인 구름에도 즉시 반영) </summary>
    public void SetCloudSpeed(float newSpeed)
    {
        cloudSpeed = Mathf.Max(0.01f, newSpeed);
        foreach (var c in clouds)
            if (c) c.SetMoveSpeed(cloudSpeed);
    }

    // === 내부 루프 ===
    IEnumerator CloudLoop(Cloud c, bool firstLeftNow)
    {
        // ★ 처음 한 번: 현재 위치에서 왼쪽으로 즉시 출발
        if (firstLeftNow)
        {
            c.LaunchFromHere(dir: -1, speed: cloudSpeed);
            yield return new WaitUntil(() => !c.IsMoving || !running);
        }

        // 이후 일반 스케줄
        while (running)
        {
            float wait = Random.Range(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(wait);
            if (!running) break;

            int dir = (Random.value < 0.5f) ? +1 : -1;   // 왼→오 or 오→왼
            c.Launch(dir, speed: cloudSpeed);

            yield return new WaitUntil(() => !c.IsMoving || !running);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!leftPos || !rightPos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(leftPos.position, 0.1f);
        Gizmos.DrawSphere(rightPos.position, 0.1f);
        Gizmos.DrawLine(leftPos.position, rightPos.position);
    }
#endif
}
