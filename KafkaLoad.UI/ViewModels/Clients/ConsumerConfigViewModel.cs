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

    private static readonly CustomConsumerConfig _defaults = new();
    public ICommand ResetMaxPollIntervalCommand { get; }
    public ICommand ResetFetchMinBytesCommand   { get; }
    public ICommand ResetFetchMaxBytesCommand   { get; }
    public ICommand ResetFetchMaxWaitCommand    { get; }

    public ConsumerConfigViewModel(IConfigRepository<CustomConsumerConfig> repository, CustomConsumerConfig? modelToEdit = null)
        : base(repository, modelToEdit)
    {
        SecurityVM = new SecurityConfigViewModel(Model.Security);

        ResetMaxPollIntervalCommand = ReactiveCommand.Create(() => { MaxPollInterval = _defaults.MaxPollInterval; });
        ResetFetchMinBytesCommand   = ReactiveCommand.Create(() => { FetchMinBytes   = _defaults.FetchMinBytes; });
        ResetFetchMaxBytesCommand   = ReactiveCommand.Create(() => { FetchMaxBytes   = _defaults.FetchMaxBytes; });
        ResetFetchMaxWaitCommand    = ReactiveCommand.Create(() => { FetchMaxWait    = _defaults.FetchMaxWait; });

        SetAutoOffsetResetCommand = ReactiveCommand.Create<AutoOffsetResetEnum>(o =>
        {
            SelectedAutoOffsetReset = o;
            this.RaisePropertyChanged(nameof(IsOffsetLatest));
            this.RaisePropertyChanged(nameof(IsOffsetEarliest));
        });
    }

    protected override void InitializeValidation()
    {
        this.ValidationRule(
            this.WhenAnyValue(x => x.Name, name => !string.IsNullOrWhiteSpace(name)),
            "Name is required");
        var fetchMinValid = this.WhenAnyValue(x => x.FetchMinBytes, v => v > 0);
        this.ValidationRule(fetchMinValid, "Fetch Min Bytes must be greater than zero");

        var fetchMaxValid = this.WhenAnyValue(x => x.FetchMaxBytes, v => v > 0);
        this.ValidationRule(fetchMaxValid, "Fetch Max Bytes must be greater than zero");

        var fetchMinMax = this.WhenAnyValue(x => x.FetchMinBytes, x => x.FetchMaxBytes,
            (min, max) => min <= max);
        this.ValidationRule(fetchMinMax, "Fetch Min Bytes must be less than or equal to Fetch Max Bytes");

        var fetchMaxWaitValid = this.WhenAnyValue(x => x.FetchMaxWait, v => v > 0);
        this.ValidationRule(fetchMaxWaitValid, "Fetch Max Wait must be greater than zero");

        var maxPollIntervalValid = this.WhenAnyValue(x => x.MaxPollInterval, v => v > 0);
        this.ValidationRule(maxPollIntervalValid, "Max Poll Interval must be greater than zero");

        var fetchWaitVsPoll = this.WhenAnyValue(x => x.FetchMaxWait, x => x.MaxPollInterval,
            (wait, poll) => wait < poll);
        this.ValidationRule(fetchWaitVsPoll, "Fetch Max Wait must be less than Max Poll Interval");
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(value, Model.Name, v => Model.Name = v);
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
