using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JusticeIsAWeapon.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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

        [Header("Typewriter")]
        [Tooltip("Reveals the text character by character instead of showing it all at once.")]
        public bool enableTypewriter = true;
        [Tooltip("Characters revealed per second while typing.")]
        public float typingSpeed = 55f;

        private readonly List<GameObject> _choiceButtons = new List<GameObject>();
        private readonly List<string> _pages = new List<string>();
        private readonly List<string> _pageSpeakers = new List<string>();
        private GameObject _choiceOverlay;
        private TMP_FontAsset _font;
        private RectTransform _viewportRect;
        private ScrollRect _scrollRect;
        private Image _bodyBackground;
        private int _pageIndex;
        private bool _isConversation;
        private Coroutine _typingRoutine;
        private bool _isTyping;
        private static TMP_FontAsset _sharedFont;

        private TMP_FontAsset Font
        {
            get
            {
                if (_font == null)
                {
                    // 1) A sharp dynamic OS font (rendered at screen resolution).
                    _font = CreateSharpFont();
                    // 2) Fallback: the project's SDF font asset.
                    if (_font == null)
                    {
                        _font = TMP_Settings.defaultFontAsset;
                    }
                    if (_font == null)
                    {
                        _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    }
                }
                return _font;
            }
        }

        /// <summary>
        /// Creates an in-memory dynamic TMP font from an OS font. Dynamic fonts
        /// are rasterized at the exact screen size, so text stays sharp instead
        /// of being scaled up from a small SDF atlas.
        /// </summary>
        private static TMP_FontAsset CreateSharpFont()
        {
            if (_sharedFont != null)
            {
                return _sharedFont;
            }

            try
            {
                UnityEngine.Font osFont = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 96);
                if (osFont == null)
                {
                    return null;
                }
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(osFont);
                asset.name = "DynamicOSFont";
                _sharedFont = asset;
                return asset;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[DialogueUI] OS font creation failed, using SDF font instead: " + e.Message);
                return null;
            }
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf)
            {
                return;
            }
            if (HasAdvanceKey())
            {
                AdvancePage();
            }
        }

        private static bool HasAdvanceKey()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
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
            StopTyping();
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
            StopTyping();
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
                _pages.Clear();
                _pageSpeakers.Clear();
                _isConversation = false;
                string full = manager.CurrentText ?? string.Empty;
                if (full.Length > 0)
                {
                    List<Turn> turns = ParseTurns(full);
                    if (turns.Count > 0)
                    {
                        // Conversation mode: one page per speaker turn, each
                        // with its own name tag. Preamble (if any) becomes a
                        // nameless page, and long turns are paginated normally.
                        _isConversation = true;
                        foreach (Turn turn in turns)
                        {
                            foreach (string page in BuildPages(turn.text))
                            {
                                _pages.Add(page);
                                _pageSpeakers.Add(turn.speaker);
                            }
                        }
                    }
                    else
                    {
                        _pages.AddRange(BuildPages(full));
                    }
                }
                _pageIndex = 0;
                bodyText.text = _pages.Count > 0 ? _pages[0] : string.Empty;
                if (_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = 1f;
                }
                UpdateNameTag();
                SetBubbleMode(_isConversation);
                StartTyping();
            }
        }

        /// <summary>
        /// Splits long text into pages that fit the text area. Choices are only
        /// shown on the last page, so the player clicks / presses Enter to read
        /// the rest before options appear.
        /// </summary>
        private List<string> BuildPages(string full)
        {
            if (_viewportRect == null || bodyText == null)
            {
                return new List<string> { full };
            }

            Canvas.ForceUpdateCanvases();
            float viewportHeight = _viewportRect.rect.height;
            float viewportWidth = _viewportRect.rect.width;
            if (viewportHeight <= 1f || viewportWidth <= 1f)
            {
                return new List<string> { full };
            }

            float maxHeight = viewportHeight;
            var pages = new List<string>();
            var current = new StringBuilder();

            foreach (string token in SplitWords(full))
            {
                string candidate = current.Length == 0 ? token : current + token;
                // Measure with an explicit width so wrapping uses the actual
                // viewport width regardless of the layout state of BodyText.
                if (current.Length > 0 && bodyText.GetPreferredValues(candidate, viewportWidth, 0f).y > maxHeight)
                {
                    pages.Add(current.ToString().Trim());
                    current.Clear();
                }
                current.Append(token);
            }

            if (current.Length > 0)
            {
                pages.Add(current.ToString().Trim());
            }
            if (pages.Count == 0)
            {
                pages.Add(full);
            }
            return pages;
        }

        /// <summary>Tokenizes text keeping spaces/newlines as separate tokens so pages split at word boundaries.</summary>
        private static List<string> SplitWords(string text)
        {
            var words = new List<string>();
            var token = new StringBuilder();
            foreach (char c in text)
            {
                if (c == ' ' || c == '\n')
                {
                    if (token.Length > 0)
                    {
                        words.Add(token.ToString());
                        token.Clear();
                    }
                    words.Add(c.ToString());
                }
                else
                {
                    token.Append(c);
                }
            }
            if (token.Length > 0)
            {
                words.Add(token.ToString());
            }
            return words;
        }

        /// <summary>
        /// Advances to the next page of the current dialogue. While text is
        /// typing out, the first press completes the page instantly instead of
        /// advancing. Returns false when already on the last page with the text
        /// fully shown (choices are showing then — click those instead).
        /// </summary>
        public bool AdvancePage()
        {
            if (_isTyping)
            {
                CompleteTyping();
                return true;
            }
            if (_pages.Count == 0)
            {
                return false;
            }
            if (_pageIndex < _pages.Count - 1)
            {
                _pageIndex++;
                if (bodyText != null)
                {
                    bodyText.text = _pages[_pageIndex];
                }
                if (_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = 1f;
                }
                UpdateNameTag();
                StartTyping();
                RefreshChoices();
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Conversation turns
        // ------------------------------------------------------------------

        private struct Turn
        {
            public string speaker;
            public string text;
        }

        /// <summary>
        /// Splits rich text into speaker turns. A turn starts at a bold label
        /// ending in ':' (e.g. <b>Detective Miller:</b>) and runs until the
        /// next label. Bold headers without a colon (''Visuals'', ''Narrative'')
        /// are not turns, so pure narration stays in the normal display mode.
        /// </summary>
        private static List<Turn> ParseTurns(string text)
        {
            var turns = new List<Turn>();
            var labels = new List<Match>();
            foreach (Match m in Regex.Matches(text, "<b>(?<name>[^<]*?)</b>"))
            {
                if (m.Groups["name"].Value.TrimEnd().EndsWith(":"))
                {
                    labels.Add(m);
                }
            }
            if (labels.Count == 0)
            {
                return turns;
            }

            // Preamble before the first label (e.g. "You select the 'Alibi' baseline.")
            string preamble = text.Substring(0, labels[0].Index).Trim();
            if (preamble.Length > 0)
            {
                turns.Add(new Turn { speaker = string.Empty, text = preamble });
            }

            for (int i = 0; i < labels.Count; i++)
            {
                Match label = labels[i];
                int start = label.Index + label.Length;
                int end = i + 1 < labels.Count ? labels[i + 1].Index : text.Length;
                string line = text.Substring(start, end - start).Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                turns.Add(new Turn
                {
                    speaker = label.Groups["name"].Value.TrimEnd(':').Trim(),
                    text = line
                });
            }
            return turns;
        }

        /// <summary>Shows the current page's speaker name (conversation mode only).</summary>
        private void UpdateNameTag()
        {
            if (speakerText == null || !_isConversation || _pageSpeakers == null
                || _pageSpeakers.Count == 0 || _pageIndex >= _pageSpeakers.Count)
            {
                return;
            }
            string name = _pageSpeakers[_pageIndex];
            if (string.IsNullOrEmpty(name))
            {
                if (speakerText.gameObject.activeSelf)
                {
                    speakerText.gameObject.SetActive(false);
                }
                return;
            }
            speakerText.gameObject.SetActive(true);
            speakerText.text = name;
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = SpeakerColor(name);
        }

        private static Color SpeakerColor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new Color(0.92f, 0.92f, 0.94f, 1f);
            }
            if (name.IndexOf("Miller", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(0.95f, 0.78f, 0.4f, 1f);
            }
            if (name.IndexOf("Note", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new Color(0.68f, 0.68f, 0.75f, 1f);
            }
            return new Color(0.88f, 0.92f, 0.97f, 1f);
        }

        /// <summary>Turns the text area into a visible speech bubble during conversations.</summary>
        private void SetBubbleMode(bool on)
        {
            if (_bodyBackground == null)
            {
                return;
            }
            _bodyBackground.color = on
                ? new Color(0.13f, 0.14f, 0.2f, 0.85f)
                : new Color(0f, 0f, 0f, 0f);
        }

        // ------------------------------------------------------------------
        // Typewriter
        // ------------------------------------------------------------------

        /// <summary>
        /// Starts revealing the current page character by character. TMP's
        /// maxVisibleCharacters is used so rich-text tags are never shown as
        /// characters themselves.
        /// </summary>
        private void StartTyping()
        {
            StopTyping();
            if (!enableTypewriter || bodyText == null)
            {
                RefreshChoices();
                return;
            }

            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            if (total <= 0)
            {
                RefreshChoices();
                return;
            }

            _isTyping = true;
            bodyText.maxVisibleCharacters = 0;
            _typingRoutine = StartCoroutine(TypeText(total));
        }

        private void StopTyping()
        {
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
                _typingRoutine = null;
            }
            _isTyping = false;
        }

        /// <summary>Reveals the whole current page at once (skip).</summary>
        private void CompleteTyping()
        {
            if (bodyText != null)
            {
                bodyText.maxVisibleCharacters = int.MaxValue;
            }
            StopTyping();
            RefreshChoices();
        }

        private IEnumerator TypeText(int total)
        {
            float revealed = 0f;
            while (bodyText.maxVisibleCharacters < total)
            {
                if (typingSpeed <= 0f)
                {
                    break;
                }
                revealed += typingSpeed * Time.deltaTime;
                int target = Mathf.Clamp(Mathf.FloorToInt(revealed), 0, total);
                if (target > bodyText.maxVisibleCharacters)
                {
                    bodyText.maxVisibleCharacters = target;
                }
                yield return null;
            }
            _typingRoutine = null;
            CompleteTyping();
        }

        private void RefreshChoices()
        {
            if (choiceList == null)
            {
                return;
            }

            // Choices live on a centered overlay and only appear once the
            // player has read the final page of the current text.
            bool onLastPage = _pages.Count == 0 || _pageIndex >= _pages.Count - 1;
            bool typingFinished = !_isTyping;

            DialogueManager manager = CurrentManager();
            bool show = manager != null && manager.IsActive && onLastPage && typingFinished;

            foreach (GameObject button in _choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _choiceButtons.Clear();

            if (_choiceOverlay != null)
            {
                _choiceOverlay.SetActive(show);
            }

            if (!show)
            {
                if (manager == null || !manager.IsActive)
                {
                    HidePanel();
                }
                return;
            }

            IReadOnlyList<DialogueManager.DialogueChoice> choices = manager.AvailableChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                CreateChoiceButton(index, choices[i].label);
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
            labelText.fontSize = 28;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.margin = new Vector4(12, 0, 12, 0);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.raycastTarget = false;

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
            panelRect.anchorMax = new Vector2(1, 0.4f);
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
            speakerText.fontSize = 34;
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.color = new Color(0.95f, 0.87f, 0.62f, 1f);
            speakerText.raycastTarget = false;
            speakerGO.GetComponent<LayoutElement>().preferredHeight = 34;

            // Scrollable body -------------------------------------------------
            // The transparent Image doubles as a click-catcher: a click on the
            // text area advances to the next page. Scrolling (wheel/drag) still
            // works because ScrollRect keeps its drag handlers on this object.
            GameObject scrollGO = new GameObject("ScrollArea",
                typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(LayoutElement));
            scrollGO.transform.SetParent(columnGO.transform, false);
            scrollGO.GetComponent<LayoutElement>().flexibleHeight = 1f;
            scrollGO.GetComponent<LayoutElement>().flexibleWidth = 1f;
            _bodyBackground = scrollGO.GetComponent<Image>();
            _bodyBackground.color = new Color(0f, 0f, 0f, 0f);

            EventTrigger clickTrigger = scrollGO.AddComponent<EventTrigger>();
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ => AdvancePage());
            clickTrigger.triggers.Add(clickEntry);

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            _viewportRect = viewportRect;

            GameObject contentGO = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);

            RectTransform contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 0);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

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
            bodyText.fontSize = 30;
            bodyText.color = new Color(0.92f, 0.92f, 0.94f, 1f);
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.raycastTarget = false;

            ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentGO.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.scrollSensitivity = 24;
            scrollRect.verticalNormalizedPosition = 1f;
            _scrollRect = scrollRect;

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

            // Choice overlay (centered on screen, separate from the text box) --
            // No Graphic on the overlay itself, so clicks elsewhere pass through
            // to the panel's advance area. Only the buttons have raycast targets.
            GameObject overlayGO = new GameObject("ChoiceOverlay", typeof(RectTransform));
            overlayGO.transform.SetParent(canvasGO.transform, false);
            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _choiceOverlay = overlayGO;

            GameObject choicesGO = new GameObject("Choices",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            choicesGO.transform.SetParent(overlayGO.transform, false);

            RectTransform choicesRect = choicesGO.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0.5f, 0.5f);
            choicesRect.anchorMax = new Vector2(0.5f, 0.5f);
            choicesRect.pivot = new Vector2(0.5f, 0.5f);
            choicesRect.anchoredPosition = new Vector2(0f, 30f);
            choicesRect.sizeDelta = new Vector2(800f, 0f);

            choiceList = choicesGO.GetComponent<VerticalLayoutGroup>();
            choiceList.spacing = 6;
            choiceList.childAlignment = TextAnchor.MiddleCenter;
            choiceList.childControlWidth = true;
            choiceList.childControlHeight = true;

            ContentSizeFitter choicesFitter = choicesGO.GetComponent<ContentSizeFitter>();
            choicesFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            overlayGO.SetActive(false);

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
