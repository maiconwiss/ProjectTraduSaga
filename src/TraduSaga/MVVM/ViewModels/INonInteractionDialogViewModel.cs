using System;

namespace TraduSaga.MVVM.ViewModels
{
    public interface INonInteractionDialogViewModel
    {
        bool IsClosed { get; }

        event EventHandler DialogIsClosed;
    }
}
