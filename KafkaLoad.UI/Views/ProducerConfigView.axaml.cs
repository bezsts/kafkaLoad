using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Validation.Extensions;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System.Reactive.Disposables.Fluent;
using KafkaLoad.UI.ViewModels;

namespace KafkaLoad.UI.Views;

public partial class ProducerConfigView : ReactiveUserControl<ProducerConfigViewModel>
{
    public ProducerConfigView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}