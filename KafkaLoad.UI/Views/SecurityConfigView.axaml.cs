using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using KafkaLoad.UI.ViewModels.Clients;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaLoad.UI.Views;

public partial class SecurityConfigView : UserControl
{
    public SecurityConfigView()
    {
        InitializeComponent();

        this.FindControl<Button>("BrowseCaBtn")!.Click +=
            async (_, _) => await BrowseAsync(path => Vm!.SslCaLocation = path);

        this.FindControl<Button>("BrowseCertBtn")!.Click +=
            async (_, _) => await BrowseAsync(path => Vm!.SslCertificateLocation = path);

        this.FindControl<Button>("BrowseKeyBtn")!.Click +=
            async (_, _) => await BrowseAsync(path => Vm!.SslKeyLocation = path);
    }

    private SecurityConfigViewModel? Vm => DataContext as SecurityConfigViewModel;

    private async Task BrowseAsync(Action<string> setter)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select file",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Certificate / Key files") { Patterns = new[] { "*.pem", "*.crt", "*.cert", "*.key", "*.p12", "*.pfx" } },
                new("All files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
            setter(files[0].Path.LocalPath);
    }
}
