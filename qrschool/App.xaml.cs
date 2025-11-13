using qrschool.Pages;
using qrschool.Service;

namespace qrschool
{
    public partial class App : Application
    {
        public App(MainPage mainPage)
        {
            InitializeComponent();
            MainPage = new NavigationPage(MainPage);
        }
    }
}
