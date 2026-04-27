using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.UI.ViewModels;

namespace KafkaLoad.UI.Views;

public partial class ConfirmDeleteDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmDeleteDialog(ConfirmDeleteInfo info)
    {
        InitializeComponent();

        var titleText = this.FindControl<TextBlock>("TitleText")!;
        var bodyText = this.FindControl<TextBlock>("BodyText")!;
        var scenarioList = this.FindControl<ItemsControl>("ScenarioList")!;
        var cancelButton = this.FindControl<Button>("CancelButton")!;
        var deleteButton = this.FindControl<Button>("DeleteButton")!;

        titleText.Text = $"Delete {info.ConfigType} config \"{info.ConfigName}\"?";
        bodyText.Text = $"This config is used by {info.AffectedScenarios.Count} test scenario(s):";
        scenarioList.ItemsSource = info.AffectedScenarios;

        cancelButton.Click += (_, _) =>
        {
            Result = false;
            Close();
        };

        deleteButton.Click += (_, _) =>
        {
            Result = true;
            Close();
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
