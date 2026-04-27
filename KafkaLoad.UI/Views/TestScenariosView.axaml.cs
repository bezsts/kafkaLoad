using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.UI.ViewModels;
using ReactiveUI.Avalonia;

namespace KafkaLoad.UI.Views;

public partial class TestScenariosView : ReactiveUserControl<TestScenariosViewModel>
{
    public TestScenariosView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}