using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5.0f;
    public float lookSensitivity = 2.0f;
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;
    public float maxLookAngle = 90.0f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float pitch = 0.0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the screen
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Reset the downward velocity when grounded
        }

        MovePlayer();
        HandleJump();
        LookAround();

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void MovePlayer()
    {
        // Get input for movement
        float moveX = Input.GetAxis("Horizontal"); // A and D keys
        float moveZ = Input.GetAxis("Vertical");   // W and S keys

        // Calculate movement direction
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime); // Move the player
    }

    void HandleJump()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // Calculate jump velocity
        }
    }

    void LookAround()
    {
        // Get mouse movement input
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        // Rotate player left and right (yaw)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up and down (pitch) with clamping
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        Camera.main.transform.localRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);
    }
}
