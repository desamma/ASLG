using System.Collections;
using UnityEditor;
using UnityEngine;

public class Monolith_Pickup : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform beginTransform;
    [SerializeField] private Transform pickupTransform;
    [SerializeField] private Animator animator;

    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField, HideInInspector] private Transform playerTransform;
    [SerializeField] private Collider2D detectCollider;

    [Header("Pickup Animation")]
    [SerializeField] private float walkToBeginSpeed = 2f;
    [SerializeField] private float pickupDuration = 2f;
    [SerializeField] private float idleDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip appear;
    [SerializeField] private AudioClip disappear;
    [SerializeField] private float volume = 1f;

    private StateManager<Monolith_State> monolithStateManager;
    private StateManager<PlayerState> playerStateManager;
    private PlayerMovement playerMovement;
    private bool isPickupActive = false;
    private string className;

    private void Start()
    {
        className = ClassManager.Instance.CurrentClassData.playerClass.ToString().ToLower();

        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = playerTransform.GetComponent<PlayerMovement>();
        monolithStateManager = new StateManager<Monolith_State>(animator, Monolith_State.Empty);
        StartCoroutine(AppearCoroutine());
    }

    private IEnumerator AppearCoroutine()
    {
        monolithStateManager.ChangeState(Monolith_State.Appear);
        yield return new WaitForSeconds(1.5f);
        monolithStateManager.ChangeState(Monolith_State.Idle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isPickupActive)
        {
            StartPickupSequence();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Pickup Sequence")]
    private void TestPickupSequence()
    {
        StartPickupSequence();
    }
#endif

    private void StartPickupSequence()
    {
        playerStateManager = playerMovement.GetStateManager();
        isPickupActive = true;
        playerMovement.SetMovementLock(true);
        monolithStateManager.ChangeState(Monolith_State.Idle);
        StartCoroutine(PickupSequenceCoroutine());
    }

    private IEnumerator PickupSequenceCoroutine()
    {
        // Step 1: Walk to beginTransform
        yield return StartCoroutine(WalkToBeginTransform());

        // Step 2: Face the monolith
        FaceTowardMonolith();

        // Step 3: Move to pickupTransform
        yield return StartCoroutine(MoveToPickupTransform());

        MonolithEndingScreen.Instance.TriggerEnding();
    }

    private IEnumerator WalkToBeginTransform()
    {
        playerMovement.FaceToward(gameObject.transform.position.x);

        // Play walk animation
        playerStateManager.ChangeState(PlayerState.SlowWalk);

        Vector3 endPosition = beginTransform.position;

        while (Vector3.Distance(playerTransform.position, endPosition) > 0.01f)
        {
            Vector3 direction = (endPosition - playerTransform.position).normalized;
            playerTransform.position += Time.deltaTime * walkToBeginSpeed * direction;
            yield return null;
        }

        playerTransform.position = endPosition;
    }

    private void FaceTowardMonolith()
    {
        // Make player face toward the monolith (pickupTransform)
        playerMovement.FaceToward(pickupTransform.position.x);
    }

    private IEnumerator MoveToPickupTransform()
    {
        // Play slowWalk animation while moving
        playerStateManager.ChangeState(PlayerState.SlowWalk);

        float elapsed = 0f;
        Vector3 startPosition = playerTransform.position;
        Vector3 endPosition = pickupTransform.position;

        while (elapsed < pickupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pickupDuration;
            playerTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        playerTransform.position = endPosition;

        // Play idle animation for 1 second
        playerStateManager.ChangeState(PlayerState.Idle);
        yield return new WaitForSeconds(idleDuration);

        // Play grabMonolith animation
        playerStateManager.ChangeState(PlayerState.MonolithPickup);
        yield return new WaitForSeconds(4f);
        //monolithStateManager.ChangeState(Monolith_State.Disappear);

        playerMovement.SetMovementLock(false);
        isPickupActive = false;
    }
    public void PlayAudio(int num)
    {
        switch(num)
        {
            case 0: SoundFXManager.Instance.PlaySoundFXClip(appear, transform, volume); break;
            case 1: SoundFXManager.Instance.PlaySoundFXClip(disappear, transform, volume); break;
        }
    }
}
