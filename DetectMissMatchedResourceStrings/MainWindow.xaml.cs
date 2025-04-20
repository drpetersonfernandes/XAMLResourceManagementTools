using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Data;
using System.Globalization;

namespace DetectMissMatchedResourceStrings;

public partial class MainWindow
{
    private string? _lastGeneratedReportPath;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event handler for input folder browse button
    /// </summary>
    private void BrowseInputButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFolder = SelectFolder("Select Input Folder");
        if (string.IsNullOrEmpty(selectedFolder)) return;

        InputFolderTextBox.Text = selectedFolder;
        UpdateProcessButtonState();
    }

    /// <summary>
    /// Event handler for output folder browse button
    /// </summary>
    private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFolder = SelectFolder("Select Folder to Save Report");
        if (string.IsNullOrEmpty(selectedFolder)) return;

        OutputFolderTextBox.Text = selectedFolder;
        UpdateProcessButtonState();
    }

    /// <summary>
    /// Enable/disable the Process button based on folder selections
    /// </summary>
    private void UpdateProcessButtonState()
    {
        ProcessButton.IsEnabled = !string.IsNullOrEmpty(InputFolderTextBox.Text) &&
                                  !string.IsNullOrEmpty(OutputFolderTextBox.Text);
    }

    /// <summary>
    /// Click handler for the Process button
    /// </summary>
    private async void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var inputFolder = InputFolderTextBox.Text;
            var reportFolder = OutputFolderTextBox.Text;

            // Check if both folders are selected
            if (string.IsNullOrEmpty(inputFolder) || string.IsNullOrEmpty(reportFolder))
            {
                System.Windows.MessageBox.Show("Please select both input and output folders.",
                    "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable the UI elements during processing
            SetControlsEnabled(false);

            // Dictionary to hold key and a set of fallback values found.
            var resourceDictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            // Define valid file extensions to scan.
            var validExtensions = new[] { ".txt", ".cs", ".xaml", ".json", ".xml" };

            // Regex pattern to match the desired pattern.
            // This regex matches: TryFindResource("KEY") ?? "VALUE"
            const string pattern = """
                                   TryFindResource\(\s*"(?<key>[^"]+)"\s*\)\s*\?\?\s*"(?<value>[^"]+)"
                                   """;

            try
            {
                // Enumerate files in the selected input folder.
                foreach (var file in Directory.EnumerateFiles(inputFolder, "*.*", SearchOption.AllDirectories))
                {
                    // Only process files with valid extensions.
                    if (!validExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    {
                        continue;
                    }

                    try
                    {
                        var content = await File.ReadAllTextAsync(file);

                        // Find all matches in the file.
                        var matches = Regex.Matches(content, pattern);
                        foreach (Match match in matches)
                        {
                            if (!match.Success) continue;

                            var key = match.Groups["key"].Value;
                            var value = match.Groups["value"].Value;

                            if (!resourceDictionary.ContainsKey(key))
                            {
                                resourceDictionary[key] = new HashSet<string>(StringComparer.Ordinal);
                            }

                            resourceDictionary[key].Add(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }

                // Prepare a list of keys that have multiple (mismatched) fallback values.
                var mismatchedEntries = resourceDictionary
                    .Where(entry => entry.Value.Count > 1)
                    .ToList();

                // Generate the report.
                var reportFilePath = Path.Combine(reportFolder, "MismatchReport.txt");
                await using (var writer = new StreamWriter(reportFilePath))
                {
                    await writer.WriteLineAsync("Resource Key Mismatch Report");
                    await writer.WriteLineAsync("============================");
                    await writer.WriteLineAsync($"Total Keys Scanned: {resourceDictionary.Count}");
                    await writer.WriteLineAsync($"Keys with mismatched values: {mismatchedEntries.Count}");
                    await writer.WriteLineAsync();

                    if (mismatchedEntries.Count != 0)
                    {
                        foreach (var entry in mismatchedEntries)
                        {
                            await writer.WriteLineAsync($"Key: {entry.Key}");
                            await writer.WriteLineAsync("Values Found:");
                            foreach (var val in entry.Value)
                            {
                                await writer.WriteLineAsync($" - {val}");
                            }

                            await writer.WriteLineAsync(new string('-', 40));
                        }
                    }
                    else
                    {
                        await writer.WriteLineAsync("No mismatched resource strings were found.");
                    }
                }

                _lastGeneratedReportPath = Path.Combine(reportFolder, "MismatchReport.txt");
                OpenReportButton.IsEnabled = File.Exists(_lastGeneratedReportPath);

                System.Windows.MessageBox.Show($"Report generated successfully at:\n{reportFilePath}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable UI elements
                SetControlsEnabled(true);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"An error occurred:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for the Open Report button
    /// </summary>
    private void OpenReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastGeneratedReportPath) && File.Exists(_lastGeneratedReportPath))
        {
            try
            {
                // Open the report file with the default application
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _lastGeneratedReportPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open the report: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            System.Windows.MessageBox.Show("No report has been generated yet or the file cannot be found.",
                "Report Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Helper method to enable or disable all controls during processing
    /// </summary>
    private void SetControlsEnabled(bool enabled)
    {
        BrowseInputButton.IsEnabled = enabled;
        BrowseOutputButton.IsEnabled = enabled;
        ProcessButton.IsEnabled = enabled && !string.IsNullOrEmpty(InputFolderTextBox.Text) &&
                                  !string.IsNullOrEmpty(OutputFolderTextBox.Text);
        // No need to enable/disable OpenReportButton here as it depends on report existence
    }

    /// <summary>
    /// Helper method to show a folder selection dialog.
    /// </summary>
    /// <param name="description">Description to display in the dialog.</param>
    /// <returns>Selected folder path or null if canceled.</returns>
    private static string? SelectFolder(string description)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = description;
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            return dialog.SelectedPath;
        }
        else
        {
            return null;
        }
    }
}

/// <inheritdoc />
/// <summary>
/// Converter to enable/disable the Process button based on whether strings are empty
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