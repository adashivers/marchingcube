using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FollowObject : MonoBehaviour
{

    public Transform target;
    public float desiredHorizontalDistance = 5;
    public float desiredRelativeHeight = 2;
    public float velocity = 10;
    public float turnSpeed = 10;
    public float maxHorizontalDistance = 10;
    public float maxVerticalDistance = 5;

    void FixedUpdate()
    {
        // get horizontal direction vector from transform to target
        Vector3 horizontalDistVector = new Vector3
        (
            target.position.x - transform.position.x, 
            0, 
            target.position.z - transform.position.z
        );
        Vector3 horizontalDir = horizontalDistVector.normalized;

        // set desired position
        float targetHeight = target.position.y;
        Vector3 desiredPosition = target.position // target pos
            - horizontalDir * desiredHorizontalDistance // horizontal offset
            + new Vector3(0, desiredRelativeHeight, 0); // vertical offset

        // move towards target
        transform.position = Vector3.MoveTowards(transform.position, desiredPosition, Time.deltaTime * velocity);

        // clamp horizontal distance
        if (horizontalDistVector.magnitude > maxHorizontalDistance)
        {
            Vector3 offset = horizontalDistVector.normalized * (horizontalDistVector.magnitude - maxHorizontalDistance);
            transform.position += offset;
        }

        // clamp vertical distance
        float heightDiff = transform.position.y - target.position.y;
        if (Mathf.Abs(heightDiff) > maxVerticalDistance)
        {
            transform.position += new Vector3(0, -(heightDiff - maxVerticalDistance), 0);
        }

        // rotate towards target
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target.position - transform.position), turnSpeed);




    }
}
