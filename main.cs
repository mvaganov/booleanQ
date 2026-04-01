// dotnet publish main.cs -c Release -r win-x64 --self-contained true
using System.Text;

class BooleanQuiz {
	static readonly ConsoleColor[] StepColors = new ConsoleColor[] {
		ConsoleColor.Yellow,
		ConsoleColor.Magenta,
		ConsoleColor.Cyan,
		ConsoleColor.Red,
		ConsoleColor.Green,
		ConsoleColor.Blue,
	};

	static void WriteColored(string text, ConsoleColor color, bool newline) {
		ConsoleColor prev = Console.ForegroundColor;
		Console.ForegroundColor = color;
		if (newline) {
			Console.WriteLine(text);
		} else {
			Console.Write(text);
		}
		Console.ForegroundColor = prev;
	}

	abstract class Node {
		public abstract override string ToString();
	}

	class TokenNode : Node {
		public string Value;

		public TokenNode(string v) {
			Value = v;
		}

		public override string ToString() {
			return Value;
		}
	}

	class ListNode : Node {
		public List<Node> Children = new List<Node>();

		public override string ToString() {
			return SimplifyToString(this);
		}
	}

	// ── Logic-list helpers ────────────────────────────────────────────────────

	static ListNode MakeList(Node a, Node b, Node c) {
		ListNode ln = new ListNode();
		ln.Children.Add(a);
		ln.Children.Add(b);
		ln.Children.Add(c);
		return ln;
	}

	static ListNode MakeList(Node a, Node b) {
		ListNode ln = new ListNode();
		ln.Children.Add(a);
		ln.Children.Add(b);
		return ln;
	}

	static int CountSublists(ListNode list) {
		int count = 0;
		for (int i = 0; i < list.Children.Count; i++) {
			if (list.Children[i] is ListNode) {
				count++;
			}
		}
		return count;
	}

