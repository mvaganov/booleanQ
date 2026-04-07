// dotnet publish main.cs -c Release -r win-x64 --self-contained true
// intentionally written in a Java-like style, for student readability
using System.Text;

class BooleanQuiz {
	static void print(object message) { Console.WriteLine(message); }
	abstract class Node {
		public abstract override string ToString();
		public abstract Node Clone();
		public abstract bool IsBool();
	}
	class TokenNode : Node {
		public string Value;
		public TokenNode(string v) { Value = v; }
		public override string ToString() { return Value; }
		public override Node Clone() { return new TokenNode(Value); }
		public override bool IsBool() { return Value == False || Value == True; }
	}
	class ListNode : Node {
		public List<Node> Children = new List<Node>();
		public override string ToString() { return SimplifyToString(this); }
		public override Node Clone() {
			ListNode listNode = new ListNode();
			for(int i = 0; i < Children.Count; ++i) {
				listNode.Children.Add(Children[i].Clone());
			}
			return listNode;
		}
		public override bool IsBool() { return false; }
		public static ListNode MakeList(Node a, Node b, Node c) {
			ListNode ln = new ListNode();
			ln.Children.Add(a); ln.Children.Add(b); ln.Children.Add(c);
			return ln;
		}
		public static ListNode MakeList(Node a, Node b) {
			ListNode ln = new ListNode();
			ln.Children.Add(a); ln.Children.Add(b);
			return ln;
		}
		public int CountSublists() {
			int count = 0;
			for (int i = 0; i < Children.Count; i++) {
				if (Children[i] is ListNode) { count++; }
			}
			return count;
		}
	}

