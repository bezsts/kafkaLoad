using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.Desktop.ViewModels;
using ReactiveUI.Avalonia;

namespace KafkaLoad.Desktop.Views;

public partial class TestScenarioEditorView : ReactiveUserControl<TestScenarioEditorViewModel>
{
    public TestScenarioEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}