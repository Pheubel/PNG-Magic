using Avalonia.Controls;
using PngMagic.Gui.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using PngMagic.Core;
using Avalonia;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml;

namespace PngMagic.Gui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ICommand SelectImageTargetCommand { get; }
        public ICommand PackIntoImageTargetCommand { get; }
        public ICommand ExtractFromImageTargetCommand { get; }
        public ICommand ViewPackedImagesTargetCommand { get; }

        //  Button _selectTargetButton;
        private readonly Button _packIntoTargetButton;
        private readonly Button _extractFromTargetButton;

        private string _selectedImage = string.Empty;
        public string SelectedImage
        {
            get => _selectedImage;
            set => this.RaiseAndSetIfChanged(ref _selectedImage, value);
        }

        public MainWindowViewModel()
        {
            // _selectTargetButton = MainWindow.Instance.Get<Button>("SelectTargetButton");
            _packIntoTargetButton = MainWindow.Instance.Get<Button>("PackIntoTargetButton");
            _extractFromTargetButton = MainWindow.Instance.Get<Button>("ExtractFromTargetButton");

            SelectImageTargetCommand = ReactiveCommand.Create(async () =>
            {
                var path = await SelectTargetFileAsync();

                if (path == null)
                    return;

                SelectedImage = path;

                // enable other controls
                _packIntoTargetButton.IsEnabled = true;
                _extractFromTargetButton.IsEnabled = true;
                
            });

            PackIntoImageTargetCommand = ReactiveCommand.Create(async () =>
            {
                var payloads = await SelectPayloadFilesAsync();

                if (payloads == null)
                    return;

                string tempPath = Path.GetTempFileName();

                string? destination;
                using (var outStream = File.OpenWrite(tempPath))
                {
                    PackOperation.Start(SelectedImage, outStream, payloads.AsSpan());

                    var dialog = new SaveFileDialog();
                    dialog.Filters.Add(new FileDialogFilter()
                    {
                        Extensions = new() { "png" },
                        Name = "PNG images"
                    });
                    dialog.InitialFileName = $"packed_{Path.GetFileName(SelectedImage)}";
                    dialog.Title = "Select ouput file";
                    dialog.DefaultExtension = ".png";
                    dialog.Directory = Path.GetDirectoryName(SelectedImage);

                    destination = await dialog.ShowAsync(MainWindow.Instance);
                    if (destination == null)
                    {
                        File.Delete(tempPath);
                        return;
                    }
                }

                try
                {
                    File.Move(tempPath, destination, true);
                }
                catch
                {
                    // TODO: catch and show the error
                }
            });

            ExtractFromImageTargetCommand = ReactiveCommand.Create(async () =>
            {
                if (!File.Exists(SelectedImage))
                {
                    // TODO: show error
                    return;
                }

                var payloads = ExtractOperation.GetInjectedPayloads(SelectedImage);

                if (!payloads.Any())
                {
                    // TODO: show warning
                    return;
                }

                var outputDirectory = await SelectOutputDirectory();

                // check if it was canceled
                if (outputDirectory == null)
                    return;

                List<Task> tasks = new();

                foreach (var payload in payloads)
                {
                    if (payload is FilePayload filePayload)
                        tasks.Add(File.WriteAllBytesAsync(Path.Combine(outputDirectory, filePayload.FileName), filePayload.PayloadData));
                }

                await Task.WhenAll(tasks);
            });

            ViewPackedImagesTargetCommand = ReactiveCommand.Create( () =>
            {
                if (!File.Exists(SelectedImage))
                {
                    // TODO: show error
                    return;
                }

                var payloads = ExtractOperation.GetInjectedPayloads(SelectedImage);

                if (!payloads.Any())
                {
                    // TODO: show warning
                    return;
                }

                // TODO: show payloads as images
            });
        }

        private static async Task<string?> SelectTargetFileAsync()
        {
            var fileDialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = "Select target file"
            };
            fileDialog.Filters.Add(new FileDialogFilter()
            {
                Extensions = new() { "png" },
                Name = "PNG images"
            });

            var dialogResult = await fileDialog.ShowAsync(MainWindow.Instance);

            if (dialogResult == null || dialogResult.Length != 1)
                return null;

            return dialogResult[0];
        }

        private static async Task<string[]?> SelectPayloadFilesAsync()
        {
            var fileDialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Title = "Select payload files"
            };

            return await fileDialog.ShowAsync(MainWindow.Instance);
        }

        private static Task<string?> SelectOutputDirectory()
        {
            var directoryDialog = new OpenFolderDialog
            {
                Title = "Select output directory"
            };

            return directoryDialog.ShowAsync(MainWindow.Instance);
        }
    }
}
