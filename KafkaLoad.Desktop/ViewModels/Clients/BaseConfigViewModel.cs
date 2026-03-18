using KafkaLoad.Desktop.Models.Interfaces;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.ViewModels.Clients;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KafkaLoad.Desktop.ViewModels
{
    public abstract class BaseConfigViewModel<TConfig> : ReactiveValidationObject
        where TConfig : class, IConfigModel, new()
    {
        protected readonly IConfigRepository<TConfig> ConfigRepository;
        protected TConfig Model;

        private readonly string _originalName;

        public IObservable<Unit> SaveComplete => SaveCommand;
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

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

            SaveCommand = ReactiveCommand.CreateFromTask(
                SaveConfigAsync,
                canExecute: this.IsValid(),
                outputScheduler: RxApp.MainThreadScheduler
            );
        }

        protected abstract void InitializeValidation();

        private async Task SaveConfigAsync()
        {
            Log.Information("User attempting to save configuration: '{ConfigName}'", Model.Name);

            // 1. Check if name changed AND if the new name is already taken
            if (Model.Name != _originalName && await ConfigRepository.ExistsAsync(Model.Name))
            {
                Log.Warning("Save aborted: Configuration with name '{ConfigName}' already exists.", Model.Name);
                throw new Exception($"Configuration with name '{Model.Name}' already exists!");
            }

            // 2. If renamed, delete the old file
            if (!string.IsNullOrEmpty(_originalName) && Model.Name != _originalName)
            {
                Log.Information("Configuration renamed from '{OldName}' to '{NewName}'. Deleting old file.", _originalName, Model.Name);
                await ConfigRepository.DeleteAsync(_originalName);
            }

            // 3. Save
            await ConfigRepository.SaveAsync(Model);
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
