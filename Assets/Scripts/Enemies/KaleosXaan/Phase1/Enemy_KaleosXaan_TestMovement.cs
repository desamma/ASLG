using System.Collections;
using UnityEngine;

/// <summary>
/// Test script for cycling through Kaleos Xaan moves for effect testing
/// Attach this to the Kaleos Xaan enemy prefab for testing
/// </summary>
public class Enemy_KaleosXaan_TestMovement : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private Enemy_KaleosXaan_State testMove = Enemy_KaleosXaan_State.Attack;
    [SerializeField] private float moveStateDuration = 2f;
    [SerializeField] private float idleStateDuration = 3f;
    [SerializeField] private bool autoStart = true;

    [Header("Components")]
    [SerializeField] private Animator animator;

    private StateManager<Enemy_KaleosXaan_State> stateManager;
    private bool isTestRunning = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        stateManager = new StateManager<Enemy_KaleosXaan_State>(animator, testMove);

        if (autoStart)
        {
            StartTest();
        }
    }

    private void Update()
    {
        // Press T to manually start/stop test
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (isTestRunning)
            {
                StopTest();
            }
            else
            {
                StartTest();
            }
        }

        // Press 1-7 to test different moves
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            testMove = Enemy_KaleosXaan_State.Attack;
            Debug.Log("Test Move changed to: Attack");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            testMove = Enemy_KaleosXaan_State.ArcaneHeart;
            Debug.Log("Test Move changed to: ArcaneHeart");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            testMove = Enemy_KaleosXaan_State.BlinkEnhance;
            Debug.Log("Test Move changed to: BlinkEnhance");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            testMove = Enemy_KaleosXaan_State.SummonCompanion;
            Debug.Log("Test Move changed to: SummonCompanion");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            testMove = Enemy_KaleosXaan_State.DaemonicLure;
            Debug.Log("Test Move changed to: DaemonicLure");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            testMove = Enemy_KaleosXaan_State.Chase;
            Debug.Log("Test Move changed to: Chase");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            testMove = Enemy_KaleosXaan_State.Idle;
            Debug.Log("Test Move changed to: Idle");
        }
    }

    public void StartTest()
    {
        if (stateManager == null)
        {
            Debug.LogError("TestMovement: StateManager is null! Make sure Enemy_KaleosXaan_Movement is attached.");
            return;
        }

        if (!isTestRunning)
        {
            isTestRunning = true;
            Debug.Log($"Starting test cycle with move: {testMove}");
            StartCoroutine(TestCycle());
        }
    }

    public void StopTest()
    {
        if (isTestRunning)
        {
            isTestRunning = false;
            StopAllCoroutines();
            if (stateManager != null)
            {
                stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
            }
            Debug.Log("Test cycle stopped");
        }
    }

    private IEnumerator TestCycle()
    {
        while (isTestRunning)
        {
            // Execute the test move
            Debug.Log($"Executing move: {testMove}");
            stateManager.ChangeState(testMove);

            // Wait for move duration
            yield return new WaitForSeconds(moveStateDuration);

            // Return to idle
            Debug.Log("Returning to idle");
            stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);

            // Wait for idle duration
            yield return new WaitForSeconds(idleStateDuration);
        }
    }

    private void OnDisable()
    {
        StopTest();
    }
}
