using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.Audio;

namespace LethalPets
{
    public abstract class PetAI : NetworkBehaviour
    {
        [SerializeField] protected NavMeshAgent agent;
        [SerializeField] protected Animator creatureAnimator;
        [SerializeField] protected Rigidbody mainRigidBody;
        [SerializeField] protected Collider mainCollider;

        [SerializeField] protected AudioSource creatureSFX;
        [SerializeField] protected AudioSource creatureVoice;
        [SerializeField] protected AudioClip[] reactionToPetSFX;

        [Header("Settings")]
        [SerializeField] private bool forceNoCollisionWithPlayers = false;
        [SerializeField] private bool forceDiageticAudio = true;

        protected NavMeshPath path;
        protected Vector3 destination;

        private bool parentedToShip = false;

        [HideInInspector]
        public PlayerControllerB ownerPlayer;
        protected PlayerControllerB targetPlayer;


        protected bool movingTowardsTargetPlayer;
        protected bool moveTowardsDestination = true;
        protected float updateDestinationInterval;
        protected float setDestinationToPlayerInterval;
        protected float addPlayerVelocityToDestination;

        public float AIIntervalTime = 0.2f;

        protected LayerMask defaultRigidbodyExcludeLayers;
        protected AudioMixerGroup defaultCreatureVoiceAudioMixerGroup;
        protected AudioMixerGroup defaultCreatureSFXAudioMixerGroup;

        public bool isInsideFactory;
        public bool isInsideShip;
        public bool isOnShip;

        public virtual void Start()
        {
            try
            {
                this.agent = base.gameObject.GetComponentInChildren<NavMeshAgent>();
                Debug.Log("Initializing pet variables");
                if (this.creatureAnimator == null)
                {
                    this.creatureAnimator = base.gameObject.GetComponentInChildren<Animator>();
                }
                if (this.mainRigidBody == null)
                {
                    this.mainRigidBody = gameObject.GetComponentInChildren<Rigidbody>();
                }
                if (this.defaultCreatureVoiceAudioMixerGroup == null && creatureVoice)
                {
                    this.defaultCreatureVoiceAudioMixerGroup = creatureVoice.outputAudioMixerGroup;
                }
                if (this.defaultCreatureSFXAudioMixerGroup == null && creatureSFX)
                {
                    this.defaultCreatureSFXAudioMixerGroup = creatureSFX.outputAudioMixerGroup;
                }

                if (mainRigidBody != null)
                {
                    defaultRigidbodyExcludeLayers = mainRigidBody.excludeLayers;
                }

                this.path = new NavMeshPath();

                UpdateSettings();
            }
            catch (Exception arg)
            {
                Debug.LogError($"Error when initializing pet variables for {base.gameObject.name} : {arg}");
            }
        }

        public virtual void Update()
        {
            SetClientCalculatingAI(true);

            if (movingTowardsTargetPlayer && targetPlayer != null)
            {
                if (setDestinationToPlayerInterval <= 0f)
                {
                    setDestinationToPlayerInterval = 0.25f;
                    destination = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f, -1);
                }
                else
                {
                    destination = new Vector3(targetPlayer.transform.position.x, destination.y, targetPlayer.transform.position.z);
                    setDestinationToPlayerInterval -= Time.deltaTime;
                }
                if (addPlayerVelocityToDestination > 0f)
                {
                    if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
                    {
                        destination += Vector3.Normalize(targetPlayer.thisController.velocity * 100f) * addPlayerVelocityToDestination;
                    }
                    else if (targetPlayer.timeSincePlayerMoving < 0.25f)
                    {
                        destination += Vector3.Normalize((targetPlayer.serverPlayerPosition - targetPlayer.oldPlayerPosition) * 100f) * addPlayerVelocityToDestination;
                    }
                }
            }

            if (updateDestinationInterval >= 0f)
            {
                updateDestinationInterval -= Time.deltaTime;
            }
            else
            {
                DoAIInterval();
                updateDestinationInterval = AIIntervalTime;
            }
        }


