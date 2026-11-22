using System;
using System.Threading.Tasks;

namespace TraduSaga.Dialog.Stages
{
    public class ActionInteractionStage : InteractionStage
    {
        private readonly Func<Task> _actionStage;

        public ActionInteractionStage(DialogService dialogService, Func<Task> actionStage, string stageName = null) : base(dialogService, stageName)
        {
            this._actionStage = actionStage;
        }

        protected override async Task<InteractionStage> ExecuteInner()
        {
            await _actionStage.Invoke();

            return NextStage;
        }
    }
}
