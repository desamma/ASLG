using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MonolithEndingScreen : MonoBehaviour
{
    public static MonolithEndingScreen Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SetAlpha(masterCanvasGroup, 0f);
        SetAlpha(choiceCanvasGroup, 0f);
        SetAlpha(endingCanvasGroup, 0f);
        SetAlpha(epilogueCanvasGroup, 0f);
        SetAlpha(mainMenuButtonGroup, 0f);
        HideText(arrivalText);
        HideText(choicePromptText);
        HideText(endingTitleText);
        HideText(endingBodyText);
        HideText(epilogueText);
        SetImageAlpha(returnEndingBackgroundImage, 0f);
        SetImageAlpha(destroyEndingBackgroundImage, 0f);


        if (returnButton != null) returnButton.onClick.AddListener(() => OnChoiceSelected(EndingChoice.Return));
        if (destroyButton != null) destroyButton.onClick.AddListener(() => OnChoiceSelected(EndingChoice.Destroy));
    }


    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup masterCanvasGroup;   
    [SerializeField] private CanvasGroup choiceCanvasGroup;    
    [SerializeField] private CanvasGroup endingCanvasGroup;    
    [SerializeField] private CanvasGroup epilogueCanvasGroup; 

    [Header("Overlay & Background")]
    [SerializeField] private Image fadeOverlay;                
    [SerializeField] private Image backgroundImage;            
    [SerializeField] private Image returnEndingBackgroundImage;      
    [SerializeField] private Image destroyEndingBackgroundImage;      

    [Header("Arrival Panel — before choice")]
    [SerializeField] private TextMeshProUGUI arrivalText;

    [Header("Choice Panel")]
    [SerializeField] private TextMeshProUGUI choicePromptText;
    [SerializeField] private Button returnButton;
    [SerializeField] private TextMeshProUGUI returnButtonLabel;
    [SerializeField] private TextMeshProUGUI returnButtonSub;   
    [SerializeField] private Button destroyButton;
    [SerializeField] private TextMeshProUGUI destroyButtonLabel;
    [SerializeField] private TextMeshProUGUI destroyButtonSub;

    [Header("Ending Lore Panel")]
    [SerializeField] private TextMeshProUGUI endingTitleText;
    [SerializeField] private TextMeshProUGUI endingBodyText;
    //[SerializeField] private Image endingIconImage;   

    [Header("Epilogue Panel")]
    [SerializeField] private TextMeshProUGUI epilogueText;
    [SerializeField] private Button toMainMenuButton;
    [SerializeField] private TextMeshProUGUI toMainMenuLabel;
    [SerializeField] private CanvasGroup mainMenuButtonGroup;

    [Header("Timing (seconds)")]
    [SerializeField] private float initialBlackHold = 1.0f;
    [SerializeField] private float screenFadeInTime = 2.0f;
    [SerializeField] private float arrivalFadeInTime = 1.4f;
    [SerializeField] private float choiceFadeInTime = 1.0f;
    [SerializeField] private float choiceHoldAfterType = 0.8f;
    [SerializeField] private float endingTransitionTime = 1.6f;
    [SerializeField] private float endingFadeInTime = 1.2f;
    [SerializeField] private float epilogueFadeInTime = 1.0f;
    [SerializeField] private float typewriterSpeed = 0.022f;
    [SerializeField] private float pauseAtParagraph = 0.35f;


    [Header("Ending Tints")]
    [SerializeField] private Color returnEndingTint = new Color(0.85f, 0.72f, 0.30f, 1f);
    [SerializeField] private Color destroyEndingTint = new Color(0.22f, 0.12f, 0.38f, 1f);

    [Header("Arrival Text")]
    [TextArea(4, 8)]
    [SerializeField]
    private string arrivalNarration =
        "The chamber is silent.\n\n" +
        "At the heart of the Sacred Grounds, where every corrupted knight once knelt " +
        "and every rift in the sky traces its origin — it stands.\n\n" +
        "The Monolith of the Sun.\n\n" +
        "Its light does not blind. It waits. As if it has always been waiting for you.";

    [TextArea(2, 4)]
    [SerializeField]
    private string choicePrompt =
        "The King's order echoes in your mind.\nBut the Monolith does not belong to any king.";

    // ─────────────────────────────────────────────────────────
    //  ENDING A — RETURN
    // ─────────────────────────────────────────────────────────
    [Header("Ending A — Return the Monolith")]
    [SerializeField] private string returnButtonText = "RETURN IT";
    [SerializeField] private string returnButtonSubText = "Fulfil the King's decree.";

    [TextArea(2, 4)]
    [SerializeField] private string returnEndingTitle = "ENDING A  ·  THE CROWNED SUN";

    [TextArea(10, 24)]
    [SerializeField]
    private string returnEndingBody =
        "You carry the Monolith back through the rifts, through the ruins, " +
        "through the gates of a kingdom that never expected you to return.\n\n" +
        "The King weeps when he sees it. Then he stops weeping.\n\n" +
        "Within a season, the rifts close. The Sacred Grounds grow quiet. " +
        "The Royal Army, empowered by the Monolith's light, marches beyond the old borders — " +
        "into lands that never asked for a king.\n\n" +
        "The White Maiden is not seen again. Some say she vanished the morning the Monolith " +
        "was placed on the throne-altar. Others say she was the first to kneel before it.\n\n" +
        "You are celebrated. Promoted. Given a name in the histories.\n\n" +
        "At night, through the palace window, you sometimes see the horizon glow gold — " +
        "the same colour as the Monolith's light. You have stopped wondering " +
        "whether that is a good thing.";

    [TextArea(2, 4)]
    [SerializeField]
    private string returnEpilogue =
        "The rifts are closed. The kingdom is vast.\nSome lights cast very long shadows.";

    // ─────────────────────────────────────────────────────────
    //  ENDING B — DESTROY
    // ─────────────────────────────────────────────────────────
    [Header("Ending B — Destroy the Monolith")]
    [SerializeField] private string destroyButtonText = "DESTROY IT";
    [SerializeField] private string destroyButtonSubText = "Let no one wield this power.";

    [TextArea(2, 4)]
    [SerializeField] private string destroyEndingTitle = "ENDING B  ·  THE LAST LIGHT";

    [TextArea(10, 24)]
    [SerializeField]
    private string destroyEndingBody =
        "Your weapon strikes the surface of the Monolith.\n\n" +
        "For a moment — nothing.\n\n" +
        "Then the light inside it spreads outward, not in an explosion, " +
        "but in something quieter. A breath releasing after centuries of being held.\n\n" +
        "The rifts seal themselves. Not slowly, not with ceremony — simply, finally, done. " +
        "The Sacred Grounds shudder once and go still. The corruption recedes like a tide " +
        "that has forgotten how to return.\n\n" +
        "The King receives no Monolith. He receives no soldier, either.\n\n" +
        "You remain in the Sacred Grounds long after the light fades. " +
        "The White Maiden finds you there, sitting in the dust where the Monolith stood. " +
        "She does not ask what you did. She already knows.\n\n" +
        "She sits beside you.\n\n" +
        "Outside, for the first time in living memory, the sky is whole.";

    [TextArea(2, 4)]
    [SerializeField]
    private string destroyEpilogue =
        "The Monolith is gone. The sky is unbroken.\nSome things are worth losing everything for.";

    [SerializeField] private string mainMenuButtonText = "Return to Main Menu";

    private enum EndingChoice { None, Return, Destroy }
    private bool _choiceEnabled = false;

    public void TriggerEnding()
    {
        gameObject.SetActive(true);
        StartCoroutine(ArrivalSequence());
    }

    private IEnumerator ArrivalSequence()
    {
        if (returnButtonLabel != null) returnButtonLabel.text = returnButtonText;
        if (returnButtonSub != null) returnButtonSub.text = returnButtonSubText;
        if (destroyButtonLabel != null) destroyButtonLabel.text = destroyButtonText;
        if (destroyButtonSub != null) destroyButtonSub.text = destroyButtonSubText;
        if (toMainMenuLabel != null) toMainMenuLabel.text = mainMenuButtonText;
        if (toMainMenuButton != null) toMainMenuButton.onClick.AddListener(OnMainMenuClicked);

        SetButtonInteractable(returnButton, false);
        SetButtonInteractable(destroyButton, false);

        SetAlpha(masterCanvasGroup, 1f);
        SetImageAlpha(fadeOverlay, 1f);


        yield return new WaitForSeconds(initialBlackHold);

        yield return StartCoroutine(FadeImage(fadeOverlay, 1f, 0f, screenFadeInTime));

        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(FadeInAndTypewrite(arrivalText, arrivalNarration, arrivalFadeInTime));

        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(FadeInAndTypewrite(choicePromptText, choicePrompt, 0.8f));

        yield return new WaitForSeconds(choiceHoldAfterType);

        SetButtonInteractable(returnButton, true);
        SetButtonInteractable(destroyButton, true);
        yield return StartCoroutine(FadeCanvasGroup(choiceCanvasGroup, 0f, 1f, choiceFadeInTime));
        _choiceEnabled = true;
    }

    private void OnChoiceSelected(EndingChoice choice)
    {
        if (!_choiceEnabled) return;
        _choiceEnabled = false;
        SetButtonInteractable(returnButton, false);
        SetButtonInteractable(destroyButton, false);
        StartCoroutine(EndingSequence(choice));
    }

    private IEnumerator EndingSequence(EndingChoice choice)
    {
        bool isReturn = choice == EndingChoice.Return;

        yield return StartCoroutine(FadeImage(fadeOverlay, 0f, 1f, endingTransitionTime * 0.6f));
        yield return StartCoroutine(FadeCanvasGroup(choiceCanvasGroup, 1f, 0f, 0.3f));
        HideText(arrivalText);
        HideText(choicePromptText);

        if (isReturn)
            SetImageAlpha(returnEndingBackgroundImage, 1f);
        else
            SetImageAlpha(destroyEndingBackgroundImage, 1f);

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(FadeImage(fadeOverlay, 1f, 0f, endingTransitionTime));

        yield return StartCoroutine(FadeCanvasGroup(endingCanvasGroup, 0f, 1f, endingFadeInTime));

        string title = isReturn ? returnEndingTitle : destroyEndingTitle;
        endingTitleText.color = isReturn ? returnEndingTint : destroyEndingTint;
        yield return StartCoroutine(FadeInAndTypewrite(endingTitleText, title, 0.8f));

        yield return new WaitForSeconds(0.5f);

        string body = isReturn ? returnEndingBody : destroyEndingBody;
        yield return StartCoroutine(FadeInAndTypewrite(endingBodyText, body, 0.6f));

        yield return new WaitForSeconds(1.0f);

        // ── Epilogue card ──
        yield return StartCoroutine(FadeImage(fadeOverlay, 0f, 1f, 1.0f));
        yield return StartCoroutine(FadeCanvasGroup(endingCanvasGroup, 1f, 0f, 0.3f));
        yield return new WaitForSeconds(0.4f);

        SetAlpha(epilogueCanvasGroup, 1f);
        HideText(epilogueText);
        SetAlpha(mainMenuButtonGroup, 0f);

        yield return StartCoroutine(FadeImage(fadeOverlay, 1f, 0f, epilogueFadeInTime));

        string epilogue = isReturn ? returnEpilogue : destroyEpilogue;
        yield return StartCoroutine(FadeInAndTypewrite(epilogueText, epilogue, 1.0f));

        yield return new WaitForSeconds(1.2f);

        yield return StartCoroutine(FadeCanvasGroup(mainMenuButtonGroup, 0f, 1f, 0.8f));
        if (toMainMenuButton != null) toMainMenuButton.interactable = true;

        PlayerPrefs.SetInt("LastEndingChoice", isReturn ? 1 : 2);
        PlayerPrefs.Save();
    }

    private void OnMainMenuClicked()
    {
        if (toMainMenuButton != null) toMainMenuButton.interactable = false;
        StartCoroutine(FadeToMainMenu());
    }

    private IEnumerator FadeToMainMenu()
    {
        yield return StartCoroutine(FadeImage(fadeOverlay, 0f, 1f, 1.2f));
        OnEndingFinished();
    }

    protected virtual void OnEndingFinished()
    {
        SceneManager.LoadScene("Start");
        Debug.Log("[MonolithEndingScreen] Ending finished — load main menu here.");
    }

    private IEnumerator FadeInAndTypewrite(TextMeshProUGUI tmp, string text, float fadeTime)
    {
        if (tmp == null) yield break;
        tmp.text = "";
        tmp.color = WithAlpha(tmp.color, 0f);
        yield return StartCoroutine(FadeInText(tmp, fadeTime));
        yield return StartCoroutine(TypewriteTextWithParagraphPauses(tmp, text));
    }

    private IEnumerator TypewriteTextWithParagraphPauses(TextMeshProUGUI tmp, string text)
    {
        if (tmp == null) yield break;
        tmp.text = "";
        for (int i = 0; i < text.Length; i++)
        {
            tmp.text += text[i];
            // Extra pause at paragraph breaks
            if (i + 1 < text.Length && text[i] == '\n' && text[i + 1] == '\n')
                yield return new WaitForSeconds(pauseAtParagraph);
            else
                yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    private IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;
        float e = 0f; Color c = img.color;
        while (e < duration)
        {
            e += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, e / duration);
            img.color = c;
            yield return null;
        }
        c.a = to; img.color = c;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float e = 0f;
        cg.interactable = to > 0f;
        cg.blocksRaycasts = to > 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, e / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeInText(TextMeshProUGUI tmp, float duration)
    {
        if (tmp == null) yield break;
        float e = 0f; Color c = tmp.color; c.a = 0f; tmp.color = c;
        while (e < duration)
        {
            e += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, e / duration);
            tmp.color = c;
            yield return null;
        }
        c.a = 1f; tmp.color = c;
    }

    private static void SetAlpha(CanvasGroup cg, float a) 
    { 
        if (cg != null) 
        {
            cg.alpha = a; 
            cg.interactable = a > 0f;
            cg.blocksRaycasts = a > 0f;
        }
    }
    private static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
    }
    private static void HideText(TextMeshProUGUI t)
    {
        if (t == null) return;
        t.text = ""; t.color = WithAlpha(t.color, 0f);
    }
    private static void SetButtonInteractable(Button b, bool v) { if (b != null) b.interactable = v; }
    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

#if UNITY_EDITOR
    [ContextMenu("Preview — Return Ending")]
    private void PreviewReturn() { TriggerEnding(); }   

    [ContextMenu("Clear Ending Save")]
    private void ClearSave() { PlayerPrefs.DeleteKey("LastEndingChoice"); }
#endif
}