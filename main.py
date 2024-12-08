# This code is under Public Domain (it's free!)
# https://repl.it/@codegiraffe/booleanq
import random

# get seed for "unique" question series
from time import time
userseed = int(time())
random.seed(userseed)
print(" ")
print("quiz seed: "+str(userseed))

def CountSublists(nestedList):
  """returns number of other lists in the given list"""
  count = 0
  for item in nestedList:
    if isinstance(item, list):
      count+=1
  return count

def Flatten(nestedList, ifSublistSmallerThan=-1, flattenToDo=[-1], filterOnFlatten=None):
  result = []
  thisIsLeaf = True
  for item in nestedList:
    if isinstance(item, list):
      thisIsLeaf = False
      # if we're allowed to flatten the child-list into this list
      if ((ifSublistSmallerThan < 0 or (len(item) <= ifSublistSmallerThan))
       and CountSublists(item) == 0 and flattenToDo[0] != 0):
        flattenToDo[0] -= 1
        flatitem =  Flatten(item, -1, [-1], filterOnFlatten)
        if isinstance(flatitem, list):
          result.extend( flatitem )
        else:
          result.append( flatitem )
      else:
        flatitem = Flatten(item, ifSublistSmallerThan, flattenToDo, filterOnFlatten)
        if isinstance(flatitem, list) and (ifSublistSmallerThan < 0 or (len(item) <= ifSublistSmallerThan)) and filterOnFlatten == None:
          result.extend( flatitem )
        else:
          result.append( flatitem )
    else:
      result.append(item)
  # if no sublists had to be flattened
  if thisIsLeaf:
    if filterOnFlatten != None and flattenToDo[0] != 0: result = filterOnFlatten(nestedList)
  return result

def FlattenSingleTerms(nestedList):
    return Flatten(nestedList, 1)

def SimplifyToString(logicList):
  flat = Flatten(logicList)
  result = ""
  for item in flat:
    if len(result) != 0:
      result += " " 
    result += str(item)
  return result

def FindSimplistListSize(nestedList):
  smallestSize = None
  for item in nestedList:
    if isinstance(item, list):
      smallestFromhere = FindSimplistListSize(item)
      if smallestSize == None or smallestFromhere < smallestSize:
        smallestSize = smallestFromhere
  if smallestSize == None:
    smallestSize = len(nestedList)
  return smallestSize

logicalOperations = ["==", "!=", "<", ">", "<=", ">="]
booleanOperations = ["and", "or"]

def GenerateBooleanQuestion(allowNot=False, chanceOfSimpleTF=0.05):
  result = None
  if chanceOfSimpleTF != 0:
    if random.random() < chanceOfSimpleTF:
      result = [ random.choice(["True", "False"]) ]
  if result == None:
    result = [
      random.randrange(0,10),
      random.choice(logicalOperations),
      random.randrange(0,10)
    ]
  if allowNot and random.randrange(0,2) == 1:
    result = ["not", result]
  return result

def GenerateQuestion(score):
  logic = GenerateBooleanQuestion(score >= 15)
  if score >= 4:
    if score < 50:
      logic2 = [random.randrange(0,2) == 1]
    else:
      logic2 = GenerateBooleanQuestion(score >= 75)
    if score >= 20 and random.randrange(0,2) == 1: logic2 = ["not", logic2]
    if score < 30:
      logic = [logic, random.choice(booleanOperations), logic2]
    else:
      logic = [logic2, random.choice(booleanOperations), logic]
    if score > 100:
      if score < 150:
        logic3 = [random.randrange(0,2) == 1]
      else:
        logic3 = GenerateBooleanQuestion(score >= 75)
      logic.insert(0,"("); logic.append(")")
      if score < 125:
        logic = [logic, random.choice(booleanOperations), logic3]
      else:
        logic = [logic3, random.choice(booleanOperations), logic]
  return FlattenSingleTerms(logic)

# escape sequence for colors. using codes specific to repl.it
__esc = "\033[%dm" #"\033[38;5;%dm"
mod = {
  "invert"    :__esc%7, "/invert"   :__esc%27,
  "italic"    :__esc%3, "/italic"   :__esc%23,
  "underline" :__esc%4, "/underline":__esc%24,
  "black"     :__esc%30,"red"       :__esc%31,
  "green"     :__esc%32,"orange"    :__esc%33,
  "blue"      :__esc%34,"purple"    :__esc%35,
  "cyan"      :__esc%36,"white"     :__esc%37,
  "/color"    :__esc%39,
  "bgblack"   :__esc%40,"bgred"     :__esc%41,
  "bggreen"   :__esc%42,"bgorange"  :__esc%43,
  "bgblue"    :__esc%44,"bgpurple"  :__esc%45,
  "bgcyan"    :__esc%46,"bgwhite"   :__esc%47,
  "/bgcolor"  :__esc%49
}

