using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Qingtu.Models;
using Qingtu.Services;

namespace Qingtu;

public partial class MainWindow : Window
{
    private readonly List<string> _files = [];
    private bool _isCompressTab = true;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(DropZone, true);
        DropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DropZone.AddHandler(DragDrop.DropEvent, OnDrop);
        SliderQuality.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == "Value")
                TxtQuality.Text = ((int)SliderQuality.Value).ToString();
        };
        UpdateTabUi();
        UpdateRunLabel();
    }

    private void OnTabCompress(object? sender, RoutedEventArgs e)
    {
        _isCompressTab = true;
        UpdateTabUi();
    }

    private void OnTabStitch(object? sender, RoutedEventArgs e)
    {
        _isCompressTab = false;
        UpdateTabUi();
    }

    private void UpdateTabUi()
    {
        PanelCompress.IsVisible = _isCompressTab;
        TxtStitchHint.IsVisible = !_isCompressTab;
        StyleTab(BtnTabCompress, _isCompressTab);
        StyleTab(BtnTabStitch, !_isCompressTab);
        UpdateRunLabel();
    }

    private static void StyleTab(Button btn, bool active)
    {
        btn.FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal;
        btn.Foreground = active
            ? (IBrush?)Application.Current!.FindResource("TealBrush")
            : (IBrush?)Application.Current!.FindResource("MutedBrush");
        btn.Background = active
            ? (IBrush?)Application.Current!.FindResource("InputBrush")
            : (IBrush?)Application.Current!.FindResource("CardBrush");
        btn.BorderBrush = active
            ? (IBrush?)Application.Current!.FindResource("TealBrush")
            : (IBrush?)Application.Current!.FindResource("BorderBrush");
    }

    private void UpdateRunLabel() =>
        BtnRun.Content = _isCompressTab ? $"开始压缩 ({_files.Count})" : $"开始拼接 ({_files.Count})";

    private async void OnPickFiles(object? sender, RoutedEventArgs e)
    {
        var sp = StorageProvider;
        if (sp is null) return;
        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif", "*.bmp"] }
            ]
        });
        AddFiles(files.Select(f => f.Path.LocalPath));
    }

    private async void OnPickOutDir(object? sender, RoutedEventArgs e)
    {
        var sp = StorageProvider;
        if (sp is null) return;
        var folders = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        var folder = folders.FirstOrDefault();
        if (folder is null) return;
        TxtOutDir.Text = folder.Path.LocalPath;
        ChkCustomOut.IsChecked = true;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
            e.DragEffects = DragDropEffects.Copy;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var paths = e.Data.GetFiles()?.Select(f => f?.Path.LocalPath).Where(p => p is not null).Cast<string>() ?? [];
        AddFiles(paths);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        foreach (var p in paths)
        {
            if (!ImagePaths.IsImageFile(p) || _files.Contains(p)) continue;
            _files.Add(p);
            FileList.Items.Add(Path.GetFileName(p));
        }

        TxtDropHint.Text = _files.Count > 0 ? $"已选 {_files.Count} 个文件" : "拖拽图片到这里";
        TxtDropHint.Foreground = _files.Count > 0
            ? (IBrush)Application.Current!.FindResource("TealBrush")!
            : (IBrush)Application.Current!.FindResource("MutedBrush")!;
        UpdateRunLabel();
    }

    private JobOptions BuildOptions()
    {
        _ = int.TryParse(TxtMaxSide.Text?.Trim(), out var maxSide);
        if (maxSide < 0) maxSide = 0;
        return new JobOptions
        {
            Quality = (int)SliderQuality.Value,
            MaxSide = maxSide,
            Format = GetFormat(),
            OutputDir = ChkCustomOut.IsChecked == true && !string.IsNullOrWhiteSpace(TxtOutDir.Text)
                ? TxtOutDir.Text.Trim()
                : null
        };
    }

    private OutputFormat GetFormat()
    {
        if (FmtJpeg.IsChecked == true) return OutputFormat.Jpeg;
        if (FmtPng.IsChecked == true) return OutputFormat.Png;
        if (FmtWebp.IsChecked == true) return OutputFormat.WebP;
        return OutputFormat.KeepOriginal;
    }

    private async void OnRun(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_files.Count == 0)
        {
            TxtLog.Text = "请先添加图片";
            return;
        }

        _busy = true;
        BtnRun.IsEnabled = false;
        try
        {
            if (_isCompressTab)
            {
                var opts = BuildOptions();
                var results = await Task.Run(() => ImageCompressService.RunJobs(_files, opts));
                TxtLog.Text = FormatResults.FormatLog(results);
            }
            else
            {
                if (_files.Count < 2)
                {
                    TxtLog.Text = "拼接需要至少 2 张图";
                    return;
                }

                var outPath = await Task.Run(() =>
                {
                    var opts = BuildOptions();
                    var path = !string.IsNullOrWhiteSpace(opts.OutputDir)
                        ? Path.Combine(opts.OutputDir!, "stitched.png")
                        : Path.Combine(ImagePaths.DefaultOutputDir(_files[0]), "stitched.png");
                    ImageStitchService.StitchHorizontal(_files, path);
                    return path;
                });
                TxtLog.Text = $"拼接完成 → {outPath}";
            }
        }
        catch (Exception ex)
        {
            TxtLog.Text = ex.Message;
        }
        finally
        {
            _busy = false;
            BtnRun.IsEnabled = true;
        }
    }

    private void OnClear(object? sender, RoutedEventArgs e)
    {
        _files.Clear();
        FileList.Items.Clear();
        TxtLog.Text = "";
        TxtDropHint.Text = "拖拽图片到这里";
        TxtDropHint.Foreground = (IBrush)Application.Current!.FindResource("MutedBrush")!;
        UpdateRunLabel();
    }
}
