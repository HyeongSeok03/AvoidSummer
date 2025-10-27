using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public enum AugmentTier { bronze, silver, gold, special }

public abstract class Upgrade
{
    public Sprite sprite { get; private set; }
    public AugmentTier tier { get; private set; }
    public string name { get; private set; }
    public string description { get; private set; }
    public float weight { get; private set; }  // 등장 확률 가중치

    public Upgrade(Sprite sprite, AugmentTier tier, string name, string description, float weight)
    {
        this.sprite = sprite;
        this.tier = tier;
        this.name = name;
        this.description = description;
        this.weight = weight;
    }

    public virtual bool IsUnlocked(List<Upgrade> playerAugments) => true;
    public abstract void Apply(Player player);
}

public class MovSpdUp : Upgrade
{
    float speedRate;

    public MovSpdUp(Sprite sprite, AugmentTier tier, string name, string description, float weight, float speedRate) : base(sprite, tier, name, description, weight)
    {
        this.speedRate = speedRate;
    }

    public override void Apply(Player player)
    {
        player.AddSpeedMultiplier(speedRate);
    }
}

public class JmpSpdUp : Upgrade
{
    float jumpRate;
    public JmpSpdUp(Sprite sprite, AugmentTier tier, string name, string description, float weight, float jumpRate) : base(sprite, tier, name, description, weight)
    {
        this.jumpRate = jumpRate;
    }

    public override void Apply(Player player)
    {
        player.AddJumpMultiplier(jumpRate);
    }
}

public class SunResUp : Upgrade
{
    float resistance;
    public SunResUp(Sprite sprite, AugmentTier tier, string name, string description, float weight, float resistance) : base(sprite, tier, name, description, weight)
    {
        this.resistance = resistance;
    }

    public override void Apply(Player player)
    {
        player.AddSunResistance(resistance);
    }
}

public class ElecResUp : Upgrade
{
    float resistance;
    public ElecResUp(Sprite sprite, AugmentTier tier, string name, string description, float weight, float resistance) : base(sprite, tier, name, description, weight)
    {
        this.resistance = resistance;
    }

    public override void Apply(Player player)
    {
        player.AddElecResistance(resistance);
    }
}

public class DiceUp : Upgrade
{
    public DiceUp(Sprite sprite, AugmentTier tier, string name, string description, float weight) : base(sprite, tier, name, description, weight)
    {
    }

    public override void Apply(Player player)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.augmentManager == null) return;

        // 이름으로 타깃 티어 판별: "은빛 주사위" -> silver, "금빛 주사위" -> gold
        AugmentTier targetTier = (name.Contains("금")) ? AugmentTier.gold : AugmentTier.silver;

        var pool = gm.augmentManager.GetAvailableAugmentsOfTier(targetTier);
        if (pool.Count == 0) return;

        // 가중치로 뽑아도 되지만 단순 균등 랜덤으로 선택
        var pick = pool[Random.Range(0, pool.Count)];

        // 효과 적용 + 보유 목록에 기록
        gm.augmentManager.GrantAugment(pick);
    }
}

public class MaxJmpUp : Upgrade
{
    int maxJump;
    MaxJmpUp preUpgrade;

    public MaxJmpUp(Sprite sprite, AugmentTier tier, string name, string description, float weight, int maxJump, MaxJmpUp preUpgrade) : base(sprite, tier, name, description, weight)
    {
        this.maxJump = maxJump;
        this.preUpgrade = preUpgrade;
    }

    public override bool IsUnlocked(List<Upgrade> playerAugments)
    {
        bool isOwned = playerAugments.All(a => a != this);
        if (preUpgrade == null)
        {
            return isOwned;
        }
        bool condition = playerAugments.Any(a => a == preUpgrade);
        return isOwned && condition;
    }

    public override void Apply(Player player)
    {
        player.maxJumps = maxJump;
    }
}

public class CloudPetUp : Upgrade
{
    public CloudPetUp(Sprite sprite, AugmentTier tier, string name, string description, float weight) : base(sprite, tier, name, description, weight)
    {
    }

    public override bool IsUnlocked(List<Upgrade> playerAugments)
    {
        bool isOwned = playerAugments.All(a => a != this);

        return isOwned;
    }

    public override void Apply(Player player)
    {
        player.cloudPet.SetActive(true);
    }
}