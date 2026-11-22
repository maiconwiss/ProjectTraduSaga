using TraduSaga.Infrastructure.Constants;

namespace TraduSaga.MVVM.ViewModels
{
    public class AboutDialogViewModel
    {
        public string ApplicationVersion { get; set; } = "v" + Global.GetVersion();

        public AboutDialogViewModel()
        {
        }
    }
}
