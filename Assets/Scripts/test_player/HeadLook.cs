using UnityEngine;

public class HeadLook : MonoBehaviour
{
    public Transform head;        // трансформ головы персонажа
    public Transform cameraTransform; // трансформ камеры

    public float rotationSpeed = 5f;
    public float maxAngle = 60f;  // ограничение по углу поворота головы

    void LateUpdate()
    {
        Vector3 direction = cameraTransform.position - head.position;
        direction.y = 0; // если нужно ограничить вращение по горизонтали (без наклонов)

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Quaternion.Angle(head.rotation, targetRotation);

        if (angle < maxAngle)
        {
            head.rotation = Quaternion.Slerp(head.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}

