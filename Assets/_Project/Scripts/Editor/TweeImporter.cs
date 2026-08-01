using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JusticeIsAWeapon.Data;
using JusticeIsAWeapon.Dialogue;
using UnityEditor;
using UnityEngine;

namespace JusticeIsAWeapon.Editor
{
    /// <summary>
    /// Converts the Twee source file (Assets/Resources/The Midnight Gallery.txt)
    /// into DialogueNodeSO / DialogueTreeSO assets under
    /// Assets/_Project/Data/ImportedDialogue.
    ///
    /// Supported syntax (the subset used by this file):
    ///   :: PassageName {"position":...} [tags]
    ///   [[label|Passage]], [[label->Passage]], [[Passage]]
    ///   (if: COND)[...] (else-if: COND)[...] (else:)[...]  (nested, chained)
    ///   (text-style: "underline")[...]
    ///   ''bold''  //italic//  ~~strike~~
    ///   (set: ...) / (for: each ...) / <!-- comments --> are dropped
    ///   (they only drive story variables, which DialogueManager recomputes).
    /// </summary>
    public static class TweeImporter
    {
        private const string SourceFile = "The Midnight Gallery";
        private const string RootFolder = "Assets/_Project/Data/ImportedDialogue";
        private const string NodeFolder = RootFolder + "/Nodes";
        private const string TreeAssetPath = RootFolder + "/The Midnight Gallery.asset";
        private const string RootPassageName = "The Cinematic Opening";

        private class Passage
        {
            public string name;
            public string body;
        }

        [MenuItem("Tools/JusticeIsAWeapon/1. Import The Midnight Gallery Dialogue")]
        public static void ImportFromMenu()
        {
            RunImport();
        }

        /// <summary>Batch entry point: Unity -executeMethod JusticeIsAWeapon.Editor.TweeImporter.RunImport</summary>
        public static void RunImport()
        {
            TextAsset source = Resources.Load<TextAsset>(SourceFile);
            if (source == null)
            {
                Debug.LogError($"[TweeImporter] Could not load Resources/{SourceFile}.txt");
                return;
            }

            List<Passage> passages = ParsePassages(source.text);
            Debug.Log($"[TweeImporter] Parsed {passages.Count} passages from {source.name}");

            EnsureFolder(RootFolder);
            EnsureFolder(NodeFolder);

            var nodesByName = new Dictionary<string, DialogueNodeSO>();
            foreach (Passage passage in passages)
            {
                string assetPath = $"{NodeFolder}/{SanitizeFileName(passage.name)}.asset";
                DialogueNodeSO node = AssetDatabase.LoadAssetAtPath<DialogueNodeSO>(assetPath);
                bool isNew = node == null;
                if (isNew)
                {
                    node = ScriptableObject.CreateInstance<DialogueNodeSO>();
                }

                node.nodeId = passage.name;
                node.blocks = ParseBlocks(passage.body, null);
                node.lineText = FlattenText(node.blocks);
                node.speakerName = string.Empty;
                node.choices = new List<DialogueChoiceSO>();
                node.isDeadEnd = false;

                if (isNew)
                {
                    AssetDatabase.CreateAsset(node, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(node);
                }

                nodesByName[passage.name] = node;
            }

            int dangling = ResolveLinks(nodesByName);

            foreach (DialogueNodeSO node in nodesByName.Values)
            {
                node.isDeadEnd = node.choices == null || node.choices.Count == 0;
                EditorUtility.SetDirty(node);
            }

            DialogueTreeSO tree = AssetDatabase.LoadAssetAtPath<DialogueTreeSO>(TreeAssetPath);
            bool treeIsNew = tree == null;
            if (treeIsNew)
            {
                tree = ScriptableObject.CreateInstance<DialogueTreeSO>();
            }

            tree.name = "The Midnight Gallery";
            if (!nodesByName.TryGetValue(RootPassageName, out DialogueNodeSO root))
            {
                Debug.LogWarning($"[TweeImporter] Root passage '{RootPassageName}' not found — using first passage as root.");
                foreach (KeyValuePair<string, DialogueNodeSO> pair in nodesByName)
                {
                    root = pair.Value;
                    break;
                }
            }
            tree.root = root;

            if (treeIsNew)
            {
                AssetDatabase.CreateAsset(tree, TreeAssetPath);
            }
            else
            {
                EditorUtility.SetDirty(tree);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TweeImporter] Done. Nodes: {nodesByName.Count} | Dangling links: {dangling} | Tree: {TreeAssetPath}");
        }

        // ------------------------------------------------------------------
        // Twee parsing
        // ------------------------------------------------------------------

        private static List<Passage> ParsePassages(string text)
        {
            var passages = new List<Passage>();
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (!trimmed.StartsWith(":: "))
                {
                    continue;
                }

                string header = trimmed.Substring(3).Trim();
                string name = ParsePassageName(header);
                if (name.Length == 0 || IsMetaPassage(name))
                {
                    continue;
                }

                var body = new List<string>();
                int j = i + 1;
                while (j < lines.Length && !lines[j].TrimStart().StartsWith(":: "))
                {
                    body.Add(lines[j]);
                    j++;
                }

                passages.Add(new Passage { name = name, body = string.Join("\n", body) });
                i = j - 1;
            }

            return passages;
        }

