using System.Collections;
using UnityEngine;

public class Mosquito : MonoBehaviour
{
    private bool moving;

    public void FlyAcross(float dir, float speed, float endX)
    {
        if (moving) return;
        StartCoroutine(Fly(dir, speed, endX));
    }

    IEnumerator Fly(float dir, float speed, float endX)
    {
        moving = true;

        Vector3 pos = transform.position;

        // 방향에 따라 스프라이트 반전 (선택 사항)
        transform.rotation = dir < 0f ? transform.rotation = Quaternion.Euler(0f, 180f, 0f) : Quaternion.Euler(0f, 0f, 0f);

        while (moving)
        {
            pos.x += dir * speed * Time.deltaTime;
            transform.position = pos;

            if ((dir > 0f && pos.x >= endX) || (dir < 0f && pos.x <= endX))
                break;

            yield return null;
        }

        Destroy(gameObject);
    }
}
