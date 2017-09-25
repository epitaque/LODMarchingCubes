using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Console : MonoBehaviour {
    public SE.Debugger Debugger;
    public InputField InputText;
    public Text ConsoleOutput;

    private string Output;
    private string[] Lines;
    private int CurrentLine = 0;
    private int MaxLines = 10;
    private string CurrentText;

    public void Start() {
        Lines = new string[MaxLines];
        for(int i = 0; i < MaxLines; i++) {
            Lines[i] = "";
        }
    }

    public void Update() {
        if(Input.GetKeyDown(KeyCode.Return)) {
            EnterPressed();
        }
    }

    public void EnterPressed() {
        PrintString(" > " + CurrentText);
        ProcessCommand(CurrentText);
    }

    public void ProcessCommand(string commandStr) {
        string[] tokens = commandStr.Split(' ');
        string command = tokens[0];
        if(command == "tnf") {
            if(tokens.Length != 4) {
                PrintString("ERROR: Wrong number of arguments. Arguments are string NodeID, string NeighborNumber, string SnapLevel");
            }
            else {
                UnityEngine.Debug.Log("TestNeighborFetching called with arguments " + tokens[1] + ", " + tokens[2] + ", " + tokens[3]);
                Debugger.TestNeighborFetching(tokens[1], tokens[2], tokens[3]);
            }
        }
        else if(command == "agn") {
            if(tokens.Length != 3) {
                PrintString("ERROR: Wrong number of arguments. Arguments are string NodeID, string NeighborNumber");
            }
            else {
                UnityEngine.Debug.Log("RecursiveGetNeighbor called with arguments " + tokens[1] + ", " + tokens[2]);
                Debugger.RecursiveGetNeighbor(tokens[1], tokens[2]);
            }
        }
        else {
            PrintString("ERROR: Invalid command");
        }
    }
    public void PrintString(string str) {
        UnityEngine.Debug.Log("PrintString called");
        Lines[CurrentLine] = str;
        CurrentLine = (CurrentLine + 1) % MaxLines;
        GenerateOutput();
    }

    private void GenerateOutput() {
        Output = "";
        for(int i = 0; i < MaxLines; i++) {
            Output += Lines[(i + CurrentLine) % MaxLines] + "\n";
        }
        ConsoleOutput.text = Output;
    }

    public void InputChanged(string useless) {
        CurrentText = InputText.text;
    }
}