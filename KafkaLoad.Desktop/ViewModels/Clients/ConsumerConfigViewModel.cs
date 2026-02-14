using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KafkaLoad.Desktop.ViewModels;

public class ConsumerConfigViewModel : BaseConfigViewModel<CustomConsumerConfig>
{
    public ConsumerConfigViewModel(IConfigRepository<CustomConsumerConfig> repository, CustomConsumerConfig? modelToEdit = null)
        : base(repository, modelToEdit)
    {
    }

    protected override void InitializeValidation()
    {
        this.ValidationRule(vm => vm.Name, name => !string.IsNullOrWhiteSpace(name), "Name is required");
        this.ValidationRule(vm => vm.BootstrapServers, s => !string.IsNullOrWhiteSpace(s), "Bootstrap servers required");
        this.ValidationRule(vm => vm.GroupId, g => !string.IsNullOrWhiteSpace(g), "Group Id required");
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(value, Model.Name, v => Model.Name = v);
    }

    public string BootstrapServers
    {
        get => Model.BootstrapServers;
        set => SetProperty(value, Model.BootstrapServers, v => Model.BootstrapServers = v);
    }

    public string GroupId
    {
        get => Model.GroupId;
        set => SetProperty(value, Model.GroupId, v => Model.GroupId = v);
    }

    public List<AutoOffsetResetEnum> AutoOffsetResetOptions { get; } = Enum.GetValues<AutoOffsetResetEnum>().ToList();

    public AutoOffsetResetEnum SelectedAutoOffsetReset
    {
        get => Model.AutoOffsetReset;
        set => SetProperty(value, Model.AutoOffsetReset, v => Model.AutoOffsetReset = v);
    }

    public int? FetchMinBytes
    {
        get => Model.FetchMinBytes;
        set => SetProperty(value ?? 1, Model.FetchMinBytes, v => Model.FetchMinBytes = v);
    }
    public int? FetchMaxBytes
    {
        get => Model.FetchMaxBytes;
        set => SetProperty(value ?? (50 * 1024 * 1024), Model.FetchMaxBytes, v => Model.FetchMaxBytes = v);
    }
    public int? FetchMaxWait
    {
        get => Model.FetchMaxWait;
        set => SetProperty(value ?? 500, Model.FetchMaxWait, v => Model.FetchMaxWait = v);
    }

    public int? MaxPollInterval
    {
        get => Model.MaxPollInterval;
        set => SetProperty(value ?? (5 * 60 * 1000), Model.MaxPollInterval, v => Model.MaxPollInterval = v);
    }
}
