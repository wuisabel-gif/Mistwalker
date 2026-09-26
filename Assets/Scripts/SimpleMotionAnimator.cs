using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMotionAnimator : MonoBehaviour
{
    Animator animator;
    int IsWalkingHash;
    int IsRunningHash;
    int velocityHash;
    float velocity = 0.0f;
    public float acceleration = 0.1f;
    public float deceleration = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        IsWalkingHash = Animator.StringToHash("IsWalking");
        IsRunningHash = Animator.StringToHash("IsRunning");
        velocityHash = Animator.StringToHash("Velocity");
    }

    // Update is called once per frame
    void Update()
    {
        bool IsWalking = animator.GetBool(IsWalkingHash);
        bool IsRunning = animator.GetBool(IsRunningHash);
        bool forwardPress = Input.GetKey("w");
        bool runPressed = Input.GetKey("left shift");
        if (forwardPress && velocity < 1.0f)
        {
            velocity += Time.deltaTime * acceleration;
        }
        if (!forwardPress && velocity > 0.0f)
        {
            velocity -= Time.deltaTime * deceleration;
        }
        if (!forwardPress && velocity < 0.0f)
        {
            velocity = 0.0f;
        }
        animator.SetFloat(velocityHash , velocity);
        //----------------------------------------------------------------
        // if (!IsWalking && forwardPress)
        // {
        //     animator.SetBool(IsWalkingHash, true);
        // }
        // if (IsWalking && !forwardPress)
        // {
        //     animator.SetBool(IsWalkingHash, false);
        // }
        // if (!IsRunning && (forwardPress && runPressed))
        // {
        //     animator.SetBool(IsRunningHash, true);
        // }
        // if (IsRunning && (!forwardPress || !runPressed))
        // {
        //     animator.SetBool(IsRunningHash, false);
        // }
    }
}
