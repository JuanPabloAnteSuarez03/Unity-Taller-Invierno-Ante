using UnityEditor;
using UnityEngine;

/// <summary>
/// Ventana para ajustar el enterramiento del coche y la uniformidad del terreno nevado.
/// Regenera la malla de Snow_Terrain (Assets/Models/SnowTerrainDrifts.asset) y mueve
/// el coche en vertical. Police_Car se queda siempre en Y = -0.5 (lo exige el taller);
/// el ajuste fino se hace en el hijo Car_Mesh.
/// </summary>
public class TerrainSnowTool : EditorWindow
{
    private const string TerrainPath = "Snow_Terrain";
    private const string CarMeshPath = "Police_Car/Car_Mesh";
    private const string AssetPath = "Assets/Models/SnowTerrainDrifts.asset";

    private const float TerrainSize = 120f;
    private const int Resolution = 240;

    // Altura de nieve delante (morro, -Z) y detras (cola, +Z)
    private float frontSnow = 0.22f;
    private float rearSnow = 0.00f;

    // Cuanto varia el terreno (0 = plano perfecto)
    private float undulation = 1.00f;   // multiplica el ruido
    private float drifts = 1.00f;       // multiplica los ventisqueros
    private float cleanRadius = 4.0f;   // radio sin ruido alrededor del coche

    // Altura del coche (hijo Car_Mesh)
    private float carY = -0.05f;

    private string status = "";

    [MenuItem("Tools/Terreno y enterramiento")]
    public static void Open()
    {
        GetWindow<TerrainSnowTool>("Terreno y enterramiento");
    }

    private void OnEnable()
    {
        GameObject car = GameObject.Find(CarMeshPath);
        if (car != null) carY = car.transform.localPosition.y;
        status = "";
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Enterramiento del coche", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Define cuanta nieve hay bajo el morro y bajo la cola. Si son iguales el " +
            "enterramiento es uniforme; si el morro es mayor, el coche queda hundido de frente.\n" +
            "Police_Car se mantiene en Y = -0.5; el ajuste vertical se aplica a Car_Mesh.",
            MessageType.Info);

        frontSnow = EditorGUILayout.Slider("Nieve en el morro", frontSnow, 0f, 0.60f);
        rearSnow = EditorGUILayout.Slider("Nieve en la cola", rearSnow, 0f, 0.60f);
        carY = EditorGUILayout.Slider("Altura del coche (Car_Mesh Y)", carY, -0.60f, 0.30f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Uniformidad del terreno", EditorStyles.boldLabel);
        undulation = EditorGUILayout.Slider("Ondulacion (0 = plano)", undulation, 0f, 1.50f);
        drifts = EditorGUILayout.Slider("Ventisqueros", drifts, 0f, 1.50f);
        cleanRadius = EditorGUILayout.Slider("Zona limpia alrededor del coche", cleanRadius, 2f, 10f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Aplicar", GUILayout.Height(30)))
        {
            status = Apply(frontSnow, rearSnow, undulation, drifts, cleanRadius, carY);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preset: uniforme"))
            {
                frontSnow = 0.18f; rearSnow = 0.18f; undulation = 0.15f; drifts = 0.2f;
                status = Apply(frontSnow, rearSnow, undulation, drifts, cleanRadius, carY);
            }
            if (GUILayout.Button("Preset: hundido de morro"))
            {
                frontSnow = 0.34f; rearSnow = 0.02f; undulation = 1.0f; drifts = 1.0f;
                status = Apply(frontSnow, rearSnow, undulation, drifts, cleanRadius, carY);
            }
        }

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Nieve sobre la carroceria", EditorStyles.boldLabel);
        if (GUILayout.Button("Abrir 'Nieve del coche' (capa sobre Car_Mesh)"))
        {
            SnowCapTool.Open();
        }
    }

    /// <summary>Regenera el terreno y ajusta la altura del coche.</summary>
    public static string Apply(float front, float rear, float undulationMul, float driftMul, float cleanR, float newCarY)
    {
        GameObject terrain = GameObject.Find(TerrainPath);
        if (terrain == null) return "No se encontro " + TerrainPath;

        int res = Resolution;
        float size = TerrainSize;
        Vector3[] verts = new Vector3[(res + 1) * (res + 1)];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[res * res * 6];

        float cleanStart = Mathf.Max(0f, cleanR);
        float cleanEnd = cleanStart + 6f;

        for (int iz = 0; iz <= res; iz++)
        {
            for (int ix = 0; ix <= res; ix++)
            {
                float x = ((float)ix / res - 0.5f) * size;
                float z = ((float)iz / res - 0.5f) * size;

                // Pendiente morro -> cola
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-4.5f, 4.5f, z));
                float h = Mathf.Lerp(front, rear, t);

                // Ruido y ventisqueros solo fuera de la zona limpia
                float r = Mathf.Sqrt(x * x + z * z);
                float far = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(cleanStart, cleanEnd, r));

                float n1 = Mathf.PerlinNoise(x * 0.05f + 13.7f, z * 0.05f + 7.3f) - 0.5f;
                float n2 = Mathf.PerlinNoise(x * 0.16f + 3.1f, z * 0.16f + 19.5f) - 0.5f;
                h += (n1 * 0.30f + n2 * 0.10f) * far * undulationMul;

                float d1 = Mathf.Pow((x - 5.0f) / 3.5f, 2f) + Mathf.Pow((z + 6.5f) / 3.5f, 2f);
                h += Mathf.Exp(-d1) * 0.45f * far * driftMul;
                float d2 = Mathf.Pow((x + 5.5f) / 4.0f, 2f) + Mathf.Pow((z - 4.0f) / 4.0f, 2f);
                h += Mathf.Exp(-d2) * 0.35f * far * driftMul;

                int i = iz * (res + 1) + ix;
                verts[i] = new Vector3(x, h, z);
                uvs[i] = new Vector2((float)ix / res, (float)iz / res);
            }
        }

        int t2 = 0;
        for (int iz = 0; iz < res; iz++)
        {
            for (int ix = 0; ix < res; ix++)
            {
                int i0 = iz * (res + 1) + ix;
                int i1 = i0 + 1;
                int i2 = i0 + (res + 1);
                int i3 = i2 + 1;
                tris[t2++] = i0; tris[t2++] = i2; tris[t2++] = i1;
                tris[t2++] = i1; tris[t2++] = i2; tris[t2++] = i3;
            }
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetPath);
        bool created = false;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "SnowTerrainDrifts";
            created = true;
        }

        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (created) AssetDatabase.CreateAsset(mesh, AssetPath);
        else EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();

        terrain.transform.localScale = Vector3.one;
        MeshFilter mf = terrain.GetComponent<MeshFilter>();
        if (mf == null) mf = terrain.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshCollider mc = terrain.GetComponent<MeshCollider>();
        if (mc != null)
        {
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }

        GameObject car = GameObject.Find(CarMeshPath);
        if (car != null)
        {
            Undo.RecordObject(car.transform, "Altura del coche");
            Vector3 lp = car.transform.localPosition;
            lp.y = newCarY;
            car.transform.localPosition = lp;
            EditorUtility.SetDirty(car);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.scene);
        return "Terreno regenerado (" + res + "x" + res + "). Guarda con Ctrl+S.";
    }
}
