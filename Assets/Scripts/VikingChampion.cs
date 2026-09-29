using UnityEngine;
using UnityEngine.Serialization;

public class VikingChampion : MonoBehaviour
{
    [Header("Vitality")]
    public int maxHealth = 120;
    public int currentHealth;
    public int scoreSnapshot;

    [Header("Movement")]
    public float moveSpeed = 3.4f;

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
    [Tooltip("How far the blade reaches from the chest; trees closer than this block the swing.")]
    public float bladeReach = 1.2f;

    const float ChestHeight = 1.2f;

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
    private float regenBuffer;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        ledger = JourneyLedger.Instance ?? FindObjectOfType<JourneyLedger>();
    }

    void Update()
    {
        if (defeated) return;

        ApplyKeyboardMovement();
        RefreshScoreReward();
        Regenerate();

        if (Input.GetMouseButtonDown(0) && Time.time >= nextStrikeTime)
        {
            nextStrikeTime = Time.time + strikeDelay;
            SwingWeapon();
        }
    }

    void ApplyKeyboardMovement()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        transform.Translate(input * moveSpeed * Time.deltaTime, Space.World);
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
        if (swingSource != null && swingClip != null)
            swingSource.PlayOneShot(swingClip);

        if (weapon == null)
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
        Debug.Log("The champion has fallen.");
    }
}
