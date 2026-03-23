using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.UI.ViewModels;
using ReactiveUI.Avalonia;

namespace KafkaLoad.UI.Views;

public partial class ConsumerConfigView : ReactiveUserControl<ConsumerConfigViewModel>
{
    public ConsumerConfigView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}