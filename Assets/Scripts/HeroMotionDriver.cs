using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class HeroMotionDriver : MonoBehaviour
{
    PlayerInput playerInput;
    CharacterController characterController;
    Animator animator;
    
    int IsWalkingHash;
    int IsRunningHash;
    int IsJumpingHash;
    int IsBasicSlashingHash;

    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;
    bool isMovementPressed;
    bool isRunPressed;
    bool isJumpPressed = false;
    bool isBasicSlashPressed = false;
    bool canJump = true;
    public float rotationFactorPerFrame = 6.0f;
    public float walkSpeed = 1.0f;
    float walkMultiplier = 1.0f;
    public float initialJumpVelocity = 1f;
    public float maxJumpHeight = 1.0f;
    public float maxJumpTime = 0.5f;
    bool isJumping = false;
    bool isJumpAnimating = false;
    bool isBasicSlashing = false;
    bool isBasicSlashAnimated = false;
    public float runMultiplier = 3.0f;
    public float gravity = -0.5f;
    public float groundedGravity = -0.5f;
    bool canMove = true;


    


    [SerializeField, FormerlySerializedAs("groundCheck")] FootingProbe footingProbe;
    void Awake()
    {
        walkMultiplier = walkSpeed;
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        IsWalkingHash = Animator.StringToHash("IsWalking");
        IsRunningHash = Animator.StringToHash("IsRunning");
        IsJumpingHash = Animator.StringToHash("isJumping");
        IsBasicSlashingHash = Animator.StringToHash("isBasicSlashing");

        playerInput.CharacterControls.Move.started += onMovementInput;
        playerInput.CharacterControls.Move.canceled += onMovementInput;
        playerInput.CharacterControls.Move.performed += onMovementInput;

        playerInput.CharacterControls.Run.started += onRun;
        playerInput.CharacterControls.Run.canceled += onRun;

        playerInput.CharacterControls.Jump.started += onJump;
        playerInput.CharacterControls.Jump.canceled += onJump;

        playerInput.CharacterControls.BasicSlash.started += onBasicSlash;
        playerInput.CharacterControls.BasicSlash.canceled += onBasicSlash;
        setupJumpVariables();
    }
    void onBasicSlash(InputAction.CallbackContext context)
    {
        if (context.started && !isBasicSlashing)
        {
            isBasicSlashPressed = true;
            Debug.Log("Basic Slash pressed");
        }
    }
    void setupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }
    void handleBasicSlash()
    {
        // if (characterController.isGrounded && isBasicSlashPressed)
        // {
        //     animator.SetTrigger("isBasicSlashingTrigger");
        //     isBasicSlashPressed = false;
        //     isBasicSlashAnimated = true;
        //     walkMultiplier = 0.0f;

            
        // }else if (!isBasicSlashPressed && isBasicSlashing && characterController.isGrounded)
        // {
        //     isBasicSlashing = false;
        // }
        if (characterController.isGrounded && isBasicSlashPressed && !isBasicSlashing)
        {
            animator.SetTrigger("isBasicSlashingTrigger");
            isBasicSlashPressed = false;
            isBasicSlashing = true;     
            isBasicSlashAnimated = true;
            canMove = false;
            canJump = false;
        }


        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (isBasicSlashing && stateInfo.IsName("BasicSlash") && stateInfo.normalizedTime >= .85f)
        {
            canMove = true;
            canJump = true;
            isBasicSlashing = false;   
            isBasicSlashAnimated = false;
        }
    }
    void handleJump()
    {
        if (!isJumping && characterController.isGrounded && isJumpPressed)
        {
            animator.SetBool(IsJumpingHash, true);
            isJumpAnimating = true;
            isJumping = true;
            rotationFactorPerFrame = 1.0f;
            currentMovement.y = initialJumpVelocity * 0.5f;
            currentRunMovement.y = initialJumpVelocity * 0.5f;
        }else if (!isJumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }
    void onJump(InputAction.CallbackContext context)
    {
            isJumpPressed = context.ReadValueAsButton();
    }
    void onRun(InputAction.CallbackContext context){
        isRunPressed = context.ReadValueAsButton();
    }
    void onMovementInput(InputAction.CallbackContext context){
        currentMovementInput = context.ReadValue<Vector2>();
        // currentMovement.x = currentMovementInput.x * walkMultiplier;
        // currentMovement.z = currentMovementInput.y * walkMultiplier;
        // currentRunMovement.x = currentMovementInput.x * walkMultiplier * runMultiplier;
        // currentRunMovement.z = currentMovementInput.y * walkMultiplier * runMultiplier;
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
        if(canMove)
        {
            currentMovement.x = currentMovementInput.x * walkMultiplier;
            currentMovement.z = currentMovementInput.y * walkMultiplier;
            currentRunMovement.x = currentMovementInput.x * walkMultiplier * runMultiplier;
            currentRunMovement.z = currentMovementInput.y * walkMultiplier * runMultiplier;
        }
        else
        {
            // قفل حرکت، سرعت صفر
            currentMovement.x = 0;
            currentMovement.z = 0;
            currentRunMovement.x = 0;
            currentRunMovement.z = 0;
        }
    }
    void handleAnimation(){
        bool IsWalking = animator.GetBool(IsWalkingHash);
        bool IsRunning = animator.GetBool(IsRunningHash);

        if(isMovementPressed && !IsWalking){
            animator.SetBool(IsWalkingHash, true);
        }
        else if(!isMovementPressed && IsWalking){
            animator.SetBool(IsWalkingHash, false);
        }

        if((isMovementPressed && isRunPressed) && !IsRunning){
            animator.SetBool(IsRunningHash ,true);
        }else if((!isMovementPressed || !isRunPressed) && IsRunning){
            animator.SetBool(IsRunningHash ,false);
        }
    }
    void handleGravity(){
        bool isFalling = currentMovement.y <= 0.0f || !isJumpPressed;
        float fallMultiplier = 2.0f;
        if (characterController.isGrounded)
        {
            if (isJumpAnimating)
            {
                rotationFactorPerFrame = 6.0f;
                animator.SetBool(IsJumpingHash, false);
                isJumpAnimating = false;
            }
            currentMovement.y = groundedGravity;
            currentRunMovement.y = groundedGravity;
        }else if (isFalling)
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * fallMultiplier * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }   
        else
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            currentMovement.y = nextYVelocity;
            currentRunMovement.y = nextYVelocity;
        }

    }
    void handleRotation(){
        Vector3 positionToLookAt;

        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = currentMovement.z;
        Quaternion currentRotation = transform.rotation;

        if(isMovementPressed){
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }
    // Update is called once per frame
    void Update()
    {

        handleRotation();
        handleAnimation();
        if (canMove)
        {
            if (isRunPressed)
                characterController.Move(currentRunMovement * Time.deltaTime);
            else
                characterController.Move(currentMovement * Time.deltaTime);
        }

        handleGravity();
        handleBasicSlash();
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0); // لایه 0
        if (stateInfo.IsName("BasicSlash"))
        {
            if (stateInfo.normalizedTime >= 1.0f)
            {
                canMove = true;
                canJump = true;
                Debug.Log("animationDone");
                isBasicSlashAnimated = false;
            }
        }
        if (canJump)
        {
            handleJump();
        }
    }
    void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }
    void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}
