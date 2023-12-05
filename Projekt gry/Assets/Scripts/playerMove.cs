using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMove : MonoBehaviour
{
    public Transform playerCamera;
    public Transform playerSpine;

    public float sensitivity = 200f;

    public float playerSpineVerticalRotation = 0f;
    public float playerSpeed = 10.0f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;

    private readonly float jumpHeight = 1.0f;
    private readonly float gravityValue = -9.81f;

    Animator playerAnimator;

    void Start()
    {
        playerAnimator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        Transform[] children = GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            if (child.CompareTag("Spine"))
            {
                playerSpine = child;
                break;
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

        if (Input.GetMouseButton(0))
        {
            transform.GetComponent<Unit>().isShooting = true;
        } 
        else 
        { 
            transform.GetComponent<Unit>().isShooting = false;
        }
    }

    private void LateUpdate()
    {
        float mouseYMove = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        // sczytywanie pochylenia siê gracza z wertykalnego ruchu myszy
        playerSpineVerticalRotation -= mouseYMove;

        // ograniczenie mo¿liwoœci pochylenia siê/ wygiêcia gracza
        playerSpineVerticalRotation = Mathf.Clamp(playerSpineVerticalRotation, -85f, 85f);

        // Spine antyterrorysty trzeba obracaæ po innej osi w celu pochylenia, st¹d ten `if`
        if (transform.CompareTag("TerroristPlayer"))
        {
            playerSpine.localRotation = Quaternion.Euler(playerSpineVerticalRotation, 0f, 0f);
        }
        else if (transform.CompareTag("CounterTerroristPlayer"))
        {
            playerSpine.localRotation = Quaternion.Euler(0f, 0f, -playerSpineVerticalRotation);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // to sprawdza w jakiej strefie na mapie znajduje siê gracz
        // u¿yjê tego do losowania strefy dla botów
        if (other.transform.parent.gameObject.CompareTag("Zone"))
        {
            GameObject zone = other.transform.parent.gameObject;
            //Debug.Log(zone.name);
        }
    }
}