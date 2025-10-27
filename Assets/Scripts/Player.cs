using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public int HP => hp;
    public int MaxHP => maxHp;
    public int Exp => exp;
    public int MaxExp => maxExp;
    public int Level => level;

    [Header("Status")]
    [SerializeField] private int hp;
    [SerializeField] private int maxHp;
    [SerializeField] private int exp;
    [SerializeField] private int maxExp;
    [SerializeField] private int level;

    [Header("Upgrade Runtime")]
    [SerializeField] private float speedMultiplier = 1f;   // 이속 퍼센트 누적 (1.0 → 1.25 → 1.5 …)
    [SerializeField] private float jumpMultiplier = 1f;
    [SerializeField] private float regenPerSec = 0f;       // 체력 재생(초당)
    [SerializeField] private float _regenAcc = 0f;

    public void AddSpeedMultiplier(float speedRate)
    {
        speedMultiplier += speedRate;
    }
    public void AddJumpMultiplier(float jumpRate)
    {
        jumpMultiplier += jumpRate;
    }

    [Header("Refs")]
    public GameObject eyes;
    public Transform shadow;
    public GameObject cloudPet;

    [Header("Move")]
    private float moveSpeed = 3f;       // 지상 기본 이동속도
    float moveX;

    [Header("Jump")]

    public float jumpSpeed = 10f;
    public int maxJumps = 2;                 // ✅ 총 점프 가능 횟수 (2=더블, 3=트리플)
    public float groundY = -3.5f;            // 착지 판단 y
    public float airJumpHighMultiplier = 1.3f; // ✅ A/D 안 누르면 2번째부터 높이 점프 배수
    int jumpsRemaining;
    bool canJump;

    public void ResetJump()
    {
        jumpsRemaining = Mathf.Max(1, maxJumps);
        isDashing = false;
        dashTimer = 0;
    }

    [Header("Air Dash (2번째 점프부터)")]
    public float dashSpeed = 12f;   // ✅ 대시 수평 속도
    public float dashDuration = 0.15f; // ✅ 대시 유지 시간(초)
    bool isDashing;
    float dashTimer;
    int dashDir; // -1 or +1

    private bool underCloud;
    public Stack<int> cloud_stack = new Stack<int>();

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        ResetJump();
        maxHp = 100;
        hp = maxHp;
    }

    bool IsGround => transform.position.y < groundY;

    void FixedUpdate()
    {
        // 착지 시 리셋
        if (IsGround && rb.linearVelocityY == 0f)
        {
            ResetJump();
        }

        rb.linearVelocityX = moveX * moveSpeed * speedMultiplier;

        if (canJump)
        {
            rb.linearVelocityY = jumpSpeed * jumpMultiplier;
            jumpsRemaining--;
        }
        canJump = false;

        // // 대시 중 수평 속도 유지 / 아니면 일반 이동
        // if (isDashing)
        // {
        //     dashTimer -= Time.fixedDeltaTime;
        //     rb.linearVelocityX = dashDir * dashSpeed;
        //     if (dashTimer <= 0f) isDashing = false;
        // }
        // else
        // {
        //     rb.linearVelocityX = moveX * speed * speedMultiplier;
        // }
        // // 점프 처리
        // if (canJump)
        // {
        //     bool isFirstJump = (jumpsRemaining == maxJumps);

        //     float vy = jumpspeed;

        //     if (!isFirstJump)
        //     {
        //         // 두 번째 점프부터: A/D(좌우 입력) 누르고 있으면 대시점프
        //         if (moveX != 0)
        //         {
        //             StartDash(moveX);
        //             // 수직 속도는 기본 jumpspeed 유지
        //         }
        //         else
        //         {
        //             // 좌우 입력이 없으면 더 높이 점프
        //             vy = jumpspeed * airJumpHighMultiplier;
        //         }
        //     }

        //     rb.linearVelocityY = vy;
        //     jumpsRemaining--;
        // }
        // canJump = false;

        // // 눈(시선) 방향
        eyes.transform.localPosition = new Vector2(moveX * 0.1f, 0);
        // 그림자 x만 따라가게
        shadow.position = new Vector2(transform.position.x, shadow.transform.position.y);
    }

    void OnMove(InputValue value)
    {
        moveX = value.Get<float>();
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && jumpsRemaining > 0)
            canJump = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Cloud"))
            cloud_stack.Push(1);

        if (collision.CompareTag("Thunder"))
            DamagedByElectric(50);

        if (collision.CompareTag("Mosquito"))
        {
            Damaged(10);
            rb.linearVelocityY = 2f;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Cloud") && cloud_stack.Count > 0)
            cloud_stack.Pop();
    }

    public void Experienced(int exp)
    {
        this.exp += exp;
        if (this.exp >= maxExp)
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        exp -= maxExp;
        level++;
        maxExp *= 2;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerLevelUp(this);
        }
    }

    public void Healed(int heal)
    {
        hp = Mathf.Clamp(hp + heal, 0, maxHp);
    }

    public void Damaged(int damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            hp = 0;
            GameManager.Instance.GameOver();
        }
    }

    public void IncreaseMaxHPAndHeal(int addMax, int healAlso)
    {
        maxHp += addMax;
        Healed(healAlso); // 상한 반영하여 회복
    }

    public void AddMoveSpeedPercent(float add)  // add=0.25 → 25% 증가
    {
        speedMultiplier += add;
    }

    public void AddRegen(float addPerSec)       // addPerSec=5 → 5/초
    {
        regenPerSec += addPerSec;
    }

    public void IncreaseMaxJumps(int add)
    {
        maxJumps += add;
        // 필요하면 현재 남은 점프도 보정:
        // jumpsRemaining = Mathf.Clamp(jumpsRemaining + add, 1, maxJumps);
    }




    void Update()
    {
        if (regenPerSec > 0f && hp < maxHp)
        {
            _regenAcc += regenPerSec * Time.deltaTime;
            if (_regenAcc >= 1f)
            {
                int heal = Mathf.FloorToInt(_regenAcc);
                _regenAcc -= heal;
                Healed(heal);
            }
        }
    }

    void StartDash(float inputX)
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashDir = inputX > 0 ? 1 : -1;
        rb.linearVelocityX = dashDir * dashSpeed;
    }

    [SerializeField] private float sunResist = 1f;   // ☀️ 햇볕 피해 배수(누적 곱): 0.9면 10%감소
    [SerializeField] private float elecResist = 1f;  // ⚡ 번개 피해 배수(누적 곱)
    [SerializeField] private bool hasCloudPet = false;
    public bool HasCloudPet => hasCloudPet;

    public void AddSunResistance(float mul) { sunResist *= Mathf.Clamp(mul, 0.0f, 1.0f); }
    public void AddElecResistance(float mul) { elecResist *= Mathf.Clamp(mul, 0.0f, 1.0f); }
    public void GrantCloudPet() { hasCloudPet = true; }

    // 햇볕/번개 전용 데미지 적용
    public void DamagedBySun(int baseDamage)
    {
        int finalDmg = Mathf.Max(0, Mathf.CeilToInt(baseDamage * sunResist));
        Damaged(finalDmg);
    }

    public void DamagedByElectric(int baseDamage)
    {
        int finalDmg = Mathf.Max(0, Mathf.CeilToInt(baseDamage * elecResist));
        Damaged(finalDmg);
    }
}
