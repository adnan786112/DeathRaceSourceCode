using UnityEngine;

public class ShowroomCameraOrbit : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float orbitSpeed = 5f;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minVerticalAngle = -10f;
    [SerializeField] private float maxVerticalAngle = 30f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;

    private float _angleY;
    private float _angleX;

    private void Start()
    {
        _angleY = transform.eulerAngles.y;
        _angleX = transform.eulerAngles.x;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            _angleY += Input.GetAxis("Mouse X") * orbitSpeed;
            _angleX -= Input.GetAxis("Mouse Y") * orbitSpeed;
            _angleX = Mathf.Clamp(_angleX, minVerticalAngle, maxVerticalAngle);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            distance -= scroll * zoomSpeed * 10f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(_angleX, _angleY, 0f);
        transform.position = target.position - rotation * Vector3.forward * distance;
        transform.LookAt(target);
    }
}