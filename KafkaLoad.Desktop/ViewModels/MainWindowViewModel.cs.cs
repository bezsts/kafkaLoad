using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Ready to start";

    [ObservableProperty]
    private string _topicName = "test-topic";

    [RelayCommand]
    private async Task StartTest()
    {
        StatusMessage = "Starting test...";

        await Task.Delay(2000);

        StatusMessage = $"Test finished for topic: {TopicName}";
    }
}