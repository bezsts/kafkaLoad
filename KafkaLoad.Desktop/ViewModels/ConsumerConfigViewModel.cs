using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.Desktop.ViewModels;

public class ConsumerConfigViewModel : ReactiveValidationObject
{
    private readonly IConfigRepository<CustomConsumerConfig> _configRepository;
    private CustomConsumerConfig _model;

    private readonly string _originalName;
    public IObservable<Unit> SaveComplete => SaveCommand;

    public ConsumerConfigViewModel(IConfigRepository<CustomConsumerConfig> configrepository, CustomConsumerConfig? modelToEdit = null)
    {
        _configRepository = configrepository;

        if (modelToEdit != null)
        {
            _model = Clone(modelToEdit);
            _originalName = _model.Name;
        }
        else
        {
            _model = new CustomConsumerConfig();
            _originalName = string.Empty;
        }

        InitializeValidation();

        SaveCommand = ReactiveCommand.CreateFromTask(
            SaveConfigAsync,
            canExecute: this.IsValid(), 
            outputScheduler: RxApp.MainThreadScheduler
        );
    }

    private CustomConsumerConfig Clone(CustomConsumerConfig source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<CustomConsumerConfig>(json)!;
    }

    private void InitializeValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Name is required");
        
        this.ValidationRule(
            viewModel => viewModel.BootstrapServers,
            servers => !string.IsNullOrWhiteSpace(servers),
            "Bootstrap servers are required");

        this.ValidationRule(
            viewModel => viewModel.GroupId,
            groupId => !string.IsNullOrWhiteSpace(groupId),
            "Group Id is required");
    }

    // --- Properties Wrappers ---

    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value)
            {
                _model.Name = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public string BootstrapServers
    {
        get => _model.BootstrapServers;
        set
        {
            if (_model.BootstrapServers != value)
            {
                _model.BootstrapServers = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public string GroupId
    {
        get => _model.GroupId;
        set
        {
            if (_model.GroupId != value)
            {
                _model.GroupId = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public List<KeyDeserializerEnum> KeyDeserializerOptions { get; } = Enum.GetValues<KeyDeserializerEnum>().ToList();

    public KeyDeserializerEnum SelectedKeyDeserializer
    {
        get => _model.KeyDeserializer;
        set
        {
            if (_model.KeyDeserializer != value)
            {
                _model.KeyDeserializer = value;
                this.RaisePropertyChanged();
            }
        }
    }
    public List<ValueDeserializerEnum> ValueDeserializerOptions { get; } = Enum.GetValues<ValueDeserializerEnum>().ToList();

    public ValueDeserializerEnum SelectedValueDeserializer
    {
        get => _model.ValueDeserializer;
        set
        {
            if (_model.ValueDeserializer != value)
            {
                _model.ValueDeserializer = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public List<AutoOffsetResetEnum> AutoOffsetResetOptions { get; } = Enum.GetValues<AutoOffsetResetEnum>().ToList();

    public AutoOffsetResetEnum SelectedAutoOffsetReset
    {
        get => _model.AutoOffsetReset;
        set
        {
            if (_model.AutoOffsetReset != value)
            {
                _model.AutoOffsetReset = value;
                this.RaisePropertyChanged();
            }
        }
    }

    // public bool EnableAutoCommit
    // {
    //     get => _model.EnableAutoCommit;
    //     set
    //     {
    //         if (_model.EnableAutoCommit != value)
    //         {
    //             _model.EnableAutoCommit = value;
    //             this.RaisePropertyChanged();
    //         }
    //     }
    // }

    public int? FetchMinBytes
    {
        get => _model.FetchMinBytes;
        set
        {
            var newValue = value ?? 1;

            if (_model.FetchMinBytes != newValue)
            {
                _model.FetchMinBytes = newValue;
                this.RaisePropertyChanged();
            }
        }
    }
    public int? FetchMaxBytes
    {
        get => _model.FetchMaxBytes;
        set
        {
            var newValue = value ?? (50 * 1024 * 1024);

            if (_model.FetchMaxBytes != newValue)
            {
                _model.FetchMaxBytes = newValue;
                this.RaisePropertyChanged();
            }
        }
    }
    public int? FetchMaxWait
    {
        get => _model.FetchMaxWait;
        set
        {
            var newValue = value ?? 500;

            if (_model.FetchMaxWait != newValue)
            {
                _model.FetchMaxWait = newValue;
                this.RaisePropertyChanged();
            }
        }
    }


    public int? MaxPollInterval
    {
        get => _model.MaxPollInterval;
        set
        {
            var newValue = value ?? (5 * 60 * 1000);

            if (_model.MaxPollInterval != newValue)
            {
                _model.MaxPollInterval = newValue;
                this.RaisePropertyChanged();
            }
        }
    }

    // --- Commands ---

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    private async Task SaveConfigAsync()
    {
        if (_model.Name != _originalName && await _configRepository.ExistsAsync(_model.Name))
        {
            throw new Exception($"Configuration with name '{_model.Name}' already exists!");
        }

        if (!string.IsNullOrEmpty(_originalName) && _model.Name != _originalName)
        {
            await _configRepository.DeleteAsync(_originalName);
        }

        await _configRepository.SaveAsync(_model);
    }
}
