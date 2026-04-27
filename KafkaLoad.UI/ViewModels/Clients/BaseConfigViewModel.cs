using KafkaLoad.Core.Models.Interfaces;
using KafkaLoad.Core.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Builder;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using ReactiveUI.Avalonia;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KafkaLoad.UI.ViewModels
{
    public abstract class BaseConfigViewModel<TConfig> : ReactiveValidationObject
        where TConfig : class, IConfigModel, new()
    {
        protected readonly IConfigRepository<TConfig> ConfigRepository;
        protected TConfig Model;

        private readonly string _originalName;

        public IObservable<Unit> SaveComplete => SaveCommand;
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        private ObservableAsPropertyHelper<string?> _validationSummary = null!;
        public string? ValidationSummary => _validationSummary.Value;

        protected BaseConfigViewModel(IConfigRepository<TConfig> configRepository, TConfig? modelToEdit)
        {
            ConfigRepository = configRepository;

            if (modelToEdit != null)
            {
                Model = Clone(modelToEdit);
                _originalName = Model.Name;
            }
            else
            {
                Model = new TConfig();
                _originalName = string.Empty;
            }

            InitializeValidation();

            var validationCtx = (ValidationContext)ValidationContext;

            string? GetSummary()
            {
                var errorList = new List<string>();
                foreach (var v in ValidationContext.Validations.Items)
                {
                    if (!v.IsValid)
                    {
                        var msg = v.Text?.ToSingleLine();
                        if (!string.IsNullOrWhiteSpace(msg))
                            errorList.Add($"• {msg}");
                    }
                }
                return errorList.Count > 0 ? string.Join("\n", errorList) : null;
            }

            _validationSummary = Observable
                .Merge(
                    Observable.Return(Unit.Default).ObserveOn(AvaloniaScheduler.Instance),
                    validationCtx.ValidationStatusChange.Select(_ => Unit.Default)
                )
                .Select(_ => GetSummary())
                .DistinctUntilChanged()
                .ObserveOn(AvaloniaScheduler.Instance)
                .ToProperty(this, nameof(ValidationSummary));

            SaveCommand = ReactiveCommand.CreateFromTask(
                SaveConfigAsync,
                canExecute: this.IsValid(),
                outputScheduler: AvaloniaScheduler.Instance
            );
        }

        protected abstract void InitializeValidation();

        private async Task SaveConfigAsync()
        {
            Log.Information("User attempting to save configuration: '{ConfigName}'", Model.Name);

            if (Model.Name != _originalName && await ConfigRepository.ExistsAsync(Model.Name))
            {
                Log.Warning("Save aborted: Configuration with name '{ConfigName}' already exists.", Model.Name);
                throw new Exception($"Configuration with name '{Model.Name}' already exists!");
            }

            if (!string.IsNullOrEmpty(_originalName) && Model.Name != _originalName)
            {
                Log.Information("Renaming configuration '{OldName}' → '{NewName}'", _originalName, Model.Name);
                await ConfigRepository.RenameAndSaveAsync(_originalName, Model);
            }
            else
            {
                await ConfigRepository.SaveAsync(Model);
            }

            Log.Debug("Configuration '{ConfigName}' successfully saved from UI.", Model.Name);
        }
        private TConfig Clone(TConfig source)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(source);
            return System.Text.Json.JsonSerializer.Deserialize<TConfig>(json)!;
        }

        protected void SetProperty<T>(T value, T currentValue, Action<T> setter, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, value)) return;
            setter(value);
            this.RaisePropertyChanged(propertyName);
        }
    }
}