        /// <summary>Strips the optional [tags] and {"position":...} suffixes from a passage header.</summary>
        private static string ParsePassageName(string header)
        {
            int cut = header.Length;
            int tagIndex = header.IndexOf(" [", StringComparison.Ordinal);
            int jsonIndex = header.IndexOf(" {", StringComparison.Ordinal);
            if (tagIndex >= 0)
            {
                cut = Math.Min(cut, tagIndex);
            }
            if (jsonIndex >= 0)
            {
                cut = Math.Min(cut, jsonIndex);
            }
            return header.Substring(0, cut).Trim();
        }

        private static bool IsMetaPassage(string name)
        {
            return name == "StoryTitle" || name == "StoryData" || name == "UserScript" || name == "UserStylesheet";
        }

        // ------------------------------------------------------------------
        // Passage body -> flat render blocks
        // ------------------------------------------------------------------

        /// <summary>
        /// Parses a passage body into flat DialogueBlocks. Conditional nesting is
        /// flattened here: each block carries the fully combined condition of all
        /// enclosing (if:)/(else-if:)/(else:) chains, so the serialized data has
        /// no recursive hierarchy (Unity's serialization depth limit is 10).
        /// </summary>
        private static List<DialogueBlock> ParseBlocks(string body, string context)
        {
            var blocks = new List<DialogueBlock>();
            int i = 0;
            int n = body.Length;
            var textBuffer = new StringBuilder();
            var pendingLinks = new List<LinkData>();

            void Flush()
            {
                if (textBuffer.Length == 0 && pendingLinks.Count == 0)
                {
                    return;
                }
                blocks.Add(new DialogueBlock
                {
                    condition = context,
                    text = ConvertMarkup(textBuffer.ToString()),
                    links = pendingLinks.Count > 0 ? new List<LinkData>(pendingLinks) : null
                });
                textBuffer.Clear();
                pendingLinks.Clear();
            }

            while (i < n)
            {
                // HTML comments
                if (body[i] == '<' && body.IndexOf("<!--", i, Math.Min(4, n - i)) == i)
                {
                    int commentEnd = body.IndexOf("-->", i);
                    i = commentEnd < 0 ? n : commentEnd + 3;
                    continue;
                }

                // Links [[label|target]], [[label->target]], [[target]]
                if (body[i] == '[' && i + 1 < n && body[i + 1] == '[')
                {
                    int close = body.IndexOf("]]", i + 2);
                    if (close < 0)
                    {
                        i++;
                        continue;
                    }

                    string inner = body.Substring(i + 2, close - (i + 2));
                    i = close + 2;

                    string label;
                    string target;
                    int pipe = inner.IndexOf('|');
                    int arrow = inner.IndexOf("->", StringComparison.Ordinal);
                    if (pipe >= 0 && (arrow < 0 || pipe < arrow))
                    {
                        label = inner.Substring(0, pipe);
                        target = inner.Substring(pipe + 1);
                    }
                    else if (arrow >= 0)
                    {
                        label = inner.Substring(0, arrow);
                        target = inner.Substring(arrow + 2);
                    }
                    else
                    {
                        label = inner;
                        target = inner;
                    }

                    pendingLinks.Add(new LinkData
                    {
                        label = label.Trim(),
                        target = target.Trim()
                    });
                    continue;
                }

                // Macros (if / else-if / else / text-style / set / for / ...)
                if (body[i] == '(')
                {
                    int macroEnd = FindMatchingClose(body, i, '(', ')');
                    if (macroEnd < 0)
                    {
                        i++;
                        continue;
                    }

                    string macroInner = body.Substring(i + 1, macroEnd - i - 1);
                    int colon = macroInner.IndexOf(':');
                    if (colon < 0)
                    {
                        i = macroEnd + 1;
                        continue;
                    }

                    string macroName = macroInner.Substring(0, colon).Trim();
                    int bodyStart = macroEnd + 1;
                    if (bodyStart >= n || body[bodyStart] != '[')
                    {
                        // Side-effect macro without a bracketed body (e.g. (set: _x to 1)) — drop it.
                        i = macroEnd + 1;
                        continue;
                    }

                    int bodyEnd = FindMatchingClose(body, bodyStart, '[', ']');
                    if (bodyEnd < 0)
                    {
                        i++;
                        continue;
                    }

                    string innerBody = body.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);
                    int after = bodyEnd + 1;

                    switch (macroName)
                    {
                        case "if":
                        {
                            Flush();

                            var chainConds = new List<string> { macroInner.Substring(colon + 1).Trim() };
                            var chainBodies = new List<string> { innerBody };
                            after = ParseElseChain(body, after, chainConds, chainBodies);

                            for (int branch = 0; branch < chainConds.Count; branch++)
                            {
                                string branchExpr = ComputeBranchCondition(chainConds, branch);
                                string childContext = CombineConditions(context, branchExpr);
                                blocks.AddRange(ParseBlocks(chainBodies[branch], childContext));
                            }
                            i = after;
                            break;
                        }

                        case "else-if":
                        case "else":
                            // Stray else group (should have been consumed by the chain parser) — drop it.
                            i = after;
                            break;

                        case "set":
                        case "for":
                        case "print":
                        case "go-to":
                        case "goto":
                        case "link-goto":
                        case "include":
                        case "append":
                        case "link":
                            // Side-effect macros — the variables they set are recomputed
                            // by DialogueManager from the visited history at runtime.
                            i = after;
                            break;

                        case "text-style":
                        {
                            string styleArg = macroInner.Substring(colon + 1).Trim();
                            if (styleArg.Contains("underline"))
                            {
                                List<DialogueBlock> wrapped = ParseBlocks(innerBody, context);
                                foreach (DialogueBlock wrappedBlock in wrapped)
                                {
                                    if (!string.IsNullOrEmpty(wrappedBlock.text))
                                    {
                                        wrappedBlock.text = "<u>" + wrappedBlock.text + "</u>";
                                    }
                                }
                                blocks.AddRange(wrapped);
                            }
                            i = after;
                            break;
                        }

                        default:
                            // Unknown macro — drop the group.
                            i = after;
                            break;
                    }
                    continue;
                }

                textBuffer.Append(body[i]);
                i++;
            }

