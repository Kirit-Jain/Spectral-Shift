using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float lookSpeed = 2f;
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravity = -9.81f;

    [Header("References")]
    [SerializeField] Transform playerCamera;
    [SerializeField] AudioListener playerListener;

    // Internal Variables
    CharacterController characterController;
    float rotationX = 0;

    //Physics Variables
    Vector3 velocity;
    bool isGrounded;

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    } 

    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            enabled = false;
            if (playerCamera) playerCamera.gameObject.SetActive(false);
            if (playerListener) playerListener.enabled = false;
        }
        else
        {
            if (playerCamera) playerCamera.gameObject.SetActive(true);
            if (playerListener) playerListener.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!IsOwner || isDead.Value) return;

        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative to keep grounded
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        characterController.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        transform.Rotate(Vector3.up * mouseX);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsOwner) return;

        if (hit.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("HIT OBSTACLE! Requesting Death...");
            SubmitDeathServerRpc();
        }
    }

    [ServerRpc]
    void SubmitDeathServerRpc()
    {
        if (isDead.Value) return;
        isDead.Value = true;
        RestartGameClientRpc();
    }

    [ClientRpc]
    void RestartGameClientRpc()
    {
        Debug.Log("GAME OVER! Respawning...");
        // Disable controller briefly to teleport without physics interference
        if(characterController) characterController.enabled = false;
        transform.position = new Vector3(0, 1, 0); 
        if(characterController) characterController.enabled = true;
        
        isDead.Value = false;
    }
}