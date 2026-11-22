using SharpDX.XInput;
using System;
using TraduSaga.HotKeys;

namespace TraduSaga.Services
{
    public interface IControllerService
    {
        ObservablePipe<Keystroke> EventPipe { get; }

        bool IsListening { get; }

        bool TryChangeListenState(bool enabled);
    }
}
