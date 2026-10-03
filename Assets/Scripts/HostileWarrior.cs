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
    [Tooltip("How far ahead the enemy looks for trees to walk around.")]
    public float lookAhead = 1.5f;

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
            Vector3 heading = Steer((flatHero - transform.position).normalized);
            transform.position += heading * pursuitSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(heading);
            return;
        }

        transform.LookAt(flatHero);

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
            StrikeHero();
        }
    }

    // Walk around trees instead of through them: try the straight line first, then fan out
    // left and right until a direction is clear.
    // ponytail: local avoidance only, can get stuck in a dense cluster; bake a NavMesh if that happens.
    static readonly float[] SteerAngles = { 0f, 35f, -35f, 70f, -70f, 105f, -105f };

    Vector3 Steer(Vector3 desired)
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        foreach (float angle in SteerAngles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * desired;
            if (!ObstacleAhead(origin, dir))
                return dir;
        }
        return desired;
    }

    bool ObstacleAhead(Vector3 origin, Vector3 dir)
    {
        if (!Physics.SphereCast(origin, 0.35f, dir, out RaycastHit hit, lookAhead, ~0, QueryTriggerInteraction.Ignore))
            return false;

        Collider c = hit.collider;
        return !c.CompareTag("Enemy") && !c.CompareTag("Player") && !(c is TerrainCollider) && !c.transform.IsChildOf(transform);
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

        VikingChampion champion = hero != null ? hero.GetComponent<VikingChampion>() : null;
        if (champion != null)
            champion.Heal(champion.healPerKill);

        Destroy(gameObject, 2.5f);
    }
}
