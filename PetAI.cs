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
        public NavMeshAgent agent;
        public Animator creatureAnimator;
        public Rigidbody mainRigidBody;
        public Collider mainCollider;

        [Header("Sound")]
        public AudioSource creatureSFX;
        public AudioSource creatureVoice;
        public AudioClip[] reactionToPetSFX;

        [Header("Settings")]
        public bool forceNoCollisionWithPlayers = false;
        public bool forceDiageticAudio = true;

        [HideInInspector]
        public NavMeshPath path;
        private Vector3 destination;

        [HideInInspector]
        public bool parentedToShip = false;

        [HideInInspector]
        public PlayerControllerB ownerPlayer;
        [HideInInspector]
        public PlayerControllerB targetPlayer;


        protected bool movingTowardsTargetPlayer;
        protected bool moveTowardsDestination = true;
        protected float updateDestinationInterval;
        protected float setDestinationToPlayerInterval;
        protected float addPlayerVelocityToDestination;

        public float AIIntervalTime = 0.2f;

        protected LayerMask defaultRigidbodyExcludeLayers;
        protected AudioMixerGroup defaultCreatureVoiceAudioMixerGroup;
        protected AudioMixerGroup defaultCreatureSFXAudioMixerGroup;

        public virtual void Start()
        {
            try
            {
                this.agent = base.gameObject.GetComponentInChildren<NavMeshAgent>();
                Debug.Log("Initializing pet animator");
                if (this.creatureAnimator == null)
                {
                    this.creatureAnimator = base.gameObject.GetComponentInChildren<Animator>();
                }
                if (this.mainRigidBody == null)
                {
                    this.mainRigidBody = gameObject.GetComponentInChildren<Rigidbody>();
                }
                if (this.defaultCreatureVoiceAudioMixerGroup == null)
                {
                    if (creatureVoice)
                        this.defaultCreatureVoiceAudioMixerGroup = creatureVoice.outputAudioMixerGroup;
                }
                if (this.defaultCreatureSFXAudioMixerGroup == null)
                {
                    if (creatureSFX)
                        this.defaultCreatureSFXAudioMixerGroup = creatureSFX.outputAudioMixerGroup;
                }

                if (mainRigidBody != null)
                {
                    defaultRigidbodyExcludeLayers = mainRigidBody.excludeLayers;
                }
                /*if (this.enemyType.isOutsideEnemy)
                {
                    this.allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                    if (GameNetworkManager.Instance.localPlayerController != null)
                    {
                        this.EnableEnemyMesh(!StartOfRound.Instance.hangarDoorsClosed || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom, false);
                    }
                }
                else
                {
                    this.allAINodes = GameObject.FindGameObjectsWithTag("AINode");
                }*/
                this.path = new NavMeshPath();

                UpdateSettings();
            }
            catch (Exception arg)
            {
                Debug.LogError($"Error when initializing enemy variables for {base.gameObject.name} : {arg}");
            }
        }

        public virtual void Update()
        {
            this.SetClientCalculatingAI(true);

            if (this.movingTowardsTargetPlayer && this.targetPlayer != null)
            {
                if (this.setDestinationToPlayerInterval <= 0f)
                {
                    this.setDestinationToPlayerInterval = 0.25f;
                    this.destination = RoundManager.Instance.GetNavMeshPosition(this.targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f, -1);
                }
                else
                {
                    this.destination = new Vector3(this.targetPlayer.transform.position.x, this.destination.y, this.targetPlayer.transform.position.z);
                    this.setDestinationToPlayerInterval -= Time.deltaTime;
                }
                if (this.addPlayerVelocityToDestination > 0f)
                {
                    if (this.targetPlayer == GameNetworkManager.Instance.localPlayerController)
                    {
                        this.destination += Vector3.Normalize(this.targetPlayer.thisController.velocity * 100f) * this.addPlayerVelocityToDestination;
                    }
                    else if (this.targetPlayer.timeSincePlayerMoving < 0.25f)
                    {
                        this.destination += Vector3.Normalize((this.targetPlayer.serverPlayerPosition - this.targetPlayer.oldPlayerPosition) * 100f) * this.addPlayerVelocityToDestination;
                    }
                }
            }

            if (this.updateDestinationInterval >= 0f)
            {
                this.updateDestinationInterval -= Time.deltaTime;
            }
            else
            {
                this.DoAIInterval();
                this.updateDestinationInterval = this.AIIntervalTime;
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
            if (StartOfRound.Instance.shipBounds.bounds.Contains(transform.position))
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
            this.agent.enabled = enable;
        }

        public virtual void DoAIInterval()
        {
            if (this.moveTowardsDestination)
            {
                this.agent.SetDestination(this.destination);
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
    }
}
