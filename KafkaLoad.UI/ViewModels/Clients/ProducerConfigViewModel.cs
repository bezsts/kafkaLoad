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

public class ProducerConfigViewModel : BaseConfigViewModel<CustomProducerConfig>
{
    public SecurityConfigViewModel SecurityVM { get; }

    // Acks button states
    public bool IsAcksNone   => SelectedAcks == AcksEnum.None;
    public bool IsAcksLeader => SelectedAcks == AcksEnum.Leader;
    public bool IsAcksAll    => SelectedAcks == AcksEnum.All;

    // Compression button states
    public bool IsCompressionNone   => SelectedCompressionType == CompressionTypeEnum.None;
    public bool IsCompressionGzip   => SelectedCompressionType == CompressionTypeEnum.Gzip;
    public bool IsCompressionSnappy => SelectedCompressionType == CompressionTypeEnum.Snappy;
    public bool IsCompressionLz4    => SelectedCompressionType == CompressionTypeEnum.Lz4;
    public bool IsCompressionZstd   => SelectedCompressionType == CompressionTypeEnum.Zstd;

    public ICommand SetAcksCommand        { get; }
    public ICommand SetCompressionCommand { get; }

    public ProducerConfigViewModel(IConfigRepository<CustomProducerConfig> repository, CustomProducerConfig? modelToEdit = null)
        : base(repository, modelToEdit)
    {
        SecurityVM = new SecurityConfigViewModel(Model.Security);

        SetAcksCommand = ReactiveCommand.Create<AcksEnum>(a =>
        {
            SelectedAcks = a;
            this.RaisePropertyChanged(nameof(IsAcksNone));
            this.RaisePropertyChanged(nameof(IsAcksLeader));
            this.RaisePropertyChanged(nameof(IsAcksAll));
        });

        SetCompressionCommand = ReactiveCommand.Create<CompressionTypeEnum>(c =>
        {
            SelectedCompressionType = c;
            this.RaisePropertyChanged(nameof(IsCompressionNone));
            this.RaisePropertyChanged(nameof(IsCompressionGzip));
            this.RaisePropertyChanged(nameof(IsCompressionSnappy));
            this.RaisePropertyChanged(nameof(IsCompressionLz4));
            this.RaisePropertyChanged(nameof(IsCompressionZstd));
        });
    }

    protected override void InitializeValidation()
    {
        this.ValidationRule(vm => vm.Name, name => !string.IsNullOrWhiteSpace(name), "Name is required");

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

    public string ClientID
    {
        get => Model.ClientID;
        set => SetProperty(value, Model.ClientID, v => Model.ClientID = v);
    }


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
