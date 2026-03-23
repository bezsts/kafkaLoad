using KafkaLoad.Core.Models;
using KafkaLoad.Core.Services.Interfaces;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KafkaLoad.UI.ViewModels
{
    public class TestScenariosViewModel : ReactiveObject
    {
        private readonly IConfigRepository<TestScenario> _scenarioRepo;
        private readonly IConfigRepository<CustomProducerConfig> _producerRepo;
        private readonly IConfigRepository<CustomConsumerConfig> _consumerRepo;

        private string _notificationMessage = string.Empty;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set => this.RaiseAndSetIfChanged(ref _notificationMessage, value);
        }

        public ObservableCollection<TestScenario> Scenarios { get; } = new();

        private TestScenario? _selectedScenario;
        public TestScenario? SelectedScenario
        {
            get => _selectedScenario;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedScenario, value);
                if (value != null) OpenEditor(value);
            }
        }

        private TestScenarioEditorViewModel? _currentEditor;
        public TestScenarioEditorViewModel? CurrentEditor
        {
            get => _currentEditor;
            set => this.RaiseAndSetIfChanged(ref _currentEditor, value);
        }

        public ReactiveCommand<Unit, Unit> CreateCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicateCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

        public TestScenariosViewModel(
            IConfigRepository<TestScenario> scenarioRepo,
            IConfigRepository<CustomProducerConfig> producerRepo,
            IConfigRepository<CustomConsumerConfig> consumerRepo)
        {
            _scenarioRepo = scenarioRepo;
            _producerRepo = producerRepo;
            _consumerRepo = consumerRepo;

            CreateCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("User clicked Create New Test Scenario.");
                OpenEditor(null);
            });

            DuplicateCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedScenario == null) return;
                var copy = Clone(SelectedScenario);

                // Smart naming logic
                string baseName = $"{SelectedScenario.Name}_Copy";
                string uniqueName = baseName;
                int counter = 1;
                while (await _scenarioRepo.ExistsAsync(uniqueName))
                {
                    uniqueName = $"{baseName}_{counter}";
                    counter++;
                }

                Log.Information("User duplicating Test Scenario from '{Original}' to '{New}'", SelectedScenario.Name, uniqueName);
                copy.Name = uniqueName;

                await _scenarioRepo.SaveAsync(copy);
                await RefreshList();

                SelectedScenario = Scenarios.FirstOrDefault(s => s.Name == copy.Name);
                ShowNotification($"Scenario '{copy.Name}' duplicated!");

            }, this.WhenAnyValue(x => x.SelectedScenario).Select(x => x != null));

            DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedScenario == null) return;
                string name = SelectedScenario.Name;

                Log.Information("User requested deletion of Test Scenario: '{Name}'", name);

                try
                {
                    await _scenarioRepo.DeleteAsync(name);
                    await RefreshList();

                    SelectedScenario = null;
                    CurrentEditor = null;

                    ShowNotification($"Scenario '{name}' deleted!");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete Test Scenario '{Name}' from UI.", name);
                    ShowNotification($"Failed to delete: {ex.Message}");
                }

            }, this.WhenAnyValue(x => x.SelectedScenario).Select(x => x != null));

            _ = RefreshList();
        }

        private void OpenEditor(TestScenario? scenario)
        {
            var vm = new TestScenarioEditorViewModel(
                _producerRepo,
                _consumerRepo,
                _scenarioRepo,
                scenario);

            vm.SaveComplete.Subscribe(async _ =>
            {
                await RefreshList();
                ShowNotification("Scenario saved successfully!");
            });

            vm.SaveCommand.ThrownExceptions.Subscribe(ex =>
            {
                Log.Error(ex, "Error occurred while trying to save Test Scenario from UI.");
                ShowNotification($"Error: {ex.Message}");
            });

            CurrentEditor = vm;
        }

        private async Task RefreshList()
        {
            Scenarios.Clear();
            var list = await _scenarioRepo.GetAllAsync();
            foreach (var item in list) Scenarios.Add(item);
        }

        private async void ShowNotification(string message)
        {
            NotificationMessage = message;
            await Task.Delay(3000);
            NotificationMessage = string.Empty;
        }

        private TestScenario Clone(TestScenario source)
        {
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<TestScenario>(json)!;
        }
    }
}