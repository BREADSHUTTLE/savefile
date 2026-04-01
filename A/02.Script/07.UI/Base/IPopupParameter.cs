
namespace CAPYBARA
{
    public interface IPopupParameter { }

    public enum ToastPopupType
    {
        Top,Mid,Bottom,
    }
    public class ToastPopupParameter : IPopupParameter
    {
        public ToastPopupType toastPopupType;
    }
}