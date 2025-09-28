using UnityEngine;
using UnityEngine.InputSystem;

public class WallClimbingInput : MonoBehaviour
{
    public float climbSpeed = 3f;
    public float minAngle = 80f;
    public float maxAngle = 110f;
    public Animator animator;

    private Rigidbody rb;
    private bool isClimbing = false;
    private Vector3 climbDirection;

    public GameObject up;    // объект для отображения эффекта лазания вверх
    public GameObject down;
    public GameObject point; // объект, который скрывать при лазании

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;
            float angle = Vector3.Angle(normal, Vector3.up);

            if (angle >= minAngle && angle <= maxAngle)
            {
                bool goingUp = Keyboard.current.eKey.isPressed;
                bool goingDown = Keyboard.current.qKey != null && Keyboard.current.qKey.isPressed;
                // Если нажата кнопка E на клавиатуре (Input System)
                if (goingUp)
                {
                    SetClimbingState(true, Vector3.Cross(normal, Vector3.Cross(Vector3.up, normal)).normalized);
                    animator.SetBool("ClimbDown", false);
                    animator.SetBool("Climb", true);
                    up.SetActive(true);
                    down.SetActive(false);
                    point.SetActive(false);
                }
                else if (goingDown)
                {
                    SetClimbingState(true, -Vector3.Cross(normal, Vector3.Cross(Vector3.up, normal)).normalized);
                    animator.SetBool("Climb", false);
                    animator.SetBool("ClimbDown", true);
                    up.SetActive(false);
                    down.SetActive(true); // можно заменить эффект, если нужно
                    point.SetActive(false);
                }
                else
                {
                    SetClimbingState(false, Vector3.zero);
                    animator.SetBool("Climb", false);
                    animator.SetBool("ClimbDown", false);
                    up.SetActive(false);
                    down.SetActive(false);
                    point.SetActive(true);
                }
                return;
            }
        }
        SetClimbingState(false, Vector3.zero);
        animator.SetBool("Climb", false);
        animator.SetBool("ClimbDown", false);
        up.SetActive(false);
        down.SetActive(false);
        point.SetActive(true);
    }

    private void SetClimbingState(bool climbing, Vector3 direction)
    {
        isClimbing = climbing;
        climbDirection = direction;
    }

    void OnCollisionExit(Collision collision)
    {
        animator.SetBool("Climb", false);
        animator.SetBool("ClimbDown", false);
        isClimbing = false;
        up.SetActive(false);
        down.SetActive(false);
        point.SetActive(true);
    }

    void FixedUpdate()
    {
        if (isClimbing)
        {
            rb.useGravity = false;
            rb.linearVelocity = climbDirection * climbSpeed;
        }
        else
        {
            rb.useGravity = true;
        }
    }
}
