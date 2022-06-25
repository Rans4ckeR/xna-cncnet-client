namespace ClientCore.INIProcessing;

public class PreprocessedIniInfo
{
    public PreprocessedIniInfo(string fileName, string originalHash, string processedHash)
    {
        FileName = fileName;
        OriginalFileHash = originalHash;
        ProcessedFileHash = processedHash;
    }

    public PreprocessedIniInfo(string[] info)
    {
        FileName = info[0];
        OriginalFileHash = info[1];
        ProcessedFileHash = info[2];
    }

    public string FileName { get; }

    public string OriginalFileHash { get; set; }

    public string ProcessedFileHash { get; set; }
}