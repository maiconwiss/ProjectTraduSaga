using System;

namespace TraduSaga.MVVM.Common
{
    public interface IAdditionalPanelController
    {
        event EventHandler<bool> PanelStateIsChanged;

        void ClosePanel();
    }
}