            Flush();
            return blocks;
        }

        /// <summary>
        /// Collects the (else-if:)/(else:) groups that immediately follow an (if:)
        /// group into the chain condition/body lists.
        /// </summary>
        private static int ParseElseChain(string body, int after, List<string> conds, List<string> bodies)
        {
            int n = body.Length;
            while (true)
            {
                int save = after;
                int scan = after;
                while (scan < n && char.IsWhiteSpace(body[scan]))
                {
                    scan++;
                }

                if (scan + 1 < n && body[scan] == '(')
                {
                    int macroEnd = FindMatchingClose(body, scan, '(', ')');
                    if (macroEnd < 0)
                    {
                        return save;
                    }

                    string macroInner = body.Substring(scan + 1, macroEnd - scan - 1);
                    int colon = macroInner.IndexOf(':');
                    string macroName = colon < 0 ? macroInner : macroInner.Substring(0, colon).Trim();
                    if (macroName == "else-if" || macroName == "else")
                    {
                        int bodyStart = macroEnd + 1;
                        if (bodyStart < n && body[bodyStart] == '[')
                        {
                            int bodyEnd = FindMatchingClose(body, bodyStart, '[', ']');
                            if (bodyEnd < 0)
                            {
                                return save;
                            }

                            conds.Add(macroName == "else" ? null : macroInner.Substring(colon + 1).Trim());
                            bodies.Add(body.Substring(bodyStart + 1, bodyEnd - bodyStart - 1));
                            after = bodyEnd + 1;
                            continue;
                        }
                    }
                }

                return save;
            }
        }

