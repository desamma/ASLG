using UnityEngine;
using System.Reflection;

[DisallowMultipleComponent]
public class SpawnerEnemyAudioDetector : MonoBehaviour
{
    private EnemyAreaSpawner _spawner;
    private bool _hasNotifiedSpawnerOfDetection;

    public void Initialize(EnemyAreaSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Update()
    {
        if (_hasNotifiedSpawnerOfDetection) return;
        if (_spawner == null) return;

        if (IsEnemyInCombatState())
        {
            _hasNotifiedSpawnerOfDetection = true;
            _spawner.OnEnemyDetectedPlayer();
        }
    }

    private bool IsEnemyInCombatState()
    {
        // Get all components that might have a StateManager
        var components = GetComponents<MonoBehaviour>();
        
        foreach (var component in components)
        {
            if (component == null) continue;

            // Look for GetStateManager method
            var method = component.GetType().GetMethod("GetStateManager",  BindingFlags.Public | BindingFlags.Instance);

            if (method == null) continue;

            object stateManager = method.Invoke(component, null);
            if (stateManager == null) continue;

            // Get the current state from the StateManager
            var currentStateProperty = stateManager.GetType().GetProperty("CurrentState",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            if (currentStateProperty != null)
            {
                object currentState = currentStateProperty.GetValue(stateManager);
                if (currentState != null)
                {
                    string stateName = currentState.ToString();
                    
                    // Check for common combat state names
                    if (stateName.Contains("Chase") || 
                        stateName.Contains("Attack") || 
                        stateName.Contains("Cast"))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
