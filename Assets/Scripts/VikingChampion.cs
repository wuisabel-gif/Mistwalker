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

        Collider[] targets = Physics.OverlapSphere(weapon.transform.position, strikeRadius);
        foreach (Collider target in targets)
        {
            if (!target.CompareTag("Enemy"))
                continue;

            HostileWarrior foe = target.GetComponent<HostileWarrior>();
            if (foe != null)
                foe.ReceiveHit(strikePower);
        }
    }

    public void ReceiveDamage(int amount)
    {
        if (defeated)
            return;

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