	static string SimplifyToString(Node node) { return SimplifyToString(node, null, null, null); }
	static string SimplifyToString(Node node, Node targetNode, string targetNodeStart, string targetNodeEnd) {
		string result;
		if (node is TokenNode) {
			result = ((TokenNode)node).Value;
		} else {
			ListNode list = (ListNode)node;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < list.Children.Count; i++) {
				if (i > 0) {
					bool skipSpace = false;
					TokenNode LastToken = list.Children[i - 1] is TokenNode ? (TokenNode)list.Children[i - 1] : null;
					bool justPrintedParenthesisOrNot = LastToken != null && (LastToken.Value == ParenOp || LastToken.Value == Not);
					if (justPrintedParenthesisOrNot) { skipSpace = true; }
					TokenNode thisToken = list.Children[i] is TokenNode ? (TokenNode)list.Children[i] : null;
					bool aboutToPrintParenthesis = thisToken != null && thisToken.Value == ParenCl;
					if (aboutToPrintParenthesis) { skipSpace = true; }
					if (!skipSpace) {
						sb.Append(' ');
					}
				}
				sb.Append(SimplifyToString(list.Children[i], targetNode, targetNodeStart, targetNodeEnd));
			}
			result = sb.ToString();
		}
		if (targetNode == node) {
			result = targetNodeStart + result + targetNodeEnd;
		}
		return result;
	}

	static List<string> GatherStrings(Node node) {
		List<string> output = new List<string>();
		GatherStrings(node, output);
		return output;
	}

	static void GatherStrings(Node node, List<string> output) {
		if (node is TokenNode) {
			output.Add(((TokenNode)node).Value);
			return;
		}
		ListNode list = (ListNode)node;
		for (int i = 0; i < list.Children.Count; i++) {
			GatherStrings(list.Children[i], output);
		}
	}

	// position kept class-level field so ParseXxx methods can advance without passing by reference
	static int _pos;

	static bool ParseOr(List<string> tokens) {
		bool left = ParseAnd(tokens);
		while (_pos < tokens.Count && tokens[_pos] == Or) {
			_pos++;
			bool right = ParseAnd(tokens);
			left = left || right;
		}
		return left;
	}

	static bool ParseAnd(List<string> tokens) {
		bool left = ParseNot(tokens);
		while (_pos < tokens.Count && tokens[_pos] == And) {
			_pos++;
			bool right = ParseNot(tokens);
			left = left && right;
		}
		return left;
	}

	static bool ParseNot(List<string> tokens) {
		if (_pos < tokens.Count && tokens[_pos] == Not) {
			_pos++;
			return !ParseNot(tokens);
		}
		return ParseComparison(tokens);
	}

	static bool ParseComparison(List<string> tokens) {
		if (_pos < tokens.Count && tokens[_pos] == ParenOp) {
			_pos++;
			bool val = ParseOr(tokens);
			if (_pos < tokens.Count && tokens[_pos] == ParenCl) {
				_pos++;
			}
			return val;
		}
		if (_pos < tokens.Count && tokens[_pos] == True) {
			_pos++;
			return true;
		}
		if (_pos < tokens.Count && tokens[_pos] == False) {
			_pos++;
			return false;
		}
		int left = int.Parse(tokens[_pos++]);
		string op = tokens[_pos++];
		int right = int.Parse(tokens[_pos++]);
		if (op == Eq) { return left == right; }
		if (op == Neq) { return left != right; }
		if (op == Lt) { return left < right; }
		if (op == Gt) { return left > right; }
		if (op == Lte) { return left <= right; }
		if (op == Gte) { return left >= right; }
		throw new InvalidOperationException("Unknown operator: " + op);
	}
	static bool EvalNode(Node node) {
		_pos = 0;
		List<string> tokens = GatherStrings(node);
		return ParseOr(tokens);
	}

	const string True = "true", False = "false", And = "&&", Or = "||", Eq = "==", Neq = "!=",
		Lt = "<", Gt = ">", Lte = "<=", Gte = ">=", Not = "!", ParenOp = "(", ParenCl = ")";
	static readonly string[] LogicalOperations = new string[] { Eq, Neq, Lt, Gt, Lte, Gte };
	static readonly string[] BooleanOperations = new string[] { And, Or };
	static Random _rng = new Random();

	static string RandomBool() { return (_rng.Next(2) == 0) ? True : False; }
	static string RandomNum() { return _rng.Next(0, 10).ToString(); }
	static string RandomLogicOp() { return LogicalOperations[_rng.Next(LogicalOperations.Length)]; }
	static string RandomBoolOp() { return BooleanOperations[_rng.Next(BooleanOperations.Length)]; }

	static Node GenerateBooleanQuestion(bool allowNot, double chanceOfSimpleTF) {
		Node result;
		if (chanceOfSimpleTF > 0 && _rng.NextDouble() < chanceOfSimpleTF) {
			result = new TokenNode(RandomBool());
		} else {
			result = ListNode.MakeList(new TokenNode(RandomNum()), new TokenNode(RandomLogicOp()), new TokenNode(RandomNum()));
		}
		if (allowNot && _rng.Next(2) == 1) {
			result = WrapInNotClause(result);
		}
		return result;
	}

	static Node WrapInNotClause(Node node) {
		if (node is TokenNode && ((TokenNode)node).IsBool()) {
			return ListNode.MakeList(new TokenNode(Not), node);
		}
		return ListNode.MakeList(new TokenNode(Not), ListNode.MakeList(new TokenNode(ParenOp), node, new TokenNode(ParenCl)));
	}

	static Node GenerateQuestion(int score) {
		Node logic = GenerateBooleanQuestion(score >= 15, 0.05);
		if (score >= 4) {
			Node logic2;
			if (score < 50) {
				logic2 = new TokenNode(RandomBool());
			} else {
				logic2 = GenerateBooleanQuestion(score >= 75, 0.05);
			}
			if (score >= 20 && _rng.Next(2) == 1) {
				logic2 = WrapInNotClause(logic2);
			}
			string boolOp = RandomBoolOp();
			ListNode combined;
			if (score < 30) {
				combined = ListNode.MakeList(logic, new TokenNode(boolOp), logic2);
			} else {
				combined = ListNode.MakeList(logic2, new TokenNode(boolOp), logic);
			}
			logic = combined;
			if (score > 100) {
				Node logic3;
				if (score < 150) {
					logic3 = new TokenNode(RandomBool());
				} else {
					logic3 = GenerateBooleanQuestion(score >= 75, 0.05);
				}
				ListNode wrapped = new ListNode();
				wrapped.Children.Add(new TokenNode(ParenOp));
				ListNode logicAsList = (ListNode)logic;
				for (int i = 0; i < logicAsList.Children.Count; i++) {
					wrapped.Children.Add(logicAsList.Children[i]);
				}
				wrapped.Children.Add(new TokenNode(ParenCl));
				string boolOp2 = RandomBoolOp();
				if (score < 125) {
					logic = ListNode.MakeList(wrapped, new TokenNode(boolOp2), logic3);
				} else {
					logic = ListNode.MakeList(logic3, new TokenNode(boolOp2), wrapped);
				}
			}
		}
		return logic;
	}

	static readonly Dictionary<string, string> OperationInfo;

	static readonly ConsoleColor[] StepColors = new ConsoleColor[] {
		ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Yellow,
		ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Blue,
	};
	const char ColorSwitchCode = '\b';
	static string ColorSwitchStr = ColorSwitchCode.ToString();
	static string C_Revert = ColorSwitchStr + '-';
	static string C_Red = ColorSwitchStr + ((int)ConsoleColor.Red).ToString("x");
	static string C_Yellow = ColorSwitchStr + ((int)ConsoleColor.DarkYellow).ToString("x");
	static string C_Green = ColorSwitchStr + ((int)ConsoleColor.Green).ToString("x");
	static string C_Blue = ColorSwitchStr + ((int)ConsoleColor.Blue).ToString("x");
	static string C_Magenta = ColorSwitchStr + ((int)ConsoleColor.Magenta).ToString("x");
	static string C_Cyan = ColorSwitchStr + ((byte)ConsoleColor.Cyan).ToString("x");
	static string C_White = ColorSwitchStr + ((int)ConsoleColor.White).ToString("x");
	static string C_Gray = ColorSwitchStr + ((int)ConsoleColor.DarkGray).ToString("x");
	static string[] StepColorTxt;

	static BooleanQuiz() {
		OperationInfo = new Dictionary<string, string>();
		OperationInfo.Add(Eq, "values are equal?");
		OperationInfo.Add(Lte, "less than or equal?");
		OperationInfo.Add(Gte, "greater than or equal?");
		OperationInfo.Add(Lt, "less than?");
		OperationInfo.Add(Gt, "greater than?");
		OperationInfo.Add(Neq, "values are NOT equal?");
		OperationInfo.Add(Not, $"{Not} means logical opposite");
		OperationInfo.Add(And, "both are true?");
		OperationInfo.Add(Or, "at least one is true?");
		StepColorTxt = new string[StepColors.Length];
		for (int i = 0; i < StepColors.Length; ++i) {
			StepColorTxt[i] = ColorSwitchStr + ((int)StepColors[i]).ToString("x");
		}
	}

	/// <summary>a reference to a node in a tree.</summary>
	struct FoundNodeInTree {
		public Node Node;
		public ListNode Parent;
		public FoundNodeInTree(Node node, ListNode parent) { Node = node; Parent = parent; }
		public int GetChildIndex() {
			if (Parent == null || Node == null) { return -1; }
			for (int i = 0; i < Parent.Children.Count; ++i) {
				if (Parent.Children[i] == Node) { return i; }
			}
			return -1;
		}
		public static FoundNodeInTree FindFirstSimpleNode(Node node) { return FindFirstSimpleNode(node, null); }
		public static FoundNodeInTree FindFirstSimpleNode(Node node, ListNode parent) {
			if (node == null || node is TokenNode) { return new FoundNodeInTree(null, parent); }
			ListNode listNode = (ListNode)node;
			for (int i = 0; i < listNode.Children.Count; ++i) {
				if (listNode.Children[i] is ListNode) {
					return FindFirstSimpleNode(listNode.Children[i], listNode);
				}
			}
			return new FoundNodeInTree(node, parent);
		}
	}

	static void PrintWork(Node logic, bool useColor) {
		int colorIndex = 0;
		Node root = logic.Clone();
		int loopGuard = 100;
		while (root != null) {
			if (--loopGuard < 0) { break; }
			// get a node that can be simplified
			FoundNodeInTree found = FoundNodeInTree.FindFirstSimpleNode(root);
			// print the state, also identifying the node that will be simplified (assuming a simplification is happening here)
			const string NodeStart = "{{{", NodeEnd = "}}}";
			string textWithNodeIdentified = SimplifyToString(root, found.Node, NodeStart, NodeEnd);
			int nodeStart = textWithNodeIdentified.IndexOf(NodeStart);
			int start = nodeStart + NodeStart.Length;
			int nodeEnd = textWithNodeIdentified.IndexOf(NodeEnd);
			bool printThisStep = false;
			string color = useColor ? StepColorTxt[colorIndex % StepColorTxt.Length] : C_Revert;
			if (nodeStart >= 0) {
				ListNode nodeToSimplify = found.Node is ListNode ? (ListNode)found.Node : null;
				string operatorKind = GetOperator(nodeToSimplify);
				printThisStep = operatorKind != null;
				if (printThisStep) {
					string firstPart = textWithNodeIdentified.Substring(0, nodeStart);
					string thisLogic = textWithNodeIdentified.Substring(start, nodeEnd - start);
					string lastPart = textWithNodeIdentified.Substring(nodeEnd + NodeEnd.Length);
					string fullOutput = firstPart + color + thisLogic + C_Revert + lastPart + " <-- " + color + OperationInfo[operatorKind] + "\n";
					WriteWithColorCodes(fullOutput);
				}
			}
			if (found.Node == null) { break; }
			// simplify the node
			int index = found.GetChildIndex();
			bool result = EvalNode(found.Node);
			string value = result ? True : False;
			Node simplifiedValue = new TokenNode(value);
			if (printThisStep) {
				StringBuilder sb = new StringBuilder();
				int extraIndent = ((nodeEnd - start) - value.Length) / 2 + nodeStart;
				for (int i = 0; i < extraIndent; ++i) { sb.Append(' '); }
				WriteWithColorCodes(sb + color + simplifiedValue + "\n");
				++colorIndex;
			}
			if (index >= 0) {
				found.Parent.Children[index] = simplifiedValue;
			} else {
				root = simplifiedValue;
			}
		}
	}

	static string GetOperator(ListNode listNode) {
		for(int i = 0; i < listNode.Children.Count; ++i) {
			TokenNode token = listNode.Children[i] as TokenNode;
			if (token == null) { continue; }
			if (OperationInfo.ContainsKey(token.Value)) {
				return token.Value;
			}
		}
		return null;
	}

	static long _userseed = 0;
	static string _allGuesses = "";
	static int _score = 0;

	static string VCode() { return _userseed + "!" + _allGuesses + "!" + _score; }

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

	static void WriteWithColorCodes(string text) {
		int start = 0, end = -1;
		ConsoleColor defaultColor = Console.ForegroundColor;
		char c = '\0';
		while (start < text.Length) {
			end = text.Length;
			for (int i = start; i < text.Length; ++i) {
				c = text[i];
				if (c == ColorSwitchCode) {
					end = i;
					break;
				}
			}
			if (end < start) { end = text.Length; }
			string substring = text.Substring(start, end - start);
			Console.Write(substring);
			if (end <= text.Length) {
				switch (c) {
					case ColorSwitchCode:
						++end;
						c = end < text.Length ? text[end] : '0';
						if (c == '-') {
							Console.ForegroundColor = defaultColor;
							break;
						}
						Console.ForegroundColor = (ConsoleColor)GetNumberFromHexCode(c);
						break;
				}
				start = ++end;
			}
		}
		Console.ForegroundColor = defaultColor;
	}
	static int GetNumberFromHexCode(char c) {
		return (c >= '0' && c <= '9') ? c - '0' : (c >= 'a' && c <= 'f') ? c - 'a' + 10 : 0;
	}
	static void Main() {
		_userseed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		_rng = new Random((int)(_userseed & 0x7FFFFFFF));
		_allGuesses = "";
		print("\nquiz seed: " + _userseed);
		int streak = 0;
		int bestStreak = 0;
		int totalWrong = 0;
		int totalAnswered = 0;
		int lastScore = 0;
		bool hacked = false;
		string userGuess = "";
		string userInputStream = "";
		int expectedscore = 0;
		string sep = "__________________________________________";
		bool running = true;
		while (running) {
			Node logic = GenerateQuestion(_score);
			string m = C_Magenta, r = C_Revert, c = C_Cyan, w = C_White, g = C_Gray, y = C_Yellow;
			WriteWithColorCodes("\n\t"+m+"if"+r+" ("+w+SimplifyToString(logic)+r+") "+g+"{"+r+
				"\n\t  "+y+"print"+r+"("+c+"\"t\""+r+");\n\t"+g+"}"+r+" "+m+"else"+r+" "+g+"{"+r+
				"\n\t  "+y+"print"+r+"("+c+"\"f\""+r+");\n\t"+g+"}"+r+"\n");
			userGuess = "?";
			if (userInputStream.Length > 0) {
				userGuess = userInputStream.Substring(0, 1);
				userInputStream = userInputStream.Substring(1);
			}
			while (!userGuess.Equals("t") && !userGuess.Equals("f") && !userGuess.Equals("q")) {
				PrintUserInputPrompt();
				userGuess = GetUserInput();
				if (userGuess.Equals("?") || userGuess.Equals("q")) {
					if (userGuess.Equals("q")) {
						_allGuesses += "q";
						running = false;
					}
					WriteWithColorCodes(sep + "\nValidation code:" + VCode() + "\n");
				}
				if (userGuess.Length > 3) {
					try {
						UserValidationCodeData data = ReadUserInputAsValidationCodeData(userGuess);
						if (data != null) {
							_userseed = data.seed;
							_rng = new Random((int)(_userseed & 0x7FFFFFFF));
							_allGuesses = "";
							userInputStream = data.input;
							expectedscore = data.score;
							_score = streak = bestStreak = totalWrong = totalAnswered = 0;
							hacked = false;
							break;
						}
					} catch (Exception) {
						// Ignore malformed bulk-input sequences.
					}
					userGuess = "";
				}
			}
			bool haveValidAnswer = userGuess.Equals("t") || userGuess.Equals("f");
			if (!haveValidAnswer) { continue; }
			totalAnswered++;
			bool finalResult = EvalNode(logic);
			bool usrRight = (finalResult && userGuess.Equals("t")) || (!finalResult && userGuess.Equals("f"));
			if (!usrRight) { print(sep); }
			PrintWork(logic, !usrRight);
			if (!usrRight) { print(sep); }
			_allGuesses += userGuess;
			if (usrRight) {
				_score++;
				streak++;
				if (streak > bestStreak) { bestStreak = streak; }
				int sm = _score;
				string msg = " YOU WERE RIGHT! ";
				while (sm > 1) {
					msg = ">" + msg + "<";
					sm /= 2;
				}
				string output = C_Cyan + msg;
				if (_score % 5 == 0 && userInputStream.Length == 0) {
					output += " " + C_Blue + VCode();
				}
				WriteWithColorCodes(output + "\n");
			} else {
				totalWrong++;
				_score = Math.Max(0, _score - 2);
				if (streak > 2) {
					WriteWithColorCodes("You answered " + C_Gray + totalWrong + C_Revert + " incorrectly " +
						"so far, and " + C_Cyan + (totalAnswered - totalWrong) + C_Revert + " correctly!\n");
				}
				streak = 0;
			}
			if (_score > lastScore + 1) { hacked = true; }
			if (userInputStream.Length == 0 && expectedscore != 0) {
				if (expectedscore == _score) {
					WriteWithColorCodes(C_Green + "--- valid ---\n");
				} else {
					WriteWithColorCodes(C_Red + "--- invalid ---\n");
				}
				expectedscore = 0;
			}
			string response = "\n\n" + (hacked ? C_Red + "hacked ": GetScoreMessage(_score));
			WriteWithColorCodes(response + ": " + C_Green + _score + "\n");
			if (!usrRight) { print("Try the next one."); }
			lastScore = _score;
		}
		print(sep);
		int correct = totalAnswered - totalWrong;
		print("Final Score: " + _score + "    (" + correct + "/" + totalAnswered + ")");
		if (streak > 2) {
			print("You just finished " + streak + " correct in a row");
		}
		if (bestStreak > 2) {
			print("Your best correct-in-a-row was " + bestStreak + "!\n\n");
		}
	}
	static void PrintUserInputPrompt() {
		string c = C_Cyan, r = C_Revert;
		string message = "What is the output? (" + c + "t" + r + " or " + c + "f" + r + ", " +
			c + "?" + r + " for code, " + c + "q" + r + " to quit) ";
		WriteWithColorCodes(message);
	}
	static string GetUserInput() {
		ConsoleColor defaultColor = Console.ForegroundColor;
		try {
			Console.ForegroundColor = ConsoleColor.Cyan;
			string line = Console.ReadLine();
			Console.ForegroundColor = defaultColor;
			if (line == null) { line = "q"; }
			return line.Trim().ToLower();
		} catch (Exception e) {
			print(e);
			Console.ForegroundColor = defaultColor;
			return "q";
		}
	}
	class UserValidationCodeData {
		public string input; public long seed; public int score;
		public UserValidationCodeData(string input, long seed, int score) { this.input = input; this.seed = seed; this.score = score; }
	}
	static UserValidationCodeData ReadUserInputAsValidationCodeData(string userGuess) {
		int bangIdx = userGuess.IndexOf('!');
		if (bangIdx < 0) { bangIdx = userGuess.IndexOf(' '); }
		if (bangIdx >= 0) {
			long newseed = (long)double.Parse(userGuess.Substring(0, bangIdx));
			int bang2 = userGuess.IndexOf('!', bangIdx + 1);
			if (bang2 < 0) { bang2 = userGuess.IndexOf(' ', bangIdx + 1); }
			if (bang2 >= 0) {
				string userInput = userGuess.Substring(bangIdx + 1, bang2 - bangIdx - 1);
				int nextscore = int.Parse(userGuess.Substring(bang2 + 1).Trim());
				return new UserValidationCodeData(userInput, newseed, nextscore);
			}
		}
		return null;
	}
}
