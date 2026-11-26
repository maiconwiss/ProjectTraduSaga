using System.Windows.Media;
using TraduSaga.Services;

namespace TraduSaga.Security.Tests;

public class ScreenColorPickerStateTests
{
    [Fact]
    public void HoverUpdatesPreviewWithoutCommittingSelection()
    {
        var state = new ScreenColorPickerState();
        var hovered = Color.FromRgb(9, 8, 7);

        Assert.True(state.Preview(hovered));

        Assert.Equal(hovered, state.PreviewColor);
        Assert.Null(state.SelectedColor);
        Assert.False(state.Commit());
    }

    [Fact]
    public void CommitUsesLatestPreviewColor()
    {
        var state = new ScreenColorPickerState();
        var first = Color.FromRgb(1, 2, 3);
        var latest = Color.FromRgb(4, 5, 6);
        state.BeginDrag();

        Assert.True(state.Preview(first));
        Assert.True(state.Preview(latest));
        Assert.True(state.Commit());

        Assert.False(state.IsDragging);
        Assert.Equal(latest, state.SelectedColor);
    }

    [Fact]
    public void CancelDiscardsPreviewWithoutSelectingOrFailing()
    {
        var state = new ScreenColorPickerState();
        state.BeginDrag();
        state.Preview(Color.FromRgb(1, 2, 3));

        state.Cancel();

        Assert.False(state.IsDragging);
        Assert.Null(state.PreviewColor);
        Assert.Null(state.SelectedColor);
        Assert.False(state.SamplingFailed);
        Assert.False(state.Commit());
    }

    [Fact]
    public void FailedSampleCannotBeCommitted()
    {
        var state = new ScreenColorPickerState();
        state.BeginDrag();
        state.Preview(Color.FromRgb(1, 2, 3));

        state.Fail();

        Assert.False(state.IsDragging);
        Assert.True(state.SamplingFailed);
        Assert.Null(state.SelectedColor);
        Assert.False(state.Commit());
    }

    [Fact]
    public void CancelRestoresOriginalColorAndInvalidHexDraft()
    {
        var original = Color.FromRgb(10, 20, 30);
        var draft = new ScreenColorSelectionDraft(original, "#12GG56", false);
        draft.Preview(Color.FromRgb(4, 5, 6));

        draft.Cancel();

        Assert.Equal(original, draft.Color);
        Assert.Equal("#12GG56", draft.HexText);
        Assert.False(draft.IsValid);
    }
}
