using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Models.Interfaces;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Confluent.Kafka.ConfigPropertyNames;

namespace KafkaLoad.Desktop.ViewModels
{
    public class ClientsConfigViewModel : ReactiveObject
    {
        private readonly IConfigRepository<CustomProducerConfig> _producerRepo;
        private readonly IConfigRepository<CustomConsumerConfig> _consumerRepo;

        private string _notificationMessage = string.Empty;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set => this.RaiseAndSetIfChanged(ref _notificationMessage, value);
        }

        public ObservableCollection<CustomProducerConfig> Producers { get; } = new();
        public ObservableCollection<CustomConsumerConfig> Consumers { get; } = new();

        private CustomProducerConfig? _selectedProducer;
        public CustomProducerConfig? SelectedProducer
        {
            get => _selectedProducer;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProducer, value);
                if (value != null) OpenEditorFor(() => new ProducerConfigViewModel(_producerRepo, value), "Producer");
            }
        }

        private CustomConsumerConfig? _selectedConsumer;
        public CustomConsumerConfig? SelectedConsumer
        {
            get => _selectedConsumer;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedConsumer, value);
                if (value != null) OpenEditorFor(() => new ConsumerConfigViewModel(_consumerRepo, value), "Consumer");
            }
        }

        private ReactiveObject? _currentEditor;
        public ReactiveObject? CurrentEditor
        {
            get => _currentEditor;
            set => this.RaiseAndSetIfChanged(ref _currentEditor, value);
        }

        // --- Commands ---
        public ReactiveCommand<Unit, Unit> CreateProducerCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicateProducerCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteProducerCommand { get; }

        public ReactiveCommand<Unit, Unit> CreateConsumerCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicateConsumerCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteConsumerCommand { get; }

        public ClientsConfigViewModel(
            IConfigRepository<CustomProducerConfig> producerRepo,
            IConfigRepository<CustomConsumerConfig> consumerRepo)
        {
            _producerRepo = producerRepo;
            _consumerRepo = consumerRepo;

            // ---Producer Commands Setup ---
            CreateProducerCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("User clicked Create New Producer Configuration.");
                OpenEditorFor(() => new ProducerConfigViewModel(_producerRepo), "Producer");
            });

            DuplicateProducerCommand = ReactiveCommand.CreateFromTask(() =>
                DuplicateItemAsync(_producerRepo, SelectedProducer, Producers, p => SelectedProducer = p, "Producer"),
                this.WhenAnyValue(x => x.SelectedProducer).Select(x => x != null));

            DeleteProducerCommand = ReactiveCommand.CreateFromTask(() =>
                DeleteItemAsync(_producerRepo, SelectedProducer, "Producer"),
                this.WhenAnyValue(x => x.SelectedProducer).Select(x => x != null));

            // --- Consumer Commands Setup ---
            CreateConsumerCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("User clicked Create New Consumer Configuration.");
                OpenEditorFor(() => new ConsumerConfigViewModel(_consumerRepo), "Consumer");
            });

            DuplicateConsumerCommand = ReactiveCommand.CreateFromTask(() =>
                DuplicateItemAsync(_consumerRepo, SelectedConsumer, Consumers, c => SelectedConsumer = c, "Consumer"),
                this.WhenAnyValue(x => x.SelectedConsumer).Select(x => x != null));

            DeleteConsumerCommand = ReactiveCommand.CreateFromTask(() =>
                DeleteItemAsync(_consumerRepo, SelectedConsumer, "Consumer"),
                this.WhenAnyValue(x => x.SelectedConsumer).Select(x => x != null));

            // Initial Load
            _ = RefreshLists();
        }

        private void OpenEditorFor<TConfig>(Func<BaseConfigViewModel<TConfig>> viewModelFactory, string typeName)
            where TConfig : class, IConfigModel, new()
        {
            // Clear selection from the OTHER list to avoid confusion
            if (typeName == "Producer") SelectedConsumer = null;
            else SelectedProducer = null;

            var vm = viewModelFactory();

            // Unified subscription logic using the BaseConfigViewModel properties
            vm.SaveComplete.Subscribe(async _ =>
            {
                await RefreshLists();
                ShowNotification($"{typeName} configuration saved successfully!");
            });

            vm.SaveCommand.ThrownExceptions.Subscribe(ex =>
            {
                Log.Error(ex, "Error occurred while trying to save {TypeName} configuration from UI.", typeName);
                ShowNotification($"Error: {ex.Message}");
            });

            CurrentEditor = vm;
        }

        private async Task DuplicateItemAsync<T>(
            IConfigRepository<T> repo,
            T? selectedItem,
            ObservableCollection<T> collection,
            Action<T> setSelectedAction,
            string typeName) where T : class, IConfigModel
        {
            if (selectedItem == null) return;

            var copy = Clone(selectedItem);

            string baseName = $"{selectedItem.Name}_Copy";
            string uniqueName = baseName;
            int counter = 1;

            while (await repo.ExistsAsync(uniqueName))
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }

            Log.Information("User duplicating {TypeName} configuration from '{OriginalName}' to '{NewName}'", typeName, selectedItem.Name, uniqueName);

            copy.Name = uniqueName;
            await repo.SaveAsync(copy);
            await RefreshLists();

            // Find and select the new item
            var newItem = collection.FirstOrDefault(x => x.Name == copy.Name);
            if (newItem != null) setSelectedAction(newItem);

            ShowNotification($"{typeName} '{copy.Name}' duplicated!");
        }

        private async Task DeleteItemAsync<T>(
            IConfigRepository<T> repo,
            T? selectedItem,
            string typeName) where T : class, IConfigModel
        {
            if (selectedItem == null) return;

            Log.Information("User requested deletion of {TypeName} configuration: '{ConfigName}'", typeName, selectedItem.Name);

            try
            {
                await repo.DeleteAsync(selectedItem.Name);
                await RefreshLists();

                SelectedProducer = null;
                SelectedConsumer = null;
                CurrentEditor = null;

                ShowNotification($"{typeName} configuration deleted successfully!");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete {TypeName} configuration '{ConfigName}' from UI.", typeName, selectedItem.Name);
                ShowNotification($"Failed to delete: {ex.Message}");
            }
        }

        private async Task RefreshLists()
        {
            Log.Debug("Refreshing UI config lists.");

            Producers.Clear();
            var pList = await _producerRepo.GetAllAsync();
            foreach (var p in pList) Producers.Add(p);

            Consumers.Clear();
            var cList = await _consumerRepo.GetAllAsync();
            foreach (var c in cList) Consumers.Add(c);
        }

        private async void ShowNotification(string message)
        {
            NotificationMessage = message;
            await Task.Delay(3000);
            NotificationMessage = string.Empty;
        }

        private T Clone<T>(T source)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(source);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
        }
    }
}
