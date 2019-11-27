using KModkit;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ValvesScript : MonoBehaviour
{
    public KMBombModule module;

    public KMBombInfo info;

    public KMAudio valveAudio;

    public KMSelectable[] valves;

    public GameObject[] valveMatObjects;

    public Material[] valveMatOptions;

    public GameObject[] valveColorObjects;

    public Material[] valveColorOptions;

    private string[] table = new string[36]
    {
        "110",
        "111",
        "001",
        "000",
        "100",
        "000",
        "001",
        "011",
        "010",
        "110",
        "011",
        "100",
        "001",
        "011",
        "100",
        "000",
        "110",
        "111",
        "011",
        "111",
        "110",
        "010",
        "100",
        "000",
        "010",
        "011",
        "010",
        "110",
        "001",
        "111",
        "000",
        "001",
        "111",
        "010",
        "101",
        "110"
    };

    private string solution;

    private static int _moduleIdCounter = 1;

    private int _moduleID;

    private int valveMatNum;

    private int[] valvesColorNum;

    private string[] valveStatus = new string[3]
    {
        "0",
        "0",
        "0"
    };

    private bool animationInProgress;

    private bool timerStarted;

    private bool bombSolved;

    private bool timerEnded;

    private bool unicorn;

    private string TwitchHelpMessage = "Use “!{0} toggle 1 2 3” to toggle each valve. They can be toggled multiple times in a command (Do the whole answer in one command, so you won't get strikes).";

    private static string[] supportedTwitchCommands = new string[3]
    {
        "press",
        "click",
        "submit"
    };

    private void Awake()
    {
        valveMatNum = Random.Range(0, 2);
        valvesColorNum = new int[3]
        {
            Random.Range(0, 2),
            Random.Range(0, 2),
            Random.Range(0, 2)
        };
    }

    private void Start()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < valveMatObjects.Length; i++)
        {
            valveMatObjects[i].GetComponent<Renderer>().material = valveMatOptions[valveMatNum];
        }
        for (int j = 0; j < valveColorObjects.Length; j++)
        {
            valveColorObjects[j].GetComponent<Renderer>().material = valveColorOptions[valvesColorNum[j]];
        }
        for (int k = 0; k < valves.Length; k++)
        {
            KMSelectable selectable = valves[k];
            selectable.OnInteract += ValvePressed(k);
        }
        solution = FindSolution();
        solution = EditSolution();
    }

    private KMSelectable.OnInteractHandler ValvePressed(int valveNumber)
    {
        return delegate
        {
            PressValve(valveNumber);
            return false;
        };
    }

    private void PressValve(int valveNumber)
    {
        if (!bombSolved && !animationInProgress)
        {
            if (!timerStarted)
            {
                StartCoroutine(Timer());
            }
            if (valveStatus[valveNumber] == "0")
            {
                valveStatus[valveNumber] = "1";
                StartCoroutine(ButtonDownAnimation(valveNumber));
            }
            else
            {
                valveStatus[valveNumber] = "0";
               StartCoroutine(ButtonUpAnimation(valveNumber));
            }
        }
    }

    private string FindSolution()
    {
        int num = info.GetSerialNumberNumbers().Sum();
        int currentSolutionIndex = num - 1;
        if (num == 0)
        {
            DebugLog("The sum of the digits in the serial number is 0! Correct answer is 101.");
            unicorn = true;
            return "101";
        }
        string currentSolution = table[currentSolutionIndex];
        DebugLog("The starting combination is {0}.", currentSolution);
        for (int i = 0; i < 6; i++)
        {
            char c = info.GetSerialNumber()[i];
            int num2 = 0;
            int num3 = 0;
            bool flag;
            int num4;
            if (char.IsLetter(c))
            {
                flag = true;
                num4 = (char.ToUpper(c) - 64) % 10;
            }
            else
            {
                flag = false;
                num4 = -int.Parse(c.ToString());
            }
            DebugLog("Character {0} of the serial number is {1}. Move {2} {3} {4}.", i + 1, c, (!flag) ? "up" : "down", Mathf.Abs(num4), (Mathf.Abs(num4) != 1) ? "times" : "time");
            currentSolutionIndex = (currentSolutionIndex + num4 + 36) % 36;
            DebugLog("The combination is {0}", table[currentSolutionIndex]);
            int num5 = (from ix in Enumerable.Range(0, 3)
                        where currentSolution[ix] == table[currentSolutionIndex][ix]
                        select ix).Count();
            if (num5 == 0)
            {
                while (num5 == 0 && num3 < 100)
                {
                    DebugLog("No positions are matching. Move {0} a space.", (!flag) ? "up" : "down");
                    currentSolutionIndex += (flag ? 1 : (-1));
                    num5 = (from ix in Enumerable.Range(0, 3)
                            where currentSolution[ix] == table[currentSolutionIndex][ix]
                            select ix).Count();
                    DebugLog("The combination is {0}", table[currentSolutionIndex]);
                    num3++;
                }
            }
            switch (num5)
            {
                case 1:
                    num2 = (from ix in Enumerable.Range(0, 3)
                            where currentSolution[ix] == table[currentSolutionIndex][ix]
                            select ix).ToArray()[0];
                    break;
                case 2:
                    num2 = (from ix in Enumerable.Range(0, 3)
                            where currentSolution[ix] != table[currentSolutionIndex][ix]
                            select ix).ToArray()[0];
                    break;
                case 3:
                    DebugLog("All 3 positions match! Correct answer is {0}.", currentSolution);
                    return currentSolution;
            }
            string str = (num2 != 0) ? currentSolution[0].ToString() : ((int.Parse(currentSolution[0].ToString()) + 1) % 2).ToString();
            string str2 = (num2 != 1) ? currentSolution[1].ToString() : ((int.Parse(currentSolution[1].ToString()) + 1) % 2).ToString();
            string str3 = (num2 != 2) ? currentSolution[2].ToString() : ((int.Parse(currentSolution[2].ToString()) + 1) % 2).ToString();
            currentSolution = str + str2 + str3;
            DebugLog("{0} positions are matching. Invert position {1}. The current combination is now {2}", num5, num2 + 1, currentSolution);
        }
        DebugLog("No more serial numbers characters left! Correct answer is {0}", currentSolution);
        return currentSolution;
    }

    private string EditSolution()
    {
        if (unicorn)
        {
            DebugLog("Valve 1 should be pressed, Valve 2 should not be pressed, and Valve 3 should be pressed.");
            return "101";
        }
        DebugLog("The material of the valves is {0}.", (valveMatNum != 1) ? "brass" : "silver");
        DebugLog("The colors on the valves in order are {0}, {1}, and {2}.", (valvesColorNum[0] != 1) ? "white" : "black", (valvesColorNum[1] != 1) ? "white" : "black", (valvesColorNum[2] != 1) ? "white" : "black");
        string text = solution;
        string str;
        string str2;
        string str3;
        if (valveMatNum == 1)
        {
            str = ((int.Parse(text[0].ToString()) + 1) % 2).ToString();
            str2 = ((int.Parse(text[1].ToString()) + 1) % 2).ToString();
            str3 = ((int.Parse(text[2].ToString()) + 1) % 2).ToString();
            text = str + str2 + str3;
        }
        str = ((valvesColorNum[0] != 1) ? text[0].ToString() : ((int.Parse(text[0].ToString()) + 1) % 2).ToString());
        str2 = ((valvesColorNum[1] != 1) ? text[1].ToString() : ((int.Parse(text[1].ToString()) + 1) % 2).ToString());
        str3 = ((valvesColorNum[2] != 1) ? text[2].ToString() : ((int.Parse(text[2].ToString()) + 1) % 2).ToString());
        text = str + str2 + str3;
        DebugLog("Valve 1 should {0}be pressed, Valve 2 should {1}be pressed, and Valve 3 should {2}be pressed.", (!(str == "1")) ? "not " : string.Empty, (!(str2 == "1")) ? "not " : string.Empty, (!(str3 == "1")) ? "not " : string.Empty);
        return text;
    }

    private void SubmitAnswer()
    {
        string text = valveStatus[0] + valveStatus[1] + valveStatus[2];
        timerEnded = true;
        if (solution == text)
        {
            DebugLog("You inputted {0}.", text);
            DebugLog("Module solved!");
            module.HandlePass();
            for (int i = 0; i < valveColorObjects.Length; i++)
            {
                valveColorObjects[i].GetComponent<Renderer>().material = valveColorOptions[0];
            }
            valveAudio.PlaySoundAtTransform("fanfare", transform);
            bombSolved = true;
        }
        else
        {
            DebugLog("You inputted {0}.", text);
            DebugLog("Strike!");
            module.HandleStrike();
        }
        ResetModule();
    }

    private void ResetModule()
    {
        for (int i = 0; i < valves.Length; i++)
        {
            if (valveStatus[i] == "1")
            {
                valveStatus[i] = "0";
                StartCoroutine(ButtonUpAnimation(i));
            }
        }
    }

    private IEnumerator Timer()
    {
        timerStarted = true;
        yield return new WaitForSeconds(3f);
        timerStarted = false;
        animationInProgress = true;
        SubmitAnswer();
        yield return new WaitForSeconds(1f);
        animationInProgress = false;
    }

    private IEnumerator ButtonDownAnimation(int valveNumber)
    {
        animationInProgress = true;
        KMSelectable valve = valves[valveNumber];
        for (int i = 0; i < 8; i++)
        {
            Transform transform = valve.transform;
            Vector3 localPosition = transform.localPosition;
            float x = localPosition.x;
            float y = localPosition.y - 0.0025f;
            float z = localPosition.z;
            transform.localPosition = new Vector3(x, y, z);
            yield return new WaitForSeconds(0.01f);
        }
        valveAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, valve.transform);
        valve.AddInteractionPunch();
        animationInProgress = false;
    }

    private IEnumerator ButtonUpAnimation(int valveNumber)
    {
        animationInProgress = true;
        KMSelectable valve = valves[valveNumber];
        for (int i = 0; i < 8; i++)
        {
            Transform transform = valve.transform;
            Vector3 localPosition = transform.localPosition;
            float x = localPosition.x;
            float y = localPosition.y + 0.0025f;
            float z = localPosition.z;
            transform.localPosition = new Vector3(x, y, z);
            yield return new WaitForSeconds(0.01f);
        }
        animationInProgress = false;
    }

    private IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parts = command.ToLowerInvariant().Split(new char[1]
        {
            ' '
        }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && parts[0] == "toggle" && parts.Skip(1).All((string part) => "123".Contains(part) && part.Length == 1) && parts.Length <= 10)
        {
            yield return null;
            for (int i = 0; i < parts.Skip(1).Count(); i++)
            {
                yield return new WaitUntil(() => !animationInProgress);
                PressValve(int.Parse(parts.Skip(1).ToArray()[i]) - 1);
                yield return new WaitForSeconds(0.2f);
            }
            yield return (!(solution == valveStatus[0] + valveStatus[1] + valveStatus[2])) ? "strike" : "solve";
        }
        else if (parts.Length > 10)
        {
            yield return "sendtochaterror Are you serious? Put in something a little shorter next time :)";
        }
    }

    private void DebugLog(string log, params object[] args)
    {
        string text = string.Format(log, args);
        Debug.LogFormat("[Valves #{0}] {1}", _moduleID, text);
    }
}