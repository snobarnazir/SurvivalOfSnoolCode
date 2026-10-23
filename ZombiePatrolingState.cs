using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombiePatrolingState : StateMachineBehaviour
{
    float timer;
    public float patrolingTime = 10f;
    NavMeshAgent agent;
    public float detectionArea = 18f;
    public float patrolingSpeed = 2f;
    Transform player;

    List<Transform> waypointList = new List<Transform>();

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.speed = patrolingSpeed;
        }

        timer = 0;
        waypointList.Clear();

        GameObject waypointCluster = GameObject.FindGameObjectWithTag("Waypoints");
        if (waypointCluster != null)
        {
            foreach (Transform t in waypointCluster.transform)
            {
                waypointList.Add(t);
            }
        }

        if (agent != null && agent.enabled && waypointList.Count > 0)
        {
            Vector3 nextPosition = waypointList[Random.Range(0, waypointList.Count)].position;
            agent.SetDestination(nextPosition);
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Enemy enemy = animator.GetComponent<Enemy>();

        // Alert check
        if (enemy != null && enemy.isAlerted.Value)
        {
            animator.SetBool("isChasing", true);
            animator.SetBool("isPatroling", false);
            return;
        }

        // Sound (can stay)
        if (enemy != null && enemy.CanLocalPlayerHearZombie())
        {
            if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
            {
                if (!SoundManager.Instance.zombieChannel.isPlaying)
                {
                    SoundManager.Instance.zombieChannel.clip = SoundManager.Instance.zombieWalking;
                    SoundManager.Instance.zombieChannel.PlayDelayed(1f);
                }
            }
        }

        // === ONLY SERVER CONTROLS PATROL MOVEMENT & DECISIONS ===
        if (enemy == null || !enemy.IsServer)
            return;

        if (agent != null && agent.enabled && waypointList.Count > 0)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.SetDestination(waypointList[Random.Range(0, waypointList.Count)].position);
            }
        }

        timer += Time.deltaTime;
        if (timer > patrolingTime)
        {
            animator.SetBool("isPatroling", false);
        }

        player = Enemy.GetNearestPlayer(animator.transform.position);

        if (player != null)
        {
            float distanceFromPlayer = Vector3.Distance(player.position, animator.transform.position);

            if (distanceFromPlayer < detectionArea)
            {
                animator.SetBool("isChasing", true);
            }
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (agent != null && agent.enabled)
        {
            agent.SetDestination(agent.transform.position);
        }

        if (SoundManager.Instance != null && SoundManager.Instance.zombieChannel != null)
        {
            SoundManager.Instance.zombieChannel.Stop();
        }
    }
}