using System.Data.Common;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public Animator animator;


    [Header("Look")]
    public Transform playerCamera;   // сюда перетащи Main Camera
    public float mouseSensitivity = 2f;

    private Rigidbody rb;
    private bool isGrounded;
    private float pitch = 0f; // угол наклона камеры

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //Falling();
        Look();
        Move();
        Jump();
    }

    void Look()
    {
        Vector2 delta = Mouse.current.delta.ReadValue();
        float mouseX = delta.x * mouseSensitivity;
        float mouseY = delta.y * mouseSensitivity;

        // вращаем тело по горизонтали
        transform.Rotate(Vector3.up * mouseX);

        // вращаем камеру по вертикали
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        Vector3 dir = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) dir += transform.forward;
        if (Keyboard.current.sKey.isPressed) dir -= transform.forward;
        if (Keyboard.current.aKey.isPressed) dir -= transform.right;
        if (Keyboard.current.dKey.isPressed) dir += transform.right;

        float speed = dir.magnitude;
        animator.SetFloat("Speed", speed);

        dir.Normalize();
        rb.MovePosition(rb.position + dir * moveSpeed * Time.deltaTime);
    }

    void Jump()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            animator.SetTrigger("jump");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void Falling()
    {
        if (!isGrounded)
        {
            animator.SetBool("fall", true);
        }
        else
        {
            animator.SetBool("fall", false);
        }
    }
}