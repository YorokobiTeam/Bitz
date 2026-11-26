using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Only return Confirmed after the game server reports that this player's revive was granted.</summary>
public interface IBitzReviveProvider
{
    void RequestRevive(Action<BitzReviveResult> completed);
}

public enum BitzReviveResult { Confirmed, Cancelled, Pending, Unavailable, Expired }
public enum BitzReviveOption { UseLife, WatchAd, CheckPending, StartNew, Unavailable }
public interface IBitzReviveOptions { void GetOption(Action<BitzReviveOption> completed); }
public interface IBitzReviveStatusSource
{
    string LastStatusMessage { get; }
    event Action<string> StatusChanged;
}

/// <summary>Gameplay HP is separate from the server's player lives.</summary>
public sealed class BitzHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(1)] private int damagePerMiss = 1;
    public int Current { get; private set; }
    public int Maximum => maxHealth;
    public bool IsDead => Current <= 0;
    public event Action Died;
    public event Action Revived;

    private IBitzReviveProvider reviveProvider;
    private IBitzReviveStatusSource statusSource;
    private Image barFill;
    private Text hpText;
    private Text statusText;
    private GameObject deathPanel;
    private Button reviveButton;
    private Text reviveButtonText;
    private bool reviveInProgress;
    private bool optionLoading;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        damagePerMiss = Mathf.Max(1, damagePerMiss);
        Current = maxHealth;
        BuildUi();
        RefreshUi();
    }

    private void OnDestroy()
    {
        if (statusSource != null) statusSource.StatusChanged -= HandleProviderStatus;
        statusSource = null;
    }

    public void SetReviveProvider(IBitzReviveProvider provider)
    {
        if (statusSource != null) statusSource.StatusChanged -= HandleProviderStatus;
        reviveProvider = provider;
        statusSource = provider as IBitzReviveStatusSource;
        if (statusSource != null) statusSource.StatusChanged += HandleProviderStatus;
        if (IsDead && provider != null) RefreshOption();
        RefreshUi();
    }

    private void HandleProviderStatus(string message)
    {
        if (this == null || statusText == null || !IsDead || string.IsNullOrEmpty(message)) return;
        statusText.text = message;
    }

    public void RegisterHit(HitResult hit)
    {
        if (IsDead || hit != HitResult.Missed) return;
        Current = Mathf.Max(0, Current - damagePerMiss);
        RefreshUi();
        if (IsDead) { Died?.Invoke(); RefreshOption(); }
    }

    public void RequestRevive()
    {
        if (!IsDead || reviveInProgress || reviveProvider == null) return;
        reviveInProgress = true;
        statusText.text = "Waiting for reward verification...";
        RefreshUi();
        try
        {
            reviveProvider.RequestRevive(result =>
            {
                if (this == null) return;
                reviveInProgress = false;
                if (!IsDead) return;
                if (result == BitzReviveResult.Confirmed)
                {
                    Current = maxHealth;
                    RefreshUi();
                    Revived?.Invoke();
                    return;
                }
                var detail = statusSource?.LastStatusMessage;
                statusText.text = !string.IsNullOrEmpty(detail) ? detail :
                    result == BitzReviveResult.Pending ? "Verification is still pending. Check again shortly." :
                    result == BitzReviveResult.Cancelled ? "The ad was closed. Try again when ready." :
                    result == BitzReviveResult.Expired ? "This revive attempt expired. Try again." :
                    "Revive is unavailable. Please try again later.";
                if (result == BitzReviveResult.Pending) reviveButtonText.text = "Check revive status";
                else if (result == BitzReviveResult.Cancelled || result == BitzReviveResult.Expired) RefreshOption();
                RefreshUi();
            });
        }
        catch (Exception)
        {
            reviveInProgress = false;
            statusText.text = "Revive is unavailable. Please try again later.";
            RefreshUi();
        }
    }

    private void RefreshOption()
    {
        if (!IsDead || reviveProvider == null || !(reviveProvider is IBitzReviveOptions options)) return;
        optionLoading = true; statusText.text = "Checking lives..."; RefreshUi();
        var expectedProvider = reviveProvider;
        try
        {
            options.GetOption(option =>
            {
                if (this == null || !IsDead || reviveProvider != expectedProvider) return;
                optionLoading = false;
                reviveButtonText.text = option == BitzReviveOption.UseLife ? "Use one life" : option == BitzReviveOption.CheckPending ? "Check revive status" : option == BitzReviveOption.StartNew ? "Start a new ad" : "Watch ad to revive";
                var detail = statusSource?.LastStatusMessage;
                statusText.text = !string.IsNullOrEmpty(detail) ? detail :
                    option == BitzReviveOption.UseLife ? "Use one life to continue." :
                    option == BitzReviveOption.CheckPending ? "Check the status of your revive." :
                    option == BitzReviveOption.StartNew ? "This attempt expired. Starting a new one may cancel a late reward." :
                    option == BitzReviveOption.WatchAd ? "Watch a rewarded ad to revive." : "Unable to check lives. Please try again.";
                RefreshUi();
            });
        }
        catch (Exception)
        {
            optionLoading = false;
            statusText.text = "Unable to check lives. Please try again.";
            RefreshUi();
        }
    }

    private void RefreshUi()
    {
        if (barFill == null) return;
        var fraction = (float)Current / maxHealth;
        barFill.gameObject.SetActive(Current > 0);
        barFill.rectTransform.anchorMax = new Vector2(fraction, 1);
        barFill.color = Color.Lerp(new Color(0.94f, 0.23f, 0.25f), new Color(0.26f, 0.86f, 0.54f), fraction);
        hpText.text = $"HP {Current} / {maxHealth}";
        deathPanel.SetActive(IsDead);
        reviveButton.interactable = IsDead && reviveProvider != null && !reviveInProgress && !optionLoading;
        if (IsDead && reviveProvider == null) statusText.text = "Ad revive is not connected yet.";
    }

    private void BuildUi()
    {
        var root = new GameObject("HP HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        if (EventSystem.current == null) new GameObject("HP EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(root.transform, false);

        var background = MakeImage("HP background", root.transform, new Color(0.07f, 0.08f, 0.12f, 0.9f));
        SetRect(background.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -34), new Vector2(340, 40), new Vector2(0, 1));
        barFill = MakeImage("HP fill", background.transform, Color.green);
        Stretch(barFill.rectTransform, 4);
        hpText = MakeText("HP text", background.transform, 22, TextAnchor.MiddleCenter);
        Stretch(hpText.rectTransform, 0);

        var veil = MakeImage("Death overlay", root.transform, new Color(0.02f, 0.02f, 0.05f, 0.8f));
        Stretch(veil.rectTransform, 0); deathPanel = veil.gameObject;
        var panel = MakeImage("Death panel", veil.transform, new Color(0.12f, 0.13f, 0.18f, 1));
        SetRect(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(600, 350), new Vector2(.5f, .5f));
        var title = MakeText("Death title", panel.transform, 36, TextAnchor.MiddleCenter); title.text = "OUT OF HP";
        SetRect(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -32), new Vector2(520, 55), new Vector2(.5f, 1));
        statusText = MakeText("Revive status", panel.transform, 22, TextAnchor.MiddleCenter);
        statusText.text = "Watch a rewarded ad to revive.";
        SetRect(statusText.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 30), new Vector2(530, 80), new Vector2(.5f, .5f));
        reviveButton = MakeButton("Watch ad to revive", panel.transform, new Vector2(0, -62), RequestRevive);
        reviveButtonText = reviveButton.GetComponentInChildren<Text>();
        MakeButton("Back to menu", panel.transform, new Vector2(0, -138), () => { Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); });
    }

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.color = color; return image;
    }

    private static Text MakeText(string name, Transform parent, int size, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size;
        text.alignment = alignment; text.color = Color.white; text.raycastTarget = false; return text;
    }

    private static Button MakeButton(string label, Transform parent, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var image = MakeImage(label, parent, new Color(.25f, .42f, .72f, 1));
        SetRect(image.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(330, 55), new Vector2(.5f, .5f));
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(onClick);
        var text = MakeText("Label", image.transform, 22, TextAnchor.MiddleCenter); text.text = label; Stretch(text.rectTransform, 0);
        return button;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
