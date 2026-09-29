using UnityEngine;
using UnityEngine.SceneManagement;

// A will-o'-the-wisp near the start of each run that speaks a little Niflheim lore.
// It spawns itself ahead of the player on every scene load, so no scene setup is needed.
public class SpiritGuide : MonoBehaviour
{
    public string[] lines =
    {
        "Oathbroken... the mist already knows your name.",
        "Niflheim keeps what Valhalla refuses. The draugr were warriors once, like you.",
        "Walk, and do not stop. Each one you put back in the ground thins the fog.",
        "Beyond the last clearing waits the Warden of the Bridge. Be ready.",
    };
    public float talkRadius = 6f;
    public float secondsPerLine = 4.5f;

    Transform hero;
    Light glow;
    Vector3 home;
    int line = -1;
    float nextLineTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (_, __) => Spawn();
        Spawn(); // the first scene is already loaded when this runs
    }

    static void Spawn()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || FindFirstObjectByType<SpiritGuide>() != null)
            return;

        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(orb.GetComponent<Collider>()); // must not block the sword or the player
        orb.name = "Spirit Guide";
        orb.transform.localScale = Vector3.one * 0.25f;
        orb.transform.position = player.transform.position + player.transform.forward * 6f + Vector3.up * 1.6f;
        orb.GetComponent<Renderer>().material.color = new Color(0.75f, 0.95f, 1f);
        orb.AddComponent<SpiritGuide>();
    }

    void Start()
    {
        hero = GameObject.FindGameObjectWithTag("Player").transform;
        home = transform.position;

        glow = new GameObject("Glow").AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.55f, 0.85f, 1f);
        glow.range = 7f;
        glow.shadows = LightShadows.None;
        glow.transform.SetParent(transform, false);
    }

    void Update()
    {
        // Drift and flicker.
        transform.position = home + new Vector3(Mathf.Sin(Time.time * 0.7f) * 0.3f, Mathf.Sin(Time.time * 1.3f) * 0.15f, 0f);
        glow.intensity = 1.8f + 0.6f * Mathf.PerlinNoise(Time.time * 3f, 1f);

        if (line == -1 && Vector3.Distance(hero.position, home) < talkRadius)
            NextLine();
        else if (line >= 0 && line < lines.Length && Time.time >= nextLineTime)
            NextLine();
    }

    void NextLine()
    {
        line++;
        nextLineTime = Time.time + secondsPerLine;
    }

    void OnGUI()
    {
        if (line < 0 || line >= lines.Length)
            return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.032f),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
        };
        var box = new Rect(Screen.width * 0.1f, Screen.height * 0.8f, Screen.width * 0.8f, Screen.height * 0.12f);
        var shadow = new Rect(box.x + 2, box.y + 2, box.width, box.height);

        style.normal.textColor = Color.black; // simple drop shadow keeps it readable on the mist
        GUI.Label(shadow, lines[line], style);
        style.normal.textColor = new Color(0.8f, 0.95f, 1f);
        GUI.Label(box, lines[line], style);
    }
}
