using System.Windows.Input;
using TraduSaga.HotKeys;

namespace TraduSaga.Security.Tests;

public class HotkeysConfigurationTests
{
    [Fact]
    public void DefaultsMatchHotkeysReferenceMappings()
    {
        var defaults = HotKeysConfiguration.Default;

        AssertHotkey(defaults.ChatVisibilityKey, Key.Oem3, KeyModifier.None);
        AssertHotkey(defaults.SettingVisibilityKey, Key.G, KeyModifier.Alt);
        AssertHotkey(defaults.SelectAreaKey, Key.Q, KeyModifier.Alt);
        AssertHotkey(defaults.TranslationStateKey, Key.T, KeyModifier.Alt);
        AssertHotkey(defaults.ShowSelectionAreaKey, Key.Y, KeyModifier.Alt);
        AssertHotkey(defaults.OnceTranslateKey, Key.F, KeyModifier.Shift);
        AssertHotkey(defaults.WindowStyleChangeKey, Key.T, KeyModifier.Ctrl);
    }

    private static void AssertHotkey(HotKeyInfo hotkey, Key key, KeyModifier modifier)
    {
        Assert.Equal(key, hotkey.Key);
        Assert.Equal(modifier, hotkey.KeyModifier);
    }
}
