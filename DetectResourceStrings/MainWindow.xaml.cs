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
    private List<string> _unmatchedStringsFromResource = new();

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
        if (string.IsNullOrEmpty(selectedFolder)) return;

        InputFolderTextBox.Text = selectedFolder;
        UpdateCompareButtonState();
    }

    /// <summary>
    /// Event handler for resource file browse button
    /// </summary>
    private void BrowseResourceButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFile = SelectFile("Select Resource File", "XAML files (*.xaml)|*.xaml");
        if (string.IsNullOrEmpty(selectedFile)) return;

        ResourceFileTextBox.Text = selectedFile;
        UpdateCompareButtonState();
    }

    /// <summary>
    /// Event handler for output folder browse button
    /// </summary>
    private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedFolder = SelectFolder("Select Folder to Save Report");
        if (string.IsNullOrEmpty(selectedFolder)) return;

        OutputFolderTextBox.Text = selectedFolder;
        UpdateCompareButtonState();
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
        try
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
                foreach (var file in EnumerateFilesWithExclusions(inputFolder, "*.*", ["bin", "obj", "Properties"]))
                {
                    try
                    {
                        // Skip binary files by checking their extensions
                        var validTextExtensions = new[] { ".txt", ".cs", ".xaml", ".json", ".xml" };
                        if (!validTextExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        {
                            continue;
                        }

                        // Read the content
                        var content = await File.ReadAllTextAsync(file);

                        var tryFindResourceMatches = MyRegex1().Matches(content);
                        foreach (Match match in tryFindResourceMatches)
                        {
                            if (match.Groups.Count > 1)
                            {
                                list1.Add(match.Groups[1].Value);
                            }
                        }

                        var dynamicResourceMatches = MyRegex2().Matches(content);
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
                _unmatchedStringsFromResource = unmatchedStringsFromResource;

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
                UpdateActionButtonsState();

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
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            UpdateActionButtonsState();
        }
    }

    private async void CleanUpButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_unmatchedStringsFromResource.Count == 0)
            {
                MessageBox.Show("No unused resource strings were found in the last analysis.", "No Action Needed",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "Select XAML Resource Files to Clean Up",
                Filter = "XAML Files (*.xaml)|*.xaml",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true) return;

            var filesToClean = openFileDialog.FileNames;
            var filesModified = 0;
            var linesRemoved = 0;

            try
            {
                SetControlsEnabled(false);

                // Use a hash set for efficient lookups of keys to remove
                var keysToRemove = new HashSet<string>(_unmatchedStringsFromResource);

                foreach (var filePath in filesToClean)
                {
                    try
                    {
                        var lines = (await File.ReadAllLinesAsync(filePath)).ToList();

                        var removedCount = lines.RemoveAll(line =>
                        {
                            var key = ExtractKey(line);
                            return key != null && keysToRemove.Contains(key);
                        });

                        if (removedCount > 0)
                        {
                            await File.WriteAllLinesAsync(filePath, lines);
                            linesRemoved += removedCount;
                            filesModified++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to process file {filePath}: {ex.Message}");
                    }
                }

                MessageBox.Show($"Cleanup complete.\n\nFiles modified: {filesModified}\nTotal lines removed: {linesRemoved}", "Cleanup Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the cleanup process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred during the cleanup process: {ex.Message}");
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

        // The state of these buttons depends on the analysis result,
        // so we only disable them when processing and re-evaluate when enabling.
        if (enabled)
        {
            UpdateActionButtonsState();
        }
        else
        {
            OpenReportButton.IsEnabled = false;
            CleanUpButton.IsEnabled = false;
        }
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

    private void UpdateActionButtonsState()
    {
        // Enable Open Report button only if the report file exists
        OpenReportButton.IsEnabled = !string.IsNullOrEmpty(_reportFilePath) && File.Exists(_reportFilePath);
        // Enable Clean Up button only if there are unused resource strings
        CleanUpButton.IsEnabled = _unmatchedStringsFromResource.Count > 0;
    }

    private static string? ExtractKey(string line)
    {
        // Use the existing generated regex to find the key
        var match = MyRegex().Match(line);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex("x:Key=\"(.*?)\"")]
    private static partial Regex MyRegex();

    [GeneratedRegex("TryFindResource\\(\"(.*?)\"\\)")]
    private static partial Regex MyRegex1();

    [GeneratedRegex("{DynamicResource (.*?)}")]
    private static partial Regex MyRegex2();

    /// <summary>
    /// Recursively enumerates files in a directory while excluding specified folders
    /// </summary>
    /// <param name="rootPath">The root directory path to start searching from</param>
    /// <param name="searchPattern">The search pattern for files (e.g. "*.*")</param>
    /// <param name="excludedFolders">Array of folder names to exclude from the search</param>
    /// <returns>An enumerable collection of file paths</returns>
    private static IEnumerable<string> EnumerateFilesWithExclusions(string rootPath, string searchPattern, string[] excludedFolders)
    {
        // Get all files in the current directory
        foreach (var file in Directory.EnumerateFiles(rootPath, searchPattern, SearchOption.TopDirectoryOnly))
        {
            yield return file;
        }

        // Process subdirectories but skip excluded ones
        foreach (var directory in Directory.EnumerateDirectories(rootPath))
        {
            var dirName = Path.GetFileName(directory);

            // Skip excluded directories
            if (excludedFolders.Any(excluded => dirName.Equals(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Recursively process subdirectories
            foreach (var file in EnumerateFilesWithExclusions(directory, searchPattern, excludedFolders))
            {
                yield return file;
            }
        }
    }
}

/// <inheritdoc />
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