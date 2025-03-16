using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Windows.Data;
using System.Globalization;
using System.Diagnostics; // For Process.Start
using MessageBox = System.Windows.MessageBox;

namespace DetectResourceStrings;

public partial class MainWindow
{
    private string? _reportFilePath;

    public MainWindow()
    {
        InitializeComponent();
        UpdateCompareButtonState();
    }

    /// <summary>
    /// Event handler for input folder browse button
    /// </summary>
    private void BrowseInputButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFolder = SelectFolder("Select Input Folder");
        if (!string.IsNullOrEmpty(selectedFolder))
        {
            InputFolderTextBox.Text = selectedFolder;
            UpdateCompareButtonState();
        }
    }

    /// <summary>
    /// Event handler for resource file browse button
    /// </summary>
    private void BrowseResourceButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFile = SelectFile("Select Resource File", "XAML files (*.xaml)|*.xaml");
        if (!string.IsNullOrEmpty(selectedFile))
        {
            ResourceFileTextBox.Text = selectedFile;
            UpdateCompareButtonState();
        }
    }

    /// <summary>
    /// Event handler for output folder browse button
    /// </summary>
    private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFolder = SelectFolder("Select Folder to Save Report");
        if (!string.IsNullOrEmpty(selectedFolder))
        {
            OutputFolderTextBox.Text = selectedFolder;
            UpdateCompareButtonState();
        }
    }

    /// <summary>
    /// Enable/disable the Compare button based on selections
    /// </summary>
    private void UpdateCompareButtonState()
    {
        CompareButton.IsEnabled = !string.IsNullOrEmpty(InputFolderTextBox.Text) &&
                                  !string.IsNullOrEmpty(ResourceFileTextBox.Text) &&
                                  !string.IsNullOrEmpty(OutputFolderTextBox.Text);
    }

    /// <summary>
    /// Event handler for the Compare button (replaces StartComparison_Click)
    /// </summary>
    private async void CompareButton_Click(object sender, RoutedEventArgs e)
    {
        var inputFolder = InputFolderTextBox.Text;
        var resourceFilePath = ResourceFileTextBox.Text;
        var reportPath = OutputFolderTextBox.Text;

        // Validate inputs
        if (string.IsNullOrEmpty(inputFolder) || string.IsNullOrEmpty(resourceFilePath) || string.IsNullOrEmpty(reportPath))
        {
            MessageBox.Show("Please select all required folders and files.",
                "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Disable UI during processing
        SetControlsEnabled(false);

        var list1 = new List<string>();
        var list2 = new List<string>();

        try
        {
            // Search for TryFindResource and DynamicResource strings in files
            foreach (var file in Directory.EnumerateFiles(inputFolder, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    // Skip binary files by checking their extensions
                    var validTextExtensions = new[] { ".txt", ".cs", ".xaml", ".json", ".xml" };
                    if (!validTextExtensions.Contains(Path.GetExtension(file).ToLower()))
                    {
                        continue;
                    }

                    // Read the content
                    var content = await File.ReadAllTextAsync(file);

                    var tryFindResourceMatches = Regex.Matches(content, "TryFindResource\\(\"(.*?)\"\\)");
                    foreach (Match match in tryFindResourceMatches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            list1.Add(match.Groups[1].Value);
                        }
                    }

                    var dynamicResourceMatches = Regex.Matches(content, "{DynamicResource (.*?)}");
                    foreach (Match match in dynamicResourceMatches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            list1.Add(match.Groups[1].Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the error for specific files
                    Debug.WriteLine($"Error reading file {file}: {ex.Message}");
                }
            }

            // Search for x:Key strings in the resource file
            var resourceContent = await File.ReadAllTextAsync(resourceFilePath);
            var resourceMatches = MyRegex().Matches(resourceContent);

            foreach (Match match in resourceMatches)
            {
                if (match.Groups.Count > 1)
                {
                    list2.Add(match.Groups[1].Value);
                }
            }

            // Compare lists
            var matchedStrings = list1.Intersect(list2).ToList();
            var unmatchedStringsFromInput = list1.Except(list2).ToList();
            var unmatchedStringsFromResource = list2.Except(list1).ToList();

            // Generate the report
            var reportFilePath = Path.Combine(reportPath, "report.txt");
            await using (var writer = new StreamWriter(reportFilePath))
            {
                await writer.WriteLineAsync($"Number of matched strings: {matchedStrings.Count}");
                await writer.WriteLineAsync($"Number of unmatched strings from input files: {unmatchedStringsFromInput.Count}");
                await writer.WriteLineAsync($"Number of unmatched strings from resource file: {unmatchedStringsFromResource.Count}");

                await writer.WriteLineAsync("\nUnmatched strings from input files:");
                foreach (var unmatched in unmatchedStringsFromInput)
                {
                    await writer.WriteLineAsync(unmatched);
                }

                await writer.WriteLineAsync("\nUnmatched strings from resource file:");
                foreach (var unmatched in unmatchedStringsFromResource)
                {
                    await writer.WriteLineAsync(unmatched);
                }
            }

            _reportFilePath = reportFilePath;
            UpdateReportButtonState();

            MessageBox.Show($"Report generated at: {reportFilePath}",
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable UI
            SetControlsEnabled(true);
        }
    }

    private void OpenReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_reportFilePath) && File.Exists(_reportFilePath))
        {
            // Open the file with the default application
            Process.Start(new ProcessStartInfo
            {
                FileName = _reportFilePath,
                UseShellExecute = true
            });
        }
        else
        {
            MessageBox.Show("Report file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            // Update button state in the case file was deleted
            UpdateReportButtonState();
        }
    }


    /// <summary>
    /// Helper method to enable or disable all controls during processing
    /// </summary>
    private void SetControlsEnabled(bool enabled)
    {
        BrowseInputButton.IsEnabled = enabled;
        BrowseResourceButton.IsEnabled = enabled;
        BrowseOutputButton.IsEnabled = enabled;
        CompareButton.IsEnabled = enabled && !string.IsNullOrEmpty(InputFolderTextBox.Text) &&
                                  !string.IsNullOrEmpty(ResourceFileTextBox.Text) &&
                                  !string.IsNullOrEmpty(OutputFolderTextBox.Text);
    }

    private static string? SelectFolder(string description)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = description;
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            return dialog.SelectedPath;
        }

        return null;
    }

    private static string? SelectFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }

    private void UpdateReportButtonState()
    {
        // Enable button only if the report file exists
        OpenReportButton.IsEnabled = !string.IsNullOrEmpty(_reportFilePath) && File.Exists(_reportFilePath);
    }

    [GeneratedRegex("x:Key=\"(.*?)\"")]
    private static partial Regex MyRegex();
}

/// <summary>
/// Converter to enable/disable buttons based on whether strings are empty
/// </summary>
public class StringNotEmptyToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return !string.IsNullOrWhiteSpace(stringValue);
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}