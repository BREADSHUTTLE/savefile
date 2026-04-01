using UnityEngine;
using UnityEngine.UI;


namespace CAPYBARA
{
    
    public class ViewCanvasProfileWindow : MonoBehaviour
    {
        public enum TopBtnType{profile=0,record }
        public enum MidBtnType { today= 0, total }
        public Button MyProfileBtn;
        public Button recordBtn;

        public GameObject MyProfileOnObj;
        public GameObject MyrecordOnObj;

        public GameObject MyProfileOffObj;
        public GameObject MyrecordOffObj;

        public GameObject MyProfileWindow;
        public GameObject MyrecordWindow;

        public Button todayBtn;
        public Button totalBtn;

        public GameObject todayOnObj;
        public GameObject totalOnObj;

        public GameObject todayOffObj;
        public GameObject totalOffObj;

        public GameObject todayWindow;
        public GameObject totalWindow;

        TopBtnType topBtnType;
        MidBtnType midBtnType;

        public void Awake()
        {
            MyProfileBtn.onClick.AddListener(()=> OnClickTopBtn(TopBtnType.profile));
            recordBtn.onClick.AddListener(() => OnClickTopBtn(TopBtnType.record));

            todayBtn.onClick.AddListener(()=> OnClickMidBtn(MidBtnType.today));
            totalBtn.onClick.AddListener(() => OnClickMidBtn(MidBtnType.total));


            topBtnType = TopBtnType.profile;
            midBtnType = MidBtnType.today;

            OnClickTopBtn(topBtnType);
            OnClickMidBtn(midBtnType);
        }
        public void OnClickTopBtn(TopBtnType _type)
        {
            topBtnType=_type;

            MyProfileOnObj.SetActive(topBtnType==TopBtnType.profile);
            MyrecordOnObj.SetActive(topBtnType == TopBtnType.record);

            MyProfileOffObj.SetActive(topBtnType == TopBtnType.record);
            MyrecordOffObj.SetActive(topBtnType == TopBtnType.profile);

            MyProfileWindow.SetActive(topBtnType == TopBtnType.profile);
            MyrecordWindow.SetActive(topBtnType == TopBtnType.record);
        }
   

        public void OnClickMidBtn(MidBtnType _type)
        {
            midBtnType = _type;

            todayOnObj.SetActive(midBtnType == MidBtnType.today);
            totalOnObj.SetActive(midBtnType == MidBtnType.total);

            todayOffObj.SetActive(midBtnType == MidBtnType.total);
            totalOffObj.SetActive(midBtnType == MidBtnType.today);

            todayWindow.SetActive(midBtnType == MidBtnType.today);
            totalWindow.SetActive(midBtnType == MidBtnType.total);
        }
    }
}
