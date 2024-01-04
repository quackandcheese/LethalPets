using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LethalPets
{
    public class MovingNavMeshShip : MonoBehaviour
    {
        public ElevatorAnimationEvents hangarShip;

        [SerializeField]
        private Vector3[] Positions;
        [SerializeField]
        private float DockDuration = 2f;
        [SerializeField]
        private float MoveSpeed = 0.01f;

        private List<NavMeshAgent> AgentsOnPlatform = new List<NavMeshAgent>();

        private void Start()
        {
            StartCoroutine(MovePlatform());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
            {
                AgentsOnPlatform.Add(agent);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
            {
                AgentsOnPlatform.Remove(agent);
            }
        }

        private IEnumerator MovePlatform()
        {
            transform.position = Positions[0];
            int positionIndex = 0;
            int lastPositionIndex;
            WaitForSeconds Wait = new WaitForSeconds(DockDuration);

            while (true)
            {
                lastPositionIndex = positionIndex;
                positionIndex++;
                if (positionIndex >= Positions.Length)
                {
                    positionIndex = 0;
                }

                Vector3 platformMoveDirection = (Positions[positionIndex] - Positions[lastPositionIndex]).normalized;
                float distance = Vector3.Distance(transform.position, Positions[positionIndex]);
                float distanceTraveled = 0;
                while (distanceTraveled < distance)
                {
                    transform.position += platformMoveDirection * MoveSpeed;
                    distanceTraveled += platformMoveDirection.magnitude * MoveSpeed;

                    for (int i = 0; i < AgentsOnPlatform.Count; i++)
                    {
                        AgentsOnPlatform[i].destination += platformMoveDirection * MoveSpeed;
                        // for always aligned agents to platforms, you can use this but it will not allow the Agent to move while the platform is moving
                        //AgentsOnPlatform[i].Warp(AgentsOnPlatform[i].transform.position + platformMoveDirection * MoveSpeed);
                    }

                    yield return null;
                }

                yield return Wait;
            }
        }
    }
}
