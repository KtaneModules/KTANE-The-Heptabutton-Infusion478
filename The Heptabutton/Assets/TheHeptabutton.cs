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

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;
   private bool colorblindModeEnabled;

   void Awake () {
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;
      Button.OnInteract += delegate () { ButtonPress(); Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Button.transform); return false; };
      Button.OnInteractEnded += delegate () { ButtonRelease(); Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Button.transform); };
   }

   void ButtonPress() {
      if (buttonHeld) {
         return;
      }
      buttonHeld = true;
      HoldTime = (int)Bomb.GetTime();
      Button.AddInteractionPunch();
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
      buttonHeld = false;
      ReleaseTime = (int)Bomb.GetTime();
      Button.AddInteractionPunch(.5f);
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
            }
         break;
      }
   }

   void Activate () {

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
      if (stage !=8) {
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
      GetComponent<KMBombModule>().HandlePass();
   }

   void Strike () {
      GetComponent<KMBombModule>().HandleStrike();
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
   private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
   }

   IEnumerator TwitchHandleForcedSolve () {
      yield return null;
   }
}
