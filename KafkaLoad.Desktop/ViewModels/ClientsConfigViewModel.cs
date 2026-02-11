using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (value != null) OpenProducerEditor(value);
            }
        }

        private CustomConsumerConfig? _selectedConsumer;
        public CustomConsumerConfig? SelectedConsumer
        {
            get => _selectedConsumer;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedConsumer, value);
                if (value != null) OpenConsumerEditor(value);
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

            // ----- Producer commands ------
            CreateProducerCommand = ReactiveCommand.Create(() => OpenProducerEditor(null));

            DuplicateProducerCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedProducer == null) return;
                var copy = Clone(SelectedProducer);
                copy.Name += "_Copy";
                await _producerRepo.SaveAsync(copy);
                await RefreshLists();
                SelectedProducer = Producers.FirstOrDefault(p => p.Name == copy.Name);
                ShowNotification($"Producer '{copy.Name}' duplicated!");
            }, this.WhenAnyValue(x => x.SelectedProducer).Select(x => x != null));

            DeleteProducerCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedProducer == null) return;
                await _producerRepo.DeleteAsync(SelectedProducer.Name); 

                await RefreshLists();
                SelectedProducer = null;
                CurrentEditor = null;
                ShowNotification("Producer configuration deleted successfully!");
            }, this.WhenAnyValue(x => x.SelectedProducer).Select(x => x != null));

            // ----- Consumer commands ------
            CreateConsumerCommand = ReactiveCommand.Create(() => OpenConsumerEditor(null));

            DuplicateConsumerCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedConsumer == null) return;
                var copy = Clone(SelectedConsumer);
                copy.Name += "_Copy";
                await _consumerRepo.SaveAsync(copy);
                await RefreshLists();
                SelectedConsumer = Consumers.FirstOrDefault(p => p.Name == copy.Name);
                ShowNotification($"Consumer '{copy.Name}' duplicated!");
            }, this.WhenAnyValue(x => x.SelectedConsumer).Select(x => x != null));

            DeleteConsumerCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedConsumer == null) return;
                await _consumerRepo.DeleteAsync(SelectedConsumer.Name); 

                await RefreshLists();
                SelectedConsumer = null;
                CurrentEditor = null;
                ShowNotification("Consumer configuration deleted successfully!");
            }, this.WhenAnyValue(x => x.SelectedConsumer).Select(x => x != null));

            _ = RefreshLists();
        }

        private void OpenProducerEditor(CustomProducerConfig? config)
        {
            SelectedConsumer = null;

            var vm = new ProducerConfigViewModel(_producerRepo, config);

            vm.SaveComplete.Subscribe(async _ =>
            {
                await RefreshLists();
                ShowNotification("Producer configuration saved successfully!");
            });

            CurrentEditor = vm;
        }

        private void OpenConsumerEditor(CustomConsumerConfig? config)
        {
            SelectedProducer = null;

            var vm = new ConsumerConfigViewModel(_consumerRepo, config);

            vm.SaveComplete.Subscribe(async _ =>
            {
                await RefreshLists();
                ShowNotification("Consumer configuration saved successfully!");
            });

            CurrentEditor = vm;
        }

        private async Task RefreshLists()
        {
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
