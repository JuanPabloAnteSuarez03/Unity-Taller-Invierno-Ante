using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de editor para regenerar la capa de nieve del coche (Car_Mesh/Snow_Cap).
/// Toma la malla del cuerpo, se queda con las caras que miran hacia arriba y las
/// desplaza hacia fuera segun su normal, con mas espesor en el capo y el frontal.
///
/// Nota: la malla se actualiza EN SITIO (mesh.Clear + SetVertices) para no romper
/// la referencia que tiene el MeshFilter de la escena.
/// </summary>
public class SnowCapTool : EditorWindow
{
    private const string BodyPath = "Police_Car/Car_Mesh/body";
    private const string CarMeshPath = "Police_Car/Car_Mesh";
    private const string AssetPath = "Assets/Models/CarSnow.asset";
    private const string SnowMaterialPath = "Assets/Materials/Snow.mat";

    // Valores por defecto = los aplicados originalmente.
    private float baseThickness = 0.010f;
    private float frontExtra = 0.050f;
    private float minNormalY = 0.45f;
    private float frontStart = 0.30f;

    private string status = "";

    [MenuItem("Tools/Nieve del coche")]
    public static void Open()
    {
        GetWindow<SnowCapTool>("Nieve del coche");
    }

    private void OnEnable()
    {
        status = "";
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Capa de nieve sobre Car_Mesh", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Valores en unidades del modelo (1.0 = 1 m del modelo, ~1.15 m en la escena).\n" +
            "Mueve los sliders y pulsa 'Regenerar nieve'. Despues guarda con Ctrl+S.",
            MessageType.Info);

        baseThickness = EditorGUILayout.Slider("Espesor base", baseThickness, 0f, 0.03f);
        frontExtra = EditorGUILayout.Slider("Extra en el capo / frontal", frontExtra, 0f, 0.08f);
        minNormalY = EditorGUILayout.Slider("Solo caras con inclinacion >", minNormalY, 0.10f, 0.90f);
        frontStart = EditorGUILayout.Slider("Donde empieza el frontal", frontStart, 0f, 0.90f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Regenerar nieve", GUILayout.Height(30)))
        {
            status = Regenerate(baseThickness, frontExtra, minNormalY, frontStart);
            Repaint();
        }

        if (GUILayout.Button("Quitar la nieve del coche"))
        {
            bool ok = Remove();
            status = ok ? "Nieve eliminada." : "No habia nieve que quitar.";
            Repaint();
        }

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(status))
            EditorGUILayout.HelpBox(status, MessageType.None);
    }

    public static bool Remove()
    {
        GameObject car = GameObject.Find(CarMeshPath);
        if (car == null) return false;

        Transform cap = car.transform.Find("Snow_Cap");
        if (cap == null) return false;

        Object.DestroyImmediate(cap.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(car.scene);
        return true;
    }

    /// <summary>Reconstruye la capa de nieve y devuelve un mensaje de resultado.</summary>
    public static string Regenerate(float baseT, float frontT, float minNy, float frontStart)
    {
        GameObject car = GameObject.Find(CarMeshPath);
        if (car == null) return "No se encontro " + CarMeshPath;

        Transform body = car.transform.Find("body");
        if (body == null) return "No se encontro el nodo 'body'.";

        MeshFilter mf = body.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return "El cuerpo no tiene malla.";

        Mesh src = mf.sharedMesh;
        Vector3[] v = src.vertices;
        Vector3[] n = src.normals;
        Vector2[] uv = src.uv;
        int[] t = src.triangles;

        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        for (int i = 0; i < v.Length; i++)
        {
            if (v[i].z < minZ) minZ = v[i].z;
            if (v[i].z > maxZ) maxZ = v[i].z;
        }
        float spanZ = Mathf.Max(0.0001f, maxZ - minZ);

        Vector3[] displaced = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            float up = Mathf.Clamp01(n[i].y);
            float front = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(minZ + spanZ * frontStart, maxZ, v[i].z));
            displaced[i] = v[i] + n[i] * ((baseT + frontT * front) * up);
        }

        List<int> keep = new List<int>();
        int keptTris = 0;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = v[t[i]];
            Vector3 b = v[t[i + 1]];
            Vector3 c = v[t[i + 2]];
            Vector3 fn = Vector3.Cross(b - a, c - a).normalized;
            if (fn.y < minNy) continue;
            keep.Add(t[i]);
            keep.Add(t[i + 1]);
            keep.Add(t[i + 2]);
            keptTris++;
        }

        if (keptTris == 0)
            return "Ninguna cara supera la inclinacion indicada. Baja el valor de inclinacion.";

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        Dictionary<int, int> map = new Dictionary<int, int>();

        for (int i = 0; i < keep.Count; i++)
        {
            int old = keep[i];
            if (!map.ContainsKey(old))
            {
                map[old] = verts.Count;
                verts.Add(displaced[old]);
                norms.Add(Vector3.Slerp(n[old], Vector3.up, 0.6f).normalized);
                uvs.Add(uv[old]);
            }
            tris.Add(map[old]);
        }

        // Actualizar la malla existente si ya esta creada (evita romper referencias).
        Mesh snow = AssetDatabase.LoadAssetAtPath<Mesh>(AssetPath);
        bool created = false;
        if (snow == null)
        {
            snow = new Mesh();
            snow.name = "CarSnow";
            created = true;
        }

        snow.Clear();
        snow.indexFormat = src.indexFormat;
        snow.SetVertices(verts);
        snow.SetNormals(norms);
        snow.SetUVs(0, uvs);
        snow.SetTriangles(tris, 0);
        snow.RecalculateBounds();

        if (created) AssetDatabase.CreateAsset(snow, AssetPath);
        else EditorUtility.SetDirty(snow);
        AssetDatabase.SaveAssets();

        Transform cap = car.transform.Find("Snow_Cap");
        if (cap == null)
        {
            GameObject go = new GameObject("Snow_Cap");
            go.transform.SetParent(car.transform, false);
            cap = go.transform;
            cap.gameObject.AddComponent<MeshFilter>();
            MeshRenderer r0 = cap.gameObject.AddComponent<MeshRenderer>();
            r0.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // La malla esta en el espacio local del cuerpo: copiamos su transform.
        cap.localPosition = body.localPosition;
        cap.localRotation = body.localRotation;
        cap.localScale = body.localScale;

        MeshFilter capFilter = cap.GetComponent<MeshFilter>();
        if (capFilter == null) capFilter = cap.gameObject.AddComponent<MeshFilter>();
        capFilter.sharedMesh = snow;

        MeshRenderer capRenderer = cap.GetComponent<MeshRenderer>();
        if (capRenderer == null) capRenderer = cap.gameObject.AddComponent<MeshRenderer>();
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(SnowMaterialPath);
        if (mat != null) capRenderer.sharedMaterial = mat;
        capRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(car.scene);
        return "Nieve regenerada: " + keptTris + " caras, " + verts.Count + " vertices.";
    }
}