# draws step-by-step resolution of nested logic
__subsectionAsString = ""
def PrintWork(logic, useColor = False):
  #if useColor == True:
  #  import sys
  #  useColor = sys.stdout.isatty()
  def ForeColor(code): return __esc % (30+code)
  completeText = SimplifyToString(logic)
  operationInfo = {
    "==":"values are equal?",
    "<=":"less than or equal?",
    ">=":"greater than or equal?",
    "< ":"less than?",
    "> ":"greater than?",
    "!=":"values are NOT equal?",
    "not":"not means logical opposite",
    "and":"both are true?",
    "or":"at least one is true?"
  }
  def EvalLogicList(logicList):
    global __subsectionAsString
    __subsectionAsString = SimplifyToString(logicList)
    return eval(__subsectionAsString)
  needToEvaluateMore = True
  colorOrder = [3, 5, 6, 1, 2, 4]
  colorIndex = 0
  ce,cs = "",""
  if useColor: ce = mod["/color"]
  while needToEvaluateMore:
    if useColor:
      cs = ForeColor(colorOrder[colorIndex])
      colorIndex += 1
      colorIndex %= len(colorOrder)
    completeText = SimplifyToString(logic)
    needToEvaluateMore = CountSublists(logic) != 0
    logic = Flatten(logic, -1, [1], EvalLogicList)
    # based on the most recent calculation, find any helpful operation info
    extraInfo = ""
    for op in operationInfo:
      if __subsectionAsString.find(op) >= 0:
        extraInfo = " <-- "+cs+operationInfo[op]+ce
    # print the result of the most recent calculation just below it
    result = eval(__subsectionAsString)
    letterindex = completeText.find(__subsectionAsString)
    if useColor:
      completeText = (completeText[:letterindex]+cs+__subsectionAsString+ce+
      completeText[letterindex+len(__subsectionAsString):])
    letterindex += 3
    resultstr = str(result)
    centerdelta = (len(__subsectionAsString) - len(resultstr))/2
    letterindex += centerdelta
    output = " " * int(letterindex)
    output += str(result)
    print("   "+completeText+extraInfo)
    print(cs+output+ce)

def ProcessBigInput(userGuess):
  global allGuesses, score, askagain
  newseed = None
  nextInputStream = None
  nextscore = None
  index = userGuess.find('!')
  if index < 0: index = userGuess.find(' ')
  if index >= 0:
    newseed = float(userGuess[0:index]) # breaks here with bad format
    print("NEWSEED "+str(newseed))
    index2 = -1
    while index2 < index:
      index2 = userGuess.find('!', index+1)
      if index2 < 0: index2 = userGuess.find(' ', index+1)
      if index2 >= 0: haveScore = True
      else: userGuess += input()
    if newseed != 0 and index >= 0:
      nextInputStream = userGuess[index+1:index2]
      nextscore = userGuess[index2+1:]
      print(userGuess)
      print("EXPECTED SCORE "+str(nextscore))
      nextscore = int(nextscore)
      userseed = newseed
      random.seed(userseed)
      allGuesses=""
      score=streak=bestStreak=totalWrong=totalAnswered=0
      askagain = True
  return newseed, nextInputStream, nextscore

# variable to store user input to question
userGuess = ""
allGuesses = ""
score = 0 # how many points the user has
streak = 0 # how many correct in-a-row
bestStreak = 0 # best streak so far
totalWrong = 0
totalAnswered = 0
userInputStream = ""
expectedscore = 0
lastScore = 0
askagain = False
hacked = False
validsequencemessage =   mod["green"]+"--- valid ---"+mod["/color"]
invalidsequencemessage = mod["red"] + "-- invalid --"+mod["/color"]
separator = "__________________________________________"

