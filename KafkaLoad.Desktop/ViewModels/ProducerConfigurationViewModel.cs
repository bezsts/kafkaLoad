using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;

namespace KafkaLoad.Desktop.ViewModels;

public class ProducerConfigurationViewModel : ViewModelBase
{
    private readonly IConfigurationManager _configManager;
    private ProducerConfiguration _model;

    public ProducerConfigurationViewModel(IConfigurationManager configManger)
    {
        _configManager = configManger;
        _model = new ProducerConfiguration();

        var canSave = this.WhenAnyValue(
            x => x.Name,
            x => x.BootstrapServers,
            (name, servers) => 
                !string.IsNullOrWhiteSpace(name) && 
                !string.IsNullOrWhiteSpace(servers)
        );

        SaveCommand = ReactiveCommand.CreateFromTask(
            SaveConfigAsync,
            canExecute: canSave, 
            outputScheduler: RxApp.MainThreadScheduler
        );
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

    public int Retries
    {
        get => _model.Retries;
        set
        {
            if (_model.Retries != value)
            {
                _model.Retries = value;
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

    public int BatchSize
    {
        get => _model.BatchSize;
        set
        {
            if (_model.BatchSize != value)
            {
                _model.BatchSize = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public double Linger
    {
        get => _model.Linger;
        set
        {
            if (_model.Linger != value)
            {
                _model.Linger = value;
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

    public long BufferMemory
    {
        get => _model.BufferMemory;
        set
        {
            if (_model.BufferMemory != value)
            {
                _model.BufferMemory = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int MaxInFlightRequestsPerConnection
    {
        get => _model.MaxInFlightRequestsPerConnection;
        set
        {
            if (_model.MaxInFlightRequestsPerConnection != value)
            {
                _model.MaxInFlightRequestsPerConnection = value;
                this.RaisePropertyChanged();
            }
        }
    }

    // --- Commands ---

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    private async System.Threading.Tasks.Task SaveConfigAsync()
    {
        // TODO: add FileDialog
        string path = $"{Name}_producer_config.json"; 
        await _configManager.SaveAsync(_model, path);
    }
}
