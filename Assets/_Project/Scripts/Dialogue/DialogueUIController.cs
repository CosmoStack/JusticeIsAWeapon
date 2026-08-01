using System.Collections.Generic;
using JusticeIsAWeapon.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace JusticeIsAWeapon.Dialogue
{
    /// <summary>
    /// The visual half of the dialogue system: portrait slot, speaker name,
    /// text box and clickable choice buttons. Binds to whatever node
    /// DialogueManager reports — no per-character code.
    ///
    /// When no scene UI exists yet (buildUIOnAwake), it constructs the whole
    /// canvas + panel at runtime so the imported tree is testable immediately.
    /// </summary>
    public class DialogueUIController : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Builds the dialogue canvas/panel/buttons at runtime when the scene has none.")]
        public bool buildUIOnAwake = true;

        [Header("Widgets (auto-assigned after BuildUI)")]
        public GameObject panel;
        public Image portraitImage;
        public TextMeshProUGUI speakerText;
        public TextMeshProUGUI bodyText;
        public VerticalLayoutGroup choiceList;

        private readonly List<GameObject> _choiceButtons = new List<GameObject>();
        private LayoutElement _choiceListLayout;
        private TMP_FontAsset _font;

        private TMP_FontAsset Font
        {
            get
            {
                if (_font == null)
                {
                    _font = TMP_Settings.defaultFontAsset;
                    if (_font == null)
                    {
                        _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    }
                }
                return _font;
            }
        }

        private void Awake()
        {
            if (buildUIOnAwake)
            {
                BuildUI();
            }
        }

        private void OnEnable()
        {
            DialogueManager manager = CurrentManager();
            if (manager == null)
            {
                return;
            }
            manager.OnNodeChanged += HandleNodeChanged;
            manager.OnChoicesRefreshed += RefreshChoices;
            manager.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            DialogueManager manager = CurrentManager();
            if (manager == null)
            {
                return;
            }
            manager.OnNodeChanged -= HandleNodeChanged;
            manager.OnChoicesRefreshed -= RefreshChoices;
            manager.OnDialogueEnded -= HandleDialogueEnded;
        }

        private static DialogueManager CurrentManager()
        {
            return DialogueManager.Instance != null ? DialogueManager.Instance : FindFirstObjectByType<DialogueManager>();
        }

        private void HandleNodeChanged(DialogueNodeSO node)
        {
            ShowPanel();
            RefreshBody();
            RefreshChoices();
        }

        private void HandleDialogueEnded()
        {
            HidePanel();
        }

        private void RefreshBody()
        {
            DialogueManager manager = CurrentManager();
            if (manager == null)
            {
                return;
            }

            bool hasSpeaker = !string.IsNullOrEmpty(manager.CurrentNode?.speakerName);
            if (speakerText != null)
            {
                speakerText.text = hasSpeaker ? manager.CurrentNode.speakerName : string.Empty;
                if (speakerText.gameObject.activeSelf != hasSpeaker)
                {
                    speakerText.gameObject.SetActive(hasSpeaker);
                }
            }
            if (bodyText != null)
            {
                bodyText.text = manager.CurrentText ?? string.Empty;
            }
        }

        private void RefreshChoices()
        {
            if (choiceList == null)
            {
                return;
            }

            foreach (GameObject button in _choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _choiceButtons.Clear();

            DialogueManager manager = CurrentManager();
            if (manager == null || !manager.IsActive)
            {
                HidePanel();
                return;
            }

            IReadOnlyList<DialogueManager.DialogueChoice> choices = manager.AvailableChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                CreateChoiceButton(index, choices[i].label);
            }

            if (_choiceListLayout != null)
            {
                _choiceListLayout.preferredHeight = Mathf.Max(0, choices.Count * 44 + Mathf.Max(0, choices.Count - 1) * 6);
            }

            if (choices.Count == 0)
            {
                HidePanel();
            }
        }

        private void CreateChoiceButton(int index, string label)
        {
            GameObject buttonGO = new GameObject("Choice_" + index, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(choiceList.transform, false);

            Image background = buttonGO.GetComponent<Image>();
            background.color = new Color(0.13f, 0.13f, 0.18f, 1f);

            LayoutElement layout = buttonGO.GetComponent<LayoutElement>();
            layout.preferredHeight = 44;

            Button button = buttonGO.GetComponent<Button>();
            button.targetGraphic = background;

            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(buttonGO.transform, false);

            TextMeshProUGUI labelText = labelGO.GetComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.font = Font;
            labelText.fontSize = 26;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.margin = new Vector4(12, 0, 12, 0);
            labelText.enableWordWrapping = false;

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            button.onClick.AddListener(() => CurrentManager()?.SelectChoice(index));
            _choiceButtons.Add(buttonGO);
        }

        // ------------------------------------------------------------------
        // Runtime UI construction (used until real scene UI ships)
        // ------------------------------------------------------------------

        public void BuildUI()
        {
            if (panel != null)
            {
                return;
            }

            EnsureEventSystem();

            GameObject canvasGO = new GameObject("DialogueCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Bottom panel ---------------------------------------------------
            panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGO.transform, false);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.94f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.28f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Portrait slot --------------------------------------------------
            GameObject portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGO.transform.SetParent(panel.transform, false);
            portraitImage = portraitGO.GetComponent<Image>();
            portraitImage.color = new Color(0.32f, 0.32f, 0.38f, 1f);

            RectTransform portraitRect = portraitGO.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0, 0);
            portraitRect.anchorMax = new Vector2(0, 1);
            portraitRect.pivot = new Vector2(0, 0.5f);
            portraitRect.offsetMin = new Vector2(16, 12);
            portraitRect.offsetMax = new Vector2(176, -12);

            // Content column (right of portrait) -----------------------------
            GameObject columnGO = new GameObject("ContentColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            columnGO.transform.SetParent(panel.transform, false);

            RectTransform columnRect = columnGO.GetComponent<RectTransform>();
            columnRect.anchorMin = new Vector2(0, 0);
            columnRect.anchorMax = new Vector2(1, 1);
            columnRect.offsetMin = new Vector2(196, 0);
            columnRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup columnLayout = columnGO.GetComponent<VerticalLayoutGroup>();
            columnLayout.padding = new RectOffset(4, 12, 12, 12);
            columnLayout.spacing = 8;
            columnLayout.childAlignment = TextAnchor.UpperLeft;
            columnLayout.childControlWidth = true;
            columnLayout.childControlHeight = true;
            columnLayout.childForceExpandWidth = true;
            columnLayout.childForceExpandHeight = false;

            // Speaker name ----------------------------------------------------
            GameObject speakerGO = new GameObject("SpeakerName", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            speakerGO.transform.SetParent(columnGO.transform, false);
            speakerText = speakerGO.GetComponent<TextMeshProUGUI>();
            speakerText.font = Font;
            speakerText.fontSize = 32;
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = new Color(0.95f, 0.87f, 0.62f, 1f);
            speakerGO.GetComponent<LayoutElement>().preferredHeight = 34;

            // Scrollable body -------------------------------------------------
            GameObject scrollGO = new GameObject("ScrollArea",
                typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(LayoutElement));
            scrollGO.transform.SetParent(columnGO.transform, false);
            scrollGO.GetComponent<LayoutElement>().flexibleHeight = 1f;
            scrollGO.GetComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject contentGO = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);

            VerticalLayoutGroup contentLayout = contentGO.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            contentLayout.spacing = 0;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject bodyGO = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGO.transform.SetParent(contentGO.transform, false);
            bodyText = bodyGO.GetComponent<TextMeshProUGUI>();
            bodyText.font = Font;
            bodyText.fontSize = 28;
            bodyText.color = new Color(0.92f, 0.92f, 0.94f, 1f);
            bodyText.alignment = TextAlignmentOptions.TopLeft;

            ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentGO.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 24;

            // Choice list ------------------------------------------------------
            GameObject choicesGO = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            choicesGO.transform.SetParent(columnGO.transform, false);
            choiceList = choicesGO.GetComponent<VerticalLayoutGroup>();
            choiceList.spacing = 6;
            choiceList.childAlignment = TextAnchor.UpperLeft;
            choiceList.childControlWidth = true;
            choiceList.childControlHeight = true;
            _choiceListLayout = choicesGO.GetComponent<LayoutElement>();

            // Restart button (handy for iterating on the tree in the test scene)
            GameObject restartGO = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            restartGO.transform.SetParent(panel.transform, false);

            RectTransform restartRect = restartGO.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(1, 1);
            restartRect.anchorMax = new Vector2(1, 1);
            restartRect.pivot = new Vector2(1, 1);
            restartRect.anchoredPosition = new Vector2(-16, -8);
            restartRect.sizeDelta = new Vector2(110, 40);

            Image restartBg = restartGO.GetComponent<Image>();
            restartBg.color = new Color(0.2f, 0.2f, 0.26f, 1f);

            GameObject restartLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            restartLabelGO.transform.SetParent(restartGO.transform, false);

            TextMeshProUGUI restartLabel = restartLabelGO.GetComponent<TextMeshProUGUI>();
            restartLabel.text = "Restart";
            restartLabel.font = Font;
            restartLabel.fontSize = 22;
            restartLabel.alignment = TextAlignmentOptions.Center;

            RectTransform restartLabelRect = restartLabelGO.GetComponent<RectTransform>();
            restartLabelRect.anchorMin = Vector2.zero;
            restartLabelRect.anchorMax = Vector2.one;
            restartLabelRect.offsetMin = Vector2.zero;
            restartLabelRect.offsetMax = Vector2.zero;

            restartGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogueManager manager = CurrentManager();
                if (manager != null && manager.CurrentTree != null)
                {
                    manager.StartTree(manager.CurrentTree);
                }
            });

            HidePanel();
        }

        private void ShowPanel()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        private void HidePanel()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void EnsureEventSystem()
        {
            EventSystem existing = FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                EnsureInputModule(existing.gameObject);
                return;
            }

            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.transform.SetParent(transform, false);
            EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();
            EnsureInputModule(eventSystemGO);
        }

        private void EnsureInputModule(GameObject go)
        {
#if ENABLE_INPUT_SYSTEM
            if (go.GetComponent<InputSystemUIInputModule>() == null)
            {
                go.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (go.GetComponent<StandaloneInputModule>() == null)
            {
                go.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }
}
