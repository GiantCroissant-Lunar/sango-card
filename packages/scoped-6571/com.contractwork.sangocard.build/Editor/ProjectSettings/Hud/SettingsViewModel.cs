#if HAS_NUGET_SYSTEM_REACTIVE
using ReactiveUI;
using System.Reactive;
#endif

namespace SangoCard.Build.Editor.ProjectSettings;

internal partial class SettingsViewModel
#if HAS_NUGET_SYSTEM_REACTIVE
    : ReactiveObject
#endif
{
    private string _inputText;

    public SettingsViewModel()
    {
#if HAS_NUGET_SYSTEM_REACTIVE
        ClearCommand = ReactiveCommand.Create(() => InputText = string.Empty);
#endif
    }

    public string InputText
    {
        get => _inputText;
#if HAS_NUGET_SYSTEM_REACTIVE
        set => this.RaiseAndSetIfChanged(ref _inputText, value);
#else
        set
        {
            if (_inputText != value)
            {
                _inputText = value;
                // Notify property change if necessary
            }
        }
#endif
    }

#if HAS_NUGET_SYSTEM_REACTIVE
    public ReactiveCommand<Unit, string> ClearCommand { get; }
#endif
}
