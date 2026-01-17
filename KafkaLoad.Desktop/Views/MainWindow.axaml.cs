using Avalonia.Controls;
using KafkaLoad.Desktop.ViewModels;

namespace KafkaLoad.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}