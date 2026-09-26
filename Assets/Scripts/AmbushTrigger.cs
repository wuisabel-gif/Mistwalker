using UnityEngine;

public class AmbushTrigger : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int baseGroupSize = 2;
    public float spawnRadius = 14f;

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
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            spawnedInThisZone++;
        }
    }
}
