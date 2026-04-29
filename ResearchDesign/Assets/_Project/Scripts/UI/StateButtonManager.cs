using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StateButtonMapping
{
    [Tooltip("The state in which these buttons should be active.")]
    public ScreenState targetState;
    
    [Tooltip("The buttons (or GameObjects) to enable during this state, and disable in others.")]
    public List<GameObject> buttons;
}

public class StateButtonManager : MonoBehaviour
{
    [Tooltip("Configure which buttons show up for which states.")]
    public List<StateButtonMapping> stateMappings = new List<StateButtonMapping>();

    private void Start()
    {
        if (ScreenController.Instance != null)
        {
            ScreenController.Instance.OnStateChanged += HandleStateChanged;
            
            // Initialize with the current state just in case Start runs after LoadState
            if (ScreenController.Instance.CurrentState != null)
            {
                HandleStateChanged(ScreenController.Instance.CurrentState);
            }
            else
            {
                // If there's no state yet, disable all managed buttons
                DisableAllManagedButtons();
            }
        }
    }

    private void OnDestroy()
    {
        if (ScreenController.Instance != null)
        {
            ScreenController.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(ScreenState newState)
    {
        // Keep track of buttons that must be enabled
        HashSet<GameObject> buttonsToEnable = new HashSet<GameObject>();
        HashSet<GameObject> allManagedButtons = new HashSet<GameObject>();

        foreach (var mapping in stateMappings)
        {
            bool isCurrentState = (mapping.targetState == newState);
            
            foreach (var button in mapping.buttons)
            {
                if (button != null)
                {
                    allManagedButtons.Add(button);
                    if (isCurrentState)
                    {
                        buttonsToEnable.Add(button);
                    }
                }
            }
        }

        // Apply final view state
        foreach (var button in allManagedButtons)
        {
            button.SetActive(buttonsToEnable.Contains(button));
        }
    }

    private void DisableAllManagedButtons()
    {
        foreach (var mapping in stateMappings)
        {
            foreach (var button in mapping.buttons)
            {
                if (button != null)
                {
                    button.SetActive(false);
                }
            }
        }
    }
}
