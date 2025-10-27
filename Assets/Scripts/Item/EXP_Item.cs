using UnityEngine;

public class EXP_Item : PooledObject
{
    int exp = 1;
    public override void Collect()
    {
        GameManager.Instance.player.Experienced(exp);
        Despawn();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Collect();
    }
}
