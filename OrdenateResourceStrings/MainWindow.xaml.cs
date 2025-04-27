using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace OrdenateResourceStrings;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SelectInputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SelectOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void ProcessFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var inputFolder = InputFolderTextBox.Text;
        var outputFolder = OutputFolderTextBox.Text;

        if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
        {
            System.Windows.MessageBox.Show("Please select a valid input folder.", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
        {
            System.Windows.MessageBox.Show("Please select a valid output folder.", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        try
        {
            var files = Directory.GetFiles(inputFolder, "*.xaml");

            foreach (var file in files)
            {
                // Read all lines of the file
                var lines = File.ReadAllLines(file).ToList();

                // Separate header, resource strings, and footer
                var headerLines = lines.TakeWhile(line => !line.Contains("<system:String x:Key=")).ToList();
                var resourceLines = lines.SkipWhile(line => !line.Contains("<system:String x:Key="))
                    .TakeWhile(line => !line.Trim().StartsWith("</ResourceDictionary>", StringComparison.Ordinal))
                    // Filter out potential empty lines within the resource block as well
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();
                var footerLines = lines.Skip(headerLines.Count + resourceLines.Count).ToList();

                // ---- New code: Remove trailing empty/whitespace lines from the header ----
                while (headerLines.Count > 0 && string.IsNullOrWhiteSpace(headerLines[^1]))
                {
                    headerLines.RemoveAt(headerLines.Count - 1);
                }
                // ---- End of new code ----

                // Process resource lines: Keep only the first occurrence of each key, then sort
                var uniqueResourceLines = new Dictionary<string, string>(); // Key: x:Key value, Value: The line
                foreach (var line in resourceLines)
                {
                    var key = ExtractKey(line);
                    if (!string.IsNullOrEmpty(key) && !uniqueResourceLines.ContainsKey(key))
                    {
                        uniqueResourceLines[key] = line; // Keep the first occurrence
                    }
                }

                // Now sort the unique lines by key
                var sortedUniqueResourceLines = uniqueResourceLines.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();

                // Combine all lines
                var outputLines = headerLines.Concat(sortedUniqueResourceLines).Concat(footerLines).ToList();

                // Write the output to the new file
                var outputFile = Path.Combine(outputFolder, Path.GetFileName(file));
                File.WriteAllLines(outputFile, outputLines);
            }

            System.Windows.MessageBox.Show("Files processed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string ExtractKey(string line)
    {
        var start = line.IndexOf("x:Key=\"", StringComparison.Ordinal);

        if (start == -1)
        {
            // Handle cases where x:Key is not in the expected format
            // You might want to log this or throw a more specific exception
            return ""; // Or some other default value
        }

        start += 7; // Move past "x:Key=\""

        var end = line.IndexOf('"', start);

        if (end == -1)
        {
            // Handle the missing closing quote
            return ""; // Or handle differently
        }

        return line.Substring(start, end - start);
    }
}
