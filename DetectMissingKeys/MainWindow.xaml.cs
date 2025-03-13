using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DetectMissingKeys;

public partial class MainWindow
{
    private string _originalFilePath = string.Empty;
    private string _inputFolderPath = string.Empty;
    private string _outputFolderPath = string.Empty;

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

        if (openFileDialog.ShowDialog() == true)
        {
            _originalFilePath = openFileDialog.FileName;
            Console.WriteLine($"Original file selected: {_originalFilePath}");
            StatusTextBlock.Text = $"Original file selected: {_originalFilePath}";
        }
    }

    private void SelectInputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog();
        folderDialog.Description = "Select Input Folder";
        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _inputFolderPath = folderDialog.SelectedPath;
            Console.WriteLine($"Input folder selected: {_inputFolderPath}");
            StatusTextBlock.Text = $"Input folder selected: {_inputFolderPath}";
        }
    }

    private void SelectOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog();
        folderDialog.Description = "Select Output Folder";
        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _outputFolderPath = folderDialog.SelectedPath;
            Console.WriteLine($"Output folder selected: {_outputFolderPath}");
            StatusTextBlock.Text = $"Output folder selected: {_outputFolderPath}";
        }
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

                report.AppendLine($"File: {Path.GetFileName(file)}");
                if (missingKeys.Count != 0)
                {
                    report.AppendLine("  Missing Keys:");
                    missingKeys.ForEach(key => report.AppendLine($"    - {key}"));
                    Console.WriteLine($"Missing keys in {Path.GetFileName(file)}: {string.Join(", ", missingKeys)}");
                }
                if (extraKeys.Count != 0)
                {
                    report.AppendLine("  Extra Keys:");
                    extraKeys.ForEach(key => report.AppendLine($"    - {key}"));
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
            StatusTextBlock.Text = $"Report generated successfully: {outputFilePath}";
            Console.WriteLine($"Report saved at: {outputFilePath}");
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            Console.WriteLine($"Error: {ex.Message}");
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
                var keyAttribute = element.Attributes().FirstOrDefault(attr => attr.Name.LocalName == "Key");
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