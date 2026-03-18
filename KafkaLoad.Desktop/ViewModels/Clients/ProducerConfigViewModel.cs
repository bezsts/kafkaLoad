using KafkaLoad.Desktop.Enums;
using KafkaLoad.Desktop.Models;
using KafkaLoad.Desktop.Services.Interfaces;
using KafkaLoad.Desktop.ViewModels.Clients;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KafkaLoad.Desktop.ViewModels;

public class ProducerConfigViewModel : BaseConfigViewModel<CustomProducerConfig>
{
    public SecurityConfigViewModel SecurityVM { get; }
    public ProducerConfigViewModel(IConfigRepository<CustomProducerConfig> repository, CustomProducerConfig? modelToEdit = null)
        : base(repository, modelToEdit)
    {
        SecurityVM = new SecurityConfigViewModel(Model.Security);
    }

    protected override void InitializeValidation()
    {
        this.ValidationRule(vm => vm.Name, name => !string.IsNullOrWhiteSpace(name), "Name is required");
        this.ValidationRule(vm => vm.BootstrapServers, s => !string.IsNullOrWhiteSpace(s), "Bootstrap servers required");

        var idempotenceAndAcks = this.WhenAnyValue(x => x.EnableIdempotence, x => x.SelectedAcks,
            (idempotence, acks) => !idempotence || acks == AcksEnum.All);

        this.ValidationRule(vm => vm.SelectedAcks, idempotenceAndAcks, "Idempotence requires Acks='All'");

        var idempotenceAndRetries = this.WhenAnyValue(x => x.EnableIdempotence, x => x.Retries,
            (idempotence, retries) => !idempotence || retries > 0);

        this.ValidationRule(vm => vm.Retries, idempotenceAndRetries, "Idempotence requires number of retries to greater than zero");

        var bufferAndBatch = this.WhenAnyValue(x => x.BufferMemory, x => x.BatchSize,
            (bufferMemory, batchSize) => bufferMemory > batchSize);

        this.ValidationRule(vm => vm.BatchSize, bufferAndBatch, "Batch size should be less than Buffer memory");

        this.ValidationRule(viewModel => viewModel.MaxInFlightRequestsPerConnection, maxFlight => maxFlight != 0,
            "Max in flight requests per connection should be greater than zero");
    }

    // --- Properties Wrappers ---

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

    public List<AcksEnum> AcksOptions { get; } = Enum.GetValues<AcksEnum>().ToList();

    public AcksEnum SelectedAcks
    {
        get => Model.Acks;
        set => SetProperty(value, Model.Acks, v => Model.Acks = v);
    }

    public int? Retries
    {
        get => Model.Retries;
        set => SetProperty(value ?? 0, Model.Retries, v => Model.Retries = v);
    }

    public bool EnableIdempotence
    {
        get => Model.EnableIdempotence;
        set => SetProperty(value, Model.EnableIdempotence, v => Model.EnableIdempotence = v);
    }

    public int? BatchSize
    {
        get => Model.BatchSize;
        set => SetProperty(value ?? 0, Model.BatchSize, v => Model.BatchSize = v);
    }

    public double? Linger
    {
        get => Model.Linger;
        set => SetProperty(value ?? 0, Model.Linger, v => Model.Linger = v);
    }

    public List<CompressionTypeEnum> CompressionTypeOptions { get; } = Enum.GetValues<CompressionTypeEnum>().ToList();

    public CompressionTypeEnum SelectedCompressionType
    {
        get => Model.CompressionType;
        set => SetProperty(value, Model.CompressionType, v => Model.CompressionType = v);
    }

    public long? BufferMemory
    {
        get => Model.BufferMemory;
        set => SetProperty(value ?? 0, Model.BufferMemory, v => Model.BufferMemory = v);
    }

    public int? MaxInFlightRequestsPerConnection
    {
        get => Model.MaxInFlightRequestsPerConnection;
        set => SetProperty(value ?? 0, Model.MaxInFlightRequestsPerConnection, v => Model.MaxInFlightRequestsPerConnection = v);
    }
}
