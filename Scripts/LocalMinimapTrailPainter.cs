using UnityEngine;

public class LocalMinimapTrailPainter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-filled at runtime from MinimapCameraRef.Instance — no need to assign manually on the prefab.")]
    private Camera minimapCamera;

    [Tooltip("The Material asset assigned to your minimap RawImage. Drag the SAME material asset used on the RawImage here.")]
    [SerializeField] private Material minimapDisplayMaterial;

    [Header("Trail Settings")]
    [SerializeField] private int trailTextureSize = 512;
    [SerializeField] private float brushRadius = 8f; // in pixels — tune to road width
    [Tooltip("Optional. Leave empty to auto-generate a soft circular brush at runtime.")]
    [SerializeField] private Texture2D softBrushTexture;

    private RenderTexture _trailMask;
    private Material _brushMaterial;
    private static readonly int TrailMaskID = Shader.PropertyToID("_TrailMask");

    private void Start()
    {
        // --- Resolve the minimap camera from the scene singleton ---
        // Prefabs can't hold direct scene-object references, so this is looked up
        // at runtime instead of being dragged in via the Inspector.
        if (minimapCamera == null)
        {
            minimapCamera = MinimapCameraRef.Instance;
        }

        if (minimapCamera == null)
        {
            Debug.LogError("LocalMinimapTrailPainter: MinimapCameraRef.Instance is null — " +
                            "is the minimap camera in the scene and does it have MinimapCameraRef attached?");
            enabled = false;
            return;
        }

        // --- Create the persistent trail mask render texture ---
        // This starts fully black (unrevealed) and only ever accumulates white
        // brush strokes over the course of the race — it is never cleared mid-race.
        _trailMask = new RenderTexture(trailTextureSize, trailTextureSize, 0, RenderTextureFormat.R8)
        {
            name = "LocalMinimapTrailMask",
            wrapMode = TextureWrapMode.Clamp
        };
        _trailMask.Create();

        RenderTexture.active = _trailMask;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;

        // --- Soft brush texture: auto-generate if none was assigned ---
        if (softBrushTexture == null)
        {
            softBrushTexture = GenerateSoftBrushTexture();
        }

        // --- Brush material: additive blend so repeated passes accumulate toward
        // fully revealed instead of alpha-blending and staying foggy ---
        Shader additiveShader = Shader.Find("Hidden/MinimapBrushAdditive");
        if (additiveShader == null)
        {
            Debug.LogError("LocalMinimapTrailPainter: Shader 'Hidden/MinimapBrushAdditive' not found. " +
                            "Make sure MinimapBrushAdditive.shader exists in the project.");
            enabled = false;
            return;
        }

        _brushMaterial = new Material(additiveShader)
        {
            mainTexture = softBrushTexture
        };

        // --- Feed the trail mask into the display material ---
        // This is the SAME material asset used on the minimap RawImage, so updating
        // its _TrailMask texture here automatically updates what's shown on screen.
        if (minimapDisplayMaterial != null)
        {
            minimapDisplayMaterial.SetTexture(TrailMaskID, _trailMask);
        }
        else
        {
            Debug.LogWarning("LocalMinimapTrailPainter: minimapDisplayMaterial is not assigned. " +
                              "Drag the RawImage's material asset into this field.");
        }
    }

    private void Update()
    {
        if (_trailMask == null) return;
        if (SaveScript.RaceStart)
        {
            PaintAtCurrentPosition();
        }
    }

    private void PaintAtCurrentPosition()
    {
        Vector2 uv = WorldToMinimapUV(transform.position);

        float px = uv.x * trailTextureSize;
        float py = uv.y * trailTextureSize;

        RenderTexture.active = _trailMask;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, trailTextureSize, trailTextureSize, 0);

        _brushMaterial.SetPass(0);
        DrawQuad(px, py, brushRadius);

        GL.PopMatrix();
        RenderTexture.active = null;
    }

    private void DrawQuad(float cx, float cy, float radius)
    {
        GL.Begin(GL.QUADS);
        GL.Color(Color.white);
        GL.TexCoord2(0, 0); GL.Vertex3(cx - radius, cy - radius, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(cx + radius, cy - radius, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(cx + radius, cy + radius, 0);
        GL.TexCoord2(0, 1); GL.Vertex3(cx - radius, cy + radius, 0);
        GL.End();
    }

    private Vector2 WorldToMinimapUV(Vector3 worldPos)
    {
        Vector3 viewportPos = minimapCamera.WorldToViewportPoint(worldPos);

        // Camera is rotated an extra 90° on Y vs. the base texture's orientation,
        // so screen-right/up are swapped relative to world X/Z. Un-rotate here:
        return new Vector2(1f - viewportPos.y, viewportPos.x);
    }

    private Texture2D GenerateSoftBrushTexture(int size = 64)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / maxDist));
                alpha = Mathf.SmoothStep(0f, 1f, alpha); // soft falloff

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        if (_trailMask != null) _trailMask.Release();
        if (_brushMaterial != null) Destroy(_brushMaterial);
        if (softBrushTexture != null) Destroy(softBrushTexture);
    }
}