using UnityEngine;
using TMPro;
using System.Diagnostics;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class MoveGenerationTester : MonoBehaviour
{
    [SerializeField] private TMP_InputField depthInputField;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Toggle detailedToggle;

    [SerializeField] private MoveGenerator moveGenerator;

    public void RunTest()
    {
        if (!int.TryParse(depthInputField.text, out int depth))
        {
            resultText.text = "Enter a number!";
            return;
        }

        int detailedDepth = detailedToggle.isOn ? depth : 0;
        string movesPositions = "";

        resultText.text = "Calculating...";

        Stopwatch stopwatch = Stopwatch.StartNew();

        int positions = moveGenerator.MoveGenerationTest(depth, ref movesPositions, detailedDepth);

        stopwatch.Stop();

        resultText.text = $"Games: {positions:N0}\n" + 
                          $"Time: {stopwatch.ElapsedMilliseconds} ms";

        if (detailedToggle.isOn)
        {
            Debug.Log(movesPositions);
        }
    }
}