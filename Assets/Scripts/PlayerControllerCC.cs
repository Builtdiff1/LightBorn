using UnityEngine;

public class PlayerControllerCC : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 2f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform; // Assign your Cinemachine camera (usually Main Camera)

    private CharacterController cc;
    private Vector3 velocity;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform; // fallback
    }

    void Update()
    {
        HandleMovement();
        HandleJumpAndGravity();
    }

    void HandleMovement()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        // Get camera forward/right flattened on Y
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        // Movement relative to camera, but no player rotation
        Vector3 move = (camForward * inputZ + camRight * inputX).normalized;

        cc.Move(move * speed * Time.deltaTime);
    }

    void HandleJumpAndGravity()
    {
        if (cc.isGrounded && velocity.y < 0)
            velocity.y = -2f; // keep grounded

        if (Input.GetButtonDown("Jump") && cc.isGrounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}
