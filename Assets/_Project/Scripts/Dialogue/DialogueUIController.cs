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
        private int _pageIndex;
        private bool _isConversation;
        private Coroutine _typingRoutine;
        private bool _isTyping;
        private static TMP_FontAsset _sharedFont;

        // Conversation layout (runtime-built, shown only during speaker turns)
        private GameObject _canvasGO;
        private GameObject _conversationRoot;
        private TextMeshProUGUI _conversationNameText;
        private TextMeshProUGUI _conversationBodyText;
        private RectTransform _conversationBubbleRect;
        private Image _conversationBubble;
        private Image _spriteImage;
        private TextMeshProUGUI _spriteLabel;
        private VerticalLayoutGroup _conversationChoiceList;
        private Transform _clueTileParent;
        private TextMeshProUGUI _alibiContent;
        private TextMeshProUGUI _timelineContent;
        private TextMeshProUGUI _relationshipContent;
        private readonly List<ClueDataSO> _collectedClues = new List<ClueDataSO>();

        /// <summary>The text widget currently on screen (narration body or conversation body).</summary>
        private TextMeshProUGUI ActiveBody
        {
            get
            {
                if (_isConversation && _conversationBodyText != null)
                {
                    return _conversationBodyText;
                }
                return bodyText;
            }
        }

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
            bool panelActive = panel != null && panel.activeSelf;
            bool conversationActive = _conversationRoot != null && _conversationRoot.activeSelf;
            if (!panelActive && !conversationActive)
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
            RefreshBody();
            ShowPanel();
            RefreshChoices();
            CollectRevealedClue(node);
            FillBandsFromNode(node);
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

            TextMeshProUGUI text = ActiveBody;
            if (text == null)
            {
                return;
            }

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
                        foreach (string page in BuildPages(turn.text, text, _conversationBubbleRect))
                        {
                            _pages.Add(page);
                            _pageSpeakers.Add(turn.speaker);
                        }
                    }
                }
                else
                {
                    _pages.AddRange(BuildPages(full, text, _viewportRect));
                }
            }
            _pageIndex = 0;
            text.text = _pages.Count > 0 ? _pages[0] : string.Empty;
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }
            UpdateNameTag();
            UpdateSprite();
            SetBubbleMode(_isConversation);
            StartTyping();
        }

        /// <summary>
        /// Splits long text into pages that fit the given text area. Choices are
        /// only shown on the last page, so the player clicks / presses Enter to
        /// read the rest before options appear.
        /// </summary>
        private List<string> BuildPages(string full, TextMeshProUGUI text, RectTransform rect)
        {
            if (rect == null || text == null)
            {
                return new List<string> { full };
            }

            Canvas.ForceUpdateCanvases();
            float viewportHeight = rect.rect.height;
            float viewportWidth = rect.rect.width;
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
                // viewport width regardless of the layout state of the text.
                if (current.Length > 0 && text.GetPreferredValues(candidate, viewportWidth, 0f).y > maxHeight)
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
                TextMeshProUGUI text = ActiveBody;
                if (text != null)
                {
                    text.text = _pages[_pageIndex];
                }
                if (_scrollRect != null)
                {
                    _scrollRect.verticalNormalizedPosition = 1f;
                }
                UpdateNameTag();
                UpdateSprite();
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

        /// <summary>Shows the current page's speaker name above the conversation bubble.</summary>
        private void UpdateNameTag()
        {
            if (!_isConversation || _conversationNameText == null || _pageSpeakers == null
                || _pageSpeakers.Count == 0 || _pageIndex >= _pageSpeakers.Count)
            {
                return;
            }
            string name = _pageSpeakers[_pageIndex];
            _conversationNameText.gameObject.SetActive(!string.IsNullOrEmpty(name));
            _conversationNameText.text = name;
            _conversationNameText.fontStyle = FontStyles.Bold;
            _conversationNameText.color = SpeakerColor(name);
        }

        /// <summary>
        /// Puts the current speaker's portrait in the left slot. Falls back to a
        /// placeholder box with the speaker's name when no portrait exists.
        /// </summary>
        private void UpdateSprite()
        {
            if (_spriteImage == null)
            {
                return;
            }
            string name = _isConversation && _pageSpeakers != null && _pageIndex < _pageSpeakers.Count
                ? _pageSpeakers[_pageIndex]
                : string.Empty;

            Sprite sprite = string.IsNullOrEmpty(name) ? null : FindSpeakerSprite(name);
            if (sprite != null)
            {
                _spriteImage.sprite = sprite;
                _spriteImage.color = Color.white;
                if (_spriteLabel != null)
                {
                    _spriteLabel.gameObject.SetActive(false);
                }
                return;
            }

            _spriteImage.sprite = null;
            _spriteImage.color = new Color(0.2f, 0.21f, 0.28f, 1f);
            if (_spriteLabel != null)
            {
                _spriteLabel.gameObject.SetActive(true);
                _spriteLabel.text = string.IsNullOrEmpty(name) ? "?" : name;
            }
        }

        /// <summary>
        /// Looks up a portrait for the speaker. Only knows the interviewee via
        /// the tree's suspect back-reference; everyone else gets the placeholder.
        /// </summary>
        private Sprite FindSpeakerSprite(string speakerName)
        {
            SuspectDataSO suspect = CurrentManager()?.CurrentTree?.suspect;
            if (suspect != null && !string.IsNullOrEmpty(suspect.suspectName)
                && speakerName.StartsWith(suspect.suspectName, StringComparison.OrdinalIgnoreCase))
            {
                return suspect.portrait;
            }
            return null;
        }

        /// <summary>Turns the conversation bubble background on/off.</summary>
        private void SetBubbleMode(bool on)
        {
            if (_conversationBubble != null)
            {
                _conversationBubble.color = on
                    ? new Color(0.16f, 0.17f, 0.24f, 0.92f)
                    : new Color(0f, 0f, 0f, 0f);
            }
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

        // ------------------------------------------------------------------
        // Clues + right panel bands
        // ------------------------------------------------------------------

        /// <summary>Adds a clue tile to the clue bar (deduplicated). Public so scenes can seed collected clues.</summary>
        public void AddClue(ClueDataSO clue)
        {
            if (clue == null || _collectedClues.Contains(clue))
            {
                return;
            }
            _collectedClues.Add(clue);
            if (_clueTileParent != null)
            {
                CreateClueTile(clue);
            }
        }

        private void CreateClueTile(ClueDataSO clue)
        {
            GameObject tileGO = new GameObject("ClueTile", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            tileGO.transform.SetParent(_clueTileParent, false);
            tileGO.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.26f, 1f);

            LayoutElement layout = tileGO.GetComponent<LayoutElement>();
            layout.preferredWidth = 130;
            layout.preferredHeight = 90;

            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(tileGO.transform, false);

            TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
            label.font = Font;
            label.fontSize = 19;
            label.color = new Color(0.95f, 0.95f, 0.97f, 1f);
            label.alignment = TextAlignmentOptions.MiddleCenter;
            label.textWrappingMode = TextWrappingModes.Wrap;
            label.text = clue.clueTitle ?? clue.name;
            label.raycastTarget = false;

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(6, 6);
            labelRect.offsetMax = new Vector2(-6, -6);
        }

        private void CollectRevealedClue(DialogueNodeSO node)
        {
            if (node == null || node.clueRevealed == null)
            {
                return;
            }
            AddClue(node.clueRevealed);
        }

        /// <summary>
        /// Fills the Alibi / Timeline / Relationship bands from the conversation
        /// content whenever the player navigates to one of those topics. The
        /// passage naming convention ("Elena - Alibi", ...) drives detection.
        /// </summary>
        private void FillBandsFromNode(DialogueNodeSO node)
        {
            if (node == null)
            {
                return;
            }
            string id = node.nodeId ?? string.Empty;
            string full = CurrentManager()?.CurrentText ?? string.Empty;
            if (id.EndsWith(" - Alibi", StringComparison.Ordinal))
            {
                SetBand(_alibiContent, ExtractAnswer(id, full));
            }
            else if (id.EndsWith(" - Timeline", StringComparison.Ordinal))
            {
                SetBand(_timelineContent, ExtractAnswer(id, full));
            }
            else if (id.EndsWith(" - Relationship", StringComparison.Ordinal))
            {
                SetBand(_relationshipContent, ExtractAnswer(id, full));
            }
        }

        private static void SetBand(TextMeshProUGUI content, string text)
        {
            if (content == null || string.IsNullOrEmpty(text))
            {
                return;
            }
            content.text = text;
        }

        /// <summary>
        /// Pulls the suspect's answer out of a topic node: the turn whose
        /// speaker starts with the passage's suspect prefix ("Elena - Alibi"
        /// → "Elena Vance"'s line). Falls back to the last non-Miller line.
        /// </summary>
        private static string ExtractAnswer(string nodeId, string fullText)
        {
            List<Turn> turns = ParseTurns(fullText);
            if (turns.Count == 0)
            {
                return string.Empty;
            }

            string suspectPrefix = string.Empty;
            int cut = nodeId.IndexOf(" - ", StringComparison.Ordinal);
            if (cut > 0)
            {
                suspectPrefix = nodeId.Substring(0, cut);
            }

            if (suspectPrefix.Length > 0)
            {
                foreach (Turn turn in turns)
                {
                    if (turn.speaker.StartsWith(suspectPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return StripQuotes(turn.text);
                    }
                }
            }

            for (int i = turns.Count - 1; i >= 0; i--)
            {
                Turn turn = turns[i];
                if (!string.IsNullOrEmpty(turn.speaker) && !IsMetaSpeaker(turn.speaker))
                {
                    return StripQuotes(turn.text);
                }
            }
            return string.Empty;
        }

        private static bool IsMetaSpeaker(string speaker)
        {
            return speaker.IndexOf("Miller", StringComparison.OrdinalIgnoreCase) >= 0
                || speaker.IndexOf("Note", StringComparison.OrdinalIgnoreCase) >= 0
                || speaker.StartsWith("[", StringComparison.Ordinal);
        }

        private static string StripQuotes(string text)
        {
            string trimmed = text.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
            {
                return trimmed.Substring(1, trimmed.Length - 2).Trim();
            }
            return trimmed;
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
            TextMeshProUGUI text = ActiveBody;
            if (!enableTypewriter || text == null)
            {
                RefreshChoices();
                return;
            }

            text.ForceMeshUpdate();
            int total = text.textInfo.characterCount;
            if (total <= 0)
            {
                RefreshChoices();
                return;
            }

            _isTyping = true;
            text.maxVisibleCharacters = 0;
            _typingRoutine = StartCoroutine(TypeText(total, text));
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
            TextMeshProUGUI text = ActiveBody;
            if (text != null)
            {
                text.maxVisibleCharacters = int.MaxValue;
            }
            StopTyping();
            RefreshChoices();
        }

        private IEnumerator TypeText(int total, TextMeshProUGUI text)
        {
            float revealed = 0f;
            while (text.maxVisibleCharacters < total)
            {
                if (typingSpeed <= 0f)
                {
                    break;
                }
                revealed += typingSpeed * Time.deltaTime;
                int target = Mathf.Clamp(Mathf.FloorToInt(revealed), 0, total);
                if (target > text.maxVisibleCharacters)
                {
                    text.maxVisibleCharacters = target;
                }
                yield return null;
            }
            _typingRoutine = null;
            CompleteTyping();
        }

        private void RefreshChoices()
        {
            VerticalLayoutGroup activeList = _isConversation ? _conversationChoiceList : choiceList;
            if (activeList == null)
            {
                return;
            }

            // Choices only appear once the player has read the final page of
            // the current text. In conversation mode they live inside the
            // dialogue column; in narration mode on the centered overlay.
            bool onLastPage = _pages.Count == 0 || _pageIndex >= _pages.Count - 1;
            bool typingFinished = !_isTyping;

            DialogueManager manager = CurrentManager();
            bool show = manager != null && manager.IsActive && onLastPage && typingFinished;

            if (_choiceOverlay != null)
            {
                _choiceOverlay.SetActive(!_isConversation && show);
            }

            foreach (GameObject button in _choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _choiceButtons.Clear();

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
                CreateChoiceButton(index, choices[i].label, activeList);
            }

            if (choices.Count == 0)
            {
                HidePanel();
            }
        }

        private void CreateChoiceButton(int index, string label, VerticalLayoutGroup list)
        {
            GameObject buttonGO = new GameObject("Choice_" + index, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGO.transform.SetParent(list.transform, false);

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
            _canvasGO = canvasGO;

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
            scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

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
                    manager.RestartTree();
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

            BuildConversationLayout();
            HidePanel();
        }

        // ------------------------------------------------------------------
        // Runtime conversation layout (shown during speaker turns)
        // ------------------------------------------------------------------

        /// <summary>
        /// Builds the full-screen conversation layout: speaker sprite (left),
        /// name tag + bubble + choices (middle), Alibi/Timeline/Relationship
        /// bands (right), and the clue bar (bottom quarter).
        /// </summary>
        private void BuildConversationLayout()
        {
            if (_conversationRoot != null || _canvasGO == null)
            {
                return;
            }

            _conversationRoot = new GameObject("ConversationLayout", typeof(RectTransform));
            _conversationRoot.transform.SetParent(_canvasGO.transform, false);
            RectTransform rootRect = _conversationRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Top 3/4: three columns --------------------------------------
            GameObject topGO = new GameObject("TopArea", typeof(RectTransform));
            topGO.transform.SetParent(_conversationRoot.transform, false);
            RectTransform topRect = topGO.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 0.25f);
            topRect.anchorMax = Vector2.one;
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;

            // Left: speaker sprite -----------------------------------------
            GameObject spriteGO = new GameObject("SpeakerSprite", typeof(RectTransform), typeof(Image));
            spriteGO.transform.SetParent(topGO.transform, false);
            _spriteImage = spriteGO.GetComponent<Image>();

            RectTransform spriteRect = spriteGO.GetComponent<RectTransform>();
            spriteRect.anchorMin = new Vector2(0f, 0f);
            spriteRect.anchorMax = new Vector2(0.22f, 1f);
            spriteRect.offsetMin = new Vector2(16, 16);
            spriteRect.offsetMax = new Vector2(-8, -8);

            GameObject spriteLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            spriteLabelGO.transform.SetParent(spriteGO.transform, false);
            _spriteLabel = spriteLabelGO.GetComponent<TextMeshProUGUI>();
            _spriteLabel.font = Font;
            _spriteLabel.fontSize = 24;
            _spriteLabel.alignment = TextAlignmentOptions.Center;
            _spriteLabel.color = new Color(0.75f, 0.78f, 0.85f, 1f);
            _spriteLabel.textWrappingMode = TextWrappingModes.Wrap;
            _spriteLabel.raycastTarget = false;

            RectTransform spriteLabelRect = spriteLabelGO.GetComponent<RectTransform>();
            spriteLabelRect.anchorMin = Vector2.zero;
            spriteLabelRect.anchorMax = Vector2.one;
            spriteLabelRect.offsetMin = new Vector2(8, 8);
            spriteLabelRect.offsetMax = new Vector2(-8, -8);

            // Middle: name tag + bubble + choices --------------------------
            GameObject middleGO = new GameObject("MiddleColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            middleGO.transform.SetParent(topGO.transform, false);

            RectTransform middleRect = middleGO.GetComponent<RectTransform>();
            middleRect.anchorMin = new Vector2(0.22f, 0f);
            middleRect.anchorMax = new Vector2(0.68f, 1f);
            middleRect.offsetMin = Vector2.zero;
            middleRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup middleLayout = middleGO.GetComponent<VerticalLayoutGroup>();
            middleLayout.padding = new RectOffset(12, 12, 8, 8);
            middleLayout.spacing = 8;
            middleLayout.childAlignment = TextAnchor.UpperCenter;
            middleLayout.childControlWidth = true;
            middleLayout.childControlHeight = true;
            middleLayout.childForceExpandWidth = true;
            middleLayout.childForceExpandHeight = false;

            GameObject nameGO = new GameObject("TurnName", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameGO.transform.SetParent(middleGO.transform, false);
            _conversationNameText = nameGO.GetComponent<TextMeshProUGUI>();
            _conversationNameText.font = Font;
            _conversationNameText.fontSize = 32;
            _conversationNameText.fontStyle = FontStyles.Bold;
            _conversationNameText.alignment = TextAlignmentOptions.Center;
            _conversationNameText.raycastTarget = false;
            nameGO.GetComponent<LayoutElement>().preferredHeight = 36;

            // Bubble doubles as the click-to-advance area (like the narration panel).
            GameObject bubbleGO = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(LayoutElement));
            bubbleGO.transform.SetParent(middleGO.transform, false);
            _conversationBubble = bubbleGO.GetComponent<Image>();
            _conversationBubbleRect = bubbleGO.GetComponent<RectTransform>();
            bubbleGO.GetComponent<LayoutElement>().flexibleHeight = 1f;
            bubbleGO.GetComponent<LayoutElement>().flexibleWidth = 1f;

            EventTrigger clickTrigger = bubbleGO.AddComponent<EventTrigger>();
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ => AdvancePage());
            clickTrigger.triggers.Add(clickEntry);

            GameObject bodyGO = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGO.transform.SetParent(bubbleGO.transform, false);
            _conversationBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
            _conversationBodyText.font = Font;
            _conversationBodyText.fontSize = 30;
            _conversationBodyText.color = new Color(0.92f, 0.92f, 0.94f, 1f);
            _conversationBodyText.alignment = TextAlignmentOptions.TopLeft;
            _conversationBodyText.raycastTarget = false;

            RectTransform bodyRect = bodyGO.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(14, 12);
            bodyRect.offsetMax = new Vector2(-14, -12);

            // Choices live inside the middle column during conversations.
            GameObject choicesGO = new GameObject("ConversationChoices", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            choicesGO.transform.SetParent(middleGO.transform, false);
            _conversationChoiceList = choicesGO.GetComponent<VerticalLayoutGroup>();
            _conversationChoiceList.spacing = 6;
            _conversationChoiceList.childAlignment = TextAnchor.MiddleCenter;
            _conversationChoiceList.childControlWidth = true;
            _conversationChoiceList.childControlHeight = true;
            choicesGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Right: Alibi / Timeline / Relationship bands -------------------
            GameObject rightGO = new GameObject("RightPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rightGO.transform.SetParent(topGO.transform, false);

            RectTransform rightRect = rightGO.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.68f, 0f);
            rightRect.anchorMax = Vector2.one;
            rightRect.offsetMin = new Vector2(8, 16);
            rightRect.offsetMax = new Vector2(-16, -16);

            VerticalLayoutGroup rightLayout = rightGO.GetComponent<VerticalLayoutGroup>();
            rightLayout.padding = new RectOffset(0, 0, 0, 0);
            rightLayout.spacing = 10;
            rightLayout.childAlignment = TextAnchor.UpperLeft;
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = true;

            _alibiContent = BuildInfoBand(rightGO.transform, "Alibi");
            _timelineContent = BuildInfoBand(rightGO.transform, "Timeline");
            _relationshipContent = BuildInfoBand(rightGO.transform, "Relationship");

            // Restart button (handy for iterating in the test scene)
            GameObject restartGO = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            restartGO.transform.SetParent(_conversationRoot.transform, false);

            RectTransform restartRect = restartGO.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(1, 1);
            restartRect.anchorMax = new Vector2(1, 1);
            restartRect.pivot = new Vector2(1, 1);
            restartRect.anchoredPosition = new Vector2(-16, -12);
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
            restartLabel.raycastTarget = false;

            RectTransform restartLabelRect = restartLabelGO.GetComponent<RectTransform>();
            restartLabelRect.anchorMin = Vector2.zero;
            restartLabelRect.anchorMax = Vector2.one;
            restartLabelRect.offsetMin = Vector2.zero;
            restartLabelRect.offsetMax = Vector2.zero;

            restartGO.GetComponent<Button>().onClick.AddListener(() => CurrentManager()?.RestartTree());

            // Clue bar (bottom 1/4) ----------------------------------------
            GameObject clueBarGO = new GameObject("ClueBar", typeof(RectTransform), typeof(Image));
            clueBarGO.transform.SetParent(_conversationRoot.transform, false);
            clueBarGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            RectTransform clueBarRect = clueBarGO.GetComponent<RectTransform>();
            clueBarRect.anchorMin = Vector2.zero;
            clueBarRect.anchorMax = new Vector2(1f, 0.25f);
            clueBarRect.offsetMin = Vector2.zero;
            clueBarRect.offsetMax = Vector2.zero;

            GameObject clueRowGO = new GameObject("ClueRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            clueRowGO.transform.SetParent(clueBarGO.transform, false);

            RectTransform clueRowRect = clueRowGO.GetComponent<RectTransform>();
            clueRowRect.anchorMin = new Vector2(0f, 0f);
            clueRowRect.anchorMax = new Vector2(1f, 1f);
            clueRowRect.offsetMin = new Vector2(16, 12);
            clueRowRect.offsetMax = new Vector2(-16, -12);

            HorizontalLayoutGroup clueRow = clueRowGO.GetComponent<HorizontalLayoutGroup>();
            clueRow.padding = new RectOffset(0, 0, 0, 0);
            clueRow.spacing = 10;
            clueRow.childAlignment = TextAnchor.UpperLeft;
            clueRow.childControlWidth = true;
            clueRow.childControlHeight = true;
            clueRow.childForceExpandWidth = false;
            clueRow.childForceExpandHeight = false;

            GameObject cluesLabelGO = new GameObject("CluesLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            cluesLabelGO.transform.SetParent(clueRowGO.transform, false);

            TextMeshProUGUI cluesLabel = cluesLabelGO.GetComponent<TextMeshProUGUI>();
            cluesLabel.font = Font;
            cluesLabel.fontSize = 26;
            cluesLabel.fontStyle = FontStyles.Bold;
            cluesLabel.color = new Color(0.85f, 0.75f, 0.5f, 1f);
            cluesLabel.text = "CLUES";
            cluesLabel.alignment = TextAlignmentOptions.MiddleLeft;
            cluesLabel.raycastTarget = false;
            cluesLabelGO.GetComponent<LayoutElement>().preferredWidth = 110;
            cluesLabelGO.GetComponent<LayoutElement>().preferredHeight = 90;

            _clueTileParent = clueRowGO.transform;

            _conversationRoot.SetActive(false);
        }

        /// <summary>Builds one titled info band (Alibi / Timeline / Relationship) with a placeholder message.</summary>
        private TextMeshProUGUI BuildInfoBand(Transform parent, string title)
        {
            GameObject bandGO = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            bandGO.transform.SetParent(parent, false);
            bandGO.GetComponent<LayoutElement>().flexibleHeight = 1f;
            bandGO.GetComponent<LayoutElement>().flexibleWidth = 1f;
            bandGO.GetComponent<Image>().color = new Color(0.1f, 0.11f, 0.16f, 0.9f);

            VerticalLayoutGroup bandLayout = bandGO.GetComponent<VerticalLayoutGroup>();
            bandLayout.padding = new RectOffset(10, 10, 8, 8);
            bandLayout.spacing = 4;
            bandLayout.childAlignment = TextAnchor.UpperLeft;
            bandLayout.childControlWidth = true;
            bandLayout.childControlHeight = true;
            bandLayout.childForceExpandWidth = true;
            bandLayout.childForceExpandHeight = false;

            GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleGO.transform.SetParent(bandGO.transform, false);

            TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.font = Font;
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.85f, 0.75f, 0.5f, 1f);
            titleText.text = title;
            titleText.alignment = TextAlignmentOptions.UpperLeft;
            titleText.raycastTarget = false;
            titleGO.GetComponent<LayoutElement>().preferredHeight = 30;

            GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
            contentGO.transform.SetParent(bandGO.transform, false);

            TextMeshProUGUI content = contentGO.GetComponent<TextMeshProUGUI>();
            content.font = Font;
            content.fontSize = 20;
            content.color = new Color(0.85f, 0.87f, 0.9f, 1f);
            content.text = "Not recorded yet";
            content.alignment = TextAlignmentOptions.UpperLeft;
            content.textWrappingMode = TextWrappingModes.Wrap;
            content.raycastTarget = false;

            return content;
        }

        private void ShowPanel()
        {
            if (_conversationRoot != null)
            {
                _conversationRoot.SetActive(_isConversation);
            }
            if (panel != null)
            {
                panel.SetActive(!_isConversation);
            }
        }

        private void HidePanel()
        {
            if (_conversationRoot != null)
            {
                _conversationRoot.SetActive(false);
            }
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
