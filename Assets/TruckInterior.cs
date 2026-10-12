using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class TruckInterior : MonoBehaviour
{
    [Header("Wiring (auto-found if left empty)")]
    public TruckController truck;
    public FollowCamera followCamera;

    [Header("Driver eye / interior camera (local to truck, metres)")]
    public Vector3 eyeOffset = new Vector3(-0.55f, 1.9f, 1.45f);
    public float eyePitch = -4f;

    [Header("Placeholder steering wheel")]
    public Vector3 steeringWheelOffset = new Vector3(-0.55f, 1.45f, 1.95f);
    public Vector3 steeringWheelEuler = new Vector3(22f, 0f, 0f);
    public float wheelRadius = 0.2f;
    public float wheelTube = 0.026f;
    [Tooltip("Total rotation (deg) of the visual wheel from full-left to full-right lock.")]
    public float wheelLockToLock = 450f;

    [Header("Gear lever")]
    public Vector3 gearLeverOffset = new Vector3(-0.25f, 1.15f, 1.65f);
    public float gearLeverLength = 0.3f;
    [Tooltip("Max tilt (deg) to either side of centre across the gear positions.")]
    public float gearLeverTilt = 22f;

    [Header("Gear display")]
    public Vector3 gearDisplayOffset = new Vector3(-0.40f, 1.50f, 2.10f);
    public Vector3 gearDisplayEuler = new Vector3(8f, 180f, 0f);
    public float gearDisplayScale = 0.05f;

    [Header("Colours")]
    public Color rimColor = new Color(0.05f, 0.05f, 0.06f);
    public Color trimColor = new Color(0.85f, 0.82f, 0.78f);
    public Color displayColor = new Color(0.25f, 1f, 0.45f);

    static readonly string[] GearNames = { "R", "1", "2", "3", "4", "5" };

    Transform _wheelPivot;
    Transform _leverPivot;
    TextMeshPro _gearText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        if (FindAnyObjectByType<TruckInterior>() != null) return;
        var t = FindAnyObjectByType<TruckController>();
        if (t == null) return;
        t.gameObject.AddComponent<TruckInterior>();
    }

    void Start()
    {
        if (truck == null) truck = GetComponent<TruckController>();
        if (truck == null) truck = FindAnyObjectByType<TruckController>();
        if (followCamera == null) followCamera = FindAnyObjectByType<FollowCamera>();

        if (truck == null)
        {
            Debug.LogWarning("TruckInterior: no TruckController found; interior not built.");
            return;
        }

        BuildSteeringWheel();
        BuildGearLever();
        BuildGearDisplay();

        if (followCamera != null)
        {
            followCamera.interiorOffset = eyeOffset;
            followCamera.interiorPitch = eyePitch;
        }
    }

    void Update()
    {
        if (truck == null) return;

        if (_wheelPivot != null)
        {
            float steerNorm = truck.maxSteeringAngle > 0.001f
                ? Mathf.Clamp(truck.CurrentSteerAngle / truck.maxSteeringAngle, -1f, 1f)
                : 0f;
            float angle = -steerNorm * (wheelLockToLock * 0.5f);
            _wheelPivot.localRotation = Quaternion.Euler(steeringWheelEuler) * Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (_leverPivot != null)
        {
            float t = GearNames.Length > 1 ? (float)CurrentGear01() / (GearNames.Length - 1) - 0.5f : 0f;
            _leverPivot.localRotation = Quaternion.Euler(t * gearLeverTilt * 2f, 0f, 0f);
        }

        if (_gearText != null)
            _gearText.text = GearNames[CurrentGear01()];
    }

    int CurrentGear01()
    {
        return Mathf.Clamp(truck.CurrentGear, 0, GearNames.Length - 1);
    }

    void BuildSteeringWheel()
    {
        var pivot = new GameObject("SteeringWheel");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = steeringWheelOffset;
        pivot.transform.localRotation = Quaternion.Euler(steeringWheelEuler);

        var rim = new GameObject("Rim");
        rim.transform.SetParent(pivot.transform, false);
        rim.AddComponent<MeshFilter>().sharedMesh = BuildTorusMesh(wheelRadius, wheelTube, 32, 12);
        rim.AddComponent<MeshRenderer>().sharedMaterial = MakeMaterial(rimColor);

        var hub = Primitive(PrimitiveType.Cylinder, "Hub", pivot.transform);
        hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hub.transform.localScale = new Vector3(wheelRadius * 0.4f, 0.02f, wheelRadius * 0.4f);
        Paint(hub, rimColor);

        for (int s = 0; s < 3; s++)
        {
            float a = s * 120f;
            var spoke = Primitive(PrimitiveType.Cube, "Spoke" + s, pivot.transform);
            spoke.transform.localPosition = Quaternion.Euler(0f, 0f, a) * new Vector3(wheelRadius * 0.5f, 0f, 0f);
            spoke.transform.localRotation = Quaternion.Euler(0f, 0f, a);
            spoke.transform.localScale = new Vector3(wheelRadius, 0.02f, 0.015f);
            Paint(spoke, trimColor);
        }

        _wheelPivot = pivot.transform;
    }

    void BuildGearLever()
    {
        var pivot = new GameObject("GearLever");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = gearLeverOffset;

        var boot = Primitive(PrimitiveType.Cylinder, "Boot", pivot.transform);
        boot.transform.localScale = new Vector3(0.1f, 0.012f, 0.1f);
        Paint(boot, rimColor);

        var stick = Primitive(PrimitiveType.Cylinder, "Lever", pivot.transform);
        stick.transform.localPosition = new Vector3(0f, gearLeverLength * 0.5f, 0f);
        stick.transform.localScale = new Vector3(0.02f, gearLeverLength * 0.5f, 0.02f);
        Paint(stick, trimColor);

        var knob = Primitive(PrimitiveType.Sphere, "Knob", pivot.transform);
        knob.transform.localPosition = new Vector3(0f, gearLeverLength, 0f);
        knob.transform.localScale = Vector3.one * 0.06f;
        Paint(knob, rimColor);

        _leverPivot = pivot.transform;
    }

    void BuildGearDisplay()
    {
        var panel = Primitive(PrimitiveType.Cube, "GearPanel", transform);
        panel.transform.localPosition = gearDisplayOffset;
        panel.transform.localRotation = Quaternion.Euler(gearDisplayEuler);
        panel.transform.localScale = new Vector3(0.34f, 0.16f, 0.012f);
        Paint(panel, rimColor);

        Vector3 faceDir = Quaternion.Euler(gearDisplayEuler) * Vector3.forward;

        var go = new GameObject("GearText");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = gearDisplayOffset + faceDir * 0.02f;
        go.transform.localRotation = Quaternion.Euler(gearDisplayEuler);
        go.transform.localScale = Vector3.one * gearDisplayScale;

        _gearText = go.AddComponent<TextMeshPro>();
        _gearText.text = "N";
        _gearText.fontSize = 4f;
        _gearText.alignment = TextAlignmentOptions.Center;
        _gearText.color = displayColor;
        _gearText.textWrappingMode = TextWrappingModes.NoWrap;
        _gearText.rectTransform.sizeDelta = new Vector2(6f, 2f);
        if (TMP_Settings.defaultFontAsset != null) _gearText.font = TMP_Settings.defaultFontAsset;
    }

    GameObject Primitive(PrimitiveType type, string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.transform.SetParent(parent, false);
        return go;
    }

    void Paint(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = MakeMaterial(color);
    }

    static UnityEngine.Material MakeMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new UnityEngine.Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.2f);
        return m;
    }

    static Mesh BuildTorusMesh(float major, float minor, int majorSeg, int minorSeg)
    {
        majorSeg = Mathf.Max(6, majorSeg);
        minorSeg = Mathf.Max(6, minorSeg);

        var verts = new Vector3[majorSeg * minorSeg];
        var norms = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];

        for (int i = 0; i < majorSeg; i++)
        {
            float u = (float)i / majorSeg * Mathf.PI * 2f;
            Vector3 center = new Vector3(Mathf.Cos(u) * major, Mathf.Sin(u) * major, 0f);
            for (int j = 0; j < minorSeg; j++)
            {
                float v = (float)j / minorSeg * Mathf.PI * 2f;
                Vector3 n = new Vector3(Mathf.Cos(v) * Mathf.Cos(u), Mathf.Cos(v) * Mathf.Sin(u), Mathf.Sin(v));
                int idx = i * minorSeg + j;
                verts[idx] = center + n * minor;
                norms[idx] = n;
                uvs[idx] = new Vector2((float)i / majorSeg, (float)j / minorSeg);
            }
        }

        var tris = new int[majorSeg * minorSeg * 6];
        int t = 0;
        for (int i = 0; i < majorSeg; i++)
        {
            int i2 = (i + 1) % majorSeg;
            for (int j = 0; j < minorSeg; j++)
            {
                int j2 = (j + 1) % minorSeg;
                int a = i * minorSeg + j;
                int b = i2 * minorSeg + j;
                int c = i2 * minorSeg + j2;
                int d = i * minorSeg + j2;
                tris[t++] = a; tris[t++] = b; tris[t++] = c;
                tris[t++] = a; tris[t++] = c; tris[t++] = d;
            }
        }

        var mesh = new Mesh { name = "SteeringWheelRim" };
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }
}
