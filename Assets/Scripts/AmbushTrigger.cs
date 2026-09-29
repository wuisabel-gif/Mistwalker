using UnityEngine;

public class AmbushTrigger : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int baseGroupSize = 2;
    public float spawnRadius = 14f;
    [Range(0f, 1f)] public float runnerChance = 0.3f;
    public GameObject wolfPrefab;
    [Range(0f, 1f)] public float wolfChance = 0.25f;

    public int spawnedInThisZone;

    private bool consumed;

    void OnTriggerEnter(Collider other)
    {
        if (consumed || !other.CompareTag("Player") || enemyPrefab == null)
            return;

        consumed = true;

        int score = JourneyLedger.Instance != null ? JourneyLedger.Instance.GetScore() : 0;
        int groupSize = baseGroupSize + Mathf.Clamp(score / 500, 0, 8);

        for (int i = 0; i < groupSize; i++)
        {
            Vector2 ring = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(ring.x, 0f, ring.y);
            if (wolfPrefab != null && Random.value < wolfChance)
            {
                Instantiate(wolfPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                GameObject foe = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                if (Random.value < runnerChance)
                    MakeRunner(foe);
            }
            spawnedInThisZone++;
        }
    }

    // A lean, quick draugr: smaller, faster, easier to kill, a little weaker.
    static void MakeRunner(GameObject foe)
    {
        foe.name += " (Runner)";
        foe.transform.localScale *= 0.8f;

        var warrior = foe.GetComponent<HostileWarrior>();
        if (warrior != null)
        {
            warrior.pursuitSpeed *= 1.6f;
            warrior.health = Mathf.CeilToInt(warrior.health * 0.6f);
            warrior.hitStrength = Mathf.CeilToInt(warrior.hitStrength * 0.7f);
        }

        var animator = foe.GetComponent<Animator>();
        if (animator != null)
            animator.speed = 1.5f; // legs keep up with the faster pursuit
    }
}
