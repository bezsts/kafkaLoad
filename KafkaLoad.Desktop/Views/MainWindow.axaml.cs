using Avalonia.Controls;
using KafkaLoad.Desktop.Services;
using KafkaLoad.Desktop.ViewModels;
using KafkaLoad.Desktop.Views;
using ReactiveUI.Avalonia;

namespace KafkaLoad.Desktop;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}