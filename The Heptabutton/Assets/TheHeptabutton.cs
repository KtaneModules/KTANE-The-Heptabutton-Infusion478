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

   public KMSelectable Button;
   public TextMesh ButtonText;
   public Material[] ButtonColors;
   public Renderer ButtonColor;

   private int colorIndex;
   private List<int> darkerColors = new List<int> {2, 3, 6, 9, 13, 16, 19, 20, 21, 23, 24};
   private string[] buttonLabels = {"PUSH", "HOLD", "BOOM", "WAIT", "LOOK", "FAIL", "DONE", "PRESS", "ABORT", "TOUCH", "CLICK", "AVOID", "SOLVE", "PUNCH", "SELECT", "BUTTON", "IGNITE", "LAUNCH", "DEFUSE", "DISARM", "IGNORE", "RELEASE", "EXPLODE", "DEPRESS", "CONTROL", "NEGLECT", "ACHIEVE", "POLYGON"};

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   void Awake () {
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;
      Button.OnInteract += delegate () { ButtonPress(); Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Button.transform); return false; };
      Button.OnInteractEnded += delegate () { ButtonRelease(); Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Button.transform); };
   }

   void ButtonPress() {
      Debug.Log("The Heptabutton has been pressed.");
   }

   void ButtonRelease() {
      Debug.Log("The Heptabutton has been released.");
   }

   void Activate () {

   }

   void Start () {
      ButtonText.text = buttonLabels[Rnd.Range(0, buttonLabels.Length)];
      Debug.LogFormat("[The Heptabutton #{0}] The button's label is {1}.", ModuleId, ButtonText.text);
      ChangeButtonColor();
   }

   void ChangeButtonColor() {
      colorIndex = Rnd.Range(0,27);
      ButtonColor.material = ButtonColors[colorIndex];
      if (darkerColors.Contains(colorIndex)) {
         ButtonText.color = Color.white;
      } else {
         ButtonText.color = Color.black;
      }
      Debug.LogFormat("[The Heptabutton #{0}] The button color for stage 1 is {1}.", ModuleId, ButtonColors[colorIndex].name);
   }

   void Solve () {
      GetComponent<KMBombModule>().HandlePass();
   }

   void Strike () {
      GetComponent<KMBombModule>().HandleStrike();
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
