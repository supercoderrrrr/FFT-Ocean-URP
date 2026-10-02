using UnityEngine;

[DisallowMultipleComponent]
public sealed class FFTOceanShowcaseCamera : MonoBehaviour
{
    [SerializeField] private bool cinematicMotion;
    [SerializeField, Min(0f)] private float cinematicYawSpeed = 0.35f;
    [SerializeField, Min(0f)] private float moveSpeed = 18f;
    [SerializeField, Min(0f)] private float lookSensitivity = 2.2f;
    [SerializeField] private Vector2 pitchLimits = new Vector2(-80f, 80f);
    [SerializeField] private float minimumHeight = -16f;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            cinematicMotion = !cinematicMotion;

        if (Input.GetMouseButton(1))
        {
            cinematicMotion = false;
            yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxisRaw("Mouse Y") * lookSensitivity;
        }
        else if (cinematicMotion)
        {
            yaw += cinematicYawSpeed * Time.deltaTime;
            pitch = 1.5f + Mathf.Sin(Time.time * 0.12f) * 1.0f;
        }

        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 input = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            GetVerticalInput(),
            Input.GetAxisRaw("Vertical")
        );
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? 3f : 1f;
        transform.position += transform.TransformDirection(input) * (moveSpeed * speedMultiplier * Time.deltaTime);
        transform.position = new Vector3(
            transform.position.x,
            Mathf.Max(minimumHeight, transform.position.y),
            transform.position.z);
    }

    private static float GetVerticalInput()
    {
        float value = 0f;
        if (Input.GetKey(KeyCode.E)) value += 1f;
        if (Input.GetKey(KeyCode.Q)) value -= 1f;
        return value;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
