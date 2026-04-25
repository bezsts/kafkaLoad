using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;
using System.Globalization;
using System.Linq;

namespace KafkaLoad.UI.Behaviors;

public class NumericSpaceSeparatorBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<NumericSpaceSeparatorBehavior, NumericUpDown, bool>("IsEnabled");

    private static readonly NumberFormatInfo SpacedFormat = new()
    {
        NumberGroupSeparator = " ",
        NumberGroupSizes = new[] { 3 },
        NumberDecimalDigits = 0,
        NumberDecimalSeparator = "."
    };

    static NumericSpaceSeparatorBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<NumericUpDown>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(NumericUpDown element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(NumericUpDown element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(NumericUpDown numUpDown, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true) return;
        numUpDown.NumberFormat = SpacedFormat;
        numUpDown.TemplateApplied += OnTemplateApplied;
    }

    private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is not NumericUpDown) return;
        // Use NameScope (populated at template-apply time) instead of visual tree search
        if (e.NameScope.Find("PART_TextBox") is not TextBox textBox) return;
        textBox.TextChanged += ReformatText;
    }

    private static bool _isFormatting;

    private static void ReformatText(object? sender, TextChangedEventArgs e)
    {
        if (_isFormatting || sender is not TextBox textBox) return;
        _isFormatting = true;
        try
        {
            var text = textBox.Text ?? string.Empty;
            var caret = textBox.CaretIndex;

            int digitsBefore = 0;
            for (int i = 0; i < caret && i < text.Length; i++)
                if (char.IsDigit(text[i])) digitsBefore++;

            var digits = new string(text.Where(char.IsDigit).ToArray());
            var formatted = InsertSpaces(digits);

            if (text == formatted) return;

            textBox.Text = formatted;

            int newCaret = 0, counted = 0;
            if (digitsBefore > 0)
            {
                for (int i = 0; i < formatted.Length; i++)
                {
                    if (char.IsDigit(formatted[i]) && ++counted == digitsBefore)
                    {
                        newCaret = i + 1;
                        break;
                    }
                }
            }
            textBox.CaretIndex = Math.Clamp(newCaret, 0, formatted.Length);
        }
        finally
        {
            _isFormatting = false;
        }
    }

    private static string InsertSpaces(string digits)
    {
        if (digits.Length <= 3) return digits;

        int spaces = (digits.Length - 1) / 3;
        var result = new char[digits.Length + spaces];
        int ri = result.Length - 1;
        int groupCount = 0;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            result[ri--] = digits[i];
            groupCount++;
            if (groupCount % 3 == 0 && i > 0)
                result[ri--] = ' ';
        }

        return new string(result);
    }
}
