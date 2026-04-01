using Cysharp.Threading.Tasks;

namespace CAPYBARA
{
    public class ControllerNotification
    {
        ViewNotification DMReqView;
        ViewNotification announceMentView;

        public ControllerNotification(ViewNotification reqView, ViewNotification anview)
        {
            DMReqView = reqView;
            announceMentView = anview;

            DMReqView.Init();
            announceMentView.Init();

            CAPYBARA.CPPlayer.Noti.notiEvent += DMReqView.NotiStart;
            CAPYBARA.CPPlayer.Noti.notiEvent += announceMentView.NotiStart;
        }
    }
}