using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    private Canvas transitionCanvas;
    private Image backgroundImage;
    private Image loadingBar;
    private Image throbber;
    private RectTransform loadingElementTransform;
    private bool isTransitioning = false;
    private AsyncOperation asyncLoad;

    [Header("General Settings")]
    [SerializeField] private float minimumLoadingTime = 1f;

    [Header("Loading Element Type")]
    [SerializeField] private bool useLoadingBar = true;
    [SerializeField] private bool isRadialLoadingBar = false;
    [SerializeField] private bool spinThrobber = true;

    [Header("Position Settings")]
    [SerializeField] private LoadingElementPosition position = LoadingElementPosition.Center;

    [Header("Scale Settings")]
    [SerializeField] private Vector2 elementScale = Vector2.one;

    private void Awake()
    {
        // Ensure singleton pattern and persistence
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTransitionCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeTransitionCanvas()
    {
        // Create canvas
        GameObject canvasObject = new GameObject("TransitionCanvas");
        transitionCanvas = canvasObject.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 1000;
        DontDestroyOnLoad(canvasObject);

        // Create black background
        backgroundImage = CreateUIElement<Image>("Background", transitionCanvas.transform);
        backgroundImage.color = Color.black;
        backgroundImage.rectTransform.anchorMin = Vector2.zero;
        backgroundImage.rectTransform.anchorMax = Vector2.one;
        backgroundImage.rectTransform.sizeDelta = Vector2.zero;

        // Create loading element (bar or throbber)
        if (useLoadingBar)
        {
            loadingBar = CreateUIElement<Image>("LoadingBar", transitionCanvas.transform);
            loadingElementTransform = loadingBar.rectTransform;
            loadingBar.type = Image.Type.Filled;
            loadingBar.fillAmount = 0f;
            loadingBar.fillMethod = Image.FillMethod.Horizontal;
            if (isRadialLoadingBar)
            {
                loadingBar.fillMethod = Image.FillMethod.Radial360;
            }
        }
        else
        {
            throbber = CreateUIElement<Image>("Throbber", transitionCanvas.transform);
            loadingElementTransform = throbber.rectTransform;
        }

        UpdateLoadingElementPositionAndScale();
        transitionCanvas.enabled = false;
    }

    private T CreateUIElement<T>(string name, Transform parent) where T : Component
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.AddComponent<T>();
    }

    private void UpdateLoadingElementPositionAndScale()
    {
        if (loadingElementTransform == null) return;

        loadingElementTransform.localScale = elementScale;

        Vector2 anchorMin, anchorMax, anchoredPosition;
        switch (position)
        {
            case LoadingElementPosition.Center:
                anchorMin = anchorMax = new Vector2(0.5f, 0.5f);
                anchoredPosition = Vector2.zero;
                break;
            case LoadingElementPosition.TopLeft:
                anchorMin = anchorMax = new Vector2(0f, 1f);
                anchoredPosition = new Vector2(50f, -50f);
                break;
            case LoadingElementPosition.TopRight:
                anchorMin = anchorMax = new Vector2(1f, 1f);
                anchoredPosition = new Vector2(-50f, -50f);
                break;
            case LoadingElementPosition.BottomLeft:
                anchorMin = anchorMax = new Vector2(0f, 0f);
                anchoredPosition = new Vector2(50f, 50f);
                break;
            case LoadingElementPosition.BottomRight:
                anchorMin = anchorMax = new Vector2(1f, 0f);
                anchoredPosition = new Vector2(-50f, 50f);
                break;
            default:
                anchorMin = anchorMax = new Vector2(0.5f, 0.5f);
                anchoredPosition = Vector2.zero;
                break;
        }

        loadingElementTransform.anchorMin = anchorMin;
        loadingElementTransform.anchorMax = anchorMax;
        loadingElementTransform.anchoredPosition = anchoredPosition;
        loadingElementTransform.sizeDelta = new Vector2(100f, 100f);
    }

    /// <summary>
    /// Starts a scene transition with optional audio clips for start and end
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    /// <param name="startClip">Audio clip to play at transition start (optional)</param>
    /// <param name="endClip">Audio clip to play at transition end (optional)</param>
    /// <param name="volume">Volume of transition sounds (default: 1f)</param>
    /// <param name="pitch">Pitch of transition sounds (default: 1f)</param>
    public static void TransitionToScene(string sceneName, AudioClip startClip = null, AudioClip endClip = null, float volume = 1f, float pitch = 1f)
    {
        if (instance != null && !instance.isTransitioning)
        {
            instance.StartCoroutine(instance.TransitionRoutine(sceneName, startClip, endClip, volume, pitch));
        }
    }

    private IEnumerator TransitionRoutine(string sceneName, AudioClip startClip, AudioClip endClip, float volume, float pitch)
    {
        isTransitioning = true;

        // Play start sound if provided
        if (startClip != null)
        {
            AudioManager.Instance.PlayClip2D(startClip, volume, pitch);
        }

        // Show loading screen
        transitionCanvas.enabled = true;

        // Start loading scene asynchronously
        asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float elapsedTime = 0f;

        // Wait for minimum loading time or until scene is loaded
        while (elapsedTime < minimumLoadingTime || asyncLoad.progress < 0.9f)
        {
            elapsedTime += Time.deltaTime;
            UpdateLoadingProgress(asyncLoad.progress);
            yield return null;
        }

        // Allow scene activation
        asyncLoad.allowSceneActivation = true;

        // Wait for scene to fully load
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Play end sound if provided
        if (endClip != null)
        {
            AudioManager.Instance.PlayClip2D(endClip, volume, pitch);
        }

        // Hide loading screen
        transitionCanvas.enabled = false;
        isTransitioning = false;
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (useLoadingBar)
        {
            if (isRadialLoadingBar)
            {
                loadingBar.fillAmount = progress;
            }
            else
            {
                loadingBar.rectTransform.localScale = new Vector3(progress, 1f, 1f);
            }
        }
        else if (throbber != null && spinThrobber)
        {
            throbber.rectTransform.Rotate(0f, 0f, -360f * Time.deltaTime);
        }
    }

    private enum LoadingElementPosition
    {
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}