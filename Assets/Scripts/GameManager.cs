using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    public Player player;
    public CloudScheduler cloudScheduler;
    public ThunderScheduler thunderScheduler;
    public MosquitoScheduler mosquitoScheduler;
    public AugmentManager augmentManager;

    [Header("Cycle (sec)")]
    [SerializeField] float cloudDuration = 40f;   // 구름 단계
    [SerializeField] float thunderDuration = 20f; // 번개/비 단계

    [Header("Cloud Speed")]
    [SerializeField] float speedMin = 1f;           // 단계 시작 속도
    [SerializeField] float speedMax = 5f;           // 단계 최대 속도
    [SerializeField] float speedRampPerSec = 0.01f; // 초당 가속량

    // === 난이도 & 경험치 ===
    [Header("Difficulty (0→1 over time)")]
    [SerializeField] float difficultyFullTime = 240f; // 이 시간에 도달하면 최대 난이도(1)

    // 번개 파라미터 곡선( difficulty01 ∈ [0,1] → 값 )
    [SerializeField] AnimationCurve thunderIntervalMinCurve = AnimationCurve.EaseInOut(0f, 2.0f, 1f, 0.35f);
    [SerializeField] AnimationCurve thunderIntervalMaxCurve = AnimationCurve.EaseInOut(0f, 4.0f, 1f, 1.00f);
    [SerializeField] AnimationCurve thunderWarnDurationCurve = AnimationCurve.EaseInOut(0f, 0.80f, 1f, 0.25f);
    [SerializeField] AnimationCurve thunderStrikeDurationCurve = AnimationCurve.EaseInOut(0f, 0.12f, 1f, 0.25f);
    [SerializeField] AnimationCurve thunderConcurrentCurve = AnimationCurve.Linear(0f, 3f, 1f, 7f);

    [Header("XP Reward")]
    [SerializeField] int xpStart = 40; // 시작 경험치
    [SerializeField] int xpMin = 5;    // 최소 경험치(바닥)
    public int CurrentXP { get; private set; } // 현재 지급 경험치
    public float Difficulty01 { get; private set; } // 외부 참조용(0~1)

    // 외부에서 참고할 수 있게 그대로 둠 (Thunder 단계에서 true)
    public bool weather_rain;

    // 상태/시간
    bool game_start;
    public float game_time;   // 전체 플레이 타임
    float phaseTime;          // 현재 단계 경과 시간

    // 햇볕 데미지용
    bool underCloud;
    bool first_heat;          // 햇볕 노출 전환 직후 1회 데미지 플래그
    float heat_timer;         // 주기 데미지 타이머

    int sunDamage = 5;

    public int phaseIndex { get; private set; } = 1;


    enum WeatherPhase { Cloud, Thunder }
    WeatherPhase phase;

    Coroutine cycleCo;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
        game_start = true;

        // 시작은 Cloud 단계로
        EnterCloudPhase();
        cycleCo = StartCoroutine(WeatherCycle());

        // 초기 난이도/경험치 적용
        ApplyDifficultyAndXP();
    }

    void OnDisable()
    {
        if (cycleCo != null) StopCoroutine(cycleCo);
    }

    IEnumerator WeatherCycle()
    {
        var waitCloud = new WaitForSeconds(cloudDuration);
        var waitThunder = new WaitForSeconds(thunderDuration);

        while (true)
        {
            yield return waitCloud;     // 40초
            Background_sky.color = sunlightGradient.Evaluate(0);
            EnterThunderPhase();

            yield return waitThunder;   // 20초
            EnterCloudPhase();

            phaseIndex++;
        }
    }

    void EnterCloudPhase()
    {
        phase = WeatherPhase.Cloud;
        weather_rain = false;
        phaseTime = 0f;

        // Thunder 정지 + Cloud 재개
        if (thunderScheduler) thunderScheduler.StopScheduler();
        if (cloudScheduler) cloudScheduler.StartScheduler();
        if (mosquitoScheduler) mosquitoScheduler.StartScheduler();

        // 햇볕 데미지 타이밍 초기화
        first_heat = false;
        heat_timer = 0f;

        // 속도는 단계 시간에 따라 상승 → 시작 속도로 초기화
        if (cloudScheduler) cloudScheduler.cloudSpeed = speedMin;
    }

    void EnterThunderPhase()
    {
        phase = WeatherPhase.Thunder;
        weather_rain = true;
        phaseTime = 0f;

        // Cloud 정지 + 비/번개 시작
        if (cloudScheduler) cloudScheduler.StopScheduler();
        if (mosquitoScheduler) mosquitoScheduler.StopScheduler();
        if (thunderScheduler)
        {
            thunderScheduler.StartRain();
            thunderScheduler.StartScheduler();
        }

        // Thunder 단계에서는 햇볕 데미지 없음
        first_heat = false;
        heat_timer = 0f;
    }

    public Text text;

    void Update()
    {
        if (!game_start) return;

        float dt = Time.deltaTime;
        game_time += dt;
        phaseTime += dt;
        text.text = (Mathf.Round(game_time * 10) * 0.1f).ToString();

        sunDamage = 5 + (phaseIndex - 1) * 2;

        // Cloud 단계: 햇볕 데미지
        if (phase == WeatherPhase.Cloud)
            SunDamaged(dt);

        // Cloud 속도 램프업
        if (phase == WeatherPhase.Cloud && cloudScheduler)
        {
            float target = Mathf.Min(speedMax, speedMin + phaseTime * speedRampPerSec);
            if (Mathf.Abs(cloudScheduler.cloudSpeed - target) > 0.0001f)
                cloudScheduler.cloudSpeed = target;
        }

        // 난이도 & 경험치 지속 반영
        ApplyDifficultyAndXP();
    }

    void ApplyDifficultyAndXP()
    {
        // 0~1 난이도
        Difficulty01 = (difficultyFullTime <= 0f)
            ? 1f
            : Mathf.Clamp01(game_time / difficultyFullTime);

        // 경험치(선형 감쇠: 40 → 5)
        CurrentXP = Mathf.RoundToInt(Mathf.Lerp(xpStart, xpMin, Difficulty01));

        // 번개 파라미터 조정 (런타임 반영)
        if (thunderScheduler)
        {
            float iMin = Mathf.Max(0.05f, thunderIntervalMinCurve.Evaluate(Difficulty01));
            float iMax = Mathf.Max(iMin + 0.05f, thunderIntervalMaxCurve.Evaluate(Difficulty01));
            float warn = Mathf.Clamp(thunderWarnDurationCurve.Evaluate(Difficulty01), 0.05f, 2f);
            float strike = Mathf.Clamp(thunderStrikeDurationCurve.Evaluate(Difficulty01), 0.05f, 0.6f);
            int concurrent = Mathf.Clamp(Mathf.RoundToInt(thunderConcurrentCurve.Evaluate(Difficulty01)), 1, 10);

            thunderScheduler.SetIntervals(new Vector2(iMin, iMax));
            thunderScheduler.SetWarnDuration(warn);
            thunderScheduler.SetStrikeDuration(strike);
            thunderScheduler.SetMaxConcurrent(concurrent);
        }
    }

    [SerializeField] private SpriteRenderer Background_sky;
    [SerializeField] private Gradient sunlightGradient; // 0→1 색상 변화용
    private float sunlightTimer = 0f;
    private const float sunlightDuration = 2f; // 색상/밝기 변화 시간
    private bool isUnderSun = false; // 햇빛 노출 중인가?
    private bool damageActive = false; // 데미지 활성화 여부
    private float baseIntensity = 1f; // 초기 밝기
    private float targetIntensity = 1.5f; // 최종 밝기

    void SunDamaged(float dt)
    {
        if (!player || Background_sky == null) return;

        underCloud = player.cloud_stack.Count > 0;

        if (!underCloud)
        {
            // ☀️ 햇빛에 처음 노출됨
            if (!isUnderSun)
            {
                isUnderSun = true;
                sunlightTimer = 0f;
                damageActive = false;
            }

            // 🌡 2초 동안 색상 및 밝기 변화
            if (sunlightTimer < sunlightDuration)
            {
                sunlightTimer += dt;
                float t = Mathf.Clamp01(sunlightTimer / sunlightDuration);

                // 색상 변화
                Background_sky.color = sunlightGradient.Evaluate(t);

                // 밝기 변화 (1 → 1.5)
                // Background_sky.intensity = Mathf.Lerp(baseIntensity, targetIntensity, t);

                return; // 아직 데미지 시작 전
            }

            // 🔥 색상 변화 완료 → 데미지 활성화
            if (!damageActive)
            {
                damageActive = true;
                heat_timer = 0f;
            }

            // ⛔ 1초마다 데미지
            if (damageActive)
            {
                heat_timer += dt;
                if (heat_timer >= 1f)
                {
                    player.DamagedBySun(sunDamage);
                    heat_timer = 0f;
                }
            }
        }
        else
        {
            // 🌥️ 다시 그늘로 돌아감 → 초기화
            isUnderSun = false;
            damageActive = false;
            sunlightTimer = 0f;
            heat_timer = 0f;

            // 색상, 밝기 즉시 초기화
            Background_sky.color = sunlightGradient.Evaluate(0f);
            // Background_sky.intensity = baseIntensity;
        }
    }

    // 외부에서 현재 경험치를 가져가서 지급하고 싶을 때 사용
    public int GetCurrentXpReward() => CurrentXP;

    // 업그레이드 순서 인덱스(0→1→2→3→0…)
    private int _nextUpgradeIndex = 0;

    // (선택) 퍼센트 증가가 누적되는 모양을 명확히 보고 싶다면 스택 카운트로 추적도 가능
    private int _speedStacks = 0; // 0→1→2→3 ... (각 25%)

    // PlayerController.LevelUp()에서 호출하도록 연결
    public void OnPlayerLevelUp(Player p)
    {
        // ApplyNextUpgrade(p);

        // // 레벨업 여러 번 연속 발생 시에도 순차 적용되게 하려면:
        // while (p.EXP >= 100)
        // {
        //     p.level++;
        //     p.EXP -= 100;
        //     ApplyNextUpgrade(p);
        // }
        augmentManager.OnLevelUp();
    }

    private void ApplyNextUpgrade(Player p)
    {
        switch (_nextUpgradeIndex)
        {
            case 0:
                // (최대체력 25 증가 및 체력 25 회복)
                p.IncreaseMaxHPAndHeal(25, 25);
                break;

            case 1:
                // (이속 25% 증가) → 25% → 50% → 75% … 누적 (기하가 아닌 “더해지는 퍼센트”)
                _speedStacks++;
                p.AddMoveSpeedPercent(0.25f); // 25%p씩 가산: 1.0 → 1.25 → 1.5 → 1.75 …
                break;

            case 2:
                // (체력 재생 5/초)
                p.AddRegen(5f);
                break;

            case 3:
                // (최대 점프 수 1 증가)
                p.IncreaseMaxJumps(1);
                break;
        }

        _nextUpgradeIndex = (_nextUpgradeIndex + 1) % 4; // 순환
    }

    public GameObject Panel;
    private bool isPaused;

    public void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void GameOver()
    {
        Panel.SetActive(true);
        PauseGame();
    }

    public void RestartGame()
    {
        // 현재 씬 다시 로드 (모든 상태 초기화)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void ToMainMenu()
    {
        SceneManager.LoadScene("Main");
    }

    public void QuitGame()
    {
        // 에디터에서는 플레이 멈추고, 빌드에서는 프로그램 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
