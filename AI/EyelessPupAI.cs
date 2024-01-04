using GameNetcodeStuff;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LethalPets.AI
{
    public class EyelessPupAI : PetAI
    {
        private Coroutine lungeCoroutine;
        float noiseApproximation = 14f;
        float distanceFromPlayerToStartLunge = 10f;
        private bool inLunge;
        private bool endingLunge;
        private float lungeCooldownMax = 0.8f;
        private float lungeCooldown;
        private float lungeDistance = 10f;

        private float rotationDegreesThreshold = 5f;

        private Vector3 previousPosition;

        private bool sitting;

        public void OnSit(PlayerControllerB activatingPlayer)
        {
            if (activatingPlayer == ownerPlayer)
            {
                sitting = !sitting;
                creatureAnimator.SetBool("IsSitting", sitting); 
            }
        }

        public override void Update()
        {
            base.Update();
            this.creatureAnimator.SetFloat("speedMultiplier", Vector3.ClampMagnitude(base.transform.position - this.previousPosition, 1f).sqrMagnitude / (Time.deltaTime / 4f));
            this.previousPosition = base.transform.position;


            if (CanMove()) 
            { 
                if (!this.inLunge)
                {
                    this.lungeCooldown -= Time.deltaTime;
                    this.agent.speed = 3.5f;

                    /* float distanceToPlayer = Vector3.Distance(base.transform.position, ownerPlayer.transform.position);
                     Vector3 targetPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(ownerPlayer.transform.position, distanceToPlayer / this.noiseApproximation, default(NavMeshHit));
                     base.SetDestinationToPosition(targetPosition, false);*/
                    SetMovingTowardsTargetPlayer(ownerPlayer);

                    if (Vector3.Distance(transform.position, ownerPlayer.transform.position) > distanceFromPlayerToStartLunge && this.lungeCooldown <= 0f)
                    {
                        // Check if pup's rotation is close to the desired rotation for moving towards the player before it lunges. This fixes the issue where the pup lunges in the wrong direction infinitely because it can't turn fast enough to lunge towards the player
                        Vector3 desiredDirection = agent.desiredVelocity.normalized;
                        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
                        Quaternion currentRotation = gameObject.transform.rotation;
                        float angle = Quaternion.Angle(currentRotation, targetRotation);
                        if (angle < rotationDegreesThreshold)
                        {
                            this.inLunge = true;
                            EnterLunge();
                        }
                    }
                }
            }
            else
                SetDestinationToPosition(transform.position);
        }

        private void EnterLunge()
        {    
            // If EndLunge is currently running, stop it
            if (lungeCoroutine != null)
            {
                StopCoroutine(lungeCoroutine);
                lungeCoroutine = null;
            }

            // Start a new EndLunge coroutine and store a reference to it
            lungeCoroutine = StartCoroutine(EndLunge());

            this.endingLunge = false;
            Vector3 directionToPlayer = (ownerPlayer.transform.position - base.transform.position).normalized;

            Ray ray = new Ray(base.transform.position + Vector3.up, transform.forward);
            Vector3 decidedPosition;
            if (Physics.Raycast(ray, out var rayHit, lungeDistance, StartOfRound.Instance.collidersAndRoomMask))
            {
                decidedPosition = rayHit.point;
            }
            else
            {
                decidedPosition = ray.GetPoint(lungeDistance);
            }
            decidedPosition = RoundManager.Instance.GetNavMeshPosition(decidedPosition);
            base.SetDestinationToPosition(decidedPosition, false);
            this.agent.speed = 13f;
        }

        private IEnumerator EndLunge() 
        {
            while (true)
            {
                this.agent.speed -= Time.deltaTime * 5f;
                if (!this.endingLunge && this.agent.speed < 1.5f)
                {
                    this.endingLunge = true;
                    this.lungeCooldown = lungeCooldownMax;
                    this.EndLungeServerRpc();
                    yield break;
                }
                yield return null;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void EndLungeServerRpc()
        {
            EndLungeClientRpc();
        }
        [ClientRpc]
        public void EndLungeClientRpc()
        {
            this.creatureAnimator.SetTrigger("EndLungeNoKill");
            this.inLunge = false;
        }

        public override bool CanFollowOwnerIntoFacility()
        {
            return !sitting;
        }

        public override bool CanMove()
        {
            return !sitting;
        }
    }
}
