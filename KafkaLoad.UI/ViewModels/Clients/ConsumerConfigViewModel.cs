using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models;
using KafkaLoad.Core.Services.Interfaces;
using KafkaLoad.UI.ViewModels.Clients;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace KafkaLoad.UI.ViewModels;

public class ConsumerConfigViewModel : BaseConfigViewModel<CustomConsumerConfig>
{
    public SecurityConfigViewModel SecurityVM { get; }

    // Auto offset reset button states
    public bool IsOffsetLatest   => SelectedAutoOffsetReset == AutoOffsetResetEnum.Latest;
    public bool IsOffsetEarliest => SelectedAutoOffsetReset == AutoOffsetResetEnum.Earliest;

    public ICommand SetAutoOffsetResetCommand { get; }

    public ConsumerConfigViewModel(IConfigRepository<CustomConsumerConfig> repository, CustomConsumerConfig? modelToEdit = null)
        : base(repository, modelToEdit)
    {
        SecurityVM = new SecurityConfigViewModel(Model.Security);

        SetAutoOffsetResetCommand = ReactiveCommand.Create<AutoOffsetResetEnum>(o =>
        {
            SelectedAutoOffsetReset = o;
            this.RaisePropertyChanged(nameof(IsOffsetLatest));
            this.RaisePropertyChanged(nameof(IsOffsetEarliest));
        });
    }

    protected override void InitializeValidation()
    {
        this.ValidationRule(vm => vm.Name, name => !string.IsNullOrWhiteSpace(name), "Name is required");
        this.ValidationRule(vm => vm.GroupId, g => !string.IsNullOrWhiteSpace(g), "Group Id required");
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(value, Model.Name, v => Model.Name = v);
    }

    public string GroupId
    {
        get => Model.GroupId;
        set => SetProperty(value, Model.GroupId, v => Model.GroupId = v);
    }


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
