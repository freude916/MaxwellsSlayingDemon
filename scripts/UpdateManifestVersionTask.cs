var hasError = false;

var text = System.IO.File.ReadAllText(ManifestPath);

var versionKeyIndex = text.IndexOf("\"version\"", System.StringComparison.Ordinal);
if (versionKeyIndex < 0)
{
    Log.LogError($"Could not find version field in '{ManifestPath}'.");
    hasError = true;
}

var colonIndex = -1;
if (!hasError)
{
    colonIndex = text.IndexOf(':', versionKeyIndex);
    if (colonIndex < 0)
    {
        Log.LogError($"Malformed version field in '{ManifestPath}'.");
        hasError = true;
    }
}

var firstQuoteIndex = -1;
if (!hasError)
{
    firstQuoteIndex = text.IndexOf('"', colonIndex + 1);
    if (firstQuoteIndex < 0)
    {
        Log.LogError($"Malformed version value in '{ManifestPath}'.");
        hasError = true;
    }
}

var secondQuoteIndex = -1;
if (!hasError)
{
    secondQuoteIndex = text.IndexOf('"', firstQuoteIndex + 1);
    if (secondQuoteIndex < 0)
    {
        Log.LogError($"Malformed version value in '{ManifestPath}'.");
        hasError = true;
    }
}

var originalVersion = string.Empty;
string[] segments = [];
if (!hasError)
{
    originalVersion = text.Substring(firstQuoteIndex + 1, secondQuoteIndex - firstQuoteIndex - 1);
    segments = originalVersion.Split('.');
    if (segments.Length < 3)
    {
        Log.LogError($"Version '{originalVersion}' does not have at least 3 segments.");
        hasError = true;
    }
}

var hourHex = string.Empty;
if (!hasError)
{
    var startUtc = new System.DateTime(2025, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    var hours = (long)System.Math.Floor((System.DateTime.UtcNow - startUtc).TotalMinutes);
    if (hours < 0)
    {
        Log.LogError("Current UTC time is earlier than 2025-01-01.");
        hasError = true;
    }
    else
    {
        hourHex = hours.ToString("x", System.Globalization.CultureInfo.InvariantCulture);
    }
}

if (!hasError)
{
    string updatedVersion;
    if (segments.Length >= 4)
    {
        segments[3] = hourHex;
        updatedVersion = string.Join(".", segments);
    }
    else
    {
        updatedVersion = originalVersion + "." + hourHex;
    }

    var updatedText = text.Substring(0, firstQuoteIndex + 1)
        + updatedVersion
        + text.Substring(secondQuoteIndex);

    if (!string.Equals(text, updatedText, System.StringComparison.Ordinal))
    {
        System.IO.File.WriteAllText(ManifestPath, updatedText);
        Log.LogMessage(
            Microsoft.Build.Framework.MessageImportance.High,
            $"Updated {ManifestPath} version: {originalVersion} -> {updatedVersion}");
    }
    else
    {
        Log.LogMessage(
            Microsoft.Build.Framework.MessageImportance.High,
            $"Version in {ManifestPath} remains: {updatedVersion}");
    }
}
