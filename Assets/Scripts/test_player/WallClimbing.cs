using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class WallClimbingInput : MonoBehaviour
{
    public float climbSpeed = 3f;
    public float minAngle = 80f;
    public float maxAngle = 110f;
    public Animator[] animators;
    //public Animator animatorFromView;

    public Movement movementScript;

    private Rigidbody rb;
    private bool isClimbing = false;
    private Vector3 climbDirection;

    public GameObject up;    // объект для отображения эффекта лазания вверх
    public GameObject down;
    public GameObject point; // объект, который скрывать при лазании

    void Start()
    {
        animators = GetComponentsInChildren<Animator>();
        //animatorFromView = GetComponentsInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void SetBoolAll(Animator[] animators, string paramName, bool value) {
        foreach (var anim in animators) {
            anim.SetBool(paramName, value);
        }
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

                    SetBoolAll(animators, "ClimbDown", false);
                    SetBoolAll(animators, "Climb", true);
                    //animator.SetBool("ClimbDown", false);
                    //animator.SetBool("Climb", true);
                    //animatorFromView.SetBool("ClimbDown", false);
                    //animatorFromView.SetBool("Climb", true);

                    up.SetActive(true);
                    down.SetActive(false);
                    point.SetActive(false);
                }
                else if (goingDown)
                {
                    SetClimbingState(true, -Vector3.Cross(normal, Vector3.Cross(Vector3.up, normal)).normalized);

                    SetBoolAll(animators, "Climb", false);
                    SetBoolAll(animators, "ClimbDown", true);
                    //animatorFromView.SetBool("Climb", false);
                    //animatorFromView.SetBool("ClimbDown", true);

                    up.SetActive(false);
                    down.SetActive(true); // можно заменить эффект, если нужно
                    point.SetActive(false);
                }
                else
                {
                    SetClimbingState(false, Vector3.zero);

                    SetBoolAll(animators, "Climb", false);
                    SetBoolAll(animators, "ClimbDown", false);
                    //animatorFromView.SetBool("Climb", false);
                    //animatorFromView.SetBool("ClimbDown", false);

                    up.SetActive(false);
                    down.SetActive(false);
                    point.SetActive(true);
                }
                return;
            }
        }
        SetClimbingState(false, Vector3.zero);

        SetBoolAll(animators, "Climb", false);
        SetBoolAll(animators, "ClimbDown", false);
        //animatorFromView.SetBool("Climb", false);
        //animatorFromView.SetBool("ClimbDown", false);

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
        SetBoolAll(animators, "Climb", false);
        SetBoolAll(animators, "ClimbDown", false);
        //animatorFromView.SetBool("Climb", false);
        //animatorFromView.SetBool("ClimbDown", false);

        isClimbing = false;
        up.SetActive(false);
        down.SetActive(false);
        point.SetActive(true);
    }

    void FixedUpdate()
    {
        if (isClimbing)
        {
            movementScript.isRotationEnabled = false;
            rb.useGravity = false;
            rb.linearVelocity = climbDirection * climbSpeed;
        }
        else
        {
            movementScript.isRotationEnabled = true;
            rb.useGravity = true;
        }
    }
}
