using UnityEditor;
using UnityEngine;

/// <summary>
/// Ventana para ajustar el humo del escape (Police_Car/Exhaust_Smoke) sin tener que
/// buscar los modulos del ParticleSystem en el Inspector.
/// Los cambios se ven en vivo en la Scene/Game y se guardan con Ctrl+S.
/// </summary>
public class SmokeTool : EditorWindow
{
    private const string SmokePath = "Police_Car/Exhaust_Smoke";

    // Posicion local respecto a Police_Car
    private Vector3 localPosition = new Vector3(0.5f, 0.95f, 1.9f);

    // Modulo Main
    private float lifetimeMin = 2.5f;
    private float lifetimeMax = 4f;
    private float sizeMin = 0.6f;
    private float sizeMax = 1.2f;
    private float speedMin = 0.2f;
    private float speedMax = 0.6f;
    private float gravity = -0.06f;
    private Color startColor = Color.white;

    // Emision
    private float rateOverTime = 18f;

    // Forma (cono)
    private float coneAngle = 15f;
    private float coneRadius = 0.08f;

    private bool systemEnabled = true;
    private string status = "";

    [MenuItem("Tools/Humo del escape")]
    public static void Open()
    {
        GetWindow<SmokeTool>("Humo del escape");
    }

    private void OnEnable()
    {
        Load();
    }

    private ParticleSystem FindSmoke()
    {
        GameObject go = GameObject.Find(SmokePath);
        return go != null ? go.GetComponent<ParticleSystem>() : null;
    }

    private void Load()
    {
        ParticleSystem ps = FindSmoke();
        if (ps == null)
        {
            status = "No se encontro " + SmokePath;
            return;
        }

        localPosition = ps.transform.localPosition;

        ParticleSystem.MainModule main = ps.main;
        lifetimeMin = main.startLifetime.constantMin;
        lifetimeMax = main.startLifetime.constantMax;
        sizeMin = main.startSize.constantMin;
        sizeMax = main.startSize.constantMax;
        speedMin = main.startSpeed.constantMin;
        speedMax = main.startSpeed.constantMax;
        gravity = main.gravityModifier.constant;
        startColor = main.startColor.color;
        systemEnabled = main.playOnAwake;

        rateOverTime = ps.emission.rateOverTime.constant;

        ParticleSystem.ShapeModule shape = ps.shape;
        coneAngle = shape.angle;
        coneRadius = shape.radius;

        status = "Valores cargados desde la escena.";
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Humo del escape", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Ajusta los valores y pulsa 'Aplicar'. Se ve en vivo en la Scene/Game.\n" +
            "Guarda la escena con Ctrl+S cuando termines.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Posicion (local respecto a Police_Car)", EditorStyles.boldLabel);
        localPosition = EditorGUILayout.Vector3Field("Posicion", localPosition);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Frente del coche")) localPosition = new Vector3(0.5f, 0.95f, 1.9f);
            if (GUILayout.Button("Parte trasera")) localPosition = new Vector3(0.55f, 0.7f, -2.25f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Emision", EditorStyles.boldLabel);
        rateOverTime = EditorGUILayout.Slider("Particulas por segundo", rateOverTime, 0f, 120f);
        startColor = EditorGUILayout.ColorField("Color inicial", startColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vida y tamano", EditorStyles.boldLabel);
        lifetimeMin = EditorGUILayout.Slider("Vida minima", lifetimeMin, 0.2f, 10f);
        lifetimeMax = EditorGUILayout.Slider("Vida maxima", lifetimeMax, 0.2f, 12f);
        sizeMin = EditorGUILayout.Slider("Tamano minimo", sizeMin, 0.05f, 3f);
        sizeMax = EditorGUILayout.Slider("Tamano maximo", sizeMax, 0.05f, 4f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movimiento", EditorStyles.boldLabel);
        speedMin = EditorGUILayout.Slider("Velocidad minima", speedMin, 0f, 5f);
        speedMax = EditorGUILayout.Slider("Velocidad maxima", speedMax, 0f, 6f);
        gravity = EditorGUILayout.Slider("Gravedad (negativa = sube)", gravity, -1f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Forma (cono)", EditorStyles.boldLabel);
        coneAngle = EditorGUILayout.Slider("Angulo", coneAngle, 0f, 60f);
        coneRadius = EditorGUILayout.Slider("Radio", coneRadius, 0f, 1f);

        EditorGUILayout.Space();
        systemEnabled = EditorGUILayout.Toggle("Activo al iniciar", systemEnabled);

        EditorGUILayout.Space();
        if (GUILayout.Button("Aplicar", GUILayout.Height(30)))
        {
            status = Apply();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preset: ligero"))
            {
                rateOverTime = 10f; sizeMin = 0.4f; sizeMax = 0.9f;
                lifetimeMin = 2f; lifetimeMax = 3f; gravity = -0.05f;
                status = Apply();
            }
            if (GUILayout.Button("Preset: denso"))
            {
                rateOverTime = 30f; sizeMin = 0.8f; sizeMax = 1.6f;
                lifetimeMin = 3f; lifetimeMax = 5f; gravity = -0.08f;
                status = Apply();
            }
        }

        if (GUILayout.Button("Recargar valores de la escena"))
        {
            Load();
            Repaint();
        }

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.None);
    }

    /// <summary>Aplica los valores al ParticleSystem y devuelve un mensaje.</summary>
    public static string ApplyValues(
        Vector3 position,
        float rate,
        Color color,
        float lifeMin, float lifeMax,
        float sizeMinV, float sizeMaxV,
        float speedMinV, float speedMaxV,
        float gravityModifier,
        float angle, float radius,
        bool playOnAwake)
    {
        ParticleSystem ps = GameObject.Find(SmokePath) != null
            ? GameObject.Find(SmokePath).GetComponent<ParticleSystem>()
            : null;
        if (ps == null) return "No se encontro " + SmokePath;

        Undo.RecordObject(ps.transform, "Ajustar humo");
        ps.transform.localPosition = position;

        Undo.RecordObject(ps, "Ajustar humo");
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Min(lifeMin, lifeMax), Mathf.Max(lifeMin, lifeMax));
        main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Min(sizeMinV, sizeMaxV), Mathf.Max(sizeMinV, sizeMaxV));
        main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Min(speedMinV, speedMaxV), Mathf.Max(speedMinV, speedMaxV));
        main.gravityModifier = gravityModifier;
        main.startColor = color;
        main.playOnAwake = playOnAwake;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = Mathf.Max(0f, rate);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.angle = angle;
        shape.radius = radius;

        EditorUtility.SetDirty(ps);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ps.gameObject.scene);
        return "Aplicado. Recuerda guardar con Ctrl+S.";
    }

    private string Apply()
    {
        return ApplyValues(
            localPosition, rateOverTime, startColor,
            lifetimeMin, lifetimeMax, sizeMin, sizeMax,
            speedMin, speedMax, gravity, coneAngle, coneRadius, systemEnabled);
    }
}
