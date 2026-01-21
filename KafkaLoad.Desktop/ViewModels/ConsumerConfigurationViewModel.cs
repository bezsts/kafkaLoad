using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI;

namespace KafkaLoad.Desktop.ViewModels;

public class ConsumerConfigurationViewModel : ViewModelBase
{
    private readonly IConfigurationManager _configManager;
    private ConsumerConfiguration _model;

    public ConsumerConfigurationViewModel(IConfigurationManager configManager)
    {
        _configManager = configManager;
        _model = new ConsumerConfiguration();

        var canSave = this.WhenAnyValue(
            x => x.Name,
            x => x.BootstrapServers,
            x => x.GroupId,
            (name, servers, groupid) => 
                !string.IsNullOrWhiteSpace(name) && 
                !string.IsNullOrWhiteSpace(servers) &&
                !string.IsNullOrWhiteSpace(groupid)
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

    public bool EnableAutoCommit
    {
        get => _model.EnableAutoCommit;
        set
        {
            if (_model.EnableAutoCommit != value)
            {
                _model.EnableAutoCommit = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int FetchMinBytes
    {
        get => _model.FetchMinBytes;
        set
        {
            if (_model.FetchMinBytes != value)
            {
                _model.FetchMinBytes = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int FetchMaxWait
    {
        get => _model.FetchMaxWait;
        set
        {
            if (_model.FetchMaxWait != value)
            {
                _model.FetchMaxWait = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int MaxPollRecords
    {
        get => _model.MaxPollRecords;
        set
        {
            if (_model.MaxPollRecords != value)
            {
                _model.MaxPollRecords = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public int MaxPollInterval
    {
        get => _model.MaxPollInterval;
        set
        {
            if (_model.MaxPollInterval != value)
            {
                _model.MaxPollInterval = value;
                this.RaisePropertyChanged();
            }
        }
    }

    // --- Commands ---

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    private async System.Threading.Tasks.Task SaveConfigAsync()
    {
        // TODO: add FileDialog
        string path = $"{Name}_consumer_config.json"; 
        await _configManager.SaveAsync(_model, path);
    }
}
