using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using QuestPDF.Fluent;

namespace BlueMax.Presentation.Wpf.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _busyMessage = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>True while an async operation is running (drives the busy indicator).</summary>
    public virtual bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Optional message shown next to the busy indicator.</summary>
    public virtual string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    /// <summary>Shortcut for <see cref="Resources.Translations.Get(string)"/>.</summary>
    protected static string T(string key) => Resources.Translations.Get(key);

    /// <summary>True when the string contains Arabic characters (needs RTL rendering).</summary>
    protected static bool IsArabicText(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        foreach (var c in value)
        {
            if (c >= 0x0600 && c <= 0x06FF)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a text span whose direction follows the content (Arabic = RTL, otherwise LTR).
    /// Prevents QuestPDF from garbling mixed Arabic/Latin runs.
    /// </summary>
    protected static QuestPDF.Fluent.TextSpanDescriptor SpanOf(QuestPDF.Fluent.TextDescriptor text, string content)
        => IsArabicText(content)
            ? text.Span(content).DirectionFromRightToLeft()
            : text.Span(content).DirectionFromLeftToRight();

    /// <summary>
    /// Emits a right-aligned row that renders one Arabic label / value pair correctly.
    /// The value is placed first (left) and the RTL label second (right), so that reading
    /// right-to-left yields "label: value". QuestPDF 2024.3 otherwise mirrors the pair when
    /// the value is strongly LTR.
    /// </summary>
    protected static void AddRtlPairRow(QuestPDF.Infrastructure.IContainer container, string label, string value,
        float labelSize = 9, string labelColor = "#6B7280",
        float valueSize = 9, string valueColor = "#1F2937", bool valueBold = false)
    {
        container.Row(row =>
        {
            row.RelativeItem();
            var valueText = row.AutoItem().Text(value).FontSize(valueSize).FontColor(valueColor);
            if (IsArabicText(value))
                valueText.DirectionFromRightToLeft();
            else
                valueText.DirectionFromLeftToRight();
            if (valueBold)
                valueText.Bold();
            row.AutoItem().Text(label + ": ").DirectionFromRightToLeft().FontSize(labelSize).FontColor(labelColor);
        });
    }

    /// <summary>
    /// Renders a letterhead contact line as right-aligned rows of direction-aware pairs
    /// (RTL label, value), separated by " - ", so Arabic labels are not scrambled when paired
    /// with numbers/Latin text. The items are split across at most two rows to avoid the
    /// QuestPDF 2024.3 bug that garbles wrapped RTL text.
    /// </summary>
    protected static void BuildHeaderContactLine(QuestPDF.Infrastructure.IContainer container, IReadOnlyList<(string Label, string Value)> items)
    {
        if (items.Count == 0)
            return;
        var first = new List<(string Label, string Value)>();
        var second = new List<(string Label, string Value)>();
        for (var i = 0; i < items.Count; i++)
        {
            if (i < 3)
                first.Add(items[i]);
            else
                second.Add(items[i]);
        }
        container.Column(col =>
        {
            col.Item().Row(row => AddContactRowItems(row, first));
            if (second.Count > 0)
                col.Item().PaddingTop(2).Row(row => AddContactRowItems(row, second));
        });
    }

    /// <summary>
    /// Emits the reversed (RTL visual order) items of one contact-line row.
    /// </summary>
    static void AddContactRowItems(QuestPDF.Fluent.RowDescriptor row, IReadOnlyList<(string Label, string Value)> items)
    {
        row.RelativeItem();
        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (i != items.Count - 1)
                row.AutoItem().Text("    -    ").FontSize(9).FontColor("#6B7280");
            var (label, value) = items[i];
            var valueText = row.AutoItem().Text(value).FontSize(9).FontColor("#6B7280");
            if (IsArabicText(value))
                valueText.DirectionFromRightToLeft();
            else
                valueText.DirectionFromLeftToRight();
            if (label.Length > 0)
                row.AutoItem().Text(label + ": ").DirectionFromRightToLeft().FontSize(9).FontColor("#6B7280");
        }
    }

    protected bool SetBusy(string message)
    {
        BusyMessage = message;
        return IsBusy = true;
    }

    protected void SetIdle()
    {
        BusyMessage = "";
        IsBusy = false;
    }

    /// <summary>
    /// Runs an async operation with the busy indicator enabled, clearing it when done.
    /// </summary>
    protected async Task RunBusyAsync(string message, System.Func<Task> action)
    {
        if (IsBusy)
            return;
        SetBusy(message);
        try
        {
            await action();
        }
        finally
        {
            SetIdle();
        }
    }

    /// <summary>
    /// Runs a synchronous operation on a background thread with the busy indicator enabled.
    /// </summary>
    protected Task RunBusyAsync(string message, System.Action action)
        => RunBusyAsync(message, () => { action(); return Task.CompletedTask; });

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