        public void UpdateSettings()
        {
            if (creatureSFX)
            {
                creatureSFX.outputAudioMixerGroup = forceDiageticAudio
                    ? SoundManager.Instance.diageticMixer.outputAudioMixerGroup
                    : defaultCreatureSFXAudioMixerGroup;
            }

            if (creatureVoice)
            {
                creatureVoice.outputAudioMixerGroup = forceDiageticAudio
                    ? SoundManager.Instance.diageticMixer.outputAudioMixerGroup
                    : defaultCreatureVoiceAudioMixerGroup;
            }

            // Maybe just scrap... cause like.. why?
            if (mainRigidBody != null)
            {
                if (forceNoCollisionWithPlayers)
                {
                    // Check if the bit at position 4 is a 0
                    if ((defaultRigidbodyExcludeLayers & (1 << 3)) >> 3 == 0)
                    {
                        // Swapping bit at position 4
                        mainRigidBody.excludeLayers = defaultRigidbodyExcludeLayers ^ (1 << 3);
                    }
                }
                else
                {
                    mainRigidBody.excludeLayers = defaultRigidbodyExcludeLayers;
                }
            }
        }

        public override void OnDestroy()
        {
            PetManager.spawnedPets.Remove(gameObject);
            base.OnDestroy();
        }

        private void LateUpdate()
        {
/*            if (StartOfRound.Instance.shipBounds.bounds.Contains(transform.position))
            {
                transform.SetParent(StartOfRound.Instance.elevatorTransform);
                parentedToShip = true;
                //agent.destination += StartOfRound.Instance.elevatorTransform
                return;
            }

            if (parentedToShip)
            {
                parentedToShip = false;
                transform.SetParent(null, true);
            }*/

            if (isOnShip && !StartOfRound.Instance.shipBounds.bounds.Contains(transform.position))
            {
                isOnShip = false;
                isInsideShip = false;
            }
            else if (!isOnShip && StartOfRound.Instance.shipBounds.bounds.Contains(transform.position))
            {
                isOnShip = true;
                if (StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position))
                {
                    isInsideShip = true;
                }
                else
                {
                    isInsideShip = false;
                }
            }
        }

        public void SetMovingTowardsTargetPlayer(PlayerControllerB playerScript)
        {
            movingTowardsTargetPlayer = true;
            targetPlayer = playerScript;
        }

        public bool SetDestinationToPosition(Vector3 position, bool checkForPath = false)
        {
            if (checkForPath)
            {
                position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 1.75f, -1);
                this.path = new NavMeshPath();
                if (!this.agent.CalculatePath(position, this.path))
                {
                    return false;
                }
                if (Vector3.Distance(this.path.corners[this.path.corners.Length - 1], RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 2.7f, -1)) > 1.55f)
                {
                    return false;
                }
            }
            this.moveTowardsDestination = true;
            this.movingTowardsTargetPlayer = false;
            this.destination = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, -1f, -1);
            return true;
        }

        public void SetClientCalculatingAI(bool enable)
        {
            agent.enabled = enable;
        }

        public virtual void DoAIInterval()
        {
            if (moveTowardsDestination)
            {
                agent.SetDestination(destination);
            }
        }

        public virtual void OnPet(PlayerControllerB activatingPlayer)
        {
            if (creatureVoice != null)
            {
                int randomSoundIndex = UnityEngine.Random.Range(0, reactionToPetSFX.Length);
                MakePetNoiseServerRpc(randomSoundIndex);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void MakePetNoiseServerRpc(int randomSoundIndex)
        {
            MakePetNoiseClientRpc(randomSoundIndex);
        }


        [ClientRpc]
        public void MakePetNoiseClientRpc(int randomSoundIndex)
        {
            MakePetNoise(randomSoundIndex);
        }

        public void MakePetNoise(int randomSoundIndex)
        {
            creatureVoice.PlayOneShot(reactionToPetSFX[randomSoundIndex]);
            WalkieTalkie.TransmitOneShotAudio(creatureVoice, this.reactionToPetSFX[randomSoundIndex], 1f);
        }

        public virtual bool CanFollowOwnerIntoFacility()
        {
            return true;
        }

        public virtual bool CanMove()
        {
            return true;
        }

        public NavMeshAgent GetAgent()
        {
            return agent;
        }
    }
}
