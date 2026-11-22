using System.Windows;
using TraduSaga.MVVM.Common;
using TraduSaga.MVVM.ViewModels;

namespace TraduSaga.Dialog.Stages
{
    public class DialogQuestionInteractionStage : ConditionalInteractionStage
    {
        public DialogQuestionInteractionStage(DialogService dialogService, string stageQuestion) : base(dialogService, async () =>
                (await dialogService.ShowDialogAsync(SimpleDialogViewModel.Create(stageQuestion, SimpleDialogTypes.Question))) == MessageBoxResult.OK)
        {
        }
    }
}
