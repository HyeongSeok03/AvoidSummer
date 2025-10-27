using System.Collections;
using UnityEngine;

public class Thunder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject warning;   // 예고 오브젝트(스프라이트/아이콘 등)
    [SerializeField] GameObject body;      // 실제 번개 비주얼

    Coroutine playCo;
    System.Action<Thunder> returnToPool;

    void Awake()
    {
        if (warning) warning.SetActive(false);
        if (body) body.SetActive(false);
    }

    public void Setup(System.Action<Thunder> onReturn)
    {
        returnToPool = onReturn;
    }

    public void PlayAt(Vector3 worldPos, float warnDuration, float warnBlinkHz, float strikeDuration)
    {
        transform.position = worldPos;
        if (playCo != null) StopCoroutine(playCo);
        playCo = StartCoroutine(PlayOnce(warnDuration, warnBlinkHz, strikeDuration));
    }

    public void ForceStopAndReturn()
    {
        if (playCo != null) StopCoroutine(playCo);
        if (warning) warning.SetActive(false);
        if (body) body.SetActive(false);
        returnToPool?.Invoke(this);
    }

    IEnumerator PlayOnce(float warnDuration, float warnBlinkHz, float strikeDuration)
    {
        // 예고 깜빡임
        if (warning) warning.SetActive(true);
        if (body) body.SetActive(false);

        float t = 0f;
        float step = (warnBlinkHz <= 0f) ? warnDuration : 0.5f / warnBlinkHz; // on/off 한 사이클
        bool vis = true;

        while (t < warnDuration)
        {
            vis = !vis;
            if (warning) warning.SetActive(vis);
            float dt = Mathf.Min(step, warnDuration - t);
            t += dt;
            yield return new WaitForSeconds(dt);
        }
        if (warning) warning.SetActive(false);

        // 본 번개
        if (body) body.SetActive(true);
        yield return new WaitForSeconds(strikeDuration);
        if (body) body.SetActive(false);

        // 풀로 반환
        returnToPool?.Invoke(this);
        playCo = null;
    }
}
