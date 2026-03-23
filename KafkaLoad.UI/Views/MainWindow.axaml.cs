using Avalonia.Controls;
using KafkaLoad.UI.Views;
using KafkaLoad.UI.ViewModels;
using ReactiveUI.Avalonia;

namespace KafkaLoad.UI;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}