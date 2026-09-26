using UnityEngine;
using UnityEngine.Serialization;

public class HostileWarrior : MonoBehaviour
{
    [Header("Stats")]
    public int health = 45;
    [FormerlySerializedAs("speed")]
    public float pursuitSpeed = 2.35f;
    [FormerlySerializedAs("attackRange")]
    public float meleeDistance = 1.6f;
    [FormerlySerializedAs("damage")]
    public int hitStrength = 12;
    [FormerlySerializedAs("attackCooldown")]
    public float attackInterval = 1.25f;

    [Header("Audio")]
    [FormerlySerializedAs("audioSource")]
    public AudioSource voiceSource;
    [FormerlySerializedAs("zombieSound")]
    public AudioClip spawnCry;

    private Animator animator;
    private Transform hero;
    private float nextAttackTime;
    private bool defeated;

    void Awake()
    {
        animator = GetComponent<Animator>();
        voiceSource = voiceSource != null ? voiceSource : GetComponent<AudioSource>();
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            hero = playerObject.transform;

        if (voiceSource != null && spawnCry != null)
            voiceSource.PlayOneShot(spawnCry);
    }

    void Update()
    {
        if (defeated || hero == null)
            return;

        Vector3 flatHero = hero.position;
        flatHero.y = transform.position.y;

        float distanceToHero = Vector3.Distance(transform.position, hero.position);
        if (distanceToHero > meleeDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, flatHero, pursuitSpeed * Time.deltaTime);
            transform.LookAt(flatHero);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
            StrikeHero();
        }
    }

    void StrikeHero()
    {
        animator?.SetTrigger("IsAttacking");

        VikingChampion champion = hero.GetComponent<VikingChampion>();
        if (champion != null)
            champion.ReceiveDamage(hitStrength);
    }

    public void ReceiveHit(int amount)
    {
        if (defeated)
            return;

        health -= amount;
        if (health <= 0)
            Collapse();
    }

    void Collapse()
    {
        defeated = true;
        animator?.SetTrigger("IsDead");

        if (JourneyLedger.Instance != null)
            JourneyLedger.Instance.AwardEnemyDefeat(125);

        Destroy(gameObject, 2.5f);
    }
}
