using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 8f;
    public float fastSpeed = 20f;

    [Header("Mouse")]
    public float mouseSensitivity = 2.0f;

    private float yaw;
    private float pitch;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Rotazione mouse
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        // Velocità
        float speed =
            Input.GetKey(KeyCode.LeftShift)
                ? fastSpeed
                : moveSpeed;

        Vector3 move =
            Vector3.zero;

        // Avanti / indietro
        move += transform.forward *
                Input.GetAxis("Vertical");

        // Destra / sinistra
        move += transform.right *
                Input.GetAxis("Horizontal");

        // Salita
        if (Input.GetKey(KeyCode.E))
            move += Vector3.up;

        // Discesa
        if (Input.GetKey(KeyCode.Q))
            move += Vector3.down;

        transform.position +=
            move * speed * Time.deltaTime;

        // Sblocca mouse
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Riblocca mouse
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

