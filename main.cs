// intentionally written in a Java-like style, in one file, for student readability
// compile with `dotnet publish main.cs -c Release -r win-x64 --self-contained true`
using System.Text;

class BooleanQuizProgram {
	static void print(object message) { Console.WriteLine(message); }
	const string sep = "__________________________________________";
	const string True = "true", False = "false", And = "&&", Or = "||", Eq = "==", Neq = "!=",
	Lt = "<", Gt = ">", Lte = "<=", Gte = ">=", Not = "!", ParenOp = "(", ParenCl = ")";
	static readonly string[] LogicalOperations = new string[] { Eq, Neq, Lt, Gt, Lte, Gte };
	static readonly string[] BooleanOperations = new string[] { And, Or };
	static Random _rng = new Random();
	static readonly Dictionary<string, string> Operators;

	static readonly ConsoleColor[] StepColors = new ConsoleColor[] {
		ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Red,
		ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Blue,
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

	static BooleanQuizProgram() {
		Operators = new Dictionary<string, string>();
		Operators.Add(Eq, "values are equal?");
		Operators.Add(Lte, "less than or equal?");
		Operators.Add(Gte, "greater than or equal?");
		Operators.Add(Lt, "less than?");
		Operators.Add(Gt, "greater than?");
		Operators.Add(Neq, "values are NOT equal?");
		Operators.Add(Not, $"{Not} means logical opposite");
		Operators.Add(And, "both are true?");
		Operators.Add(Or, "at least one is true?");
		StepColorTxt = new string[StepColors.Length];
		for (int i = 0; i < StepColors.Length; ++i) {
			StepColorTxt[i] = ColorSwitchStr + ((int)StepColors[i]).ToString("x");
		}
	}

	static void Main() {
		BooleanQuiz quiz = new BooleanQuiz();
		quiz.Initialize();
		while (!quiz.ShouldQuit) {
			quiz.GenerateQuestionIfNeeded();
			quiz.AskQuestion();
			quiz.PromptUserInput();
			quiz.EvaluateUserInput();
		}
		quiz.FinalResults();
	}

	class BooleanQuiz {
		public long _userseed;
		public int _score, lastScore, expectedscore, streak, bestStreak, totalAnswered, totalWrong;
		public string userGuess = "?", userInputStream = "", _allGuesses = "";
		public bool ShouldQuit, hacked;
		public Node logic = null;
		public void Initialize() {
			_userseed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			_rng = new Random((int)(_userseed & 0x7FFFFFFF));
			_allGuesses = "";
			WriteWithColorCodes(C_Gray + "\nquiz seed: " + _userseed + "\n");
		}
		public string VCode() { return _userseed + "!" + _allGuesses + "!" + _score; }
		public void GenerateQuestionIfNeeded() {
			if (logic != null) { return; }
			logic = GenerateQuestion(_score);
		}
		public void AskQuestion() {
			string m = C_Magenta, r = C_Revert, c = C_Cyan, w = C_White, g = C_Gray, y = C_Yellow;
			WriteWithColorCodes("\n\t" + m + "if" + r + " (" + w + WriteToString(logic) + r + ") " + g + "{" + r +
				"\n\t  " + y + "print" + r + "(" + c + "\"t\"" + r + ");\n\t" + g + "}" + r + " " + m + "else" + r + " " + g + "{" + r +
				"\n\t  " + y + "print" + r + "(" + c + "\"f\"" + r + ");\n\t" + g + "}" + r + "\n");
		}
		public void PromptUserInput() {
			if (userInputStream.Length > 0) {
				userGuess = userInputStream.Substring(0, 1);
				userInputStream = userInputStream.Substring(1);
				return;
			}
			string c = C_Cyan, r = C_Revert;
			string message = "What is the output? (" + c + "t" + r + " or " + c + "f" + r + ", " +
				c + "?" + r + " for code, " + c + "q" + r + " to quit) ";
			WriteWithColorCodes(message);
			userGuess = ReadUserInput();
			if (userGuess.Equals("?") || userGuess.Equals("q")) {
				ShouldQuit = userGuess.Equals("q");
				WriteWithColorCodes(sep + "\nValidation code: " + VCode() + "\n");
			}
			ReadValidationCodeData();
		}
		bool ReadValidationCodeData() {
			if (userGuess.Length < 3) {
				return false;
			}
			try {
				UserValidationCodeData data = UserValidationCodeData.FromString(userGuess);
				userGuess = "";
				if (data != null) {
					_userseed = data.seed;
					_rng = new Random((int)(_userseed & 0x7FFFFFFF));
					_allGuesses = "";
					userInputStream = data.input;
					expectedscore = data.score;
					_score = streak = bestStreak = totalWrong = totalAnswered = 0;
					hacked = false;
					logic = null;
					WriteWithColorCodes(C_Gray + "\nquiz seed: " + _userseed + "\n");
					return true;
				}
			} catch (Exception) {
				// Ignore malformed bulk-input sequences.
				userGuess = "";
			}
			return false;
		}
		static string ReadUserInput() {
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
		public void EvaluateUserInput() {
			bool haveValidAnswer = userGuess.Equals("t") || userGuess.Equals("f");
			if (!haveValidAnswer) { return; }
			totalAnswered++;
			bool finalResult = BoolEvalNode(logic);
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
			if (userInputStream.Length == 0 && expectedscore != 0) {
				if (expectedscore == _score) {
					WriteWithColorCodes(C_Green + "--- valid ---\n");
				} else {
					WriteWithColorCodes(C_Red + "--- invalid ---\n");
				}
				expectedscore = 0;
			}
			string response = "\n\n" + (hacked ? C_Red + "hacked " : GetScoreMessage(_score));
			WriteWithColorCodes(response + ": " + C_Green + _score + "\n");
			if (!usrRight) { print("Try the next one."); }
			lastScore = _score;
			logic = null;
		}
		public void FinalResults() {
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
	}

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
		public override string ToString() { return WriteToString(this); }
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

	/// <summary>a reference to a node in a tree.</summary>
	struct FoundNodeInTree {
		public Node Node;
		public ListNode Parent;
		public static FoundNodeInTree NotFound = new FoundNodeInTree(null, null);
		public FoundNodeInTree(Node node, ListNode parent) { Node = node; Parent = parent; }
		public int GetIndex() {
			if (Parent == null || Node == null) { return -1; }
			for (int i = 0; i < Parent.Children.Count; ++i) {
				if (Parent.Children[i] == Node) { return i; }
			}
			return -1;
		}
		internal string GetToken() {
			if (Node is TokenNode) {
				return ((TokenNode)Node).Value;
			}
			return null;
		}
		public static FoundNodeInTree FindFirstSimpleNode(Node node) { return FindFirstSimpleNode(node, null); }
		public static FoundNodeInTree FindFirstSimpleNode(Node node, ListNode parent) {
			if (node == null || node is TokenNode) { return new FoundNodeInTree(node, parent); }
			ListNode listNode = (ListNode)node;
			for (int i = 0; i < listNode.Children.Count; ++i) {
				if (listNode.Children[i] is ListNode) {
					return FindFirstSimpleNode(listNode.Children[i], listNode);
				}
			}
			return new FoundNodeInTree(node, parent);
		}
	}

	class UserValidationCodeData {
		public string input; public long seed; public int score;
		public UserValidationCodeData(string input, long seed, int score) { this.input = input; this.seed = seed; this.score = score; }
		public static UserValidationCodeData FromString(string userInput) {
			int bangIdx = userInput.IndexOf('!');
			if (bangIdx < 0) { bangIdx = userInput.IndexOf(' '); }
			if (bangIdx >= 0) {
				long newseed = (long)double.Parse(userInput.Substring(0, bangIdx));
				int bang2 = userInput.IndexOf('!', bangIdx + 1);
				if (bang2 < 0) { bang2 = userInput.IndexOf(' ', bangIdx + 1); }
				if (bang2 >= 0) {
					string input = userInput.Substring(bangIdx + 1, bang2 - bangIdx - 1);
					int nextscore = int.Parse(userInput.Substring(bang2 + 1).Trim());
					return new UserValidationCodeData(input, newseed, nextscore);
				}
			}
			return null;
		}
	}

	static string WriteToString(Node node) { return WriteToString(node, null, null, null); }
	static string WriteToString(Node node, Node targetNode, string targetNodeStart, string targetNodeEnd) {
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
				sb.Append(WriteToString(list.Children[i], targetNode, targetNodeStart, targetNodeEnd));
			}
			result = sb.ToString();
		}
		if (targetNode == node) {
			result = targetNodeStart + result + targetNodeEnd;
		}
		return result;
	}

	static bool BoolEvalNode(Node node) {
		string resultString = ResolveNode(node);
		if (string.IsNullOrEmpty(resultString) || resultString != True && resultString != False) {
			throw new Exception("unexpected resolution \"" + resultString + "\" to " + node);
		}
		return resultString == True;
	}

	static string ResolveNode(Node node) {
		if (node is TokenNode) {
			return ((TokenNode)node).Value;
		}
		ListNode list = node as ListNode;
		FoundNodeInTree operatorNode = GetOperator(list);
		string token = null;
		if (operatorNode.Node != null) {
			token = operatorNode.GetToken();
			int index = operatorNode.GetIndex();
			string left = null, right = null;
			switch (token) {
				case Eq: case Neq: case Lt: case Gt: case Lte: case Gte: case And: case Or:
					left = ResolveNode(list.Children[index - 1]);
					right = ResolveNode(list.Children[index + 1]);
					break;
				case Not: left = right = ResolveNode(list.Children[index + 1]); break;
				default: throw new Exception("unknown operator: " + token);
			}
			token = ResolveBinaryLogic(token, left, right) ? True : False;
		} else {
			for (int i = 0; i < list.Children.Count; ++i) {
				TokenNode tokenNode = list.Children[i] as TokenNode;
				if (tokenNode != null) {
					switch (tokenNode.Value) {
						case ParenOp: case ParenCl: continue;
					}
				}
				token = ResolveNode(list.Children[i]);
			}
		}
		return token;
	}

	static FoundNodeInTree GetOperator(ListNode listNode) {
		if (listNode == null) {
			return FoundNodeInTree.NotFound;
		}
		List<Node> list = listNode.Children;
		for (int i = 0; i < list.Count; ++i) {
			TokenNode token = list[i] as TokenNode;
			if (token == null) { continue; }
			if (Operators.ContainsKey(token.Value)) {
				return new FoundNodeInTree(token, listNode);
			}
		}
		return FoundNodeInTree.NotFound;
	}

	static bool ResolveBinaryLogic(string op, string left, string right) {
		switch (op) {
			case Eq: return int.Parse(left) == int.Parse(right);
			case Neq: return int.Parse(left) != int.Parse(right);
			case Lt: return int.Parse(left) < int.Parse(right);
			case Gt: return int.Parse(left) > int.Parse(right);
			case Lte: return int.Parse(left) <= int.Parse(right);
			case Gte: return int.Parse(left) >= int.Parse(right);
			case And: return (left == True) && (right == True);
			case Or: return (left == True) || (right == True);
			case Not: return (right != True);
			default: throw new Exception("unknown operator: " + op);
		}
	}

	static Node GenerateQuestion(int score) {
		Node logic = GenerateBooleanCondition(score >= 15, 0.05);
		if (score >= 4) {
			Node logic2;
			if (score < 50) {
				logic2 = new TokenNode(RandomBool());
			} else {
				logic2 = GenerateBooleanCondition(score >= 75, 0.05);
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
					logic3 = GenerateBooleanCondition(score >= 75, 0.05);
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

	static Node GenerateBooleanCondition(bool allowNot, double chanceOfSimpleTF) {
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

	static string RandomBool() { return (_rng.Next(2) == 0) ? True : False; }
	static string RandomNum() { return _rng.Next(0, 10).ToString(); }
	static string RandomLogicOp() { return LogicalOperations[_rng.Next(LogicalOperations.Length)]; }
	static string RandomBoolOp() { return BooleanOperations[_rng.Next(BooleanOperations.Length)]; }

	static Node WrapInNotClause(Node node) {
		if (node is TokenNode && ((TokenNode)node).IsBool()) {
			return ListNode.MakeList(new TokenNode(Not), node);
		}
		return ListNode.MakeList(new TokenNode(Not), ListNode.MakeList(new TokenNode(ParenOp), node, new TokenNode(ParenCl)));
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
			string textWithNodeIdentified = WriteToString(root, found.Node, NodeStart, NodeEnd);
			int nodeStart = textWithNodeIdentified.IndexOf(NodeStart);
			int start = nodeStart + NodeStart.Length;
			int nodeEnd = textWithNodeIdentified.IndexOf(NodeEnd);
			bool printThisStep = false;
			string color = useColor ? StepColorTxt[colorIndex % StepColorTxt.Length] : C_Revert;
			if (nodeStart >= 0) {
				ListNode nodeToSimplify = found.Node is ListNode ? (ListNode)found.Node : null;
				FoundNodeInTree foundOperator = GetOperator(nodeToSimplify);
				string operatorKind = foundOperator.GetToken();
				printThisStep = operatorKind != null;
				if (printThisStep) {
					string firstPart = textWithNodeIdentified.Substring(0, nodeStart);
					string thisLogic = textWithNodeIdentified.Substring(start, nodeEnd - start);
					string lastPart = textWithNodeIdentified.Substring(nodeEnd + NodeEnd.Length);
					string fullOutput = firstPart + color + thisLogic + C_Revert + lastPart + " <-- " + color + Operators[operatorKind] + "\n";
					WriteWithColorCodes(fullOutput);
				}
			}
			if (found.Node == null) { break; }
			// simplify the node and replace the node with it's simpler version
			int index = found.GetIndex();
			string value = ResolveNode(found.Node);
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
}
