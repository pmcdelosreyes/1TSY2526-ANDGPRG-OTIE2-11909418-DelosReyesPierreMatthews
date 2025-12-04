using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    public float cameraSensitivity = 90;

    [SerializeField]
    public float normalMoveSpeed = 10;
    [SerializeField]
    public float fastMoveFactor = 3;

    [SerializeField]
    public float elevationSpeed = 2;

    public Vector2 rangePosX = new Vector2(0,0);
    public Vector2 rangePosY = new Vector2(0, 0);
    public Vector2 posRangeZ = new Vector2(0, 0);

    private float rotationX;
    private float rotationY;
    private float rotationZ;

    private float minAngleX = -70F;
    private float maxAngleX = 90F;
    private float minAngleY = -360.0F;
    private float maxAngleY = 360.0F;

    bool middleMousePressed = false;

    public Rigidbody rb;

    float movementCooldownTimer = 0;

    void Start() 
    {
        // Initialize to current camera rotation
        // This prevents snapping to 0,0,0 on start
        // and allows setting initial rotation in editor
        rotationX = transform.rotation.eulerAngles.x;
        rotationY = transform.rotation.eulerAngles.y;
        rotationZ = transform.rotation.eulerAngles.z;
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void MouseEventListener()
    {
        // Middle mouse button
        // Toggle rotation mode
        middleMousePressed = buttonToggle(KeyCode.Mouse2, middleMousePressed);
        //middleMousePressed = buttonToggle(KeyCode.G, middleMousePressed);
    }

    // Button togle helper class
    // Returns true on key down, false on key up, current state otherwise
    private bool buttonToggle(KeyCode key, bool current)
    {
        if (Input.GetKeyDown(key))
            return true;
        if (Input.GetKeyUp(key))
            return false;
        return current;
    }

    void Update() 
    {
        bool resetDir = false;

        // Kepp in bounds if exists
        // Clamp position
        Vector3 curPosition = transform.position;
        if (rangePosX != null && rangePosX.x != rangePosX.y) 
        { 
            curPosition.x = Mathf.Clamp(curPosition.x, rangePosX.x, rangePosX.y);
        }

        if (rangePosY != null && rangePosY.x != rangePosY.y)
        {
            // Clamp Y
            curPosition.y = Mathf.Clamp(curPosition.y, rangePosY.x, rangePosY.y);
        }

        if (posRangeZ != null && posRangeZ.x != posRangeZ.y)
        {
            curPosition.z = Mathf.Clamp(curPosition.z, posRangeZ.x, posRangeZ.y);
        }
        transform.position = curPosition;


        movementCooldownTimer += Time.deltaTime;

        MouseEventListener();
       
        bool isMovementIntended = false;

        // Middle mouse pressed, allow y axis movement
        if (middleMousePressed)
        {

            // Hide and lock mouse if we are in rotation mode
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            //Gather input
            rotationY += Input.GetAxis("Mouse X") * cameraSensitivity * Time.deltaTime;
            rotationX -= Input.GetAxis("Mouse Y") * cameraSensitivity * Time.deltaTime;

            // Clamp rotation
            rotationY = Mathf.Clamp(SanitizeAngle(rotationY), minAngleY, maxAngleY);
            rotationX = Mathf.Clamp(SanitizeAngle(rotationX), minAngleX, maxAngleX);

            // Apply rotation
            transform.localRotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
            resetDir = true;
        }
        else
        {
            // cursor not locked if middle mouse is not pressed
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /**
         * Displacement
         */

        // remove the y axis of freedom from the movement
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0;

        float speed = normalMoveSpeed;
        // High speed movement
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            speed *= fastMoveFactor;
        }




        
        // Common
        KeyCode k;
        Vector3 dir;
        Vector3 summedDir = new Vector3(0, 0, 0);
        float forceScaler = 100F;



        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
        {
            rb.AddForce(Vector3.up * -Input.mouseScrollDelta.y * elevationSpeed * forceScaler);
            isMovementIntended = true;
        }


        // Speed multiplier
        k = KeyCode.LeftShift;
        if (Input.GetKeyDown(k))
            resetDir = true;
        if (Input.GetKeyUp(k))
            resetDir = true;


        // Forward
        k = KeyCode.W;
        dir = forward;
        if (Input.GetKey(k))
        {
            summedDir += dir;
            isMovementIntended = true;
        }
        if (Input.GetKeyDown(k))
            resetDir = true;
        if (Input.GetKeyUp(k))
            resetDir = true;


        // Backward
        k = KeyCode.S;
        dir = -forward;
        if (Input.GetKey(k))
        {
            summedDir += dir;
            isMovementIntended = true;
        }
        if (Input.GetKeyDown(k))
            resetDir = true;
        if (Input.GetKeyUp(k))
            resetDir = true;


        // Right
        k = KeyCode.D;
        dir = right;
        if (Input.GetKey(k))
        {
            summedDir += dir;
            isMovementIntended = true;
        }
        if (Input.GetKeyDown(k))
            resetDir = true;
        if (Input.GetKeyUp(k))
            resetDir = true;

        // Left
        k = KeyCode.A;
        dir = -right;
        if (Input.GetKey(k))
        {
            summedDir += dir;
            isMovementIntended = true;
        }
        if (Input.GetKeyDown(k))
            resetDir = true;
        if (Input.GetKeyUp(k))
            resetDir = true;




        // Reset forces when required
        if (resetDir)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(summedDir.normalized * speed * forceScaler);
        }
        if (isMovementIntended)
        {
            movementCooldownTimer = 0;
        }
        if (movementCooldownTimer > 0.1F)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }




    }



    public static float SanitizeAngle(float angle)
    {
        angle = angle % 360;

        if (angle < -360F)
            angle += 360F;

        if (angle > 360F)
            angle -= 360F;

        return angle;
    }
}
