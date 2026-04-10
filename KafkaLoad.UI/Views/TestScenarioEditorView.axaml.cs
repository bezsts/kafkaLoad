using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using KafkaLoad.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace KafkaLoad.UI.Views;

public partial class TestScenarioEditorView : ReactiveUserControl<TestScenarioEditorViewModel>
{
    public TestScenarioEditorView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d.Add(ViewModel!.ImportAvroSchemaInteraction.RegisterHandler(async ctx =>
            {
                ctx.SetOutput(await PickFileAsync("Import Avro Schema",
                [
                    new FilePickerFileType("Avro Schema") { Patterns = ["*.avsc", "*.json"] },
                    new FilePickerFileType("All Files") { Patterns = ["*"] }
                ]));
            }));

            d.Add(ViewModel!.ImportProtobufSchemaInteraction.RegisterHandler(async ctx =>
            {
                ctx.SetOutput(await PickFileAsync("Import Protobuf Schema",
                [
                    new FilePickerFileType("Proto Schema") { Patterns = ["*.proto"] },
                    new FilePickerFileType("All Files") { Patterns = ["*"] }
                ]));
            }));
        });
    }

    private async Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFileType> filters)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } sp)
            return null;

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        if (files.Count == 0)
            return null;

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
