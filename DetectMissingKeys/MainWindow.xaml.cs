using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;

namespace DetectMissingKeys;

public partial class MainWindow
{
    private string _originalFilePath = string.Empty;
    private string _inputFolderPath = string.Empty;
    private string _outputFolderPath = string.Empty;
    private string _reportFilePath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void SelectOriginalFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "XAML Files (*.xaml)|*.xaml",
            Title = "Select the Original XAML File"
        };

        if (openFileDialog.ShowDialog() != true) return;

        _originalFilePath = openFileDialog.FileName;
        Console.WriteLine($"Original file selected: {_originalFilePath}");
        StatusTextBlock.Text = $"Original file selected: {_originalFilePath}";
        OriginalFileTextBox.Text = _originalFilePath;
    }

    private void SelectInputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog();
        folderDialog.Description = "Select Input Folder";
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        _inputFolderPath = folderDialog.SelectedPath;
        Console.WriteLine($"Input folder selected: {_inputFolderPath}");
        StatusTextBlock.Text = $"Input folder selected: {_inputFolderPath}";
        InputFolderTextBox.Text = _inputFolderPath;
    }

    private void SelectOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog();
        folderDialog.Description = "Select Output Folder";
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        _outputFolderPath = folderDialog.SelectedPath;
        Console.WriteLine($"Output folder selected: {_outputFolderPath}");
        StatusTextBlock.Text = $"Output folder selected: {_outputFolderPath}";
        OutputFolderTextBox.Text = _outputFolderPath;
    }

    private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_originalFilePath) || string.IsNullOrEmpty(_inputFolderPath) || string.IsNullOrEmpty(_outputFolderPath))
        {
            StatusTextBlock.Text = "Please select all required inputs.";
            Console.WriteLine("Error: Missing required inputs.");
            return;
        }

        try
        {
            Console.WriteLine("Extracting keys from the original file...");
            var originalKeys = ExtractKeys(_originalFilePath);
            Console.WriteLine($"Extracted {originalKeys.Count} keys from the original file.");

            var report = new StringBuilder();

            foreach (var file in Directory.GetFiles(_inputFolderPath, "*.xaml"))
            {
                Console.WriteLine($"Processing file: {file}");
                var fileKeys = ExtractKeys(file);
                Console.WriteLine($"Extracted {fileKeys.Count} keys from file: {Path.GetFileName(file)}");

                var missingKeys = originalKeys.Except(fileKeys).ToList();
                var extraKeys = fileKeys.Except(originalKeys).ToList();

                report.AppendLine(CultureInfo.InvariantCulture, $"File: {Path.GetFileName(file)}");
                if (missingKeys.Count != 0)
                {
                    report.AppendLine("  Missing Keys:");
                    missingKeys.ForEach(key => report.AppendLine(CultureInfo.InvariantCulture, $"    - {key}"));
                    Console.WriteLine($"Missing keys in {Path.GetFileName(file)}: {string.Join(", ", missingKeys)}");
                }

                if (extraKeys.Count != 0)
                {
                    report.AppendLine("  Extra Keys:");
                    extraKeys.ForEach(key => report.AppendLine(CultureInfo.InvariantCulture, $"    - {key}"));
                    Console.WriteLine($"Extra keys in {Path.GetFileName(file)}: {string.Join(", ", extraKeys)}");
                }

                if (missingKeys.Count == 0 && extraKeys.Count == 0)
                {
                    report.AppendLine("  All keys matched.");
                    Console.WriteLine($"All keys matched for file: {Path.GetFileName(file)}");
                }

                report.AppendLine();
            }

            var outputFilePath = Path.Combine(_outputFolderPath, "XamlKeyComparisonReport.txt");
            File.WriteAllText(outputFilePath, report.ToString());

            _reportFilePath = outputFilePath;
            StatusTextBlock.Text = $"Report generated successfully: {Path.GetFileName(_reportFilePath)}";

            // Enable the open report button
            OpenReportButton.IsEnabled = true;

            StatusTextBlock.Text = $"Report generated successfully: {outputFilePath}";
            Console.WriteLine($"Report saved at: {outputFilePath}");
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void OpenReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_reportFilePath) && File.Exists(_reportFilePath))
        {
            try
            {
                // Use Process.Start to open the file with the default application
                Process.Start(new ProcessStartInfo
                {
                    FileName = _reportFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error opening report: {ex.Message}";
            }
        }
        else
        {
            StatusTextBlock.Text = "Report file not found. Please generate the report first.";
        }
    }

    private static List<string> ExtractKeys(string filePath)
    {
        var keys = new List<string>();
        try
        {
            var xDocument = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);

            foreach (var element in xDocument.Descendants())
            {
                var keyAttribute = element.Attributes().FirstOrDefault(static attr => attr.Name.LocalName == "Key");
                if (keyAttribute != null)
                {
                    keys.Add(keyAttribute.Value);
                }
            }

            Console.WriteLine($"Extracted {keys.Count} keys from {Path.GetFileName(filePath)}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing file {filePath}: {ex.Message}");
        }

        return keys;
    }
}