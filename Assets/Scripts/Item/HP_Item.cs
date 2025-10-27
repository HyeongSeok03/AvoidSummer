using UnityEngine;

public class HP_Item : PooledObject
{
    public int heal;
    public override void Collect()
    {
        GameManager.Instance.player.Healed(heal);
        Despawn();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Collect();
    }
}
