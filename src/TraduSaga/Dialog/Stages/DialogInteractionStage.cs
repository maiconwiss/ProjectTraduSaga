using TraduSaga.MVVM.Common;
using TraduSaga.MVVM.ViewModels;

namespace TraduSaga.Dialog.Stages
{
    public class DialogInteractionStage : ActionInteractionStage
    {
        public DialogInteractionStage(DialogService dialogService, string dialogMessage) : base(dialogService, 
            () => dialogService.ShowDialogAsync(SimpleDialogViewModel.Create(dialogMessage, SimpleDialogTypes.Info)))
        {
        }
    }
}
