using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class VitalityDisplay : MonoBehaviour
{
    public Image healthBarFill;
    public Text healthText;
    [FormerlySerializedAs("player")]
    public VikingChampion champion;

    void Start()
    {
        if (champion == null)
            champion = FindObjectOfType<VikingChampion>();

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (champion == null)
            return;

        float percent = champion.maxHealth > 0
            ? Mathf.Clamp01((float)champion.currentHealth / champion.maxHealth)
            : 0f;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = percent;
            healthBarFill.color = PickColor(percent);
        }

        if (healthText != null)
            healthText.text = champion.currentHealth + " / " + champion.maxHealth;
    }

    Color PickColor(float percent)
    {
        if (percent > 0.65f) return new Color(0.2f, 0.85f, 0.25f);
        if (percent > 0.35f) return new Color(1f, 0.75f, 0.1f);
        return new Color(0.9f, 0.18f, 0.12f);
    }
}
