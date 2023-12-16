using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCameraScript : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float fastMovementSpeed = 100f;
    public float freeLookSensitivity = 3f;
    public float zoomSensitivity = 10f;
    public float fastZoomSensitivity = 50f;

    private bool isEnabled = false;
    private Camera freeCamera;
    private bool looking = false;


    private void Start()
    {
        freeCamera = GetComponentInParent<Camera>();
        StartLooking();
    }

    void Update()
    {
        isEnabled = freeCamera.enabled;

        if (isEnabled)
        {
            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

            if (Input.GetKey(KeyCode.Escape))
            {
                if (looking)
                {
                    StopLooking();
                }
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (looking)
                {
                    StopLooking();
                }
                else if (!looking)
                {
                    StartLooking();
                }
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * -transform.right);
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * transform.right);
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * transform.forward);
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * -transform.forward);
            }

            if (Input.GetKey(KeyCode.Q))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * transform.up);
            }

            if (Input.GetKey(KeyCode.E))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * -transform.up);
            }

            if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * Vector3.up);
            }

            if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
            {
                transform.position = transform.position + (movementSpeed * Time.deltaTime * -Vector3.up);
            }

            if (looking)
            {
                float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
                float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
                transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            }

            float axis = Input.GetAxis("Mouse ScrollWheel");
            if (axis != 0)
            {
                var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
                transform.position = transform.position + axis * zoomSensitivity * transform.forward;
            }
        }
    }

    void OnDisable()
    {
        StopLooking();
    }

    private void OnEnable()
    {
        StartLooking();
    }

    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