        /// <summary>
        /// Builds the condition expression under which chain branch `index` is the
        /// active branch: its own condition AND none of the earlier branches
        /// matched (else-if/else semantics).
        /// </summary>
        private static string ComputeBranchCondition(List<string> conds, int index)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(conds[index]))
            {
                parts.Add(conds[index]);
            }
            for (int j = 0; j < index; j++)
            {
                if (string.IsNullOrEmpty(conds[j]))
                {
                    continue;
                }
                parts.Add("not (" + conds[j] + ")");
            }
            return parts.Count == 0 ? null : string.Join(" and ", parts);
        }

        private static string CombineConditions(string context, string branchExpr)
        {
            if (string.IsNullOrEmpty(branchExpr))
            {
                return context;
            }
            if (string.IsNullOrEmpty(context))
            {
                return branchExpr;
            }
            return context + " and (" + branchExpr + ")";
        }

        /// <summary>Finds the closing bracket for an opening bracket, counting nesting depth.</summary>
        private static int FindMatchingClose(string s, int openIndex, char open, char close)
        {
            int depth = 0;
            for (int i = openIndex; i < s.Length; i++)
            {
                if (s[i] == open)
                {
                    depth++;
                }
                else if (s[i] == close)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>''bold'' //italic// ~~strike~~ -> TMP rich text.</summary>
        private static string ConvertMarkup(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            string result = Regex.Replace(text, @"''([^'']*)''", "<b>$1</b>");
            result = Regex.Replace(result, @"//([^/]*)//", "<i>$1</i>");
            result = Regex.Replace(result, @"~~([^~]*)~~", "<s>$1</s>");
            return result;
        }

        // ------------------------------------------------------------------
        // Post-parse: link resolution + helpers
        // ------------------------------------------------------------------

        private static int ResolveLinks(Dictionary<string, DialogueNodeSO> nodesByName)
        {
            int dangling = 0;
            foreach (DialogueNodeSO node in nodesByName.Values)
            {
                if (node.choices == null)
                {
                    node.choices = new List<DialogueChoiceSO>();
                }
                node.choices.Clear();

                if (node.blocks == null)
                {
                    continue;
                }

                foreach (DialogueBlock block in node.blocks)
                {
                    if (block.links == null)
                    {
                        continue;
                    }

                    foreach (LinkData link in block.links)
                    {
                        if (!nodesByName.TryGetValue(link.target, out DialogueNodeSO target))
                        {
                            Debug.LogWarning($"[TweeImporter] Dangling link '{link.target}' in passage '{node.nodeId}'");
                            dangling++;
                            continue;
                        }

                        link.node = target;
                        node.choices.Add(new DialogueChoiceSO
                        {
                            choiceLabel = link.label,
                            nextNode = target
                        });
                    }
                }
            }
            return dangling;
        }

        private static string FlattenText(List<DialogueBlock> blocks)
        {
            var builder = new StringBuilder();
            if (blocks != null)
            {
                foreach (DialogueBlock block in blocks)
                {
                    if (!string.IsNullOrEmpty(block.text))
                    {
                        builder.Append(block.text);
                    }
                }
            }
            return builder.ToString();
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return builder.ToString();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string parent = current;
                current = $"{parent}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.CreateFolder(parent, parts[i]);
                }
            }
        }
    }
}
