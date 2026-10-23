using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieIdleState : StateMachineBehaviour
{
    float timer;
    public float idleTime = 0f;
    Transform player;
    public float detectionAreaRadius = 18f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0;
        player = Enemy.GetNearestPlayer(animator.transform.position);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy enemy = animator.GetComponent<Enemy>();

        // Alert check (works on all machines because of NetworkVariable)
        if (enemy != null && enemy.isAlerted.Value)
        {
            animator.SetBool("isChasing", true);
            animator.SetBool("isPatroling", false);
            return;
        }

        // Only Server should decide to start chasing from detection
        if (enemy == null || !enemy.IsServer)
            return;

        timer += Time.deltaTime;
        if (timer > idleTime)
        {
            animator.SetBool("isPatroling", true);
        }

        player = Enemy.GetNearestPlayer(animator.transform.position);

        if (player != null)
        {
            float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

            if (distanceFromPlayer < detectionAreaRadius)
            {
                animator.SetBool("isChasing", true);
            }
        }
    }
}