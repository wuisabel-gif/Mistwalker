using UnityEngine;
using UnityEditor;

public static class SwordAttacher
{
    const string PrefabPath = "Assets/Medieval Viking Sword/Built-In/Prefabs/Viking_Sword.prefab";

    [MenuItem("Tools/Attach Viking Sword")]
    static void AttachSword()
    {
        // Find the player's current weapon object (the old Axe) to locate the hand + grip transform.
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null) { Debug.LogError("[SwordAttacher] No VikingChampion found in scene."); return; }

        GameObject weapon = pc.weapon;
        if (weapon == null) { Debug.LogError("[SwordAttacher] VikingChampion.weapon is null."); return; }

        Transform hand = weapon.transform.parent;
        if (hand == null) { Debug.LogError("[SwordAttacher] Weapon has no parent (hand)."); return; }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("[SwordAttacher] Prefab not found at " + PrefabPath); return; }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(inst, "Attach Viking Sword");

        // Parent into the hand, keeping the prefab's own (correct) local scale.
        inst.transform.SetParent(hand, false);
        // Seat it where the axe was gripped, as a starting point.
        inst.transform.localPosition = weapon.transform.localPosition;
        inst.transform.localRotation = weapon.transform.localRotation;
        inst.name = "Viking_Sword";

        // Hide the old axe's (now broken) renderer so only the sword shows. Keep the object : 
        // VikingChampion still uses it as the weapon reference for hit detection.
        var mr = weapon.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Selection.activeGameObject = inst;
        Debug.Log("[SwordAttacher] DONE. Sword instantiated under '" + hand.name +
                  "'. Local pos copied from weapon = " + weapon.transform.localPosition +
                  ", scale = " + inst.transform.localScale + ". Tweak rotation in Inspector if needed.");
    }

    // Orient the selected sword so its blade points DOWN-and-forward from the grip
    // (a natural "holding a sword at the side" pose), regardless of the hand bone's rotation.
    [MenuItem("Tools/Orient Sword Down")]
    static void OrientDown() { OrientBlade(new Vector3(0f, -1f, 0.35f).normalized, false); }

    [MenuItem("Tools/Orient Sword Down (Flipped)")]
    static void OrientDownFlipped() { OrientBlade(new Vector3(0f, -1f, 0.35f).normalized, true); }

    static void OrientBlade(Vector3 worldBladeDir, bool flip)
    {
        var sword = Selection.activeGameObject;
        if (sword == null) { Debug.LogError("[SwordAttacher] Select the Viking_Sword first."); return; }

        var mf = sword.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) { Debug.LogError("[SwordAttacher] No mesh on sword."); return; }

        // Longest local axis of the mesh = the blade's long axis.
        Vector3 ext = mf.sharedMesh.bounds.extents;
        Vector3 bladeLocal;
        if (ext.x >= ext.y && ext.x >= ext.z) bladeLocal = Vector3.right;
        else if (ext.y >= ext.z) bladeLocal = Vector3.up;
        else bladeLocal = Vector3.forward;
        if (flip) bladeLocal = -bladeLocal;

        Undo.RecordObject(sword.transform, "Orient Sword");
        sword.transform.rotation = Quaternion.FromToRotation(bladeLocal, worldBladeDir);
        Debug.Log("[SwordAttacher] Oriented. blade local axis=" + bladeLocal +
                  " mesh ext=" + ext + " -> world dir " + worldBladeDir +
                  ". New local euler=" + sword.transform.localEulerAngles);
    }

    // ---- Robust placement that accounts for the mesh's offset pivot ----
    // Orients the blade in a chosen world direction, then SLIDES the sword so the
    // grip (hilt) end of the actual geometry sits exactly at the hand's grip point.
    [MenuItem("Tools/Place Sword In Hand")]
    static void PlaceInHand() { Place(new Vector3(0f, -1f, 0.35f).normalized, +1f); }

    [MenuItem("Tools/Place Sword In Hand (Flip Hilt)")]
    static void PlaceInHandFlip() { Place(new Vector3(0f, -1f, 0.35f).normalized, -1f); }

    static void Place(Vector3 worldBladeDir, float hiltSign)
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null || pc.weapon == null) { Debug.LogError("[SwordAttacher] No VikingChampion/weapon."); return; }
        Transform hand = pc.weapon.transform.parent;
        Vector3 handGripWorld = hand.TransformPoint(pc.weapon.transform.localPosition);

        // Find the sword we instantiated under the hand (don't rely on the GUI selection).
        Transform swordT = hand.Find("Viking_Sword");
        if (swordT == null)
        {
            foreach (Transform c in hand)
                if (c.name.Contains("Viking_Sword")) { swordT = c; break; }
        }
        if (swordT == null) { Debug.LogError("[SwordAttacher] No 'Viking_Sword' child under the hand. Run 'Attach Viking Sword' first."); return; }
        GameObject sword = swordT.gameObject;

        var mf = sword.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) { Debug.LogError("[SwordAttacher] No mesh on sword."); return; }
        Mesh mesh = mf.sharedMesh;

        // Blade long axis in local space = longest mesh extent.
        Vector3 ext = mesh.bounds.extents;
        Vector3 bladeLocal;
        if (ext.x >= ext.y && ext.x >= ext.z) bladeLocal = Vector3.right;
        else if (ext.y >= ext.z) bladeLocal = Vector3.up;
        else bladeLocal = Vector3.forward;

        Undo.RecordObject(sword.transform, "Place Sword");

        // 1) Orient the blade in the desired world direction.
        sword.transform.rotation = Quaternion.FromToRotation(bladeLocal, worldBladeDir);

        // 2) Find the hilt end of the geometry (one extreme along the blade axis),
        //    then slide the sword so that point lands on the hand grip.
        float halfLen = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z));
        Vector3 hiltLocal = mesh.bounds.center + bladeLocal * halfLen * hiltSign;
        Vector3 hiltWorldNow = mf.transform.TransformPoint(hiltLocal);
        sword.transform.position += (handGripWorld - hiltWorldNow);

        Selection.activeGameObject = sword;
        Debug.Log("[SwordAttacher] Placed. hiltSign=" + hiltSign + " bladeLocal=" + bladeLocal +
                  " halfLen=" + halfLen + " -> sword world pos now " + sword.transform.position +
                  ", localPos=" + sword.transform.localPosition + ". Blade world dir=" + worldBladeDir);
    }

    // ---- Pose-stable orientation: copy the axe's LOCAL transform (relative to the hand)
    // so the sword grips and points exactly like the axe in EVERY animation frame.
    // The sword's pivot is its grip, so localPosition = axe localPosition seats it correctly.
    // A local spin corrects for the sword mesh's blade-axis differing from the axe mesh's.
    [MenuItem("Tools/Match Axe (no spin)")]      static void MatchAxe0()   { MatchAxe(Vector3.zero); }
    [MenuItem("Tools/Match Axe (spin X 90)")]    static void MatchAxeX90() { MatchAxe(new Vector3(90, 0, 0)); }
    [MenuItem("Tools/Match Axe (spin X 180)")]   static void MatchAxeX180(){ MatchAxe(new Vector3(180, 0, 0)); }
    [MenuItem("Tools/Match Axe (spin X -90)")]   static void MatchAxeXm90(){ MatchAxe(new Vector3(-90, 0, 0)); }
    [MenuItem("Tools/Match Axe (spin Z 90)")]    static void MatchAxeZ90() { MatchAxe(new Vector3(0, 0, 90)); }
    [MenuItem("Tools/Match Axe (spin Z -90)")]   static void MatchAxeZm90(){ MatchAxe(new Vector3(0, 0, -90)); }

    static void MatchAxe(Vector3 localSpinEuler)
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null || pc.weapon == null) { Debug.LogError("[SwordAttacher] No VikingChampion/weapon."); return; }
        Transform axe = pc.weapon.transform;
        Transform hand = axe.parent;

        Transform swordT = hand.Find("Viking_Sword");
        if (swordT == null) foreach (Transform c in hand) if (c.name.Contains("Viking_Sword")) { swordT = c; break; }
        if (swordT == null) { Debug.LogError("[SwordAttacher] No 'Viking_Sword' under hand."); return; }

        Undo.RecordObject(swordT, "Match Axe");
        swordT.localPosition = axe.localPosition;
        swordT.localRotation = axe.localRotation * Quaternion.Euler(localSpinEuler);
        Selection.activeGameObject = swordT.gameObject;
        Debug.Log("[SwordAttacher] Matched axe local transform. spin=" + localSpinEuler +
                  " | sword.localPos=" + swordT.localPosition + " localEuler=" + swordT.localEulerAngles);
    }

    // Temporarily make the sword glow bright magenta so its blade direction is unmistakable
    // in the dark scene while tuning orientation. Restore afterwards.
    [MenuItem("Tools/Highlight Sword (bright)")]
    static void Highlight()
    {
        var sword = FindSword();
        if (sword == null) return;
        var mr = sword.GetComponentInChildren<MeshRenderer>();
        var m = new Material(Shader.Find("Unlit/Color"));
        m.color = Color.magenta;
        mr.sharedMaterial = m;
        Debug.Log("[SwordAttacher] Highlighted sword bright magenta (temporary).");
    }

    [MenuItem("Tools/Restore Sword Material")]
    static void RestoreMat()
    {
        var sword = FindSword();
        if (sword == null) return;
        var mr = sword.GetComponentInChildren<MeshRenderer>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval Viking Sword/Built-In/Materials/Viking_Sword.mat");
        if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval Viking Sword/Materials/Viking_Sword.mat");
        if (mat != null) { mr.sharedMaterial = mat; Debug.Log("[SwordAttacher] Restored Viking_Sword material."); }
        else Debug.LogError("[SwordAttacher] Could not find Viking_Sword.mat to restore.");
    }

    // Diagnostic: drop a bright cube exactly where the sword's grip sits, parented to the
    // same hand bone. If the CUBE renders but the sword doesn't, the sword mesh is the culprit;
    // if neither renders, the bone-parenting/location/culling is the culprit.
    const string SwordPrefabPath = "Assets/Medieval Viking Sword/Built-In/Prefabs/Viking_Sword.prefab";

    // THE REAL FIX: attach the sword to the ACTIVE warrior's hand bone ("hand.r" under Warrior (1)),
    // not the disabled duplicate's "Warrior_RightHand". This is the bone that actually renders & animates.
    [MenuItem("Tools/Attach To Active Hand")]
    static void AttachToActiveHand()
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null) { Debug.LogError("[SwordAttacher] No VikingChampion."); return; }
        Transform root = pc.transform.root;

        Transform hand = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == "hand.r") { hand = t; break; }
        if (hand == null) { Debug.LogError("[SwordAttacher] Could not find 'hand.r' under " + root.name); return; }

        // Reuse the existing sword instance if we already made one (anywhere in the scene), else instantiate.
        GameObject sword = null;
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name != "Viking_Sword") continue;
            if (t.hideFlags != HideFlags.None || !t.gameObject.scene.IsValid()) continue;
            sword = t.gameObject; break;
        }
        if (sword == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
            sword = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            sword.name = "Viking_Sword";
            Undo.RegisterCreatedObjectUndo(sword, "Attach Sword Active");
        }
        else Undo.RecordObject(sword.transform, "Attach Sword Active");

        sword.transform.SetParent(hand, true);   // keep world for now; then zero local
        sword.transform.localPosition = Vector3.zero;
        sword.transform.localRotation = Quaternion.identity;
        sword.transform.localScale = Vector3.one;
        Selection.activeGameObject = sword;
        Debug.Log("[SwordAttacher] Attached sword to ACTIVE 'hand.r'. activeInHierarchy=" + sword.activeInHierarchy +
                  " worldPos=" + sword.transform.position + ". Now visible: tune orientation next.");
    }

    [MenuItem("Tools/List Warrior1 Hand Bones")]
    static void ListHandBones()
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null) { Debug.LogError("[SwordAttacher] No VikingChampion."); return; }
        Transform root = pc.transform.root;
        string s = "[SwordAttacher] Bones under '" + root.name + "' containing 'hand'/'r' (candidates for right hand):\n";
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower();
            if (n.Contains("hand") || n.Contains("fore") || n.EndsWith(".r") || n.EndsWith("_r"))
                s += "  " + FullPath(t) + "  (active=" + t.gameObject.activeInHierarchy + ")\n";
        }
        Debug.Log(s);
    }

    static string FullPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }

    [MenuItem("Tools/Map Warriors")]
    static void MapWarriors()
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        string s = "[SwordAttacher] === WARRIOR MAP ===\n";
        if (pc != null)
        {
            s += "VikingChampion on '" + pc.name + "' activeInHierarchy=" + pc.gameObject.activeInHierarchy +
                 " | root='" + pc.transform.root.name + "' rootActive=" + pc.transform.root.gameObject.activeInHierarchy + "\n";
            if (pc.weapon != null)
                s += "pc.weapon='" + pc.weapon.name + "' activeInHierarchy=" + pc.weapon.activeInHierarchy +
                     " | weaponRoot='" + pc.weapon.transform.root.name + "'\n";
        }
        else s += "No active VikingChampion found.\n";

        // Every Warrior_RightHand in the scene (incl. inactive), with its root + active state.
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        s += "All 'Warrior_RightHand' bones:\n";
        foreach (var t in all)
        {
            if (t.name != "Warrior_RightHand") continue;
            if (t.hideFlags != HideFlags.None) continue;
            if (!t.gameObject.scene.IsValid()) continue; // skip assets
            s += "  root='" + t.root.name + "' activeInHierarchy=" + t.gameObject.activeInHierarchy + "\n";
        }
        Debug.Log(s);
    }

    [MenuItem("Tools/Trace Active Chain")]
    static void TraceActive()
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null || pc.weapon == null) { Debug.LogError("[SwordAttacher] No VikingChampion/weapon."); return; }
        var t = pc.weapon.transform.parent;
        string s = "[SwordAttacher] Active chain from hand up to root:\n";
        while (t != null)
        {
            s += "  '" + t.name + "' activeSelf=" + t.gameObject.activeSelf + " activeInHierarchy=" + t.gameObject.activeInHierarchy + "\n";
            t = t.parent;
        }
        Debug.Log(s);
    }

    [MenuItem("Tools/Test Cube At Hand")]
    static void TestCube()
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null || pc.weapon == null) { Debug.LogError("[SwordAttacher] No VikingChampion/weapon."); return; }
        Transform hand = pc.weapon.transform.parent;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "TEST_CUBE";
        Undo.RegisterCreatedObjectUndo(cube, "Test Cube");
        cube.transform.SetParent(hand, false);
        cube.transform.localPosition = pc.weapon.transform.localPosition;
        cube.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        var m = new Material(Shader.Find("Unlit/Color"));
        m.color = Color.green;
        cube.GetComponent<MeshRenderer>().sharedMaterial = m;
        Selection.activeGameObject = cube;
        Debug.Log("[SwordAttacher] Spawned green TEST_CUBE at hand. world pos=" + cube.transform.position +
                  " lossyScale=" + cube.transform.lossyScale + " activeInHierarchy=" + cube.activeInHierarchy);
    }

    [MenuItem("Tools/Delete Test Cube")]
    static void DeleteCube()
    {
        var c = GameObject.Find("TEST_CUBE");
        if (c != null) { Undo.DestroyObjectImmediate(c); Debug.Log("[SwordAttacher] Deleted TEST_CUBE."); }
        else Debug.Log("[SwordAttacher] No TEST_CUBE found.");
    }

    static GameObject FindSword()
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name != "Viking_Sword") continue;
            if (t.hideFlags != HideFlags.None || !t.gameObject.scene.IsValid()) continue;
            return t.gameObject;
        }
        Debug.LogError("[SwordAttacher] No 'Viking_Sword' found in scene.");
        return null;
    }

    // The active warrior's bones carry a large world scale, so localScale=1 makes the sword huge.
    // Counter the parent's lossyScale so the sword renders at its true ~1 m size.
    [MenuItem("Tools/Normalize Sword Scale")]
    static void NormalizeScale()
    {
        var s = FindSword(); if (s == null) return;
        Transform p = s.transform.parent;
        Vector3 ls = p != null ? p.lossyScale : Vector3.one;
        Undo.RecordObject(s.transform, "Normalize Sword Scale");
        s.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
        Selection.activeGameObject = s;
        var r = s.GetComponentInChildren<Renderer>();
        Debug.Log("[SwordAttacher] Parent lossyScale=" + ls + " -> sword localScale=" + s.transform.localScale +
                  ". New world bounds size=" + (r != null ? r.bounds.size.ToString() : "?") + " lossyScale=" + s.transform.lossyScale);
    }

    // Nudge the sword's LOCAL rotation by 90-degree steps (pose-stable, relative to the hand bone).
    [MenuItem("Tools/Sword Local Rot +X90")] static void RX() { Nudge(new Vector3(90,0,0)); }
    [MenuItem("Tools/Sword Local Rot +Y90")] static void RY() { Nudge(new Vector3(0,90,0)); }
    [MenuItem("Tools/Sword Local Rot +Z90")] static void RZ() { Nudge(new Vector3(0,0,90)); }
    static void Nudge(Vector3 e)
    {
        var s = FindSword(); if (s == null) return;
        Undo.RecordObject(s.transform, "Nudge Sword");
        s.transform.localRotation = s.transform.localRotation * Quaternion.Euler(e);
        Selection.activeGameObject = s;
        Debug.Log("[SwordAttacher] Nudged local rot by " + e + ". New localEuler=" + s.transform.localEulerAngles);
    }

    [MenuItem("Tools/Report Sword Size")]
    static void ReportSize()
    {
        var sword = Selection.activeGameObject;
        if (sword == null) { Debug.LogError("[SwordAttacher] Select the Viking_Sword in the Hierarchy first."); return; }

        var sr = sword.GetComponentInChildren<Renderer>();
        string swordInfo = sr != null
            ? "Sword world bounds size = " + sr.bounds.size + " (max dim = " + Mathf.Max(sr.bounds.size.x, sr.bounds.size.y, sr.bounds.size.z) + "m)"
            : "Sword has NO renderer!";

        var pc = Object.FindObjectOfType<VikingChampion>();
        string warriorInfo = "no warrior renderer";
        if (pc != null)
        {
            var smr = pc.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null) smr = Object.FindObjectOfType<SkinnedMeshRenderer>();
            if (smr != null) warriorInfo = "Warrior world bounds size = " + smr.bounds.size + " (height = " + smr.bounds.size.y + "m)";
        }

        Debug.Log("[SwordAttacher] " + swordInfo + "  ||  " + warriorInfo +
                  "  ||  Sword localScale = " + sword.transform.localScale +
                  ", lossyScale = " + sword.transform.lossyScale);
    }

    [MenuItem("Tools/Diagnose Placement")]
    static void Diagnose()
    {
        var pc = Object.FindObjectOfType<VikingChampion>();
        if (pc == null) { Debug.LogError("[SwordAttacher] No VikingChampion."); return; }
        Transform weapon = pc.weapon != null ? pc.weapon.transform : null;
        Transform hand = weapon != null ? weapon.parent : null;

        Transform swordT = hand != null ? hand.Find("Viking_Sword") : null;
        var sr = swordT != null ? swordT.GetComponentInChildren<Renderer>() : null;

        // Where is the warrior's body actually, in world space?
        var smr = Object.FindObjectOfType<SkinnedMeshRenderer>();

        Debug.Log("[DIAG] weapon='" + (weapon ? weapon.name : "null") +
                  "' worldPos=" + (weapon ? weapon.position.ToString() : "-") +
                  " | hand='" + (hand ? hand.name : "null") +
                  "' worldPos=" + (hand ? hand.position.ToString() : "-") +
                  " | sword child=" + (swordT ? "FOUND" : "MISSING") +
                  " swordPivot=" + (swordT ? swordT.position.ToString() : "-") +
                  " swordRenderCenter=" + (sr ? sr.bounds.center.ToString() : "-") +
                  " | BODY '" + (smr ? smr.name : "null") + "' center=" + (smr ? smr.bounds.center.ToString() : "-") +
                  " size=" + (smr ? smr.bounds.size.ToString() : "-"));
    }
}
