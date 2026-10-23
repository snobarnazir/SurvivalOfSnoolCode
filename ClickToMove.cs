using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ClickToMove : MonoBehaviour
{
    private NavMeshAgent navAgent;
    // Start is called before the first frame update
    private void Start()
    {
        navAgent=GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity,NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.point);
            }
        }
    }
}
