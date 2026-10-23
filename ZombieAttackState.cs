/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAttackState : StateMachineBehaviour
{
    public float stopAttackingDistance = 2.5f;
    Transform player;
    NavMeshAgent agent;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = Enemy.GetNearestPlayer(animator.transform.position);
        agent = animator.GetComponent<NavMeshAgent>();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy enemy = animator.GetComponent<Enemy>();

        // Sound
        if (enemy != null && enemy.CanLocalPlayerHearZombie())
        {
            if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
            {
                if (!SoundManager.Instance.zombieChannel.isPlaying)
                {
                    SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieAttack);
                }
            }
        }

        // === ONLY SERVER CONTROLS ROTATION & LOGIC ===
        if (enemy == null || !enemy.IsServer)
            return;

        player = Enemy.GetNearestPlayer(animator.transform.position);

        if (player == null) return;

        // Smooth look at player (Server only)
        Vector3 direction = (player.position - animator.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, lookRotation, Time.deltaTime * 10f);
        }

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

        if (distanceFromPlayer > stopAttackingDistance)
        {
            animator.SetBool("isAttacking", false);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }
}*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAttackState : StateMachineBehaviour
{
    public float stopAttackingDistance = 2.5f;
    Transform player;
    NavMeshAgent agent;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = Enemy.GetNearestPlayer(animator.transform.position);
        agent = animator.GetComponent<NavMeshAgent>();

        // Desperation: make attack animation faster
        Enemy enemy = animator.GetComponent<Enemy>();
        if (enemy != null)
        {
            float mult = enemy.GetDesperationMultiplier();
            animator.speed = mult;
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy enemy = animator.GetComponent<Enemy>();

        // Sound
        if (enemy != null && enemy.CanLocalPlayerHearZombie())
        {
            if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
            {
                if (!SoundManager.Instance.zombieChannel.isPlaying)
                {
                    SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieAttack);
                }
            }
        }

        // === ONLY SERVER CONTROLS ROTATION & LOGIC ===
        if (enemy == null || !enemy.IsServer)
            return;

        player = Enemy.GetNearestPlayer(animator.transform.position);

        if (player == null) return;

        // Keep updating attack speed while in this state
        float mult = enemy.GetDesperationMultiplier();
        animator.speed = mult;

        // Smooth look at player
        Vector3 direction = (player.position - animator.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, lookRotation, Time.deltaTime * 10f);
        }

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

        if (distanceFromPlayer > stopAttackingDistance)
        {
            animator.SetBool("isAttacking", false);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Reset animation speed
        animator.speed = 1f;

        if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }
}