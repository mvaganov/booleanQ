// Boolean Quiz - C# equivalent of the Python original
// Original Python code is under Public Domain
// https://repl.it/@codegiraffe/booleanq

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class BooleanQuiz
{
    // ── Console color helpers ─────────────────────────────────────────────────

    // Write text in a given color, then restore the previous foreground color.
    static void WriteColored(string text, ConsoleColor color, bool newline = false)
    {
        ConsoleColor prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (newline) Console.WriteLine(text);
        else         Console.Write(text);
        Console.ForegroundColor = prev;
    }

    // The Python code cycled through six "foreground color" indices (offsets
    // from the ANSI color base-30): 3=yellow, 5=magenta, 6=cyan, 1=red, 2=green, 4=blue.
    static readonly ConsoleColor[] StepColors =
    {
        ConsoleColor.Yellow,   // ANSI 33
        ConsoleColor.Magenta,  // ANSI 35
        ConsoleColor.Cyan,     // ANSI 36
        ConsoleColor.Red,      // ANSI 31
        ConsoleColor.Green,    // ANSI 32
        ConsoleColor.Blue,     // ANSI 34
    };

    // ── Logic-list helpers ────────────────────────────────────────────────────

    /// <summary>
    /// A "logic node" is either a string token (number, operator, keyword)
    /// or a nested list of logic nodes – mirroring Python's nested-list design.
    /// </summary>
    abstract class Node { }

    class TokenNode : Node
    {
        public string Value;
        public TokenNode(string v) { Value = v; }
        public override string ToString() => Value;
    }

    class ListNode : Node
    {
        public List<Node> Children = new List<Node>();
        public override string ToString() => SimplifyToString(this);
    }

    static ListNode MakeList(params object[] items)
    {
        var ln = new ListNode();
        foreach (var item in items)
        {
            if (item is Node n) ln.Children.Add(n);
            else if (item is bool b) ln.Children.Add(new TokenNode(b ? "True" : "False"));
            else ln.Children.Add(new TokenNode(item.ToString()));
        }
        return ln;
    }

    static int CountSublists(ListNode list)
    {
        int count = 0;
        foreach (var child in list.Children)
            if (child is ListNode) count++;
        return count;
    }

    static string SimplifyToString(Node node)
    {
        if (node is TokenNode t) return t.Value;
        var list = (ListNode)node;
        var sb = new StringBuilder();
        foreach (var child in list.Children)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(SimplifyToString(child));
        }
        return sb.ToString();
    }

    // Flatten one level of sublists (those with no nested sublists themselves).
    // Mirrors Python's Flatten(list, 1).
    static ListNode FlattenSingleTerms(ListNode list)
    {
        var result = new ListNode();
        foreach (var child in list.Children)
        {
            if (child is ListNode sub && CountSublists(sub) == 0)
                result.Children.AddRange(sub.Children);
            else
                result.Children.Add(child);
        }
        return result;
    }

    // ── Expression evaluator ──────────────────────────────────────────────────

    static int _pos;

    static bool ParseOr(List<string> tokens, ref int pos)
    {
        bool left = ParseAnd(tokens, ref pos);
        while (pos < tokens.Count && tokens[pos] == "or")
        {
            pos++;
            bool right = ParseAnd(tokens, ref pos);
            left = left || right;
        }
        return left;
    }

    static bool ParseAnd(List<string> tokens, ref int pos)
    {
        bool left = ParseNot(tokens, ref pos);
        while (pos < tokens.Count && tokens[pos] == "and")
        {
            pos++;
            bool right = ParseNot(tokens, ref pos);
            left = left && right;
        }
        return left;
    }

    static bool ParseNot(List<string> tokens, ref int pos)
    {
        if (pos < tokens.Count && tokens[pos] == "not")
        {
            pos++;
            return !ParseNot(tokens, ref pos);
        }
        return ParseComparison(tokens, ref pos);
    }

    static bool ParseComparison(List<string> tokens, ref int pos)
    {
        if (pos < tokens.Count && tokens[pos] == "(")
        {
            pos++;
            bool val = ParseOr(tokens, ref pos);
            if (pos < tokens.Count && tokens[pos] == ")") pos++;
            return val;
        }
        if (pos < tokens.Count && tokens[pos] == "True")  { pos++; return true;  }
        if (pos < tokens.Count && tokens[pos] == "False") { pos++; return false; }

        int left = int.Parse(tokens[pos++]);
        string op = tokens[pos++];
        int right = int.Parse(tokens[pos++]);
        return op switch
        {
            "==" => left == right,
            "!=" => left != right,
            "<"  => left <  right,
            ">"  => left >  right,
            "<=" => left <= right,
            ">=" => left >= right,
            _ => throw new InvalidOperationException($"Unknown op: {op}")
        };
    }

    static bool EvalNode(Node node)
    {
        _pos = 0;
        var tokens = SimplifyToString(node).Split(' ').ToList();
        return ParseOr(tokens, ref _pos);
    }

    // ── Question generation ───────────────────────────────────────────────────

    static readonly string[] LogicalOperations = { "==", "!=", "<", ">", "<=", ">=" };
    static readonly string[] BooleanOperations  = { "and", "or" };
    static Random _rng = new Random();

    static Node GenerateBooleanQuestion(bool allowNot = false, double chanceOfSimpleTF = 0.05)
    {
        Node result;
        if (chanceOfSimpleTF > 0 && _rng.NextDouble() < chanceOfSimpleTF)
        {
            result = new TokenNode(_rng.Next(2) == 0 ? "True" : "False");
        }
        else
        {
            result = MakeList(
                _rng.Next(0, 10),
                LogicalOperations[_rng.Next(LogicalOperations.Length)],
                _rng.Next(0, 10)
            );
        }

        if (allowNot && _rng.Next(2) == 1)
            result = MakeList(new TokenNode("not"), result);

        return result;
    }

    static ListNode GenerateQuestion(int score)
    {
        Node logic = GenerateBooleanQuestion(score >= 15);

        if (score >= 4)
        {
            Node logic2 = (score < 50)
                ? (Node)new TokenNode(_rng.Next(2) == 0 ? "False" : "True")
                : GenerateBooleanQuestion(score >= 75);

            if (score >= 20 && _rng.Next(2) == 1)
                logic2 = MakeList(new TokenNode("not"), logic2);

            string boolOp = BooleanOperations[_rng.Next(BooleanOperations.Length)];
            ListNode combined = score < 30
                ? MakeList(logic, new TokenNode(boolOp), logic2)
                : MakeList(logic2, new TokenNode(boolOp), logic);
            logic = combined;

            if (score > 100)
            {
                Node logic3 = (score < 150)
                    ? (Node)new TokenNode(_rng.Next(2) == 0 ? "False" : "True")
                    : GenerateBooleanQuestion(score >= 75);

                var wrapped = new ListNode();
                wrapped.Children.Add(new TokenNode("("));
                wrapped.Children.AddRange(((ListNode)logic).Children);
                wrapped.Children.Add(new TokenNode(")"));

                string boolOp2 = BooleanOperations[_rng.Next(BooleanOperations.Length)];
                logic = score < 125
                    ? MakeList(wrapped, new TokenNode(boolOp2), logic3)
                    : MakeList(logic3, new TokenNode(boolOp2), wrapped);
            }
        }

        return FlattenSingleTerms((ListNode)logic);
    }

    // ── Step-by-step work display ─────────────────────────────────────────────

    static readonly Dictionary<string, string> OperationInfo = new()
    {
        ["=="]  = "values are equal?",
        ["<="]  = "less than or equal?",
        [">="]  = "greater than or equal?",
        ["< "]  = "less than?",
        ["> "]  = "greater than?",
        ["!="]  = "values are NOT equal?",
        ["not"] = "not means logical opposite",
        ["and"] = "both are true?",
        ["or"]  = "at least one is true?"
    };

    static string _lastSubsection = "";

    static (Node result, bool changed) CollapseOneLeaf(Node node)
    {
        if (node is TokenNode) return (node, false);

        var list = (ListNode)node;

        if (CountSublists(list) == 0)
        {
            _lastSubsection = SimplifyToString(list);
            bool val = EvalNode(list);
            return (new TokenNode(val ? "True" : "False"), true);
        }

        var newChildren = new List<Node>(list.Children);
        for (int i = 0; i < newChildren.Count; i++)
        {
            if (newChildren[i] is ListNode)
            {
                var (collapsed, changed) = CollapseOneLeaf(newChildren[i]);
                if (changed)
                {
                    newChildren[i] = collapsed;
                    var result = new ListNode();
                    result.Children.AddRange(newChildren);
                    return (result, true);
                }
            }
        }
        return (node, false);
    }

    static void PrintWork(ListNode logic, bool useColor)
    {
        // colorOrder mirrors the Python original's index sequence (1-based ANSI offsets
        // from 30, mapped to StepColors which is 0-indexed).
        int[] colorOrder = { 3, 5, 6, 1, 2, 4 };
        int colorIndex = 0;

        Node current = logic;

        while (true)
        {
            ConsoleColor stepColor = StepColors[colorOrder[colorIndex % colorOrder.Length] - 1];
            colorIndex++;

            string completeText = SimplifyToString(current);
            var (next, changed) = CollapseOneLeaf(current);

            if (!changed)
            {
                // Nothing left to collapse – print the final single value.
                Console.WriteLine("   " + completeText);
                string finalVal = EvalNode(current) ? "True" : "False";
                int center = Math.Max(0, (completeText.Length - finalVal.Length) / 2 + 3);
                if (useColor)
                    WriteColored(new string(' ', center) + finalVal, stepColor, newline: true);
                else
                    Console.WriteLine(new string(' ', center) + finalVal);
                break;
            }

            // Find the extra hint string for the operator in the sub-expression.
            string extraInfo = "";
            foreach (var kv in OperationInfo)
            {
                if (_lastSubsection.Contains(kv.Key))
                {
                    extraInfo = " <-- " + kv.Value;
                    break;
                }
            }

            // Print the full expression, coloring the active sub-expression and hint.
            int idx = completeText.IndexOf(_lastSubsection, StringComparison.Ordinal);
            Console.Write("   ");
            if (useColor && idx >= 0)
            {
                Console.Write(completeText[..idx]);
                WriteColored(_lastSubsection, stepColor);
                Console.Write(completeText[(idx + _lastSubsection.Length)..]);
                if (extraInfo.Length > 0)
                    WriteColored(extraInfo, stepColor);
            }
            else
            {
                Console.Write(completeText + extraInfo);
            }
            Console.WriteLine();

            // Print the result of the sub-expression, centred beneath it.
            bool subResult = EvalNode(FlattenSingleTerms(
                MakeList(_lastSubsection.Split(' ').Select(t => (object)t).ToArray())));
            string subResultStr = subResult ? "True" : "False";
            int subIdx = idx >= 0 ? idx : 0;
            int spaces = Math.Max(0, (int)(subIdx + 3 + (_lastSubsection.Length - subResultStr.Length) / 2.0));
            if (useColor)
                WriteColored(new string(' ', spaces) + subResultStr, stepColor, newline: true);
            else
                Console.WriteLine(new string(' ', spaces) + subResultStr);

            current = next;

            // If the tree is now fully flat, do one final print and exit.
            if (current is not ListNode curList || CountSublists(curList) == 0)
            {
                string finalText = SimplifyToString(current);
                bool finalResult = EvalNode(current);
                string finalStr  = finalResult ? "True" : "False";
                if (useColor)
                {
                    WriteColored("   " + finalText, stepColor, newline: true);
                    int fc = Math.Max(0, (finalText.Length - finalStr.Length) / 2 + 3);
                    WriteColored(new string(' ', fc) + finalStr, stepColor, newline: true);
                }
                else
                {
                    Console.WriteLine("   " + finalText);
                    int fc = Math.Max(0, (finalText.Length - finalStr.Length) / 2 + 3);
                    Console.WriteLine(new string(' ', fc) + finalStr);
                }
                break;
            }
        }
    }

    // ── Validation code helpers ───────────────────────────────────────────────

    static long   _userseed   = 0;
    static string _allGuesses = "";
    static int    _score      = 0;

    static string VCode() => $"{_userseed}!{_allGuesses}!{_score}";

    // ── Main ──────────────────────────────────────────────────────────────────

    static void Main()
    {
        _userseed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _rng = new Random((int)(_userseed & 0x7FFFFFFF));

        Console.WriteLine();
        Console.WriteLine("quiz seed: " + _userseed);

        int  score         = 0;
        int  streak        = 0;
        int  bestStreak    = 0;
        int  totalWrong    = 0;
        int  totalAnswered = 0;
        int  lastScore     = 0;
        bool hacked        = false;
        string userGuess   = "";
        string allGuesses  = "";
        string userInputStream = "";
        int    expectedscore   = 0;

        string sep = "__________________________________________";

        while (userGuess != "q")
        {
            bool askagain = false;
            var logic = GenerateQuestion(score);

            Console.WriteLine("\n\tif " + SimplifyToString(logic) +
                              ":\n\t  print(\"t\")\n\telse:\n\t  print(\"f\")\n");

            userGuess = "?";

            if (userInputStream.Length > 0)
            {
                userGuess = userInputStream[0].ToString();
                userInputStream = userInputStream[1..];
            }

            while (userGuess != "t" && userGuess != "f" && userGuess != "q")
            {
                if (expectedscore != 0)
                {
                    if (expectedscore == score)
                        WriteColored("--- valid ---", ConsoleColor.Green, newline: true);
                    else
                        WriteColored("-- invalid --", ConsoleColor.Red,   newline: true);
                    expectedscore = 0;
                }

                try
                {
                    Console.Write("What is the output? (");
                    WriteColored("t", ConsoleColor.Cyan);
                    Console.Write(" or ");
                    WriteColored("f", ConsoleColor.Cyan);
                    Console.Write(", ");
                    WriteColored("?", ConsoleColor.Cyan);
                    Console.Write(" for code, ");
                    WriteColored("q", ConsoleColor.Cyan);
                    Console.Write(" to quit) ");
                    userGuess = (Console.ReadLine() ?? "q").Trim().ToLower();
                }
                catch
                {
                    userGuess = "q";
                }

                if (userGuess == "?" || userGuess == "q")
                {
                    if (userGuess == "q") allGuesses += "q";
                    Console.WriteLine(sep + "\nValidation code:");
                    Console.WriteLine(VCode());
                }

                if (userGuess.Length > 3)
                {
                    try
                    {
                        int bangIdx = userGuess.IndexOf('!');
                        if (bangIdx < 0) bangIdx = userGuess.IndexOf(' ');
                        if (bangIdx >= 0)
                        {
                            long newseed = (long)double.Parse(userGuess[..bangIdx]);
                            int bang2 = userGuess.IndexOf('!', bangIdx + 1);
                            if (bang2 < 0) bang2 = userGuess.IndexOf(' ', bangIdx + 1);
                            if (bang2 >= 0)
                            {
                                userInputStream = userGuess[(bangIdx + 1)..bang2];
                                int nextscore = int.Parse(userGuess[(bang2 + 1)..].Trim());
                                Console.WriteLine("NEWSEED " + newseed);
                                Console.WriteLine("EXPECTED SCORE " + nextscore);
                                _userseed  = newseed;
                                _rng       = new Random((int)(newseed & 0x7FFFFFFF));
                                allGuesses = _allGuesses = "";
                                score = streak = bestStreak = totalWrong = totalAnswered = 0;
                                expectedscore = nextscore;
                                askagain = true;
                                break;
                            }
                        }
                    }
                    catch { /* ignore malformed input */ }
                }
            }

            if (askagain) continue;
            if (userGuess == "q") break;

            totalAnswered++;
            bool finalResult = EvalNode(logic);
            bool usrRight = (finalResult && userGuess == "t") || (!finalResult && userGuess == "f");

            if (!usrRight) Console.WriteLine("##########################################");
            PrintWork(logic, !usrRight);
            if (!usrRight) Console.WriteLine("##########################################");

            allGuesses  += userGuess;
            _allGuesses  = allGuesses;

            if (usrRight)
            {
                score++;
                streak++;
                if (streak > bestStreak) bestStreak = streak;

                string msg = " YOU WERE RIGHT! ";
                int sm = score;
                while (sm > 1) { msg = ">" + msg + "<"; sm /= 2; }

                WriteColored(msg, ConsoleColor.Cyan);
                if (score % 5 == 0 && userInputStream.Length == 0)
                {
                    Console.Write(" ");
                    WriteColored(VCode(), ConsoleColor.Blue);
                }
                Console.WriteLine();
            }
            else
            {
                totalWrong++;
                score = Math.Max(0, score - 2);
                if (streak > 2)
                    Console.WriteLine($"You've answered {totalWrong} incorrectly so far, " +
                                      $"and {totalAnswered - totalWrong} correctly!");
                streak = 0;
            }

            if (score > lastScore + 1) hacked = true;
            _score = score;

            string scoremsg = score switch
            {
                < 10  => "score",
                < 20  => "Your Score",
                < 30  => "Pretty Good Score",
                < 40  => "Very Nice Score",
                < 50  => "Impressive Score",
                < 60  => "Great Score",
                < 70  => "Outstanding Score",
                < 80  => "Amazing Score",
                < 90  => "Fantastic Score",
                < 100 => "Astonishing Score",
                < 120 => "Achievement Unlocked!",
                < 140 => "Brilliant Score",
                < 160 => "Outrageous Score",
                < 180 => "Incredible Score",
                < 200 => "Unbelievable Score",
                < 225 => "What Score is This?",
                < 250 => "I can't even",
                < 275 => "this is just crazy",
                < 300 => "How are you doing this?",
                < 400 => "Are you a wizard?",
                _     => "You are a wizard... "
            };

            Console.WriteLine("\n\n");
            if (hacked)
                WriteColored("hacked ", ConsoleColor.Red);
            else
                Console.Write(scoremsg);
            Console.Write(": ");
            WriteColored(score.ToString(), ConsoleColor.Green, newline: true);

            if (!usrRight) Console.WriteLine("Try the next one.");

            lastScore = score;
        }

        Console.WriteLine(sep);
        int correct = totalAnswered - totalWrong;
        Console.WriteLine($"Final Score: {score}    ({correct}/{totalAnswered})");
        if (streak > 2)     Console.WriteLine($"You just finished {streak} correct in a row");
        if (bestStreak > 2) Console.WriteLine($"Your best correct-in-a-row was {bestStreak}!\n\n");
        if (expectedscore != 0)
        {
            if (expectedscore != score)
                WriteColored("-- invalid --", ConsoleColor.Red,   newline: true);
            else
                WriteColored("--- valid ---", ConsoleColor.Green, newline: true);
        }
    }
}
