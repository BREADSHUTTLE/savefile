using UnityEngine;
using AdvancedInputFieldPlugin;    // 플러그인 네임스페이스

public class ChatInputController : MonoBehaviour
{
    [Header("Advanced Input Field 컴포넌트")]
    public AdvancedInputField advancedInputField;


    void Update()
    {
        // 포커스 되어 있는 동안 항상 UI가 최신으로 렌더링되도록 강제 갱신
        if (advancedInputField.Selected)
        {
            advancedInputField.UpdateCaretPosition();
        }
    }
}
