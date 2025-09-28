using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Transform cameraTransform; // камеру ставим сюда
    public float sensX = 200f;
    public float sensY = 200f;

    private float pitch; // угол по вертикали

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensY * Time.deltaTime;

        // вращаем тело по горизонтали
        transform.Rotate(Vector3.up * mouseX);

        // вращаем камеру по вертикали
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
