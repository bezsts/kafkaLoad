using KafkaLoad.Desktop.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Validation.Extensions;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System.Reactive.Disposables.Fluent;

namespace KafkaLoad.Desktop.Views;

public partial class ProducerConfigurationView : ReactiveUserControl<ProducerConfigurationViewModel>
{
    public ProducerConfigurationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}