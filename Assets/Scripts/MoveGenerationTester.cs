using UnityEngine;
using TMPro;
using System.Diagnostics;

public class MoveGenerationTester : MonoBehaviour
{
    [SerializeField] private TMP_InputField depthInputField;
    [SerializeField] private TMP_Text resultText;

    [SerializeField] private MoveGenerator moveGenerator;

    public void RunTest()
    {
        if (!int.TryParse(depthInputField.text, out int depth))
        {
            resultText.text = "Enter a number!";
            return;
        }

        resultText.text = "Calculating...";

        Stopwatch stopwatch = Stopwatch.StartNew();

        int positions = moveGenerator.MoveGenerationTest(depth);

        stopwatch.Stop();

        resultText.text = $"Depth: {depth}\n" +
                          $"Games: {positions:N0}\n" + 
                          $"Time: {stopwatch.ElapsedMilliseconds} ms";
    }
}