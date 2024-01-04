using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace LethalPets
{
    [RequireComponent(typeof(InteractTrigger))]
    public class HoldInteractTriggerEvent : MonoBehaviour
    {
        public InteractEvent onInteract;
        public InteractEvent onInteractHolding;
        public InteractEvent onInteractHoldingCancel;
        InteractTrigger localInteractTrigger;
        bool isHolding;
        float holdingTimer;
        [SerializeField] private float holdingTimerMax = 0.5f;

        public void Awake()
        {
            localInteractTrigger = GetComponent<InteractTrigger>();
            localInteractTrigger.interactable = false;
        }

        public void OnEnable()
        {
            IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").canceled += InteractCanceled;
        }

        private void InteractCanceled(InputAction.CallbackContext ctx)
        {
            if (holdingTimer < holdingTimerMax)
            {
                onInteract?.Invoke(GameNetworkManager.Instance.localPlayerController);
            }
            else
            {
                onInteractHoldingCancel?.Invoke(GameNetworkManager.Instance.localPlayerController);
            }

            holdingTimer = 0;
        }

        public void Update()
        {
            isHolding = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").ReadValue<float>() > 0.0f;
            if (isHolding)
            {
                if (GameNetworkManager.Instance.localPlayerController.hoveringOverTrigger != localInteractTrigger)
                    return;

                holdingTimer += Time.deltaTime;

                if (holdingTimer > holdingTimerMax)
                {
                    onInteractHolding?.Invoke(GameNetworkManager.Instance.localPlayerController);
                }
            }
        }
    }
}
