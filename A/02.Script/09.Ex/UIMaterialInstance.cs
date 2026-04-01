using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIMaterialInstance : MonoBehaviour
{
    public Material sourceMaterial;

    Graphic graphic;
    Material inst;

    void OnEnable()
    {
        graphic = GetComponent<Graphic>();

        if (inst == null)
        {
            var baseMat = sourceMaterial != null ? sourceMaterial : graphic.material;
            if (baseMat != null)
            {
                inst = Instantiate(baseMat);
                graphic.material = inst;
            }
        }
        else
        {
            // 재활성화 시에도 다시 물려주기
            graphic.material = inst;
        }
    }

    void OnDisable()
    {

    }
}
