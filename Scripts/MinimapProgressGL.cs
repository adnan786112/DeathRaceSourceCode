using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode;
public class MinimapProgressGL : MonoBehaviour
{
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private int lineThickness = 8;
    [SerializeField] private float offset;
    private Transform[] waypoints;
    private Material glMaterial;

    private void Start()
    {
        StartCoroutine(WaitForWaypoints());
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        if (glMaterial != null)
            Destroy(glMaterial);
    }

    private System.Collections.IEnumerator WaitForWaypoints()
    {
        while (AssignPosScript.instance == null)
            yield return null;

        while (AssignPosScript.instance.waypointCircuit == null)
            yield return null;

        Transform[] circuitWaypoints = AssignPosScript.instance.waypointCircuit.Waypoints;
        waypoints = new Transform[circuitWaypoints.Length];
        for (int i = 0; i < circuitWaypoints.Length; i++)
            waypoints[i] = circuitWaypoints[i];

        glMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        glMaterial.hideFlags = HideFlags.HideAndDontSave;
        glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        glMaterial.SetInt("_ZWrite", 0);
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam != minimapCamera) return;
        if (waypoints == null || glMaterial == null) return;
        if (ProgressTracker.localInstance == null) return;

        // Map 161 progress waypoints to 77 circuit waypoints
        int rawWP = ProgressTracker.localInstance.CurrentWP.Value;
        int currentWP = Mathf.RoundToInt((float)rawWP / 161f * waypoints.Length);
        currentWP = Mathf.Clamp(currentWP, 0, waypoints.Length - 1);

        GL.PushMatrix();
        glMaterial.SetPass(0);
        GL.LoadPixelMatrix(0, minimapCamera.pixelWidth, minimapCamera.pixelHeight, 0);

        // White — travelled
        for (int i = 0; i < currentWP - 1; i++)
            DrawScreenLine(waypoints[i].position, waypoints[i + 1].position, Color.white);

        // Green — remaining
        for (int i = currentWP; i < waypoints.Length - 1; i++)
            DrawScreenLine(waypoints[i].position, waypoints[i + 1].position, Color.green);

        GL.PopMatrix();
    }

    private void DrawScreenLine(Vector3 worldA, Vector3 worldB, Color color)
    {
        worldA.y = -173f;
        worldB.y = -173f;
        // Shift waypoints to align with road center — adjust X and Z
        worldA.x += offset;  // try different values
        worldB.x += offset;
        worldA.z += offset;
        worldB.z += offset;
        Vector3 screenA = minimapCamera.WorldToScreenPoint(worldA);
        Vector3 screenB = minimapCamera.WorldToScreenPoint(worldB);

        if (screenA.z < 0 || screenB.z < 0) return;

        Vector2 dir = new Vector2(screenB.x - screenA.x, screenB.y - screenA.y).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        float half = lineThickness * 0.5f;

        Vector3 a1 = new Vector3(screenA.x + perp.x * half, screenA.y + perp.y * half, 0);
        Vector3 a2 = new Vector3(screenA.x - perp.x * half, screenA.y - perp.y * half, 0);
        Vector3 b1 = new Vector3(screenB.x + perp.x * half, screenB.y + perp.y * half, 0);
        Vector3 b2 = new Vector3(screenB.x - perp.x * half, screenB.y - perp.y * half, 0);

        GL.Begin(GL.TRIANGLES);
        GL.Color(color);
        GL.Vertex(a1);
        GL.Vertex(a2);
        GL.Vertex(b1);
        GL.Vertex(a2);
        GL.Vertex(b2);
        GL.Vertex(b1);
        GL.End();
    }
}