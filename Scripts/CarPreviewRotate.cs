using UnityEngine;

public class CarPreviewRotate : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minFOV = 20f;
    [SerializeField] private float maxFOV = 60f;

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            float drag = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, -drag * rotateSpeed, Space.World);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && _cam != null)
        {
            _cam.fieldOfView -= scroll * zoomSpeed * 10f;
            _cam.fieldOfView = Mathf.Clamp(_cam.fieldOfView, minFOV, maxFOV);
        }
    }
}

