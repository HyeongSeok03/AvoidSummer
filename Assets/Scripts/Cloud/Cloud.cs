using System.Collections;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    [SerializeField] float defaultMoveSpeed = 3f;
    [SerializeField] float defaultFadeSpeed = 20f;

    Transform leftPos;
    Transform rightPos;

    Coroutine moveCo;
    bool isMoving;
    Vector3 targetPos;
    float currentSpeed; // 이동 중 속도(Spawner에서 실시간 변경 반영)

    public void ConfigureAnchors(Transform left, Transform right)
    {
        leftPos = left;
        rightPos = right;
    }

    /// <summary> 좌/우 가장자리에서 반대편으로 즉시 출발 (dir=+1: 왼→오, -1: 오→왼) </summary>
    public void Launch(int dir, float speed)
    {
        if (!leftPos || !rightPos) { Debug.LogWarning("[Cloud] Anchors not set."); return; }
        if (moveCo != null) StopCoroutine(moveCo);

        float y = transform.position.y, z = transform.position.z;

        Vector3 start = (dir >= 1)
            ? new Vector3(leftPos.position.x, y, z)
            : new Vector3(rightPos.position.x, y, z);
        targetPos = (dir >= 1)
            ? new Vector3(rightPos.position.x, y, z)
            : new Vector3(leftPos.position.x, y, z);

        transform.position = start;

        currentSpeed = (speed > 0f) ? speed : defaultMoveSpeed;
        moveCo = StartCoroutine(MoveToTarget());
    }

    /// <summary> ★ 현재 위치에서 지정 방향 가장자리로 이동 (초기 1회용) </summary>
    public void LaunchFromHere(int dir, float speed)
    {
        if (!leftPos || !rightPos) { Debug.LogWarning("[Cloud] Anchors not set."); return; }
        if (moveCo != null) StopCoroutine(moveCo);

        float y = transform.position.y, z = transform.position.z;

        targetPos = (dir >= 1)
            ? new Vector3(rightPos.position.x, y, z)
            : new Vector3(leftPos.position.x, y, z);

        currentSpeed = (speed > 0f) ? speed : defaultMoveSpeed;
        moveCo = StartCoroutine(MoveToTarget());
    }

    /// <summary> 현재 위치에서 가까운 가장자리로 빠르게 이탈 </summary>
    public void StopAndFadeOut(float fadeSpeed = -1f)
    {
        if (!leftPos || !rightPos) return;
        if (moveCo != null) StopCoroutine(moveCo);

        float y = transform.position.y, z = transform.position.z;
        Vector3 toLeft = new Vector3(leftPos.position.x, y, z);
        Vector3 toRight = new Vector3(rightPos.position.x, y, z);
        targetPos = (Vector3.Distance(transform.position, toLeft) < Vector3.Distance(transform.position, toRight))
            ? toLeft : toRight;

        currentSpeed = (fadeSpeed > 0f) ? fadeSpeed : defaultFadeSpeed;
        moveCo = StartCoroutine(MoveToTarget());
    }

    IEnumerator MoveToTarget()
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);
            yield return null;
        }
        isMoving = false;
        moveCo = null;
    }

    /// <summary> 이동 중 속도를 즉시 갱신(Spawner가 호출) </summary>
    public void SetMoveSpeed(float speed)
    {
        currentSpeed = speed;
    }

    public bool IsMoving => isMoving;
}
