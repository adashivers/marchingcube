using UnityEngine;

public class OrbitPlayer : MonoBehaviour
{
    public Transform player;        //Public variable to store a reference to the player game object
    public float turnSpeed = 4.0f;
    public float distance = 10;
    [Range(0,1)] public float maxPitch = 0.99f;

    private float dX = 0.0f;
    private float dY = 0.0f;

    private Vector3 offset;

    private void Start()
    {
        offset = new Vector3(0, 0, -distance);
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        dX = (turnSpeed * Input.GetAxis("Mouse X") * Time.deltaTime * 50);
        dY = (turnSpeed * -Input.GetAxis("Mouse Y") * Time.deltaTime * 50);
    }
    void LateUpdate()
    {
        // rotate around global y axis
        offset = Quaternion.AngleAxis(dX, Vector3.up) * offset;

        // rotate around local x axis clamped
        Vector3 desiredYOffset = Quaternion.AngleAxis(dY, transform.localToWorldMatrix * Vector3.right) * offset;
        if (Mathf.Abs(desiredYOffset.normalized.y) < maxPitch)
        {
            offset = desiredYOffset;
        }

        transform.position = player.position + offset;
        transform.LookAt(player.position);
    }
}