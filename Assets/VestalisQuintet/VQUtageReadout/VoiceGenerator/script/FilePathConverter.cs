using System;

public static class FilePathConverter
{
    public static string ConvertWindowsPathToFileUri(string windowsFilePath)
    {
        if (string.IsNullOrEmpty(windowsFilePath))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(windowsFilePath));
        }

        // WindowsのパスをURI形式に変換
        string uriPath = Uri.EscapeUriString(windowsFilePath)
            .Replace("\\", "/");

        // ファイルパスがドライブ文字で始まる場合（例：C:\path\to\file.txt）
        if (uriPath.Length >= 2 && uriPath[1] == ':')
        {
            // ドライブ文字の後にあるコロンをエスケープ
            uriPath = uriPath.Substring(0, 1) + "%3A" + uriPath.Substring(2);
        }

        // URIスキームを追加
        return "file:///" + uriPath;
    }
}
