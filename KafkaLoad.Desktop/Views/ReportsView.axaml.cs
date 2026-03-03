using Avalonia.Markup.Xaml;
using KafkaLoad.Desktop.ViewModels.Reports;
using ReactiveUI.Avalonia;

namespace KafkaLoad.Desktop.Views;

public partial class ReportsView : ReactiveUserControl<ReportsViewModel>
{
    public ReportsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}