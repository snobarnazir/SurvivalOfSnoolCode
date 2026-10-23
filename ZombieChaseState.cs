/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieChaseState : StateMachineBehaviour
{
    NavMeshAgent agent;
    Transform player;

    public float chaseSpeed = 6f;
    public float stopChasingDistance = 21f;
    public float attackingDistance = 2.5f;
    public float hordeAlertRadius = 45f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = Enemy.GetNearestPlayer(animator.transform.position);

        agent = animator.GetComponent<NavMeshAgent>();
        
        if (agent != null)
         {
             agent.speed = chaseSpeed;
         }
        



        animator.SetBool("isPatroling", false);

        if (player != null)
        {
            Enemy.AlertNearbyZombies(animator.transform.position, player.position, hordeAlertRadius);
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy enemy = animator.GetComponent<Enemy>();

        // Sound (can stay on all machines)
        if (enemy != null && enemy.CanLocalPlayerHearZombie())
        {
            if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
            {
                if (!SoundManager.Instance.zombieChannel.isPlaying)
                {
                    SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieChase);
                }
            }
        }

        // === ONLY SERVER CONTROLS MOVEMENT ===
        if (enemy == null || !enemy.IsServer)
            return;

        player = Enemy.GetNearestPlayer(animator.transform.position);

        if (player == null || agent == null || !agent.enabled)
            return;

        // Move
        agent.SetDestination(player.position);

        // Smooth rotation (Server only)
        Vector3 direction = (player.position - animator.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, lookRotation, Time.deltaTime * 8f);
        }

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

        // Attack
        if (distanceFromPlayer < attackingDistance)
        {
            animator.SetBool("isAttacking", true);
            return;
        }

        // Disengage
        float effectiveDisengageDistance = stopChasingDistance;
        if (enemy.isAlerted.Value)
        {
            effectiveDisengageDistance = hordeAlertRadius;
        }

        if (distanceFromPlayer > effectiveDisengageDistance)
        {
            enemy.isAlerted.Value = false;
            animator.SetBool("isChasing", false);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy enemy = animator.GetComponent<Enemy>();

        if (enemy != null && enemy.IsServer)
        {
            enemy.isAlerted.Value = false;
        }

        if (agent != null && agent.enabled)
        {
            agent.SetDestination(animator.transform.position);
        }

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

public class ZombieChaseState : StateMachineBehaviour
{
    NavMeshAgent agent;
    Transform player;

    public float chaseSpeed = 6f;
    public float stopChasingDistance = 21f;
    public float attackingDistance = 2.5f;
    public float hordeAlertRadius = 45f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = Enemy.GetNearestPlayer(animator.transform.position);

        agent = animator.GetComponent<NavMeshAgent>();
        Enemy enemy = animator.GetComponent<Enemy>();

        if (agent != null)
        {
            float speedMult = (enemy != null) ? enemy.GetDesperationMultiplier() : 1f;
            agent.speed = chaseSpeed * speedMult;
        }

        animator.SetBool("isPatroling", false);

        if (player != null)
        {
            Enemy.AlertNearbyZombies(animator.transform.position, player.position, hordeAlertRadius);
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
                    SoundManager.Instance.zombieChannel.PlayOneShot(SoundManager.Instance.zombieChase);
                }
            }
        }

        // === ONLY SERVER CONTROLS MOVEMENT ===
        if (enemy == null || !enemy.IsServer)
            return;

        player = Enemy.GetNearestPlayer(animator.transform.position);

        if (player == null || agent == null || !agent.enabled)
            return;

        // Update speed based on current HP (Desperation)
        float speedMult = enemy.GetDesperationMultiplier();
        agent.speed = chaseSpeed * speedMult;

        // Move
        agent.SetDestination(player.position);

        // Smooth rotation
        Vector3 direction = (player.position - animator.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            animator.transform.rotation = Quaternion.Slerp(animator.transform.rotation, lookRotation, Time.deltaTime * 8f);
        }

        float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

        // Attack
        if (distanceFromPlayer < attackingDistance)
        {
            animator.SetBool("isAttacking", true);
            return;
        }

        // Disengage
        float effectiveDisengageDistance = stopChasingDistance;
        if (enemy.isAlerted.Value)
        {
            effectiveDisengageDistance = hordeAlertRadius;
        }

        if (distanceFromPlayer > effectiveDisengageDistance)
        {
            enemy.isAlerted.Value = false;
            animator.SetBool("isChasing", false);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy enemy = animator.GetComponent<Enemy>();

        if (enemy != null && enemy.IsServer)
        {
            enemy.isAlerted.Value = false;
        }

        if (agent != null && agent.enabled)
        {
            agent.SetDestination(animator.transform.position);
        }

        if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }
}