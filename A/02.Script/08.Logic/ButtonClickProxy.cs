using UnityEngine;
using UnityEngine.UI;

public class ButtonClickProxy : MonoBehaviour
{
    public Button[] targets;

    public void ClickAllTargetsInOrder()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].onClick.Invoke();
            }
        }
    }
}
