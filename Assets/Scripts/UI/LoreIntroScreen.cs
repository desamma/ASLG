using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LoreIntroScreen : MonoBehaviour
{

    [Header("Panel & Overlay")]
    [SerializeField] private CanvasGroup mainCanvasGroup;   
    [SerializeField] private Image fadeOverlay;       
    [SerializeField] private Image backgroundPanel;   

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI titleText;        
    [SerializeField] private TextMeshProUGUI loreText;         
    [SerializeField] private TextMeshProUGUI footerText;       

    [Header("Button")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonLabel;
    [SerializeField] private CanvasGroup buttonCanvasGroup; 

    [Header("Optional FX")]
    [SerializeField] private ParticleSystem ambientParticles;


    [Header("Timing (seconds)")]
    [SerializeField] private float initialBlackHold = 0.5f;  
    [SerializeField] private float screenFadeInTime = 1.8f;  
    [SerializeField] private float titleFadeInTime = 1.2f;
    [SerializeField] private float loreFadeInTime = 1.4f;
    [SerializeField] private float footerFadeInTime = 0.8f;
    [SerializeField] private float buttonFadeInTime = 0.9f;
    [SerializeField] private float delayBetweenElements = 0.6f;
    [SerializeField] private float exitFadeOutTime = 1.2f;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float typewriterSpeed = 0.025f; 


    [Header("Lore Content")]
    [TextArea(3, 6)]
    [SerializeField] private string loreTitle = "IN THE BEGINNING…";

    [TextArea(8, 20)]
    [SerializeField]
    private string loreBody =
        "Before the age of kingdoms, the world was kept in balance by the Sacred Grounds — " +
        "ancient sanctuaries where the boundary between realms grew thin, and the power of creation " +
        "itself lay dormant within colossal Monoliths.\n\n" +
        "For centuries, the Royal Army stood watch over these places, " +
        "guarding them from those who would corrupt their light.\n\n" +
        "Then the sky tore open.\n\n" +
        "Rifts now scar the heavens — wounds that bridge the Outside World and the Sacred Grounds, " +
        "bleeding darkness into the sanctuaries and awakening horrors long sealed away.\n\n" +
        "The Monolith of the Sun has gone silent. The King has issued a decree. " +
        "And somewhere, in the ruins of a soldier's camp, a single pair of eyes opens.";

    [TextArea(2, 4)]
    [SerializeField] private string loreFooter = "Your story begins now.";

    [SerializeField] private string buttonText = "Let's Get Started";

    //  PlayerPrefs key — set to true after first view
    private const string SEEN_INTRO_KEY = "LoreIntro_Seen";


    private bool _skippable = false;   

    public CharacterCreation characterCreation;


    private void Awake()
    {
        SetAlpha(mainCanvasGroup, 0f);
        SetAlpha(buttonCanvasGroup, 0f);
        HideText(titleText);
        HideText(loreText);
        HideText(footerText);

        if (startButtonLabel != null)
            startButtonLabel.text = buttonText;

        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);

        if (characterCreation != null)
        {
            characterCreation.OnStartButtonClickEvent += () => StartCoroutine(PlayIntroSequence());
        }
    }

    private void Start()
    {
        // PlayerPrefs.DeleteKey(SEEN_INTRO_KEY);

        if (PlayerPrefs.GetInt(SEEN_INTRO_KEY, 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        //StartCoroutine(PlayIntroSequence());
    }

    private void OnDestroy()
    {
        if (characterCreation != null)
        {
            characterCreation.OnStartButtonClickEvent -= () => StartCoroutine(PlayIntroSequence());
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        SetAlpha(mainCanvasGroup, 1f);

        SetImageAlpha(fadeOverlay, 1f);

        if (ambientParticles != null)
            ambientParticles.Play();

        yield return new WaitForSeconds(initialBlackHold);

        // ── 1. Fade in background panel, fade out black overlay simultaneously ──
        yield return StartCoroutine(FadeImage(fadeOverlay, 1f, 0f, screenFadeInTime));

        yield return new WaitForSeconds(delayBetweenElements * 0.5f);

        // ── 2. Title ──
        if (titleText != null)
        {
            titleText.text = "";
            titleText.color = WithAlpha(titleText.color, 0f);
            yield return StartCoroutine(FadeInText(titleText, titleFadeInTime));
            if (useTypewriter)
                yield return StartCoroutine(TypewriteText(titleText, loreTitle));
            else
                titleText.text = loreTitle;
        }

        yield return new WaitForSeconds(delayBetweenElements);

        // ── 3. Lore body ──
        if (loreText != null)
        {
            loreText.text = "";
            loreText.color = WithAlpha(loreText.color, 0f);
            yield return StartCoroutine(FadeInText(loreText, loreFadeInTime));
            if (useTypewriter)
                yield return StartCoroutine(TypewriteText(loreText, loreBody));
            else
                loreText.text = loreBody;
        }

        yield return new WaitForSeconds(delayBetweenElements);

        // ── 4. Footer ──
        if (footerText != null)
        {
            footerText.text = "";
            footerText.color = WithAlpha(footerText.color, 0f);
            yield return StartCoroutine(FadeInText(footerText, footerFadeInTime));
            if (useTypewriter)
                yield return StartCoroutine(TypewriteText(footerText, loreFooter));
            else
                footerText.text = loreFooter;
        }

        yield return new WaitForSeconds(delayBetweenElements);

        // ── 5. Button ──
        yield return StartCoroutine(FadeCanvasGroup(buttonCanvasGroup, 0f, 1f, buttonFadeInTime));
        buttonCanvasGroup.interactable = true;
        buttonCanvasGroup.blocksRaycasts = true;
        if (startButton != null) startButton.interactable = true;
        _skippable = true;
    }

    private void OnStartButtonClicked()
    {
        if (!_skippable) return;
        StartCoroutine(ExitSequence());
    }

    private IEnumerator ExitSequence()
    {
        _skippable = false;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
        if (startButton != null) startButton.interactable = false;

        yield return StartCoroutine(FadeImage(fadeOverlay, 0f, 1f, exitFadeOutTime));

        PlayerPrefs.SetInt(SEEN_INTRO_KEY, 1);
        PlayerPrefs.Save();

        OnIntroComplete();

        //gameObject.SetActive(false);
    }

    protected virtual void OnIntroComplete()
    {
        SceneManager.LoadScene("StarterVillage"); 
        Debug.Log("[LoreIntroScreen] Intro complete — transition to gameplay here.");
    }


    private IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;
        float elapsed = 0f;
        Color c = img.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            img.color = c;
            yield return null;
        }
        c.a = to; img.color = c;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeInText(TextMeshProUGUI tmp, float duration)
    {
        if (tmp == null) yield break;
        float elapsed = 0f;
        Color c = tmp.color;
        c.a = 0f; tmp.color = c;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            tmp.color = c;
            yield return null;
        }
        c.a = 1f; tmp.color = c;
    }

    private IEnumerator TypewriteText(TextMeshProUGUI tmp, string fullText)
    {
        if (tmp == null) yield break;
        tmp.text = "";
        foreach (char ch in fullText)
        {
            tmp.text += ch;

            float elapsed = 0f;
            while (elapsed < typewriterSpeed)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    tmp.text = fullText;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
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

    private static void HideText(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.text = "";
        tmp.color = WithAlpha(tmp.color, 0f);
    }

    private static Color WithAlpha(Color c, float a)
    {
        c.a = a; return c;
    }

#if UNITY_EDITOR
    // ── Quick reset key during play mode (press R) ──
    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.R))
    //    {
    //        PlayerPrefs.DeleteKey(SEEN_INTRO_KEY);
    //        Debug.Log("[LoreIntroScreen] Intro flag reset — restart play mode to see it again.");
    //    }
    //}
#endif
}