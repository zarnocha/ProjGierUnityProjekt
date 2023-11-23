using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public Transform playerCamera;
    public float sensitivity = 200f;
    public float playerSpineVerticalRotation = 0f;
    public Transform playerSpine;
    private Transform playerCrosshair;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;

    private readonly float playerSpeed = 1.0f;
    private readonly float jumpHeight = 1.0f;
    private readonly float gravityValue = -9.81f;

    Animator playerAnimator;


    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        Transform[] children = GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            if (child.CompareTag("Spine"))
            {
                playerSpine = child;
                Debug.Log("Znaleziono krêgos³up: " + playerSpine.name);
            }

            if (child.CompareTag("Crosshair"))
            {
                playerCrosshair = child;
                Debug.Log("Znaleziono celownik: " + playerCrosshair.name);
            }
        }
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(playerSpeed * Time.deltaTime * move);

        // je¿eli pozycja gracza nie zmienia siê ani na osi X ani na osi Z, to oznacza, ¿e nie idzie - czyli brak animacji
        bool isWalking = move.x != 0 || move.z != 0;
        playerAnimator.SetBool("isWalking", isWalking);

        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            //Debug.Log("Jumped");
            //playerAnimator.SetBool("isJumping", true);
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }
         
        if (playerVelocity.y <= 0)
        {
            //Debug.Log("Grounded");
            //playerAnimator.SetBool("isJumping", false);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        float mouseXMove = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseXMove);
    }

    private void LateUpdate()
    {
        float mouseYMove = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // sczytywanie pochylenia siê gracza z wertykalnego ruchu myszy
        playerSpineVerticalRotation -= mouseYMove;

        // ograniczenie mo¿liwoœci pochylenia siê/ wygiêcia gracza
        playerSpineVerticalRotation = Mathf.Clamp(playerSpineVerticalRotation, -85f, 85f);

        // Spine antyterrorysty trzeba obracaæ po innej osi w celu pochylenia, st¹d ten `if`
        if (transform.CompareTag("Terrorist") || transform.CompareTag("TerroristPlayer"))
        {
            playerSpine.localRotation = Quaternion.Euler(playerSpineVerticalRotation, 0f, 0f);
        }
        else if (transform.CompareTag("CounterTerrorist") || transform.CompareTag("CounterTerroristPlayer"))
        {
            playerSpine.localRotation = Quaternion.Euler(0f, 0f, -playerSpineVerticalRotation);
        }

    }
}