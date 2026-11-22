namespace TraduSaga.MVVM.Common
{
    public class OpenLinkCommand : CommandBase<OpenLinkCommand>
    {
        public override bool CanExecute(object parameter)
        {
            return SafeExternalUrlPolicy.IsAllowed(parameter?.ToString());
        }

        public override void Execute(object parameter)
        {
            if (parameter?.ToString() is string url && SafeExternalUrlPolicy.IsAllowed(url))
            {
                SafeExternalUrlPolicy.Open(url);
            }
        }
    }
}
