using KafkaLoad.Core.Models;
using KafkaLoad.Core.Models.Interfaces;
using KafkaLoad.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace KafkaLoad.UI.ViewModels
{
    public enum ClientSort
    {
        NameAsc,
        NameDesc,
        DateDesc,
        DateAsc
    }

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

        private bool _notificationIsError;
        public bool NotificationIsError
        {
            get => _notificationIsError;
            private set => this.RaiseAndSetIfChanged(ref _notificationIsError, value);
        }

        // --- Producers ---
        public ObservableCollection<CustomProducerConfig> Producers { get; } = new();
        public ObservableCollection<CustomProducerConfig> FilteredProducers { get; } = new();

        private string _producerSearchText = string.Empty;
        public string ProducerSearchText
        {
            get => _producerSearchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _producerSearchText, value);
                RefreshFilteredProducers();
            }
        }

        private SortOption<ClientSort> _selectedProducerSort;
        public SortOption<ClientSort> SelectedProducerSort
        {
            get => _selectedProducerSort;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProducerSort, value);
                RefreshFilteredProducers();
            }
        }

        // --- Consumers ---
        public ObservableCollection<CustomConsumerConfig> Consumers { get; } = new();
        public ObservableCollection<CustomConsumerConfig> FilteredConsumers { get; } = new();

        private string _consumerSearchText = string.Empty;
        public string ConsumerSearchText
        {
            get => _consumerSearchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _consumerSearchText, value);
                RefreshFilteredConsumers();
            }
        }

        private SortOption<ClientSort> _selectedConsumerSort;
        public SortOption<ClientSort> SelectedConsumerSort
        {
            get => _selectedConsumerSort;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedConsumerSort, value);
                RefreshFilteredConsumers();
            }
        }

        public List<SortOption<ClientSort>> SortOptions { get; } = new()
        {
            new("Name A→Z", ClientSort.NameAsc),
            new("Name Z→A", ClientSort.NameDesc),
            new("Newest",   ClientSort.DateDesc),
            new("Oldest",   ClientSort.DateAsc),
        };

        // --- Selection ---
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

            _selectedProducerSort = SortOptions[0];
            _selectedConsumerSort = SortOptions[0];

            // --- Producer Commands ---
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

            // --- Consumer Commands ---
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

            _ = RefreshLists();
        }

        private void OpenEditorFor<TConfig>(Func<BaseConfigViewModel<TConfig>> viewModelFactory, string typeName)
            where TConfig : class, IConfigModel, new()
        {
            if (typeName == "Producer") SelectedConsumer = null;
            else SelectedProducer = null;

            var vm = viewModelFactory();

            vm.SaveComplete.Subscribe(async _ =>
            {
                await RefreshLists();
                ShowNotification($"{typeName} configuration saved successfully!");
            });

            vm.SaveCommand.ThrownExceptions.Subscribe(ex =>
            {
                Log.Error(ex, "Error occurred while trying to save {TypeName} configuration from UI.", typeName);
                ShowNotification($"Error: {ex.Message}", isError: true);
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

            RefreshFilteredProducers();
            RefreshFilteredConsumers();
        }

        private void RefreshFilteredProducers()
        {
            var q = Producers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(ProducerSearchText))
                q = q.Where(p => p.Name.Contains(ProducerSearchText, StringComparison.OrdinalIgnoreCase));

            q = (_selectedProducerSort?.Value ?? ClientSort.NameAsc) switch
            {
                ClientSort.NameDesc => q.OrderByDescending(p => p.Name),
                ClientSort.DateDesc => q.OrderByDescending(p => p.CreatedAt),
                ClientSort.DateAsc  => q.OrderBy(p => p.CreatedAt),
                _                  => q.OrderBy(p => p.Name),
            };

            FilteredProducers.Clear();
            foreach (var p in q) FilteredProducers.Add(p);
        }

        private void RefreshFilteredConsumers()
        {
            var q = Consumers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(ConsumerSearchText))
                q = q.Where(c => c.Name.Contains(ConsumerSearchText, StringComparison.OrdinalIgnoreCase));

            q = (_selectedConsumerSort?.Value ?? ClientSort.NameAsc) switch
            {
                ClientSort.NameDesc => q.OrderByDescending(c => c.Name),
                ClientSort.DateDesc => q.OrderByDescending(c => c.CreatedAt),
                ClientSort.DateAsc  => q.OrderBy(c => c.CreatedAt),
                _                  => q.OrderBy(c => c.Name),
            };

            FilteredConsumers.Clear();
            foreach (var c in q) FilteredConsumers.Add(c);
        }

        private async void ShowNotification(string message, bool isError = false)
        {
            NotificationIsError = isError;
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