	static string SimplifyToString(Node node) {
		if (node is TokenNode) {
			return ((TokenNode)node).Value;
		}
		ListNode list = (ListNode)node;
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < list.Children.Count; i++) {
			if (sb.Length > 0) {
				sb.Append(' ');
			}
			sb.Append(SimplifyToString(list.Children[i]));
		}
		return sb.ToString();
	}

	// Flatten one level of sublists (those with no nested sublists themselves).
	static ListNode FlattenSingleTerms(ListNode list) {
		ListNode result = new ListNode();
		for (int i = 0; i < list.Children.Count; i++) {
			Node child = list.Children[i];
			if (child is ListNode && CountSublists((ListNode)child) == 0) {
				ListNode sub = (ListNode)child;
				for (int j = 0; j < sub.Children.Count; j++) {
					result.Children.Add(sub.Children[j]);
				}
			} else {
				result.Children.Add(child);
			}
		}
		return result;
	}

	// position kept class-level field so ParseXxx methods can advance without passing by reference
	static int _pos;

	static bool ParseOr(List<string> tokens) {
		bool left = ParseAnd(tokens);
		while (_pos < tokens.Count && tokens[_pos] == "or") {
			_pos++;
			bool right = ParseAnd(tokens);
			left = left || right;
		}
		return left;
	}

	static bool ParseAnd(List<string> tokens) {
		bool left = ParseNot(tokens);
		while (_pos < tokens.Count && tokens[_pos] == "and") {
			_pos++;
			bool right = ParseNot(tokens);
			left = left && right;
		}
		return left;
	}

	static bool ParseNot(List<string> tokens) {
		if (_pos < tokens.Count && tokens[_pos] == "not") {
			_pos++;
			return !ParseNot(tokens);
		}
		return ParseComparison(tokens);
	}

	static bool ParseComparison(List<string> tokens) {
		if (_pos < tokens.Count && tokens[_pos] == "(") {
			_pos++;
			bool val = ParseOr(tokens);
			if (_pos < tokens.Count && tokens[_pos] == ")") {
				_pos++;
			}
			return val;
		}
		if (_pos < tokens.Count && tokens[_pos] == "True") {
			_pos++;
			return true;
		}
		if (_pos < tokens.Count && tokens[_pos] == "False") {
			_pos++;
			return false;
		}
		int left = int.Parse(tokens[_pos++]);
		string op = tokens[_pos++];
		int right = int.Parse(tokens[_pos++]);
		if (op == "==") { return left == right; }
		if (op == "!=") { return left != right; }
		if (op == "<") { return left < right; }
		if (op == ">") { return left > right; }
		if (op == "<=") { return left <= right; }
		if (op == ">=") { return left >= right; }
		throw new InvalidOperationException("Unknown operator: " + op);
	}

	static bool EvalNode(Node node) {
		_pos = 0;
		string expr = SimplifyToString(node);
		string[] parts = expr.Split(' ');
		List<string> tokens = new List<string>();
		for (int i = 0; i < parts.Length; i++)
		{
			tokens.Add(parts[i]);
		}
		return ParseOr(tokens);
	}

	static readonly string[] LogicalOperations = new string[] { "==", "!=", "<", ">", "<=", ">=" };
	static readonly string[] BooleanOperations = new string[] { "and", "or" };
	static Random _rng = new Random();

	static Node GenerateBooleanQuestion(bool allowNot, double chanceOfSimpleTF) {
		Node result;
		if (chanceOfSimpleTF > 0 && _rng.NextDouble() < chanceOfSimpleTF) {
			string tfValue = (_rng.Next(2) == 0) ? "True" : "False";
			result = new TokenNode(tfValue);
		} else {
			result = MakeList(
					new TokenNode(_rng.Next(0, 10).ToString()),
					new TokenNode(LogicalOperations[_rng.Next(LogicalOperations.Length)]),
					new TokenNode(_rng.Next(0, 10).ToString())
			);
		}
		if (allowNot && _rng.Next(2) == 1) {
			result = MakeList(new TokenNode("not"), result);
		}
		return result;
	}

	static ListNode GenerateQuestion(int score) {
		Node logic = GenerateBooleanQuestion(score >= 15, 0.05);
		if (score >= 4) {
			Node logic2;
			if (score < 50) {
				string tfValue = (_rng.Next(2) == 0) ? "False" : "True";
				logic2 = new TokenNode(tfValue);
			} else {
				logic2 = GenerateBooleanQuestion(score >= 75, 0.05);
			}
			if (score >= 20 && _rng.Next(2) == 1) {
				logic2 = MakeList(new TokenNode("not"), logic2);
			}
			string boolOp = BooleanOperations[_rng.Next(BooleanOperations.Length)];
			ListNode combined;
			if (score < 30) {
				combined = MakeList(logic, new TokenNode(boolOp), logic2);
			} else {
				combined = MakeList(logic2, new TokenNode(boolOp), logic);
			}
			logic = combined;
			if (score > 100) {
				Node logic3;
				if (score < 150) {
					string tfValue = (_rng.Next(2) == 0) ? "False" : "True";
					logic3 = new TokenNode(tfValue);
				} else {
					logic3 = GenerateBooleanQuestion(score >= 75, 0.05);
				}
				ListNode wrapped = new ListNode();
				wrapped.Children.Add(new TokenNode("("));
				ListNode logicAsList = (ListNode)logic;
				for (int i = 0; i < logicAsList.Children.Count; i++) {
					wrapped.Children.Add(logicAsList.Children[i]);
				}
				wrapped.Children.Add(new TokenNode(")"));
				string boolOp2 = BooleanOperations[_rng.Next(BooleanOperations.Length)];
				if (score < 125) {
					logic = MakeList(wrapped, new TokenNode(boolOp2), logic3);
				} else {
					logic = MakeList(logic3, new TokenNode(boolOp2), wrapped);
				}
			}
		}
		return FlattenSingleTerms((ListNode)logic);
	}

	static readonly Dictionary<string, string> OperationInfo;

	static BooleanQuiz() {
		OperationInfo = new Dictionary<string, string>();
		OperationInfo.Add("==", "values are equal?");
		OperationInfo.Add("<=", "less than or equal?");
		OperationInfo.Add(">=", "greater than or equal?");
		OperationInfo.Add("< ", "less than?");
		OperationInfo.Add("> ", "greater than?");
		OperationInfo.Add("!=", "values are NOT equal?");
		OperationInfo.Add("not", "not means logical opposite");
		OperationInfo.Add("and", "both are true?");
		OperationInfo.Add("or", "at least one is true?");
	}

	// most recently collapsed sub-expression so PrintWork can highlight it
	static string _lastSubsection = "";

	class CollapseResult {
		public Node ResultNode;
		public bool Changed;

		public CollapseResult(Node node, bool changed) {
			ResultNode = node;
			Changed = changed;
		}
	}

	static CollapseResult CollapseOneLeaf(Node node) {
		if (node is TokenNode) {
			return new CollapseResult(node, false);
		}
		ListNode list = (ListNode)node;
		if (CountSublists(list) == 0) {
			_lastSubsection = SimplifyToString(list);
			bool val = EvalNode(list);
			string resultToken = val ? "True" : "False";
			return new CollapseResult(new TokenNode(resultToken), true);
		}
		List<Node> newChildren = new List<Node>();
		for (int i = 0; i < list.Children.Count; i++) {
			newChildren.Add(list.Children[i]);
		}
		for (int i = 0; i < newChildren.Count; i++) {
			if (newChildren[i] is ListNode) {
				CollapseResult inner = CollapseOneLeaf(newChildren[i]);
				if (inner.Changed) {
					newChildren[i] = inner.ResultNode;
					ListNode updated = new ListNode();
					for (int j = 0; j < newChildren.Count; j++) {
						updated.Children.Add(newChildren[j]);
					}
					return new CollapseResult(updated, true);
				}
			}
		}
		return new CollapseResult(node, false);
	}

	static void PrintWork(ListNode logic, bool useColor) {
		int[] colorOrder = new int[] { 3, 5, 6, 1, 2, 4 };
		int colorIndex = 0;
		Node current = logic;
		while (true) {
			ConsoleColor stepColor = StepColors[colorOrder[colorIndex % colorOrder.Length] - 1];
			colorIndex++;
			string completeText = SimplifyToString(current);
			CollapseResult collapse = CollapseOneLeaf(current);
			if (!collapse.Changed) {
				Console.WriteLine("   " + completeText);
				string finalVal = EvalNode(current) ? "True" : "False";
				int center = Math.Max(0, (completeText.Length - finalVal.Length) / 2 + 3);
				string finalLine = new string(' ', center) + finalVal;
				if (useColor) {
					WriteColored(finalLine, stepColor, true);
				} else {
					Console.WriteLine(finalLine);
				}
				break;
			}
			string extraInfo = "";
			foreach (KeyValuePair<string, string> kv in OperationInfo) {
				if (_lastSubsection.Contains(kv.Key)) {
					extraInfo = " <-- " + kv.Value;
					break;
				}
			}
			int idx = completeText.IndexOf(_lastSubsection, StringComparison.Ordinal);
			Console.Write("   ");
			if (useColor && idx >= 0) {
				Console.Write(completeText.Substring(0, idx));
				WriteColored(_lastSubsection, stepColor, false);
				Console.Write(completeText.Substring(idx + _lastSubsection.Length));
				if (extraInfo.Length > 0) {
					WriteColored(extraInfo, stepColor, false);
				}
			} else {
				Console.Write(completeText + extraInfo);
			}
			Console.WriteLine();
			string[] subParts = _lastSubsection.Split(' ');
			ListNode subList = new ListNode();
			for (int i = 0; i < subParts.Length; i++) {
				subList.Children.Add(new TokenNode(subParts[i]));
			}
			bool subResult = EvalNode(FlattenSingleTerms(subList));
			string subResultStr = subResult ? "True" : "False";
			int subIdx = (idx >= 0) ? idx : 0;
			int spaces = Math.Max(0, (int)(subIdx + 3 + (_lastSubsection.Length - subResultStr.Length) / 2.0));
			string subLine = new string(' ', spaces) + subResultStr;
			if (useColor) {
				WriteColored(subLine, stepColor, true);
			} else {
				Console.WriteLine(subLine);
			}
			current = collapse.ResultNode;
			bool isFullyFlat = !(current is ListNode) || CountSublists((ListNode)current) == 0;
			if (isFullyFlat) {
				string finalText = SimplifyToString(current);
				bool finalResult = EvalNode(current);
				string finalStr = finalResult ? "True" : "False";
				int fc = Math.Max(0, (finalText.Length - finalStr.Length) / 2 + 3);
				string finalTextLine = "   " + finalText;
				string finalValLine = new string(' ', fc) + finalStr;
				if (useColor) {
					WriteColored(finalTextLine, stepColor, true);
					WriteColored(finalValLine, stepColor, true);
				} else {
					Console.WriteLine(finalTextLine);
					Console.WriteLine(finalValLine);
				}
				break;
			}
		}
	}

	static long _userseed = 0;
	static string _allGuesses = "";
	static int _score = 0;

	static string VCode() {
		return _userseed + "!" + _allGuesses + "!" + _score;
	}

	static string GetScoreMessage(int score) {
		if (score < 10) { return "score"; }
		if (score < 20) { return "Your Score"; }
		if (score < 30) { return "Pretty Good Score"; }
		if (score < 40) { return "Very Nice Score"; }
		if (score < 50) { return "Impressive Score"; }
		if (score < 60) { return "Great Score"; }
		if (score < 70) { return "Outstanding Score"; }
		if (score < 80) { return "Amazing Score"; }
		if (score < 90) { return "Fantastic Score"; }
		if (score < 100) { return "Astonishing Score"; }
		if (score < 120) { return "Achievement Unlocked!"; }
		if (score < 140) { return "Brilliant Score"; }
		if (score < 160) { return "Outrageous Score"; }
		if (score < 180) { return "Incredible Score"; }
		if (score < 200) { return "Unbelievable Score"; }
		if (score < 225) { return "What Score is This?"; }
		if (score < 250) { return "I can't even"; }
		if (score < 275) { return "this is just crazy"; }
		if (score < 300) { return "How are you doing this?"; }
		if (score < 400) { return "Are you a wizard?"; }
		return "You are a wizard... ";
	}

	static void Main() {
		_userseed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		_rng = new Random((int)(_userseed & 0x7FFFFFFF));
		Console.WriteLine();
		Console.WriteLine("quiz seed: " + _userseed);
		int score = 0;
		int streak = 0;
		int bestStreak = 0;
		int totalWrong = 0;
		int totalAnswered = 0;
		int lastScore = 0;
		bool hacked = false;
		string userGuess = "";
		string allGuesses = "";
		string userInputStream = "";
		int expectedscore = 0;
		string sep = "__________________________________________";
		while (!userGuess.Equals("q")) {
			bool askagain = false;
			ListNode logic = GenerateQuestion(score);
			Console.WriteLine("\n\tif " + SimplifyToString(logic) +
												":\n\t  print(\"t\")\n\telse:\n\t  print(\"f\")\n");
			userGuess = "?";
			if (userInputStream.Length > 0) {
				userGuess = userInputStream.Substring(0, 1);
				userInputStream = userInputStream.Substring(1);
			}
			while (!userGuess.Equals("t") && !userGuess.Equals("f") && !userGuess.Equals("q")) {
				if (expectedscore != 0) {
					if (expectedscore == score) {
						WriteColored("--- valid ---", ConsoleColor.Green, true);
					} else {
						WriteColored("-- invalid --", ConsoleColor.Red, true);
					}
					expectedscore = 0;
				}
				try {
					Console.Write("What is the output? (");
					WriteColored("t", ConsoleColor.Cyan, false);
					Console.Write(" or ");
					WriteColored("f", ConsoleColor.Cyan, false);
					Console.Write(", ");
					WriteColored("?", ConsoleColor.Cyan, false);
					Console.Write(" for code, ");
					WriteColored("q", ConsoleColor.Cyan, false);
					Console.Write(" to quit) ");
					string line = Console.ReadLine();
					if (line == null) { line = "q"; }
					userGuess = line.Trim().ToLower();
				} catch (Exception) {
					userGuess = "q";
				}
				if (userGuess.Equals("?") || userGuess.Equals("q")) {
					if (userGuess.Equals("q")) {
						allGuesses += "q";
					}
					Console.WriteLine(sep + "\nValidation code:");
					Console.WriteLine(VCode());
				}
				if (userGuess.Length > 3) {
					try {
						int bangIdx = userGuess.IndexOf('!');
						if (bangIdx < 0) { bangIdx = userGuess.IndexOf(' '); }
						if (bangIdx >= 0) {
							long newseed = (long)double.Parse(userGuess.Substring(0, bangIdx));
							int bang2 = userGuess.IndexOf('!', bangIdx + 1);
							if (bang2 < 0) { bang2 = userGuess.IndexOf(' ', bangIdx + 1); }
							if (bang2 >= 0) {
								userInputStream = userGuess.Substring(bangIdx + 1, bang2 - bangIdx - 1);
								int nextscore = int.Parse(userGuess.Substring(bang2 + 1).Trim());
								Console.WriteLine("NEWSEED " + newseed);
								Console.WriteLine("EXPECTED SCORE " + nextscore);
								_userseed = newseed;
								_rng = new Random((int)(newseed & 0x7FFFFFFF));
								allGuesses = "";
								_allGuesses = "";
								score = 0;
								streak = 0;
								bestStreak = 0;
								totalWrong = 0;
								totalAnswered = 0;
								expectedscore = nextscore;
								askagain = true;
								break;
							}
						}
					} catch (Exception) {
						// Ignore malformed bulk-input sequences.
					}
				}
			}
			if (askagain) { continue; }
			if (userGuess.Equals("q")) { break; }
			totalAnswered++;
			bool finalResult = EvalNode(logic);
			bool usrRight = (finalResult && userGuess.Equals("t")) || (!finalResult && userGuess.Equals("f"));
			if (!usrRight) { Console.WriteLine("##########################################"); }
			PrintWork(logic, !usrRight);
			if (!usrRight) { Console.WriteLine("##########################################"); }
			allGuesses += userGuess;
			_allGuesses = allGuesses;
			if (usrRight) {
				score++;
				streak++;
				if (streak > bestStreak) { bestStreak = streak; }
				string msg = " YOU WERE RIGHT! ";
				int sm = score;
				while (sm > 1) {
					msg = ">" + msg + "<";
					sm /= 2;
				}
				WriteColored(msg, ConsoleColor.Cyan, false);
				if (score % 5 == 0 && userInputStream.Length == 0) {
					Console.Write(" ");
					WriteColored(VCode(), ConsoleColor.Blue, false);
				}
				Console.WriteLine();
			} else {
				totalWrong++;
				score = Math.Max(0, score - 2);
				if (streak > 2) {
					Console.WriteLine("You've answered " + totalWrong + " incorrectly so far, " +
														"and " + (totalAnswered - totalWrong) + " correctly!");
				}
				streak = 0;
			}
			if (score > lastScore + 1) { hacked = true; }
			_score = score;
			Console.WriteLine("\n\n");
			if (hacked) {
				WriteColored("hacked ", ConsoleColor.Red, false);
			} else {
				Console.Write(GetScoreMessage(score));
			}
			Console.Write(": ");
			WriteColored(score.ToString(), ConsoleColor.Green, true);
			if (!usrRight) { Console.WriteLine("Try the next one."); }
			lastScore = score;
		}

		Console.WriteLine(sep);
		int correct = totalAnswered - totalWrong;
		Console.WriteLine("Final Score: " + score + "    (" + correct + "/" + totalAnswered + ")");
		if (streak > 2) {
			Console.WriteLine("You just finished " + streak + " correct in a row");
		}
		if (bestStreak > 2) {
			Console.WriteLine("Your best correct-in-a-row was " + bestStreak + "!\n\n");
		}
		if (expectedscore != 0) {
			if (expectedscore != score) {
				WriteColored("-- invalid --", ConsoleColor.Red, true);
			} else {
				WriteColored("--- valid ---", ConsoleColor.Green, true);
			}
		}
	}
}
