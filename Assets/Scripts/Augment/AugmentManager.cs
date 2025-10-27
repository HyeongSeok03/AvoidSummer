using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AugmentManager : MonoBehaviour
{
    public Player player;
    public Sprite[] sprites;
    private List<Upgrade> allAugments;
    private List<Upgrade> currentChoices;
    private List<Upgrade> playerAugments;
    public List<Upgrade> GetPlayerAugments() => new List<Upgrade>(playerAugments);


    private MovSpdUp oldSpeedBoots, strongSpeedBoots;
    private JmpSpdUp oldSpringBoots, strongSpringBoots;
    private SunResUp sunscreen, bronzeSkin;
    private DiceUp silverDice, goldDice;
    private MaxJmpUp doubleJump, tripleJump, quadraJump;
    private ElecResUp rubberMan;
    private CloudPetUp cloudKeeper;

    void Start()
    {
        playerAugments = new List<Upgrade>();
        LoadAllAugments();
    }
    public Color[] tierColor;
    public GameObject panel;
    public Button[] buttons;
    public Image[] currentSprites;
    public Text[] titles;
    public Text[] descriptions;

    public void OnLevelUp()
    {
        Setup();
        panel.SetActive(true);
        GameManager.Instance.PauseGame();
    }

    public void Setup()
    {
        ShowUpgradeChoices();
        for (int i = 0; i < currentChoices.Count; i++)
        {
            buttons[i].image.color = tierColor[(int)currentChoices[i].tier];
            currentSprites[i].sprite = currentChoices[i].sprite;
            titles[i].text = currentChoices[i].name;
            descriptions[i].text = currentChoices[i].description;
        }
    }

    void LoadAllAugments()
    {
        oldSpeedBoots = new MovSpdUp(sprites[0], AugmentTier.bronze, "낡은 이속 장화", "이동 속도가\n10퍼센트 향상됩니다.", 10, 0.1f);
        oldSpringBoots = new JmpSpdUp(sprites[1], AugmentTier.bronze, "낡은 스프링 장화", "점프력이\n10퍼센트 향상됩니다.", 10, 0.1f);
        sunscreen = new SunResUp(sprites[2], AugmentTier.bronze, "선크림", "햇빛 데미지가\n10퍼센트 감소됩니다.", 10, 0.9f);
        silverDice = new DiceUp(sprites[3], AugmentTier.bronze, "은빛 주사위", "무작위 실버 업그레이드가\n선택됩니다.", 9);
        strongSpeedBoots = new MovSpdUp(sprites[4], AugmentTier.silver, "튼튼한 이속 장화", "이동 속도가\n20퍼센트 향상됩니다.", 8, 0.2f);
        strongSpringBoots = new JmpSpdUp(sprites[5], AugmentTier.silver, "튼튼한 스프링 장화", "점프력이\n20퍼센트 향상됩니다.", 8, 0.2f);
        doubleJump = new MaxJmpUp(sprites[6], AugmentTier.silver, "바람을 타는자", "2단 점프가\n가능해집니다.", 8, 2, null);
        goldDice = new DiceUp(sprites[7], AugmentTier.silver, "금빛 주사위", "무작위 골드 업그레이드가\n선택됩니다.", 7);
        tripleJump = new MaxJmpUp(sprites[8], AugmentTier.gold, "구름을 밝는 자", "3단 점프가\n가능해집니다.", 6, 3, doubleJump);
        rubberMan = new ElecResUp(sprites[9], AugmentTier.gold, "고무인간", "번개 데미지가\n20퍼센트 감소합니다.", 6, 0.8f);
        bronzeSkin = new SunResUp(sprites[10], AugmentTier.gold, "구릿빛 피부", "햇빛 데미지가\n20퍼센트 감소합니다.", 6, 0.8f);
        quadraJump = new MaxJmpUp(sprites[11], AugmentTier.special, "하늘을 나는 자", "4단 점프가\n가능해집니다.", 3, 4, tripleJump);
        cloudKeeper = new CloudPetUp(sprites[12], AugmentTier.special, "구름 사육사", "작은 구름이\n플레이어를 따라다닙니다.", 3);

        allAugments = new List<Upgrade>()
        {
            oldSpeedBoots, oldSpringBoots, sunscreen, silverDice, strongSpeedBoots, strongSpringBoots, doubleJump, goldDice, tripleJump, rubberMan, bronzeSkin, quadraJump, cloudKeeper
        };
    }

    public void ShowUpgradeChoices()
    {
        var available = allAugments.Where(a => a.IsUnlocked(playerAugments)).ToList();
        currentChoices = WeightedRandomSelect(available, 3);
    }

    List<Upgrade> WeightedRandomSelect(List<Upgrade> candidates, int count)
    {
        List<Upgrade> result = new List<Upgrade>();

        for (int i = 0; i < count; i++)
        {
            float totalWeight = candidates.Sum(a => a.weight);
            float roll = Random.Range(0f, totalWeight);

            float cumulative = 0f;
            foreach (var aug in candidates)
            {
                cumulative += aug.weight;
                if (roll <= cumulative)
                {
                    result.Add(aug);
                    candidates.Remove(aug);
                    break;
                }
            }
        }
        return result;
    }

    public List<Upgrade> GetAvailableAugmentsOfTier(AugmentTier tier)
    {
        // 현재 잠금 조건(IsUnlocked) 충족하면서, 아직 안 가진 동일 티어만
        return allAugments
            .Where(a => a.tier == tier)
            .Where(a => a.IsUnlocked(playerAugments))
            .Where(a => !playerAugments.Contains(a))
            .ToList();
    }


    public void GrantAugment(Upgrade aug)
    {
        if (aug == null) return;
        aug.Apply(player);
        playerAugments.Add(aug);
    }

    public void SelectUpgrade(int index)
    {
        Upgrade chosen = currentChoices[index];
        chosen.Apply(player);
        playerAugments.Add(chosen);
        panel.SetActive(false);
        GameManager.Instance.ResumeGame();
    }

    [ContextMenu("test showupgradechoices")]
    public void TestShowUpgradeChoices()
    {
        var available = allAugments.Where(a => a.IsUnlocked(playerAugments)).ToList();
        currentChoices = WeightedRandomSelect(available, 3);
        if (currentChoices.Count == 3)
            print(currentChoices[0].name + ", " + currentChoices[1].name + ", " + currentChoices[2].name);
        else
            print("currentChoices count is not 3");
    }
    [ContextMenu("test select first upgrade")]
    public void TestSelectUpgrade()
    {
        if (currentChoices.Count != 0)
        {
            Upgrade chosen = currentChoices[0];
            chosen.Apply(player);
            playerAugments.Add(chosen);
            print(chosen.name);
        }
        else
        {
            print("current augments is null.");
        }
    }
}
