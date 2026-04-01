using UnityEngine;

namespace CAPYBARA
{
    public class CPSelector : MonoBehaviour
    {
        public GameObject[] activepanels = null;
        public GameObject[] inactivepanels = null;

        public void Show(int index)
        {
            for (int i = 0; i < activepanels.Length; i++)
            {
                activepanels[i].SetActive(index == i);
                if (i < inactivepanels.Length)
                    inactivepanels[i].SetActive(index != i);
            }
        }
    }

}
