using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Audio;

namespace LethalPets
{
    public class CatAI : PetAI
    {
        [SerializeField] private float runningSpeed = 10;
        [SerializeField] private float walkingSpeed = 3.5f;
        private float targetSpeed;
        private float timeElapsed;

        [SerializeField] private float speedTransitionTimeMultiplier = 8f;

        [Header("Sound")]
        public AudioClip[] meowSFX;
        public AudioClip purrSFX;
        public float maxIntervalBetweenMeows = 12f;
        public float minIntervalBetweenMeows = 5f;
        private float randomMeowInterval = 5f;
        private float timeSinceLastMeow = 0f;

        [HideInInspector]
        public bool sitting = false;

        private bool purring = false;

        public override void Start()
        {
            base.Start();
            RandomizeMeowInterval();
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (CanMove())
                SetMovingTowardsTargetPlayer(ownerPlayer);
            else
                SetDestinationToPosition(transform.position);
        }
        public void OnSit(PlayerControllerB activatingPlayer)
        {
            if (activatingPlayer == ownerPlayer)
            {
                sitting = !sitting;
                creatureAnimator.SetBool("IsSitting", sitting);
            }
        }

        public override void OnPet(PlayerControllerB activatingPlayer)
        {
            StartPurr();
        }

        public void OnCancelInteract(PlayerControllerB activatingPlayer)
        {
            StartCoroutine(StopPurr());
        }

        private void StartPurr()
        {
            purring = true;
            int randomSoundIndex = UnityEngine.Random.Range(0, reactionToPetSFX.Length);
            if (!creatureVoice.isPlaying)
                MakePetNoiseServerRpc(randomSoundIndex);
        } 

        private IEnumerator StopPurr()
        {
            while (purring)
            {
                yield return new WaitForSeconds(1f);

                creatureVoice.Stop();
                purring = false;
                yield return null;
            }
            yield break;
        }

        public void MeowRandomly()
        {
            if (timeSinceLastMeow > randomMeowInterval && !creatureVoice.isPlaying)
            {
                timeSinceLastMeow = 0f;
                RoundManager.PlayRandomClip(creatureVoice, meowSFX, true, 1f, 0);
                RandomizeMeowInterval();
            }
        }

        private void RandomizeMeowInterval()
        {
            randomMeowInterval = UnityEngine.Random.Range(minIntervalBetweenMeows, maxIntervalBetweenMeows);
        }

        public override void Update()
        {
            base.Update();
            timeSinceLastMeow += Time.deltaTime;
            MeowRandomly();

            if (Vector3.Distance(transform.position, ownerPlayer.transform.position) > 10)
            {
                targetSpeed = runningSpeed;
            }
            else if (Vector3.Distance(transform.position, ownerPlayer.transform.position) < 10 && Vector3.Distance(transform.position, ownerPlayer.transform.position) > 5)
            {
                targetSpeed = (runningSpeed + walkingSpeed) / 2;
            }
            else 
            {
                targetSpeed = walkingSpeed;
            }
            //Mathf.Lerp(agent.acceleration, walkingSpeed, t);
            if (agent.speed != targetSpeed)
            {
                float nextSpeed = Mathf.Lerp(agent.speed, targetSpeed, speedTransitionTimeMultiplier * Time.deltaTime);
                agent.speed = nextSpeed;
            }

            if (CanMove())
            {
                creatureAnimator.SetFloat("Speed", Vector3.Magnitude(agent.velocity) / runningSpeed);
            }
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
