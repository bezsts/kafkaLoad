using KafkaLoad.Core.Enums;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace KafkaLoad.UI.ViewModels
{
    public enum ScenarioSort
    {
        NameAsc,
        NameDesc,
        TypeAsc,
        DateDesc,
        DateAsc
    }

    public class SortOption<T>
    {
        public string Label { get; }
        public T Value { get; }
        public SortOption(string label, T value) { Label = label; Value = value; }
        public override string ToString() => Label;
    }

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

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                RefreshFiltered();
            }
        }

        private TestType? _activeTypeFilter = null;
        public TestType? ActiveTypeFilter
        {
            get => _activeTypeFilter;
            set
            {
                this.RaiseAndSetIfChanged(ref _activeTypeFilter, value);
                this.RaisePropertyChanged(nameof(IsFilterAll));
                this.RaisePropertyChanged(nameof(IsFilterLoad));
                this.RaisePropertyChanged(nameof(IsFilterStress));
                this.RaisePropertyChanged(nameof(IsFilterSpike));
                this.RaisePropertyChanged(nameof(IsFilterSoak));
                RefreshFiltered();
            }
        }

        public bool IsFilterAll    => !_activeTypeFilter.HasValue;
        public bool IsFilterLoad   => _activeTypeFilter == TestType.Load;
        public bool IsFilterStress => _activeTypeFilter == TestType.Stress;
        public bool IsFilterSpike  => _activeTypeFilter == TestType.Spike;
        public bool IsFilterSoak   => _activeTypeFilter == TestType.Soak;

        private SortOption<ScenarioSort> _selectedSort;
        public SortOption<ScenarioSort> SelectedSort
        {
            get => _selectedSort;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSort, value);
                RefreshFiltered();
            }
        }

        public List<SortOption<ScenarioSort>> SortOptions { get; } = new()
        {
            new("Name A→Z",   ScenarioSort.NameAsc),
            new("Name Z→A",   ScenarioSort.NameDesc),
            new("Type",       ScenarioSort.TypeAsc),
            new("Newest",     ScenarioSort.DateDesc),
            new("Oldest",     ScenarioSort.DateAsc),
        };

        public ObservableCollection<TestScenario> Scenarios { get; } = new();
        public ObservableCollection<TestScenario> FilteredScenarios { get; } = new();

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
        public ReactiveCommand<object?, Unit> SetFilterCommand { get; }

        public TestScenariosViewModel(
            IConfigRepository<TestScenario> scenarioRepo,
            IConfigRepository<CustomProducerConfig> producerRepo,
            IConfigRepository<CustomConsumerConfig> consumerRepo)
        {
            _scenarioRepo = scenarioRepo;
            _producerRepo = producerRepo;
            _consumerRepo = consumerRepo;

            _selectedSort = SortOptions[0];

            CreateCommand = ReactiveCommand.Create(() =>
            {
                Log.Information("User clicked Create New Test Scenario.");
                OpenEditor(null);
            });

            DuplicateCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedScenario == null) return;
                var copy = Clone(SelectedScenario);

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

            SetFilterCommand = ReactiveCommand.Create<object?>(param =>
            {
                ActiveTypeFilter = param is TestType t ? t : (TestType?)null;
            });

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

        public Task RefreshAsync() => RefreshList();

        private async Task RefreshList()
        {
            Scenarios.Clear();
            var list = await _scenarioRepo.GetAllAsync();
            foreach (var item in list) Scenarios.Add(item);
            RefreshFiltered();
        }

        private void RefreshFiltered()
        {
            var q = Scenarios.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
                q = q.Where(s => s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            if (ActiveTypeFilter.HasValue)
                q = q.Where(s => s.TestType == ActiveTypeFilter.Value);

            q = (_selectedSort?.Value ?? ScenarioSort.NameAsc) switch
            {
                ScenarioSort.NameDesc => q.OrderByDescending(s => s.Name),
                ScenarioSort.TypeAsc  => q.OrderBy(s => s.TestType).ThenBy(s => s.Name),
                ScenarioSort.DateDesc => q.OrderByDescending(s => s.CreatedAt),
                ScenarioSort.DateAsc  => q.OrderBy(s => s.CreatedAt),
                _                    => q.OrderBy(s => s.Name),
            };

            FilteredScenarios.Clear();
            foreach (var s in q) FilteredScenarios.Add(s);
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
