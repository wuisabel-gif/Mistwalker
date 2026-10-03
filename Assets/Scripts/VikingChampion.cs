using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class VikingChampion : MonoBehaviour
{
    [Header("Vitality")]
    public int maxHealth = 120;
    public int currentHealth;
    public int scoreSnapshot;

    [Header("Combat")]
    public GameObject weapon;
    [FormerlySerializedAs("attackRange")]
    public float strikeRadius = 2.2f;
    [FormerlySerializedAs("attackDamage")]
    public int strikePower = 9;
    [FormerlySerializedAs("attackCooldown")]
    public float strikeDelay = 1.6f;
    [Tooltip("Degrees in front of the champion the swing covers.")]
    public float strikeArc = 100f;
    [Tooltip("Seconds into the slash animation when the blade connects.")]
    public float strikeHitDelay = 0.35f;
    [Tooltip("How far the blade reaches from the chest; trees closer than this block the swing.")]
    public float bladeReach = 1.2f;

    const float ChestHeight = 1.2f;

    [Header("Lantern")]
    public Color lanternColor = new Color(1f, 0.62f, 0.3f);
    public float lanternRange = 9f;
    public float lanternIntensity = 1.6f;
    [Tooltip("Optional flame effect shown inside the lantern light.")]
    public GameObject lanternFlame;
    public float lanternFlameScale = 0.2f;

    [Header("Recovery")]
    [Tooltip("HP restored for each enemy killed.")]
    public int healPerKill = 15;
    [Tooltip("Seconds without taking damage before health starts coming back.")]
    public float regenDelay = 4f;
    public float regenPerSecond = 5f;

    [Header("Axe Audio")]
    [FormerlySerializedAs("audioSource")]
    public AudioSource swingSource;
    [FormerlySerializedAs("axeSound")]
    public AudioClip swingClip;

    [Header("Death Audio")]
    [FormerlySerializedAs("audioSource2")]
    public AudioSource deathSource;
    [FormerlySerializedAs("deathSound")]
    public AudioClip deathClip;

    private Animator animator;
    private JourneyLedger ledger;
    private float nextStrikeTime;
    private int rewardedTier;
    private bool defeated;
    private float lastHurtTime = -999f;
    private Light lantern;
    private int finalScore, bestScore;
    const string BestScoreKey = "Mistwalker.BestScore";
    private float regenBuffer;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        ledger = JourneyLedger.Instance ?? FindObjectOfType<JourneyLedger>();

        lantern = new GameObject("Lantern").AddComponent<Light>();
        lantern.type = LightType.Point;
        lantern.color = lanternColor;
        lantern.range = lanternRange;
        lantern.intensity = lanternIntensity;
        lantern.shadows = LightShadows.None; // ponytail: no shadows, cheap on WebGL; enable Soft if it looks flat
        lantern.transform.SetParent(transform, false);
        lantern.transform.localPosition = new Vector3(-0.35f, 1.5f, 0.3f); // off-hand side, chest height

        if (lanternFlame != null)
        {
            var flame = Instantiate(lanternFlame, lantern.transform);
            flame.transform.localPosition = Vector3.zero;
            flame.transform.localScale = Vector3.one * lanternFlameScale;
        }
    }

    void Update()
    {
        if (lantern != null)
            lantern.intensity = lanternIntensity * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 4f, 0f));

        if (defeated)
        {
            if (Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        RefreshScoreReward();
        Regenerate();
    }

    // Called by HeroMotionDriver when the slash animation starts. Owns the cooldown and the
    // tree check; the damage itself lands mid-swing so it matches what the player sees.
    public bool TryStartSwing()
    {
        if (defeated || Time.time < nextStrikeTime || BladeBlocked())
            return false;

        nextStrikeTime = Time.time + strikeDelay;
        if (swingSource != null && swingClip != null)
            swingSource.PlayOneShot(swingClip);
        Invoke(nameof(SwingWeapon), strikeHitDelay);
        return true;
    }


    void RefreshScoreReward()
    {
        if (ledger == null)
            ledger = JourneyLedger.Instance ?? FindObjectOfType<JourneyLedger>();
        if (ledger == null)
            return;

        scoreSnapshot = ledger.GetScore();
        int currentTier = scoreSnapshot / 900;

        if (currentTier <= rewardedTier)
            return;

        rewardedTier = currentTier;
        currentHealth = Mathf.Min(maxHealth, currentHealth + 20);
    }

    void SwingWeapon()
    {
        if (defeated || weapon == null)
            return;

        weapon.SetActive(true);

        if (BladeBlocked())
            return;

        Vector3 chest = transform.position + Vector3.up * ChestHeight;
        Collider[] targets = Physics.OverlapSphere(transform.position, strikeRadius);
        foreach (Collider target in targets)
        {
            if (!target.CompareTag("Enemy"))
                continue;

            // Only what the champion is facing, and nothing hidden behind a tree.
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (Vector3.Angle(transform.forward, toTarget) > strikeArc * 0.5f)
                continue;
            if (Physics.Linecast(chest, target.bounds.center, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider != target && !hit.transform.IsChildOf(transform))
                continue;

            HostileWarrior foe = target.GetComponent<HostileWarrior>();
            if (foe != null)
                foe.ReceiveHit(strikePower);
        }
    }

    // True when something solid (a tree) sits within sword reach in front, so a swing would pass through it.
    public bool BladeBlocked()
    {
        Vector3 chest = transform.position + Vector3.up * ChestHeight;
        foreach (RaycastHit hit in Physics.SphereCastAll(chest, 0.15f, transform.forward, bladeReach, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.collider.CompareTag("Enemy") && !hit.transform.IsChildOf(transform) && !(hit.collider is TerrainCollider))
                return true;
        return false;
    }

    // Out of combat for a while: health trickles back.
    void Regenerate()
    {
        if (currentHealth >= maxHealth || Time.time - lastHurtTime < regenDelay)
        {
            regenBuffer = 0f;
            return;
        }

        regenBuffer += regenPerSecond * Time.deltaTime;
        int whole = Mathf.FloorToInt(regenBuffer);
        regenBuffer -= whole;
        Heal(whole);
    }

    public void Heal(int amount)
    {
        if (defeated || amount <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ReceiveDamage(int amount)
    {
        if (defeated)
            return;

        lastHurtTime = Time.time;
        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (currentHealth == 0)
            FallInBattle();
    }

    void FallInBattle()
    {
        defeated = true;

        if (deathSource != null && deathClip != null)
            deathSource.PlayOneShot(deathClip);

        animator?.SetTrigger("IsDead");

        var mover = GetComponent<HeroMotionDriver>();
        if (mover != null)
            mover.enabled = false; // the fallen don't walk

        finalScore = ledger != null ? ledger.GetScore() : 0;
        bestScore = Mathf.Max(finalScore, PlayerPrefs.GetInt(BestScoreKey, 0));
        PlayerPrefs.SetInt(BestScoreKey, bestScore);
        PlayerPrefs.Save();
    }

    void OnGUI()
    {
        if (!defeated)
            return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.06f),
            alignment = TextAnchor.MiddleCenter,
            richText = true,
        };
        int small = Mathf.RoundToInt(style.fontSize * 0.5f);
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
            "<color=#b3261e>YOU HAVE FALLEN</color>\n" +
            $"<size={small}>Score {finalScore}    Best {bestScore}\n\nPress R to rise again</size>", style);
    }
}
