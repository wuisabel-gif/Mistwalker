using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendTreeMotionDriver : MonoBehaviour
{
    Animator animator;
    int VelocityZHash;
    int VelocityXHash;
    float VelocityX = 0.0f;
    float VelocityZ = 0.0f;
    public float acceleration = 2.0f;
    public float deceleration = 2.0f;
    public float MaximumWalkVelocity = 0.5f;
    public float MaximumRunVelocity = 2.0f;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>(); 
        VelocityZHash = Animator.StringToHash("VelocityZ");
        VelocityXHash = Animator.StringToHash("VelocityX");
    }

    void ChangeVelocity(bool forwardPress, bool leftPress, bool rightPress, bool runPress, float CurrentMaxVelocity)
    {
        if (forwardPress && VelocityZ < CurrentMaxVelocity)
        {
            VelocityZ += Time.deltaTime * acceleration;
        }
        if (leftPress && VelocityX > -CurrentMaxVelocity)
        {
            VelocityX -= Time.deltaTime * acceleration;
        }
        if (rightPress && VelocityX < CurrentMaxVelocity)
        {
            VelocityX += Time.deltaTime * acceleration;
        }
        if (!forwardPress && VelocityZ > 0.0f)
        {
            VelocityZ -= Time.deltaTime * deceleration;
        }
        if (!leftPress && VelocityX < 0.0f)
        {
            VelocityX += Time.deltaTime * deceleration;
        }
        if (!rightPress && VelocityX > 0.0f)
        {
            VelocityX -= Time.deltaTime * deceleration;
        }
    }
    void LockOrResetVelocity(bool forwardPress, bool leftPress, bool rightPress, bool runPress, float CurrentMaxVelocity)
    {
        if (!forwardPress && VelocityZ < 0.0f)
        {
            VelocityZ = 0.0f;
        }
        if (!leftPress && !rightPress && VelocityX != 0.0f && (VelocityX > -0.05f && VelocityX < 0.05f))
        {
            VelocityX = 0.0f;
        }
        //-----------------------forwardLock-----------------------
        if(forwardPress && runPress && VelocityZ > CurrentMaxVelocity)
        {
            VelocityZ = CurrentMaxVelocity;
        }
        else if (forwardPress && VelocityZ > CurrentMaxVelocity)
        {
            VelocityZ -= Time.deltaTime * deceleration;
            if(VelocityZ > CurrentMaxVelocity && VelocityZ < (CurrentMaxVelocity + 0.05))
            {
                VelocityZ = CurrentMaxVelocity;
            }
        }
        else if (forwardPress && VelocityZ < CurrentMaxVelocity && VelocityZ > (CurrentMaxVelocity - 0.05))
        {
            VelocityZ = CurrentMaxVelocity;
        }
        //-----------------------leftLock-----------------------
        if(leftPress && runPress && VelocityX < -CurrentMaxVelocity)
        {
            VelocityX = -CurrentMaxVelocity;
        }
        else if (leftPress && VelocityX < -CurrentMaxVelocity)
        {
            VelocityX += Time.deltaTime * deceleration;
            if(VelocityX < -CurrentMaxVelocity && VelocityX > (-CurrentMaxVelocity - 0.05))
            {
                VelocityX = -CurrentMaxVelocity;
            }
        }
        else if (leftPress && VelocityX > -CurrentMaxVelocity && VelocityX < (-CurrentMaxVelocity + 0.05))
        {
            VelocityX = -CurrentMaxVelocity;
        }
        //-----------------------rightLock-----------------------
        if(rightPress && runPress && VelocityX > CurrentMaxVelocity)
        {
            VelocityX = CurrentMaxVelocity;
        }
        else if (rightPress && VelocityX > CurrentMaxVelocity)
        {
            VelocityX -= Time.deltaTime * deceleration;
            if(VelocityX > CurrentMaxVelocity && VelocityX < (CurrentMaxVelocity + 0.05))
            {
                VelocityX = CurrentMaxVelocity;
            }
        }
        else if (rightPress && VelocityX < CurrentMaxVelocity && VelocityX > (CurrentMaxVelocity - 0.05))
        {
            VelocityX = CurrentMaxVelocity;
        }
    }
    // Update is called once per frame
    void Update()
    {
        bool forwardPress = Input.GetKey(KeyCode.W);
        bool leftPress = Input.GetKey(KeyCode.A);
        bool rightPress = Input.GetKey(KeyCode.D);
        bool runPress = Input.GetKey(KeyCode.LeftShift);
        animator.SetFloat(VelocityZHash, VelocityZ);
        animator.SetFloat(VelocityXHash, VelocityX);
        //------------------------------------------------
        float CurrentMaxVelocity = runPress ? MaximumRunVelocity : MaximumWalkVelocity;
        //------------------------------------------------
        ChangeVelocity(forwardPress, leftPress, rightPress, runPress, CurrentMaxVelocity);
        LockOrResetVelocity(forwardPress, leftPress, rightPress, runPress, CurrentMaxVelocity);
        
    }
}
