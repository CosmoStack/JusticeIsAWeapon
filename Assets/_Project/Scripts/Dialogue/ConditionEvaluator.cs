using System;
using System.Text.RegularExpressions;

namespace JusticeIsAWeapon.Dialogue
{
    /// <summary>
    /// Minimal evaluator for the subset of Harlowe conditionals used by the
    /// imported Twee file:
    ///   (history:) contains "Passage Name"   /   (history:) does not contain "X"
    ///   not / and / or / parentheses / story variables (e.g. _keyFirst).
    /// Unknown tokens evaluate to false; unknown variables to false.
    /// </summary>
    public static class ConditionEvaluator
    {
        public static bool Evaluate(string condition, Func<string, bool> historyContains, Func<string, bool> varValue)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            string s = condition.Trim();
            s = s.Replace("(history:)", " H ");
            s = Regex.Replace(s, @"\bdoes not contain\b", " DNC ");
            s = Regex.Replace(s, @"\bcontains\b", " C ");
            s = Regex.Replace(s, @"\band\b", " A ");
            s = Regex.Replace(s, @"\bor\b", " O ");
            s = Regex.Replace(s, @"\bnot\b", " N ");

            MatchCollection tokens = Regex.Matches(s, @"""[^""]*""|\(|\)|[A-Za-z_][A-Za-z0-9_]*");
            int i = 0;
            return ParseOr(tokens, ref i, historyContains, varValue);
        }

        private static bool ParseOr(MatchCollection tokens, ref int i, Func<string, bool> historyContains, Func<string, bool> varValue)
        {
            bool value = ParseAnd(tokens, ref i, historyContains, varValue);
            while (Peek(tokens, i) == "O")
            {
                i++;
                bool right = ParseAnd(tokens, ref i, historyContains, varValue);
                value = value || right;
            }
            return value;
        }

        private static bool ParseAnd(MatchCollection tokens, ref int i, Func<string, bool> historyContains, Func<string, bool> varValue)
        {
            bool value = ParseUnary(tokens, ref i, historyContains, varValue);
            while (Peek(tokens, i) == "A")
            {
                i++;
                bool right = ParseUnary(tokens, ref i, historyContains, varValue);
                value = value && right;
            }
            return value;
        }

        private static bool ParseUnary(MatchCollection tokens, ref int i, Func<string, bool> historyContains, Func<string, bool> varValue)
        {
            if (Peek(tokens, i) == "N")
            {
                i++;
                return !ParseUnary(tokens, ref i, historyContains, varValue);
            }
            return ParsePrimary(tokens, ref i, historyContains, varValue);
        }

        private static bool ParsePrimary(MatchCollection tokens, ref int i, Func<string, bool> historyContains, Func<string, bool> varValue)
        {
            if (i >= tokens.Count)
            {
                return false;
            }

            string token = tokens[i].Value;

            if (token == "(")
            {
                i++;
                bool inner = ParseOr(tokens, ref i, historyContains, varValue);
                if (i < tokens.Count && tokens[i].Value == ")")
                {
                    i++;
                }
                return inner;
            }

            if (token == "H")
            {
                i++;
                if (i >= tokens.Count)
                {
                    return false;
                }
                string op = tokens[i].Value;
                i++;
                if (i >= tokens.Count)
                {
                    return false;
                }
                string target = tokens[i].Value.Trim('"');
                i++;
                bool contains = historyContains(target);
                return op == "C" ? contains : !contains;
            }

            // Story variable (e.g. _keyFirst) — unknown variables are false.
            i++;
            return varValue(token);
        }

        private static string Peek(MatchCollection tokens, int i)
        {
            return i < tokens.Count ? tokens[i].Value : null;
        }
    }
}
