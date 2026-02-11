using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.Desktop.ViewModels;
using ReactiveUI.Avalonia;

namespace KafkaLoad.Desktop.Views;

public partial class ClientsConfigView : ReactiveUserControl<ClientsConfigViewModel>
{
    public ClientsConfigView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}