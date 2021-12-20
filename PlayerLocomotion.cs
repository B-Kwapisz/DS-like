using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
* DS-Like 
* PlayerLocomotion.cs
* 
*
*
*/
public class PlayerLocomotion : MonoBehaviour
{

    Transform cameraObject;
    InputManager inputManager;
    PlayerManager playerManager;
    public Vector3 moveDirection;


    [HideInInspector]
    public Transform myTransform;
    [HideInInspector]
    public AnimationHandler animatorHandler;


    public new Rigidbody rigidbody;
    public GameObject normalCamera;

    [Header("Ground and Air Detection Stats")]
    [SerializeField]
    float groundDetectionRayStartPoint = 0.5f;
    [SerializeField]
    float minimumDistanceNeededToBeginFall = 1f;
    [SerializeField]
    float groundDirectionRayDistance = 0.2f;
    LayerMask ignoreForGroundCheck;
    public float inAirTimer;

    [Header("Stats")]
    [SerializeField]
    float movementSpeed = 5;
    [SerializeField]
    float rotationSpeed = 10;
    [SerializeField]
    float sprintSpeed = 7;
    [SerializeField]
    float fallingSpeed = 45f;
    [SerializeField]
    float walkingSpeed = 2;


    public void Start()
    {
        //initialize components on Start
        rigidbody = GetComponent<Rigidbody>();
        inputManager = GetComponent<InputManager>();
        animatorHandler = GetComponentInChildren<AnimationHandler>();
        cameraObject = Camera.main.transform;
        myTransform = transform;
        animatorHandler.Initialize();
        playerManager = GetComponent<PlayerManager>();
        playerManager.isGrounded = true;
        ignoreForGroundCheck = ~(1 << 8 | 1 << 11);
    }

    #region Movement
    Vector3 normalVector;
    Vector3 targetPosition;

    private void HandleRotation(float delta)
    {
        Vector3 targetDir = Vector3.zero;
        float moveOverride = inputManager.moveAmount;

        targetDir = cameraObject.forward * inputManager.vertical;
        targetDir += cameraObject.right * inputManager.horizontal;

        targetDir.Normalize();
        targetDir.y = 0;

        if (targetDir == Vector3.zero)
            targetDir = myTransform.forward;

        float rs = rotationSpeed;

        Quaternion tr = Quaternion.LookRotation(targetDir);
        Quaternion targetRotation = Quaternion.Slerp(myTransform.rotation, tr, rs * delta);

        myTransform.rotation = targetRotation;
    }

    public void HandleMovement(float delta)
    {

        if (inputManager.rollFlag)
            return;

        if (playerManager.isInteracting)
            return;
        
        //movement
        moveDirection = cameraObject.forward * inputManager.vertical;
        moveDirection += cameraObject.right * inputManager.horizontal;
        moveDirection.Normalize();
        //restrict motion to the x-axis
        moveDirection.y = 0;

        float speed = movementSpeed;

        if (inputManager.sprintFlag && inputManager.moveAmount > 0.5f)
        {
            speed = sprintSpeed;
            playerManager.isSprinting = true;
            moveDirection *= speed;
        }
        else
        {
            if(inputManager.moveAmount < 0.5)
            {
                moveDirection *= walkingSpeed;
                playerManager.isSprinting = false;
            }
            moveDirection *= speed;
            playerManager.isSprinting = false;
        }
        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
        rigidbody.velocity = projectedVelocity;

        animatorHandler.UpdateAnimatorValues(inputManager.moveAmount, 0, playerManager.isSprinting);

        //animation
        if (animatorHandler.canRotate)
        {
            HandleRotation(delta);
        }
    }

    public void HandleRollingAndSprinting(float delta)
    {
        if (animatorHandler.anim.GetBool("isInteracting"))
            return;

        if (inputManager.rollFlag)
        {
            moveDirection = cameraObject.forward * inputManager.vertical;
            moveDirection += cameraObject.right * inputManager.horizontal;

            if (inputManager.moveAmount > 0)
            { //if PC is moving => roll animation
                animatorHandler.PlayTargetAnimation("Roll", true);
                moveDirection.y = 0;
                Quaternion rollRotation = Quaternion.LookRotation(moveDirection);
                myTransform.rotation = rollRotation;
            }
            else
            { //if PC is not moving => backstep animation
                animatorHandler.PlayTargetAnimation("Backstep", true);
            }
        }
    }

    public void HandleFalling(float delta, Vector3 moveDirection)
    {
        playerManager.isGrounded = false;
        RaycastHit hit;
        Vector3 origin = myTransform.position;
        origin.y += groundDetectionRayStartPoint;

        if (Physics.Raycast(origin, myTransform.forward, out hit, 0.4f))
        {
            moveDirection = Vector3.zero;
        }

        if (playerManager.isInAir)
        {
            rigidbody.AddForce(-Vector3.up * fallingSpeed);
            rigidbody.AddForce(moveDirection * fallingSpeed / 10f);
        }

        Vector3 dir = moveDirection;
        dir.Normalize();
        origin = origin + dir * groundDirectionRayDistance;

        targetPosition = myTransform.position;

        Debug.DrawRay(origin, -Vector3.up * minimumDistanceNeededToBeginFall, Color.red, 0.1f, false);
        if (Physics.Raycast(origin, -Vector3.up, out hit, minimumDistanceNeededToBeginFall, ignoreForGroundCheck))
        {
            normalVector = hit.normal;
            Vector3 tp = hit.point;
            playerManager.isGrounded = true;
            targetPosition.y = tp.y;

            if (inAirTimer > 0.5f)
            {
                Debug.Log("You were in the air for " + inAirTimer);
                animatorHandler.PlayTargetAnimation("Land", true);
                inAirTimer = 0;
            }
            else
            {
                animatorHandler.PlayTargetAnimation("Locomotion", false);
                inAirTimer = 0;
            }

            playerManager.isInAir = false;
        }
        else
        {
            if (playerManager.isGrounded)
            {
                playerManager.isGrounded = false;
            }

            if (playerManager.isInAir == false)
            {
                if (playerManager.isInteracting == false)
                {
                    animatorHandler.PlayTargetAnimation("Fall", true);
                }

                Vector3 vel = rigidbody.velocity;
                vel.Normalize();
                rigidbody.velocity = vel * (movementSpeed / 2);
                playerManager.isInAir = true;
            }
        }
        if (playerManager.isGrounded || inputManager.moveAmount > 0)
        {

            myTransform.position = Vector3.Lerp(myTransform.position, targetPosition, Time.deltaTime);

        }
        else
        {
            myTransform.position = targetPosition;
        }
        }

    }
#endregion


