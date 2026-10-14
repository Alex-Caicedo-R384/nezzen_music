using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Themes.Fluent;
using Nezzen.Application;
using Nezzen.Core;
using Nezzen.Infrastructure;

namespace Nezzen.Desktop;

public sealed class App : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NezzenMusic");
            Directory.CreateDirectory(dataDirectory);
            var repository = new SqliteLibraryRepository(Path.Combine(dataDirectory, "library.db"));
            var scanner = new IncrementalLibraryScanner(repository, new PhysicalMusicFileSystem(), new TagLibMetadataReader(), new StreamingFileFingerprinter());
            var vm = new MainWindowViewModel(repository, scanner);
            var window = new Window { Title = "Nezzen Music — Biblioteca", Width = 960, Height = 720, MinWidth = 640, MinHeight = 560, DataContext = vm };
            var layout = new Grid { Margin = new Thickness(24), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,*") };
            var heading = new TextBlock { Text = "Biblioteca musical", FontSize = 26, Margin = new Thickness(0, 0, 0, 16) }; layout.Children.Add(heading);
            var pathRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 0, 0, 12) };
            var input = new TextBox { PlaceholderText = "Ruta de carpeta musical", MinWidth = 180 };
            input.Bind(TextBox.TextProperty, new Binding(nameof(vm.FolderPath)) { Mode = BindingMode.TwoWay });
            var choose = new Button { Content = "Seleccionar…" };
            choose.Bind(Button.IsEnabledProperty, new Binding(nameof(vm.IsBusy)) { Converter = new Avalonia.Data.Converters.FuncValueConverter<bool, bool>(busy => !busy) });
            choose.Click += async (_, _) =>
            {
                try
                {
                    var chosen = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Carpeta musical", AllowMultiple = false });
                    if (chosen.FirstOrDefault()?.TryGetLocalPath() is { } path) vm.FolderPath = path;
                }
                catch (Exception) { input.PlaceholderText = "Selector no disponible; escribe la ruta de la carpeta."; }
            };
            var register = ButtonFor("Registrar carpeta", nameof(vm.RegisterCommand));
            Grid.SetColumn(choose, 1); Grid.SetColumn(register, 2); pathRow.Children.Add(input); pathRow.Children.Add(choose); pathRow.Children.Add(register);
            Grid.SetRow(pathRow, 1); layout.Children.Add(pathRow);
            var folderRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 0, 0, 12) };
            var folders = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Sin carpetas registradas",
                ItemTemplate = new FuncDataTemplate<MusicFolder>((folder, _) => new TextBlock { Text = folder is null ? "" : $"{(folder.Enabled ? "Habilitada" : "Deshabilitada")} · {folder.Path}" })
            };
            folders.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(vm.Folders)));
            folders.Bind(ComboBox.SelectedItemProperty, new Binding(nameof(vm.SelectedFolder)) { Mode = BindingMode.TwoWay });
            var toggle = ButtonFor("Habilitar / deshabilitar", nameof(vm.ToggleFolderCommand)); Grid.SetColumn(toggle, 1);
            folderRow.Children.Add(folders); folderRow.Children.Add(toggle); Grid.SetRow(folderRow, 2); layout.Children.Add(folderRow);
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 0, 0, 12),
                Children =
                { ButtonFor("Analizar habilitadas", nameof(vm.AnalyzeCommand)), ButtonFor("Aplicar cambios", nameof(vm.ApplyCommand)), ButtonFor("Cancelar", nameof(vm.CancelCommand)) }
            };
            Grid.SetRow(actions, 3); layout.Children.Add(actions);
            var status = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
            status.Bind(TextBlock.TextProperty, new Binding(nameof(vm.Status))); Grid.SetRow(status, 4); layout.Children.Add(status);
            var summary = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
            summary.Bind(TextBlock.TextProperty, new Binding(nameof(vm.Summary))); Grid.SetRow(summary, 5); layout.Children.Add(summary);
            var results = new ListBox(); results.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(vm.Results))); Grid.SetRow(results, 6); layout.Children.Add(results);
            window.Content = layout;
            window.Opened += async (_, _) => await vm.InitializeAsync();
            window.Closing += (_, _) => vm.Cancel();
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
    private static Button ButtonFor(string label, string command)
    {
        var button = new Button { Content = label };
        button.Bind(Button.CommandProperty, new Binding(command)); return button;
    }
}
