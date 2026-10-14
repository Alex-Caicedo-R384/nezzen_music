using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nezzen.Application;
using Nezzen.Core;

namespace Nezzen.Desktop;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly ILibraryRepository repository;
    private readonly MusicFolderService folders;
    private readonly ILibraryScanner scanner;
    private CancellationTokenSource? operation;
    private ScanPreview? preview;
    private bool isBusy;
    private string folderPath = "";
    private string status = "Registra una carpeta para comenzar. Analizar no modifica la biblioteca.";
    private string summary = "Sin análisis pendiente.";
    private MusicFolder? selectedFolder;
    public ObservableCollection<MusicFolder> Folders { get; } = [];
    public ObservableCollection<string> Results { get; } = [];
    public string FolderPath { get => folderPath; set => SetProperty(ref folderPath, value); }
    public string Status { get => status; private set => SetProperty(ref status, value); }
    public string Summary { get => summary; private set => SetProperty(ref summary, value); }
    public MusicFolder? SelectedFolder { get => selectedFolder; set { SetProperty(ref selectedFolder, value); RefreshCommands(); } }
    public bool IsBusy { get => isBusy; private set { SetProperty(ref isBusy, value); RefreshCommands(); } }
    public AsyncRelayCommand RegisterCommand { get; }
    public AsyncRelayCommand ToggleFolderCommand { get; }
    public AsyncRelayCommand AnalyzeCommand { get; }
    public AsyncRelayCommand ApplyCommand { get; }
    public RelayCommand CancelCommand { get; }

    public MainWindowViewModel(ILibraryRepository repository, ILibraryScanner scanner)
    {
        this.repository = repository; folders = new(repository); this.scanner = scanner;
        RegisterCommand = new(() => RunAsync(async token =>
        {
            await Task.Run(() => folders.RegisterAsync(FolderPath, token), token);
            preview = null; await ReloadAsync(token); Status = "Carpeta registrada. Puedes analizar todas las carpetas habilitadas.";
        }), () => !IsBusy);
        ToggleFolderCommand = new(() => RunAsync(async token =>
        {
            if (SelectedFolder is not { } folder) return;
            await Task.Run(() => folders.SetEnabledAsync(folder.Id, !folder.Enabled, token), token);
            preview = null; await ReloadAsync(token); Status = "Estado de carpeta actualizado. Vuelve a analizar.";
        }), () => !IsBusy && SelectedFolder is not null);
        AnalyzeCommand = new(() => RunAsync(async token =>
        {
            preview = null; Results.Clear(); Summary = "Analizando…";
            preview = await scanner.AnalyzeAsync(Folders.ToArray(), Progress(), token);
            var r = preview.Result;
            Summary = $"Nuevos: {r.New} · Modificados: {r.Modified} · Sin cambios: {r.Unchanged}\nMovidos: {r.Moved} · No encontrados: {r.Missing} · No disponibles: {r.Unavailable}\nErrores: {r.Errors} · Movimientos ambiguos: {r.PossibleMoves}";
            foreach (var entry in preview.Entries.Take(500)) Results.Add($"{Label(entry.Change)} · {entry.Path}" + (entry.Message is null ? "" : $" — {entry.Message}"));
            if (preview.Entries.Count > 500) Results.Add($"Se muestran 500 de {preview.Entries.Count} resultados. El resumen incluye todos.");
            Status = "Vista previa lista. Revisa el resumen y pulsa Aplicar cambios para confirmar.";
        }), () => !IsBusy && Folders.Any(f => f.Enabled));
        ApplyCommand = new(() => RunAsync(async token =>
        {
            if (preview is null) return;
            var id = preview.Id;
            try { await scanner.ApplyAsync(id, Progress(), token); Status = "Cambios aplicados. Los archivos musicales se conservaron intactos."; }
            finally { preview = null; }
        }), () => !IsBusy && preview is not null);
        CancelCommand = new(() => operation?.Cancel(), () => IsBusy);
    }
    public Task InitializeAsync() => RunAsync(async token =>
    {
        await Task.Run(() => repository.InitializeAsync(token), token); await ReloadAsync(token);
    });
    private async Task ReloadAsync(CancellationToken token)
    {
        var items = await Task.Run(() => folders.GetAsync(token), token);
        Folders.Clear(); foreach (var folder in items) Folders.Add(folder);
        SelectedFolder = Folders.FirstOrDefault();
    }
    private IProgress<ScanProgress> Progress() => new Progress<ScanProgress>(p =>
        Status = $"{p.State} · Encontrados: {p.Found} · Procesados: {p.Processed} · Errores: {p.Errors}\n{p.CurrentPath}");
    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy) return;
        using var cts = new CancellationTokenSource(); operation = cts; IsBusy = true;
        try { await action(cts.Token); }
        catch (OperationCanceledException) { preview = null; Status = "Operación cancelada; no se aplicaron cambios parciales."; }
        catch (Exception e) { preview = null; Status = $"No se pudo completar: {e.Message} Vuelve a analizar o revisa la carpeta."; }
        finally { operation = null; IsBusy = false; }
    }
    public void Cancel() => operation?.Cancel();
    private void RefreshCommands()
    {
        RegisterCommand?.NotifyCanExecuteChanged(); ToggleFolderCommand?.NotifyCanExecuteChanged();
        AnalyzeCommand?.NotifyCanExecuteChanged(); ApplyCommand?.NotifyCanExecuteChanged(); CancelCommand?.NotifyCanExecuteChanged();
    }
    private static string Label(FileChange change) => change switch
    {
        FileChange.New => "Nuevo",
        FileChange.Modified => "Modificado",
        FileChange.Unchanged => "Sin cambios",
        FileChange.Moved => "Movido",
        FileChange.Missing => "No encontrado",
        FileChange.Error => "Error",
        FileChange.Unavailable => "No disponible",
        _ => change.ToString()
    };
}
