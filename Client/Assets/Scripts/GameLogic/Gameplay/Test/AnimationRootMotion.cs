using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationRootMotion : MonoBehaviour
{
    void OnAnimatorMove()
    {
        Animator animator = GetComponent<Animator>();
        if (animator)
        {
            Vector3 position = animator.deltaPosition;
            Quaternion rotation = animator.deltaRotation;
            // Apply the root motion to the GameObject's transform
            transform.position += position;
            transform.rotation *= rotation;
        }
    }
}
