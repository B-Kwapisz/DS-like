using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    InputManager inputManager;
    Animator anim;
    CameraHandler cameraHandler;
    PlayerLocomotion playerLocomotion;


    [Header("Player Flags")]
    public bool isInteracting;
    public bool isSprinting;
    public bool isGrounded;
    public bool isInAir;

    // Start is called before the first frame update
    void Start()
    {
        inputManager = GetComponent<InputManager>();
        anim = GetComponentInChildren<Animator>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
    }

    private void Awake()
    {
        cameraHandler = CameraHandler.singleton;
    }
    // Update is called once per frame
    void Update()
    {
        isInteracting = anim.GetBool("isInteracting");

        float delta = Time.deltaTime;
        isSprinting = inputManager.b_input;
        inputManager.TickInput(delta);
        playerLocomotion.HandleMovement(delta);

        playerLocomotion.HandleRollingAndSprinting(delta);
        playerLocomotion.HandleFalling(delta, playerLocomotion.moveDirection);
    }

    private void FixedUpdate()
    {
        float delta = Time.fixedDeltaTime;

        if (cameraHandler != null)
        {
            cameraHandler.FollowTarget(delta);
            cameraHandler.HandleCameraRotation(delta, inputManager.mouseX, inputManager.mouseY);
        }
    }

    private void LateUpdate()
    {
        inputManager.rollFlag = false;
        inputManager.sprintFlag = false;
        

        if (isInAir)
        {
            playerLocomotion.inAirTimer = playerLocomotion.inAirTimer + Time.deltaTime;
        }
    }

}
