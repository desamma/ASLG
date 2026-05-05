﻿using UnityEngine;

public class BringerOfDeath_Spell : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float magicDamage = 20f;
    [SerializeField] private float destroyTime = 2f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D spellCollider;
    private StateManager<Enemy_BringerOfDeath_State> stateManager;

    [Header("Audio")]
    [SerializeField] private AudioClip boomAudioClip;
    [SerializeField] private float volume = 1f;
    private bool hitPlayer = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        // ensure collider starts disabled so it only detects when explicitly enabled
        if (spellCollider != null)
            spellCollider.enabled = false;

        var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
        magicDamage *= difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);

        stateManager = new StateManager<Enemy_BringerOfDeath_State>(animator, Enemy_BringerOfDeath_State.Spell);
    }

    public void EnableTrigger()
    {
        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
            spellCollider.enabled = true;
        }

        SoundFXManager.Instance.PlaySoundFXClip(boomAudioClip, transform, volume);

        Destroy(gameObject, destroyTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if ((collision.CompareTag("Player") || collision.CompareTag("NPC")) && !hitPlayer)
        {
            hitPlayer = true;
            if (collision.CompareTag("Player"))
            {
                StatsManager.instance.TakeDamage(magicDamage);
            }
            else if (collision.CompareTag("NPC"))
            {
                if (collision.TryGetComponent<NPCCompanion>(out var npc))
                {
                    npc.TakeDamage(magicDamage, false);
                }
            }
        }
    }
}