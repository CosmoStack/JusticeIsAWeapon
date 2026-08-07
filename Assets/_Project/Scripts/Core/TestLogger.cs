using UnityEngine;

public class TestLogger : MonoBehaviour
{
    // This creates a public function the Inspector can actually see and click on
    public void LogMessage(string message)
    {
        Debug.Log(message);
    }
}