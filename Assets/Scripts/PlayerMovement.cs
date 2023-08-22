
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float maxWalkSpeed = 100;
    public float maxAirSpeed = 150;
    public float walkAcceleration = 40;
    public float airAcceleration = 60;
    public float turnSpeed = 50;
    public float jumpHeight = 5;
    [Range(0, 1)] public float maxJumpableSlope = 0.1f; 

    private Rigidbody rb;
    private Quaternion lastLookRotation = Quaternion.identity;
    private Vector2 input;
    private bool desiredJump = false;
    private Vector3 desiredVelocity;
    private bool onGround = false;
    private Vector3 velocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;   
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(transform.localToWorldMatrix * Vector3.forward));
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            onGround |= normal.y >= 1 - maxJumpableSlope;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        desiredJump |= Input.GetButtonDown("Jump");
    }


    private void FixedUpdate()
    {
        // get desired horizontal move direction
        Vector3 adjustedCamForward = Camera.main.transform.localToWorldMatrix * Vector3.forward;
        adjustedCamForward.y = 0;
        Vector3 desiredMoveDirection = Quaternion.LookRotation(adjustedCamForward.normalized, Vector3.up) * new Vector3(input.normalized.x, 0, input.normalized.y);

        Vector3 currVel = rb.velocity;

        // get acceleration and maxspeed
        float acceleration = onGround ? walkAcceleration : airAcceleration;
        float maxSpeed = onGround ? maxWalkSpeed : maxAirSpeed;


        // set next velocity
        desiredVelocity = desiredMoveDirection * maxSpeed;
        velocity = Vector3.MoveTowards(currVel, desiredVelocity, acceleration * Time.deltaTime);
        velocity.y = currVel.y;
        if (desiredJump && onGround)
        {
            desiredJump = false;
            Jump();
        }


        // apply velocity and rotation
        rb.velocity = velocity;
        rb.rotation = Quaternion.RotateTowards(rb.rotation, lastLookRotation, turnSpeed);

        if (input != Vector2.zero)
        {
            lastLookRotation = Quaternion.LookRotation(desiredMoveDirection);
        }

        onGround = false;
    }


    private void Jump()
    {
        velocity.y += Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
    }
}
