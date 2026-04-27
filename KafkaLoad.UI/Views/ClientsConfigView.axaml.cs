using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using KafkaLoad.UI.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System.Reactive.Disposables.Fluent;

namespace KafkaLoad.UI.Views;

public partial class ClientsConfigView : ReactiveUserControl<ClientsConfigViewModel>
{
    public ClientsConfigView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel!.ConfirmDeleteInteraction.RegisterHandler(async ctx =>
            {
                var dialog = new ConfirmDeleteDialog(ctx.Input);
                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner != null)
                    await dialog.ShowDialog(owner);
                else
                    dialog.Show();
                ctx.SetOutput(dialog.Result);
            }).DisposeWith(disposables);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
