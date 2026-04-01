using CAPYBARA.Model;

namespace CAPYBARA
{
    public static class LoginData
    {
        private static LoginCloudData _cloudLoadData;
        public static LoginCloudData Cloud
        {
            get
            {
                if (_cloudLoadData == null)
                    _cloudLoadData = LoginData.New();

                return _cloudLoadData;
            }
            set
            {
                _cloudLoadData = value;
            }
        }

        public static LoginCloudData New()
        {
            var loginCloudData = new LoginCloudData { };
            return loginCloudData;
        }
    }
}
