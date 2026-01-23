using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace KafkaLoad.Desktop.ViewModels;

public class ProducerConfigViewModel : ReactiveValidationObject
{
    private readonly IConfigRepository<CustomProducerConfig> _configRepository;
    private CustomProducerConfig _model;

    public ProducerConfigViewModel(IConfigRepository<CustomProducerConfig> configRepository)
    {
        _configRepository = configRepository;
        _model = new CustomProducerConfig();

        InitializeValidation();

        SaveCommand = ReactiveCommand.CreateFromTask(
            SaveConfigAsync,
            canExecute: this.IsValid(), 
            outputScheduler: RxApp.MainThreadScheduler
        );
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

        var idempotenceAndAcks = this.WhenAnyValue(
            x => x.EnableIdempotence,
            x => x.SelectedAcks,
            (idempotence, acks) => 
            {
                if (!idempotence) return true; 
                return acks == AcksEnum.All;
            }
        );

        this.ValidationRule(
            vm => vm.SelectedAcks,
            idempotenceAndAcks,
            "Idempotence requires Acks to be set to 'All'"
        );

        var idempotenceAndRetries = this.WhenAnyValue(
            x => x.EnableIdempotence,
            x => x.Retries,
            (idempotence, retries) => 
            {
                if (!idempotence) return true; 
                return retries > 0;
            }
        );

        this.ValidationRule(
            vm => vm.Retries,
            idempotenceAndRetries,
            "Idempotence requires number of retries to greater than zero"
        );

        var bufferAndBatch = this.WhenAnyValue(
            x => x.BufferMemory,
            x => x.BatchSize,
            (bufferMemory, batchSize) => 
            {
                return bufferMemory > batchSize;
            }
        );

        this.ValidationRule(
            vm => vm.BatchSize,
            bufferAndBatch,
            "Batch size should be less than Buffer memory"
        );

        this.ValidationRule(
            viewModel => viewModel.MaxInFlightRequestsPerConnection,
            maxFlight => maxFlight != 0,
            "Max in flight requests per connection should be greater than zero");
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

    public List<KeySerializerEnum> KeySerializerOptions { get; } = Enum.GetValues<KeySerializerEnum>().ToList();

    public KeySerializerEnum SelectedKeySerializer
    {
        get => _model.KeySerializer;
        set
        {
            if (_model.KeySerializer != value)
            {
                _model.KeySerializer = value;
                this.RaisePropertyChanged();
            }
        }
    }
    public List<ValueSerializerEnum> ValueSerializerOptions { get; } = Enum.GetValues<ValueSerializerEnum>().ToList();

    public ValueSerializerEnum SelectedValueSerializer
    {
        get => _model.ValueSerializer;
        set
        {
            if (_model.ValueSerializer != value)
            {
                _model.ValueSerializer = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public List<AcksEnum> AcksOptions { get; } = Enum.GetValues<AcksEnum>().ToList();

    public AcksEnum SelectedAcks
    {
        get => _model.Acks;
        set
        {
            if (_model.Acks != value)
            {
                _model.Acks = value;
                this.RaisePropertyChanged(); 
            }
        }
    }

    public int? Retries
    {
        get => _model.Retries;
        set
        {
            var newValue = value ?? 0;

            if (_model.Retries != newValue)
            {
                _model.Retries = newValue;
                this.RaisePropertyChanged();
            }
        }
    }

    public bool EnableIdempotence
    {
        get => _model.EnableIdempotence;
        set
        {
            if (_model.EnableIdempotence != value)
            {
                _model.EnableIdempotence = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? BatchSize
    {
        get => _model.BatchSize;
        set
        {
            var newValue = value ?? 0;

            if (_model.BatchSize != newValue)
            {
                _model.BatchSize = newValue;
                this.RaisePropertyChanged();
            }
        }
    }

    public double? Linger
    {
        get => _model.Linger;
        set
        {
            var newValue = value ?? 0;

            if (_model.Linger != newValue)
            {
                _model.Linger = newValue;
                this.RaisePropertyChanged();
            }
        }
    }

    public List<CompressionTypeEnum> CompressionTypeOptions { get; } = Enum.GetValues<CompressionTypeEnum>().ToList();

    public CompressionTypeEnum SelectedCompressionType
    {
        get => _model.CompressionType;
        set
        {
            if (_model.CompressionType != value)
            {
                _model.CompressionType = value;
                this.RaisePropertyChanged(); 
            }
        }
    }

    public long? BufferMemory
    {
        get => _model.BufferMemory;
        set
        {
            var newValue = value ?? 0;

            if (_model.BufferMemory != newValue)
            {
                _model.BufferMemory = newValue;
                this.RaisePropertyChanged();
            }
        }
    }

    public int? MaxInFlightRequestsPerConnection
    {
        get => _model.MaxInFlightRequestsPerConnection;
        set
        {
            var newValue = value ?? 0;

            if (_model.MaxInFlightRequestsPerConnection != newValue)
            {
                _model.MaxInFlightRequestsPerConnection = newValue;
                this.RaisePropertyChanged();
            }
        }
    }

    // --- Commands ---

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    private async System.Threading.Tasks.Task SaveConfigAsync()
    {
        await _configRepository.SaveAsync(_model);
    }
}
