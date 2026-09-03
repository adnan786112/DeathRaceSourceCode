using UnityEngine;
using Unity.Netcode;

public class MinimapProgressDriver : NetworkBehaviour
{
    [SerializeField] private Material greenMeshMaterial;
    [SerializeField] private int bakeResolution = 512; // baked once, not per-frame

    private static readonly int ProgressID = Shader.PropertyToID("_Progress");

    private WaypointCircuit _circuit;
    private Vector3[] _bakedPoints;
    private float _totalLength;
    private float _lastDist = 0f;
    private bool _baked = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { enabled = false; return; }
        _circuit = WaypointCircuit.instance;
    }

    private void TryBake()
    {
       
        if (_circuit == null) return;

        _totalLength = _circuit.Length;
        _bakedPoints = new Vector3[bakeResolution];
        float step = _totalLength / bakeResolution;

        for (int i = 0; i < bakeResolution; i++)
            _bakedPoints[i] = _circuit.GetRoutePosition(i * step);

        _baked = true;
    }

    private void Update()
    {
        if (!IsOwner || greenMeshMaterial == null) return;

        if (!_baked) { TryBake(); return; }
        if (SaveScript.RaceStart)
        {

            float progress = GetProgress(transform.position);
            greenMeshMaterial.SetFloat(ProgressID, progress);
        }
    }

    private float GetProgress(Vector3 carPos)
    {
        float stepSize = _totalLength / bakeResolution;

        // Search only within ±10% of track around last known position
        int searchWindow = bakeResolution / 10;
        int lastIndex = Mathf.RoundToInt(_lastDist / stepSize);

        float bestSqr = float.MaxValue;
        int bestIndex = lastIndex;

        for (int i = -searchWindow; i <= searchWindow; i++)
        {
            int idx = (lastIndex + i + bakeResolution) % bakeResolution;
            float sqr = (carPos - _bakedPoints[idx]).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestIndex = idx;
            }
        }

        _lastDist = bestIndex * stepSize;
        return _lastDist / _totalLength;
    }
}