def vcode(): return str(userseed)+'!'+allGuesses+'!'+str(score)
# keep asking questions as long as the user doesn't want to quit
while userGuess != "q":
  askagain = False
  logic = GenerateQuestion(score)

  # print out the random question
  question = "\tif "+SimplifyToString(logic)+":\n\t  print(\"t\")\n\telse:\n\t  print(\"f\")"
  print("\n"+question+"\n")
  # get a guess from the user
  userGuess = "?"
  # if there is an input stream, read from that
  if len(userInputStream) > 0 and userGuess != "t" and userGuess != "f":
    userGuess = userInputStream[0]
    userInputStream = userInputStream[1:]
  # ask the user for their guess (will be skipped if the input stream had data pulled out)
  while userGuess != "t" and userGuess != "f" and userGuess != "q":
    # once all input-stream responses are read, check if the expected score was correct
    if expectedscore != 0:
      if expectedscore == score: print(validsequencemessage)
      else:                      print(invalidsequencemessage)
      expectedscore = 0
    try:
        cy, _c = mod["cyan"], mod["/color"]
        userGuess = input(f"What is the output? ({cy}t{_c} or {cy}f{_c}, {cy}?{_c} for code, {cy}q{_c} to quit) ")
        userGuess = userGuess.lower()
    except:
        userGuess = 'q'

    if userGuess == "?" or userGuess == "q":
      if userGuess == 'q': allGuesses += 'q'
      print(separator+"\nValidation code:")
      print(vcode())
    if len(userGuess) > 3:
      newseed, userInputStream, expectedscore = ProcessBigInput(userGuess)
      if newseed != None:
        print(newseed)
        userseed = newseed
        random.seed(userseed)
        allGuesses=""
        score=streak=bestStreak=totalWrong=totalAnswered=0
        askagain = True
        break
  if askagain: continue
  if userGuess == 'q':
    break
  totalAnswered += 1
  finalresult = eval(SimplifyToString(logic))
  # calculate if user was right or wrong
  usrRight = (
    (     finalresult  and userGuess == 't') or
    ((not finalresult) and userGuess == 'f')
  )
  # if the user was wrong, emphasize the process
  if not usrRight: print("##########################################")
  PrintWork(logic, not usrRight) # show logic, step-by-step
  if not usrRight: print("##########################################")
  allGuesses += userGuess
  # tell user if they were right or wrong
  if usrRight:
    score = score + 1
    streak += 1
    if streak > bestStreak:
      bestStreak = streak
    msg = " YOU WERE RIGHT! "
    scoremagnitude = score
    while scoremagnitude > 1:
      msg = ">"+msg+"<"
      scoremagnitude /= 2
    if score % 5 == 0 and len(userInputStream) == 0: msg += " "+mod["blue"]+vcode()+mod["/color"]
    print(mod["cyan"]+msg+mod["/color"])
  else:
    totalWrong += 1
    score = score - 2
    if streak > 2:
      print("You've answered "+str(totalWrong)+" incorrectly so far, and "+str(totalAnswered-totalWrong)+" correctly!")
    streak = 0
    if score < 0: score = 0
  if score > lastScore+1: hacked = True
  scoremsg = ""
  if   score < 10: scoremsg = "score"
  elif score < 20: scoremsg = "Your Score"
  elif score < 30: scoremsg = "Pretty Good Score"
  elif score < 40: scoremsg = "Very Nice Score"
  elif score < 50: scoremsg = "Impressive Score"
  elif score < 60: scoremsg = "Great Score"
  elif score < 70: scoremsg = "Outstanding Score"
  elif score < 80: scoremsg = "Amazing Score"
  elif score < 90: scoremsg = "Fantastic Score"
  elif score < 100: scoremsg = "Astonishing Score"
  elif score < 120: scoremsg = "Achievement Unlocked!"
  elif score < 140: scoremsg = "Brilliant Score"
  elif score < 160: scoremsg = "Outrageous Score"
  elif score < 180: scoremsg = "Incredible Score"
  elif score < 200: scoremsg = "Unbelievable Score"
  elif score < 225: scoremsg = "What Score is This?"
  elif score < 250: scoremsg = "I can't even"
  elif score < 275: scoremsg = "this is just crazy"
  elif score < 300: scoremsg = "How are you doing this?"
  elif score < 400: scoremsg = "Are you a wizard?"
  else: scoremsg = "You are a wizard... "
  if hacked: scoremsg = mod["red"]+"hacked "+mod["/color"]
  print("\n\n\n"+scoremsg+": "+mod["green"]+str(score)+mod["/color"])
  if not usrRight:
    print("Try the next one.")
  lastScore = score
print(separator)
correct = totalAnswered-totalWrong
print("Final Score: "+str(score)+"    ("+str(correct)+"/"+str(totalAnswered)+")")
if streak > 2:
  print("You just finished "+str(streak)+" correct in a row")
if bestStreak > 2:
  print("Your best correct-in-a-row was "+str(bestStreak)+"!\n\n")
if expectedscore != 0:
  if expectedscore != score: print(invalidsequencemessage)
  else:                      print(validsequencemessage)