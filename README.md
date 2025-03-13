# XAML Resource Management Tools

A collection of four WPF utilities for managing, organizing, and validating XAML resource files in .NET applications. These tools help ensure consistency, organization, and proper usage of resource strings across your application.

## System Requirements

- .NET 9.0 or higher
- Windows OS with WPF support

## Tool Overview

### 1. Resource String Consistency Checker (DetectMissMatchedResourceStrings)

**Purpose:** Identifies inconsistent fallback values for resource strings in your code.

**Features:**
- Scans source code files for TryFindResource patterns with fallback values
- Detects when the same resource key has different fallback values across files
- Generates comprehensive report of all inconsistencies
- Helps maintain consistent user experience by ensuring fallback values match throughout your application

**How it works:**
This tool scans your codebase for patterns like: `TryFindResource("ResourceKey") ?? "FallbackValue"` and identifies cases where the same ResourceKey has different FallbackValues in different places. This can lead to inconsistent user experience if resources are missing.

**Usage:**
1. Select the source folder containing your code files
2. Choose an output folder for the report
3. Click "Generate Consistency Report"

---

### 2. Resource String Comparison Tool (DetectResourceStrings)

**Purpose:** Identifies mismatches between resource strings used in code and those defined in resource files.

**Features:**
- Scans code for TryFindResource and DynamicResource references
- Compares these references with resource keys defined in a XAML resource file
- Identifies resources referenced in code but missing from the resource file
- Identifies resource keys defined but not used in code
- Generates detailed comparison report

**How it works:**
The tool extracts all resource keys referenced in code through TryFindResource and DynamicResource, then compares this list with keys defined in a XAML resource file. This helps ensure all referenced resources are properly defined and all defined resources are actually used.

**Usage:**
1. Select the input folder containing code files to scan
2. Choose a XAML resource file to compare against
3. Choose an output folder for the comparison report
4. Click "Generate Comparison Report"

---

### 3. Resource String Organizer (OrdenateResourceStrings)

**Purpose:** Alphabetically sorts resource strings in XAML files by their x:Key values.

**Features:**
- Processes XAML resource files and sorts string resources alphabetically
- Preserves file structure (headers, footers, etc.)
- Creates sorted copies, leaving original files untouched
- Makes resource files more maintainable and helps identify duplicates

**How it works:**
The tool specifically looks for `<system:String x:Key="...">` entries in your XAML files and sorts them alphabetically while preserving the file header and footer content. This makes resource files easier to manage, review, and maintain.

**Usage:**
1. Select the input folder containing XAML resource files
2. Choose an output folder where sorted files will be saved
3. Click "Sort Resource Strings"

**Benefits:**
- Easier to find specific resources when editing
- Helps identify duplicate keys
- Makes file differences more meaningful in version control

---

### 4. XAML Key Comparer (DetectMissingKeys)

**Purpose:** Compares resource keys between XAML files to identify missing or extra keys.

**Features:**
- Takes a reference XAML file as the master template
- Compares it with other XAML files to find missing or extra keys
- Particularly useful for comparing localized resource files
- Identifies which keys are missing from which files

**How it works:**
The tool extracts all x:Key attributes from the reference file and each comparison file, then performs set comparisons to identify keys that are missing from comparison files or extra keys not found in the reference file.

**Usage:**
1. Select a reference XAML file containing the expected set of keys
2. Choose an input folder containing XAML files to check against the reference
3. Choose an output folder for the report
4. Click "Generate Comparison Report"

## Common Usage Scenarios

- **During Localization**: Ensure all resource files have consistent keys across languages
- **Code Reviews**: Validate that resource files are well-organized and consistent
- **Refactoring**: Detect unused resources and missing resource definitions
- **Quality Assurance**: Identify potential runtime issues related to missing resources

## Contributing

Contributions, suggestions, and bug reports are welcome! Please open an issue or submit a pull request if you have improvements to share.