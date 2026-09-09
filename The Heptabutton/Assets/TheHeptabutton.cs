using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class TheHeptabutton : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMColorblindMode ColorblindMode;

   public KMSelectable Button;
   public TextMesh ButtonText;
   public Material[] ButtonColors;
   public Renderer ButtonColor;
   public TextMesh ColorblindIndicator;
   public GameObject[] StageLights;
   public GameObject[] StageLightEffects;
   public Renderer BackgroundColor;
   
   public string[] Sounds = {"Heptastic Solve!", "Unfortunate..."};

   private int colorIndex;
   private bool buttonHeld;
   private List<int> darkerColors = new List<int> {2, 3, 6, 9, 13, 16, 19, 20, 21, 23, 24};
   private string[] buttonLabels = {"PUSH", "HOLD", "BOOM", "WAIT", "LOOK", "FAIL", "DONE", "PRESS", "ABORT", "TOUCH", "CLICK", "AVOID", "SOLVE", "PUNCH", "SELECT", "BUTTON", "IGNITE", "LAUNCH", "DEFUSE", "DISARM", "IGNORE", "RELEASE", "EXPLODE", "DEPRESS", "CONTROL", "NEGLECT", "ACHIEVE", "POLYGON"};
   private int stage;
   private List<int> colorValues = new List<int> {122, 012, 000, 002, 221, 022, 010, 111, 020, 001, 021, 120, 202, 100, 112, 121, 110, 210, 212, 101, 200, 201, 211, 011, 102, 222, 220};
   private List<int> stageColors = new List<int> {};
   private int HoldTime;
   private int ReleaseTime;
   private string[] morse = {".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--.."};
   private int stageThreeColor;
   private bool stageFourCondition;
   private List<int> tapCode = new List<int> {11, 12, 13, 14, 15, 21, 22, 23, 24, 25, 13, 31, 32, 33, 34, 35, 41, 42, 43, 44, 45, 51, 52, 53, 54, 55};
   private int X;
   private int Y;
   private bool stageSixCondition;
   private int stageSixReference;
   private int stageSevenTarget;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;
   private bool colorblindModeEnabled;

   void Awake () {
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;
      Button.OnInteract += delegate () { ButtonPress(); Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Button.transform); return false; };
      Button.OnInteractEnded += delegate () { ButtonRelease(); Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, Button.transform); };
   }

   void ButtonPress() {
      Button.AddInteractionPunch();
      if (buttonHeld || ModuleSolved) {
         return;
      }
      buttonHeld = true;
      HoldTime = (int)Bomb.GetTime();
      StartCoroutine(HoldButton());
      if (stage == 2) {
         if (Rnd.Range(0,2) == 0) {
            stageThreeColor = colorIndex;
            do {
               colorIndex = Rnd.Range(0,27);
            } while (colorIndex == stageThreeColor);
            ButtonColor.material = ButtonColors[colorIndex];
            if (darkerColors.Contains(colorIndex)) {
               ButtonText.color = Color.white;
            } else {
               ButtonText.color = Color.black;
            }
            ColorblindIndicator.text = ButtonColors[colorIndex].name.ToUpper();
            stageFourCondition = true;
            Debug.LogFormat("[The Heptabutton #{0}] While the button is being held, it changes to {1}!", ModuleId, ButtonColors[colorIndex].name);
         } else {
            stageFourCondition = false;
            Debug.LogFormat("[The Heptabutton #{0}] The button is staying the same color while being held.", ModuleId);
         }
      }
   }

   void ButtonRelease() {
      Button.AddInteractionPunch(.5f);
      if (ModuleSolved) {
         return;
      }
      buttonHeld = false;
      ReleaseTime = (int)Bomb.GetTime();
      StartCoroutine(ReleaseButton());
      switch (stage) {
         case 0:
            if (HoldTime % 10 == (ButtonText.text.Length + Bomb.GetSerialNumberNumbers().First()) % 10 && HoldTime - ReleaseTime == 0) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was tapped when the last digit of the timer was {1}. Correct!", ModuleId, HoldTime % 10);
               AdvanceStage();
            } else if (HoldTime % 10 == (ButtonText.text.Length + Bomb.GetSerialNumberNumbers().First()) % 10) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held for {1} second(s) when the last digit of the timer was {2}. Correct time, but the button was supposed to be tapped. Strike.", ModuleId, HoldTime - ReleaseTime, HoldTime % 10);
               Strike();
            } else {
               Debug.LogFormat("[The Heptabutton #{0}] The button was {1} when the last digit of the timer was {2}. That is incorrect. Strike.", ModuleId, (HoldTime - ReleaseTime == 0)?"tapped":"held", HoldTime % 10);
               Strike();
            } break;
         case 1:
            int r = (((stageColors[0]) / 100) + ((stageColors[1]) / 100)) % 3;
            int g = (((stageColors[0]) / 10) % 10 + ((stageColors[1]) / 10) % 10) % 3;
            int b = (stageColors[0] % 10 + stageColors[1] % 10) % 3;
            if (HoldTime % 10 == (r * 3 + g) && (ReleaseTime % 10) % 3 == b) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held on {1} and released on {2}. Correct!", ModuleId, HoldTime % 10, ReleaseTime % 10);
               AdvanceStage();
            } else if (HoldTime % 10 == (r * 3 + g)) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held on {1} and released on {2}. The hold time was correct, but not the release time. Strike.", ModuleId, HoldTime % 10, ReleaseTime % 10);
               Strike();
            } else if ((ReleaseTime % 10) % 3 == b) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held on {1} and released on {2}. The release time was correct, but not the hold time. Strike.", ModuleId, HoldTime % 10, ReleaseTime % 10);
               Strike();
            } else {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held on {1} and released on {2}. Both of those times are incorrect. Strike.", ModuleId, HoldTime % 10, ReleaseTime % 10);
               Strike();
            } break;
         case 2:
            string code = morse[Array.IndexOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), Bomb.GetSerialNumberLetters().First())];
            int length = 0;
            for (int i = 0; i < code.Length; i++) {
               length += (code[i].ToString() == ".")?1:3;
            }
            if (HoldTime - ReleaseTime == length && ReleaseTime % 10 == Bomb.GetSerialNumberNumbers().Last()) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held for {1} second(s), and released when the last digit was {2}. Correct!", ModuleId, HoldTime - ReleaseTime, ReleaseTime % 10);
               AdvanceStage();
            } else if (HoldTime - ReleaseTime == length) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held for {1} second(s), but it was released when the last digit was {2}. Strike.", ModuleId, HoldTime - ReleaseTime, ReleaseTime % 10);
               Strike();
               RevertColor();
            } else if (ReleaseTime % 10 == Bomb.GetSerialNumberNumbers().Last()) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was released when the last digit was {1}, but it was held for {2} second(s). Strike.", ModuleId, ReleaseTime % 10, HoldTime - ReleaseTime);
               Strike();
               RevertColor();
            } else {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held for {1} second(s), and released when the last digit was {2}. Neither of those numbers are correct. Strike.", ModuleId, HoldTime - ReleaseTime, ReleaseTime % 10);
               Strike();
               RevertColor();
            } break;
         case 3:
            if (stageFourCondition) {
               if (HoldTime.ToString().Contains('0') && HoldTime - ReleaseTime == 0) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was tapped at {1} seconds remaining. Correct!", ModuleId, HoldTime);
                  AdvanceStage();
               } else if (HoldTime.ToString().Contains('0')) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held for {1} second(s) at {2} seconds remaining, but it was supposed to be tapped. Strike.", ModuleId, HoldTime - ReleaseTime, HoldTime);
                  Strike();
               } else if (HoldTime - ReleaseTime == 0) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was tapped at {1} seconds remaining, which does not contain the digit 0. Strike.", ModuleId, HoldTime);
                  Strike();
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held for {1} second(s) at {2} seconds remaining. Both of those are incorrect. Strike.", ModuleId, HoldTime - ReleaseTime, HoldTime);
                  Strike();
               }
            } else {
               if (HoldTime % 10 == (Bomb.GetBatteryCount()) % 10 && HoldTime - ReleaseTime >=6 && HoldTime - ReleaseTime <= 8) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, and released after {2} seconds. Correct!", ModuleId, HoldTime % 10, HoldTime - ReleaseTime);
                  AdvanceStage();
               } else if (HoldTime % 10 == (Bomb.GetBatteryCount()) % 10) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, but it was released after {2} second(s). Strike.", ModuleId, HoldTime % 10, HoldTime - ReleaseTime);
                  Strike();
               } else if (HoldTime - ReleaseTime >=6 && HoldTime - ReleaseTime <= 8) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was released after {1} seconds, but it was held when the last digit was {2}. Strike.", ModuleId, HoldTime - ReleaseTime, HoldTime % 10);
                  Strike();
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, and it was released after {2} second(s). Neither of those are correct. Strike.", ModuleId, HoldTime % 10, HoldTime - ReleaseTime);
                  Strike();
               }
            } break;
         case 4:
            if (ButtonText.text.Length % 2 == 1) {
               if (HoldTime % 10 == X && ReleaseTime % 10 == Y) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, and released when the last digit was {2}. Correct!", ModuleId, HoldTime % 10, ReleaseTime % 10);
                  AdvanceStage();
               } else if (HoldTime % 10 == X) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, but it was released when the last digit was {2}. Strike.", ModuleId, HoldTime % 10, ReleaseTime % 10);
                  Strike();
               } else if (ReleaseTime % 10 == Y) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was released when the last digit was {1}, but it was held when the last digit was {2}. Strike.", ModuleId, ReleaseTime % 10, HoldTime % 10);
                  Strike();
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, and released when the last digit was {2}. Neither of those are correct. Strike.", ModuleId, HoldTime % 10, ReleaseTime % 10);
                  Strike();
               }
            } else {
               if (HoldTime % 10 == Y && ReleaseTime % 10 == X) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, and released when the last digit was {2}. Correct!", ModuleId, HoldTime % 10, ReleaseTime % 10);
                  AdvanceStage();
               } else if (HoldTime % 10 == Y) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, but it was released when the last digit was {2}. Strike.", ModuleId, HoldTime % 10, ReleaseTime % 10);
                  Strike();
               } else if (ReleaseTime % 10 == X) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was released when the last digit was {1}, but it was held when the last digit was {2}. Strike.", ModuleId, ReleaseTime % 10, HoldTime % 10);
                  Strike();
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, and released when the last digit was {2}. Neither of those are correct. Strike.", ModuleId, HoldTime % 10, ReleaseTime % 10);
                  Strike();
               }
            } break;
         case 5:
            if (stageSixReference < 8) {
               if (HoldTime % 10 == stageSixReference && HoldTime - ReleaseTime == 0) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was tapped when the last digit was {1}. Correct!", ModuleId, HoldTime % 10);
                  AdvanceStage();
               } else if (HoldTime % 10 == stageSixReference) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, but it was supposed to be tapped. Strike.", ModuleId, HoldTime % 10);
                  Strike();
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was {1} when the last digit was {2}. Strike.", ModuleId, (HoldTime - ReleaseTime == 0)?"tapped":"held", HoldTime % 10);
                  Strike();
               }
            } else {
               if (HoldTime % 10 == stageSixReference % 10 && HoldTime - ReleaseTime == 0) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was tapped when the last digit was {1}. Correct!", ModuleId, HoldTime % 10);
                  AdvanceStage();
               } else if (HoldTime % 10 == stageSixReference % 10) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was held when the last digit was {1}, but it was supposed to be tapped. Strike.", ModuleId, HoldTime % 10);
                  Strike();
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] The button was {1} when the last digit was {2}. Strike.", ModuleId, (HoldTime - ReleaseTime == 0)?"tapped":"held", HoldTime % 10);
                  Strike();
               }
            } break;
         case 6:
            if (HoldTime % 60 == stageSevenTarget && HoldTime - ReleaseTime == 7) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held at {1}{2}{3}{4}, and released after exactly 7 seconds. Well done!", ModuleId, (HoldTime / 60), ":", ((HoldTime % 60) < 10)?"0":"", (HoldTime % 60));
               AdvanceStage();
            } else if (HoldTime % 60 == stageSevenTarget) {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held at {1}{2}{3}{4}, but it was released after {5} second(s). Come on! Strike...", ModuleId, (HoldTime / 60), ":", ((HoldTime % 60) < 10)?"0":"", (HoldTime % 60), HoldTime - ReleaseTime);
               Strike();
            } else if (HoldTime - ReleaseTime == 7) {
               Debug.LogFormat("[The Heptabutton #{0}] Well, the button was held for 7 seconds, but it was held at {1}{2}{3}{4}. Strike...", ModuleId, (HoldTime / 60), ":", ((HoldTime % 60) < 10)?"0":"", (HoldTime % 60));
               Strike();
            } else {
               Debug.LogFormat("[The Heptabutton #{0}] The button was held at {1}{2}{3}{4}, AND it was released after {5} second(s). Strike!", ModuleId, (HoldTime / 60), ":", ((HoldTime % 60) < 10)?"0":"", (HoldTime % 60), HoldTime - ReleaseTime);
               Strike();
            }
         break;
      }
   }

   void Activate () {
      return;
   }

   void Start () {
      ButtonText.text = buttonLabels[Rnd.Range(0, buttonLabels.Length)];
      if (ColorblindMode.ColorblindModeActive) {
         colorblindModeEnabled = true;
         ColorblindIndicator.gameObject.SetActive(colorblindModeEnabled);
      }
      Debug.LogFormat("[The Heptabutton #{0}] Colorblind mode is {1} for this module.", ModuleId, colorblindModeEnabled?"enabled":"disabled");
      Debug.LogFormat("[The Heptabutton #{0}] The button's label is {1}.", ModuleId, ButtonText.text);
      ChangeButtonColor();
      Debug.LogFormat("[The Heptabutton #{0}] Stage 1: The button's label is {1} letters long, and the first digit of the serial number is {2}.", ModuleId, ButtonText.text.Length, Bomb.GetSerialNumberNumbers().First());
      Debug.LogFormat("[The Heptabutton #{0}] Therefore, the button must be tapped when the last digit of the timer is {1}.", ModuleId, (ButtonText.text.Length + Bomb.GetSerialNumberNumbers().First()) % 10);
   }

   void ChangeButtonColor() {
      colorIndex = Rnd.Range(0,27);
      ButtonColor.material = ButtonColors[colorIndex];
      if (darkerColors.Contains(colorIndex)) {
         ButtonText.color = Color.white;
      } else {
         ButtonText.color = Color.black;
      }
      ColorblindIndicator.text = ButtonColors[colorIndex].name.ToUpper();
      stageColors.Add(colorValues[colorIndex]);
      Debug.LogFormat("[The Heptabutton #{0}] The button color for stage {1} is {2}.", ModuleId, stage + 1, ButtonColors[colorIndex].name);
   }

   void AdvanceStage () {
      stage++;
      StageLights[stage - 1].GetComponent<MeshRenderer>().material = ButtonColors[8];
      StageLightEffects[stage - 1].gameObject.SetActive(true);
      if (stage !=7) {
         ChangeButtonColor();
         switch (stage) {
            case 1:
               int r = (((stageColors[0]) / 100) + ((stageColors[1]) / 100)) % 3;
               int g = (((stageColors[0]) / 10) % 10 + ((stageColors[1]) / 10) % 10) % 3;
               int b = (stageColors[0] % 10 + stageColors[1] % 10) % 3;
               Debug.LogFormat("[The Heptabutton #{0}] Stage 2: Adding each RGB value gives {1}, {2}, and {3}.", ModuleId, r, g, b);
               Debug.LogFormat("[The Heptabutton #{0}] Therefore, the button must be held when the last digit is {1}, and released when the last digit, modulo 3, is {2}.", ModuleId, r * 3 + g, b);
            break;
            case 2:
               string code = morse[Array.IndexOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), Bomb.GetSerialNumberLetters().First())];
               int length = 0;
               for (int i = 0; i < code.Length; i++) {
                  length += (code[i].ToString() == ".")?1:3;
               }
               Debug.LogFormat("[The Heptabutton #{0}] Stage 3: The Morse code equivalent of the first character of the serial number is {1}", ModuleId, code);
               Debug.LogFormat("[The Heptabutton #{0}] Therefore, the button must be held for {1} second(s), and released when the last digit is {2}.", ModuleId, length, Bomb.GetSerialNumberNumbers().Last());
            break;
            case 3:
               if (stageFourCondition) {
                  Debug.LogFormat("[The Heptabutton #{0}] Stage 4: Because the button changed color on the previous stage, the button must be tapped when the total number of seconds remaining contains the digit 0.", ModuleId);
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] Stage 4: Because the button did not change color on the previous stage, the button must be held when the last digit is {1}, and released after between 6 and 8 seconds.", ModuleId, (Bomb.GetBatteryCount()) % 10);
               }
            break;
            case 4:
               int rowSum = 0;
               int colSum = 0;
               int taps = 0;
               for (int i = 0; i < ButtonText.text.Length; i++) {
                  taps = tapCode[Array.IndexOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), ButtonText.text.ToCharArray()[i])];
                  rowSum += (taps / 10);
                  colSum += (taps % 10);
               }
               if (rowSum % 7 == 0) {
                  X = 7;
               } else if (rowSum % 11 == 0) {
                  X = (Bomb.GetSerialNumberNumbers().Sum() % 10);
               } else {
                  bool thirdCondition = true;
                  for (int i = 0; i < rowSum.ToString().Length; i++) {
                     if (Bomb.GetSerialNumberNumbers().Join("").Contains(rowSum.ToString()[i])) {
                        thirdCondition = false;
                     }
                  }
                  if (thirdCondition) {
                     X = Math.Abs(Bomb.GetSerialNumberNumbers().First() - Bomb.GetSerialNumberNumbers().Last());
                  } else if (rowSum > colSum) {
                     X = (rowSum + colSum) / 10;
                  } else {
                     X = 0;
                  }
               }
               if (colSum % 7 == 0) {
                  Y = 7;
               } else if (colSum % 11 == 0) {
                  Y = (Bomb.GetSerialNumberNumbers().Sum() % 10);
               } else {
                  bool thirdCondition = true;
                  for (int i = 0; i < colSum.ToString().Length; i++) {
                     if (Bomb.GetSerialNumberNumbers().Join("").Contains(colSum.ToString()[i])) {
                        thirdCondition = false;
                     }
                  }
                  if (thirdCondition) {
                     Y = Math.Abs(Bomb.GetSerialNumberNumbers().First() - Bomb.GetSerialNumberNumbers().Last());
                  } else if (colSum > rowSum) {
                     Y = (colSum + rowSum) / 10;
                  } else {
                     Y = 0;
                  }
               }
               Debug.LogFormat("[The Heptabutton #{0}] Stage 5: The sum of the tap code rows in the button's text is {1}, and the sum of the columns is {2}.", ModuleId, rowSum, colSum);
               Debug.LogFormat("[The Heptabutton #{0}] This makes X equal to {1} and Y equal to {2}.", ModuleId, X, Y);
               if (ButtonText.text.Length % 2 == 1) {
                  Debug.LogFormat("[The Heptabutton #{0}] The button's label has an odd number of letters, so it must be held when the last digit is {1}, and released when the last digit is {2}.", ModuleId, X, Y);
               } else {
                  Debug.LogFormat("[The Heptabutton #{0}] The button's label has an even number of letters, so it must be held when the last digit is {1}, and released when the last digit is {2}.", ModuleId, Y, X);
               }
            break;
            case 5:
               for (int i = 0; i < 5; i++) {
                  if (colorValues[colorIndex] == stageColors[i]) {
                     stageSixCondition = true;
                     stageSixReference = (i + 1);
                     break;
                  }
               }
               if (stageSixCondition) {
                  Debug.LogFormat("[The Heptabutton #{0}] Stage 6: The button was the same color on stage {1} as it is on the current stage.", ModuleId, stageSixReference);
                  Debug.LogFormat("[The Heptabutton #{0}] Therefore, the button must be tapped when the last digit of the timer is {1}.", ModuleId, stageSixReference);
               } else if (ButtonText.text.Length == 7) {
                  stageSixReference = 7;
                  Debug.LogFormat("[The Heptabutton #{0}] Stage 6: The button has not yet been the color that it is showing now, and its label's length is 7.", ModuleId);
                  Debug.LogFormat("[The Heptabutton #{0}] Therefore, the button must be tapped when the last digit is 7.", ModuleId);
               } else {
                  stageSixReference = 0;
                  for (int i = 0; i < ButtonText.text.Length; i++) {
                     stageSixReference += (Array.IndexOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(), ButtonText.text.ToCharArray()[i])) + 1;
                  } 
                  Debug.LogFormat("[The Heptabutton #{0}] Stage 6: The button has not yet been the color that it is showing now, and the sum of the alphabetic positions of the label is {1}.", ModuleId, stageSixReference);
                  Debug.LogFormat("[The Heptabutton #{0}] Therefore, the button must be tapped when the last digit is {1}.", ModuleId, stageSixReference % 10);
               }
            break;
            case 6:
               stageSevenTarget = 0;
               for (int i = 0; i < 7; i++) {
                  stageSevenTarget += (stageColors[i] / 100);
                  stageSevenTarget += ((stageColors[i] / 10) % 10);
                  stageSevenTarget += (stageColors[i] % 10);
               }
               Debug.LogFormat("[The Heptabutton #{0}] Stage 7: The sum of every RGB value of each stage's button color is {1}.", ModuleId, stageSevenTarget);
               stageSevenTarget += (Bomb.GetSerialNumberNumbers().Sum() % 18);
               Debug.LogFormat("[The Heptabutton #{0}] After adding the sum of the serial number digits, modulo 18, the new value is {1}.", ModuleId, stageSevenTarget);
               if (!(Bomb.GetModuleNames().Count().ToString().Contains('7'))) {
                  stageSevenTarget = (60 - stageSevenTarget);
                  Debug.LogFormat("[The Heptabutton #{0}] However, the number of modules does not contain a 7, so the new value is {1}.", ModuleId, stageSevenTarget);
               }
               Debug.LogFormat("[The Heptabutton #{0}] Therefore, the button must be held when the seconds digits are {1}{2}, and released after 7 seconds.", ModuleId, (stageSevenTarget < 10)?"0":"", stageSevenTarget);
            break;
         }
      } else {
         Solve();
      }
   }

   void RevertColor () {
      if (colorIndex == stageThreeColor) {
         return;
      }
      colorIndex = stageThreeColor;
      ButtonColor.material = ButtonColors[colorIndex];
      if (darkerColors.Contains(colorIndex)) {
         ButtonText.color = Color.white;
      } else {
         ButtonText.color = Color.black;
      }
      ColorblindIndicator.text = ButtonColors[colorIndex].name.ToUpper();
      Debug.LogFormat("[The Heptabutton #{0}] Due to the strike, the button changed back to {1}.", ModuleId, ButtonColors[colorIndex].name);
   }

   void Solve () {
      ModuleSolved = true;
      StartCoroutine(SolveAnimation());
   }

   void Strike () {
      Audio.PlaySoundAtTransform(Sounds[1], Button.transform);
      GetComponent<KMBombModule>().HandleStrike();
   }

   IEnumerator SolveAnimation() {
      Audio.PlaySoundAtTransform(Sounds[0], Button.transform);
      ButtonColor.material = ButtonColors[8];
      BackgroundColor.material = ButtonColors[2];
      ButtonText.color = Color.black;
      ButtonText.text = "A";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "AM";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "AMA";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "AMAZ";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "AMAZI";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "AMAZIN";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "AMAZING";
      ButtonColor.material = ButtonColors[25];
      BackgroundColor.material = ButtonColors[6];
      Debug.LogFormat("[The Heptabutton #{0}] Module solved! Amazing!", ModuleId);
      GetComponent<KMBombModule>().HandlePass();
   }

   IEnumerator HoldButton() {
      for (int i = 0; i < 4; i++) {
         Button.transform.localPosition = new Vector3(Button.transform.localPosition.x, Button.transform.localPosition.y - 0.001f, Button.transform.localPosition.z);
         yield return new WaitForSeconds(0.01f);
      }
   }

   IEnumerator ReleaseButton() {
      for (int i = 0; i < 4; i++) {
         Button.transform.localPosition = new Vector3(Button.transform.localPosition.x, Button.transform.localPosition.y + 0.001f, Button.transform.localPosition.z);
         yield return new WaitForSeconds(0.01f);
      }
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"!{0} t (number) [Taps the button at the specified time] | !{0} h (number) [Holds the button at the specified time] | !{0} r (number) [Releases the button at the specified time] | Commands can be chained with semicolons (;). If a time is one digit, it will be considered as the last seconds digit. If it is two digits, it will be considered as the two seconds digits. | !{0} colorblind [Enables colorblind mode]";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToLower();
      yield return null;
      if (ModuleSolved) {
         yield return "sendtochaterror The module is solving.";
         yield break;
      }
      if (Command == "colorblind") {
         if (!colorblindModeEnabled) {
            colorblindModeEnabled = true;
            ColorblindIndicator.gameObject.SetActive(colorblindModeEnabled);
            Debug.LogFormat("[The Heptabutton #{0}] Colorblind mode was enabled by Twitch Plays.", ModuleId);
         }
         yield break;
      }
      string[] Commands = Command.Split(';');
      for (int i = 0; i < Commands.Length; i++) {
         if (!"thr".Contains(Commands[i][0]) || Commands[i][1] != ' ' || Commands[i].Length > 4 || Commands[i].Length < 3 || !"0123456789".Contains(Commands[i][2]) || (Commands[i].Length == 4 && !"0123456789".Contains(Commands[i][3]))) {
            yield return "sendtochaterror Invalid command.";
            yield break;
         }
      }
      for (int i = 0; i < Commands.Length; i++) {
         if (Commands[i][0] == 't') {
            if (Commands[i].Length == 3) {
               while ((((int)Bomb.GetTime() % 60) % 10) != int.Parse(Commands[i][2].ToString())) yield return "trycancel The button press was canceled.";
            } else {
               while (((int)Bomb.GetTime() % 60) != (int.Parse(Commands[i][2].ToString()) * 10 + int.Parse(Commands[i][3].ToString()))) yield return "trycancel The button press was canceled.";
            }
            Button.OnInteract();
            Button.OnInteractEnded();
         } else if (Commands[i][0] == 'h') {
            if (Commands[i].Length == 3) {
               while ((((int)Bomb.GetTime() % 60) % 10) != int.Parse(Commands[i][2].ToString())) yield return "trycancel The button hold was canceled.";
            } else {
               while (((int)Bomb.GetTime() % 60) != (int.Parse(Commands[i][2].ToString()) * 10 + int.Parse(Commands[i][3].ToString()))) yield return "trycancel The button hold was canceled.";
            }
            Button.OnInteract();
         } else if (Commands[i][0] == 'r') {
            if (Commands[i].Length == 3) {
               while ((((int)Bomb.GetTime() % 60) % 10) != int.Parse(Commands[i][2].ToString())) yield return "trycancel The button release was canceled.";
            } else {
               while (((int)Bomb.GetTime() % 60) != (int.Parse(Commands[i][2].ToString()) * 10 + int.Parse(Commands[i][3].ToString()))) yield return "trycancel The button release was canceled.";
            }
            Button.OnInteractEnded();
         } else {
            yield return "sendtochaterror Something went wrong, or the command was invalid.";
            yield break;
         }
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      for (int i = (stage + 1); i < 8; i++) {
         StageLights[i - 1].GetComponent<MeshRenderer>().material = ButtonColors[8];
         StageLightEffects[i - 1].gameObject.SetActive(true);
      }
      ModuleSolved = true;
      Audio.PlaySoundAtTransform(Sounds[0], Button.transform);
      ButtonColor.material = ButtonColors[2];
      BackgroundColor.material = ButtonColors[25];
      ButtonText.color = Color.white;
      ButtonText.text = "C";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "CH";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "CHE";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "CHEA";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "CHEAT";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "CHEATE";
      yield return new WaitForSeconds(.666666f);
      ButtonText.text = "CHEATER";
      ButtonColor.material = ButtonColors[25];
      BackgroundColor.material = ButtonColors[2];
      ButtonText.color = Color.black;
      Debug.LogFormat("[The Heptabutton #{0}] Module autosolved by Twitch Plays.", ModuleId);
      GetComponent<KMBombModule>().HandlePass();
   }
}
