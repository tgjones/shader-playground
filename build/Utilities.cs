using System;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Net;
using Cake.Common.Tools.MSBuild;
using Cake.Compression;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

internal static class Utilities
{
    public static void RunAndCheckResult(this BuildContext context, FilePath exe, ProcessSettings settings)
    {
        var res = context.StartProcess(exe, settings);

        context.Log.Information(Verbosity.Verbose, $"Process {exe} {settings.Arguments} completed with exit code {res}");

        if (res != 0)
        {
            throw new Exception($"'{exe}' failed with error code {res}");
        }
    }

    public static string DownloadCompiler(this BuildContext context, string url, string binariesFolderName, string version, bool cache)
    {
        var tempFileName = $"{context.ArtifactsPath}/{binariesFolderName}-{version}.zip";

        if (!(cache || context.AlwaysCache) || !context.FileExists(tempFileName))
        {
            context.DownloadFile(url, tempFileName);
            context.Log.Information(Verbosity.Verbose, $"Downloaded {url} to {tempFileName}");
            context.Log.Information($"Downloaded {version}");
        }
        else
        {
            context.Log.Information(Verbosity.Verbose, $"{url} already cached in {tempFileName}");
            context.Log.Information($"{version} already downloaded");
        }

        return tempFileName;
    }

    public enum ZipFormat
    {
        Zip,
        GZip,
        SevenZip
    }

    public static string GetFullPath(this BuildContext context, string fileName)
    {
        if (context.FileExists(fileName))
        {
            return fileName;
        }

        var values = Environment.GetEnvironmentVariable("PATH");
        foreach (var path in values.Split(System.IO.Path.PathSeparator))
        {
            var dir = DirectoryPath.FromString(path);
            var fullPath = dir.GetFilePath(fileName).ToString();
            if (context.FileExists(fullPath))
            {
                return fullPath;
            }
        }
        return null;
    }

    public static void Unzip(this BuildContext context, string zip, string dstFolder, string filesToCopy, ZipFormat format)
    {
        switch (format)
        {
            case ZipFormat.Zip:
            case ZipFormat.GZip:
                {
                    if (filesToCopy == null)
                    {
                        context.EnsureDirectoryExists(dstFolder);
                        context.CleanDirectory(dstFolder);
                        if (format == ZipFormat.Zip)
                        {
                            context.ZipUncompress(zip, dstFolder);
                        }
                        else
                        {
                            context.GZipUncompress(zip, dstFolder);
                        }
                    }
                    else
                    {
                        string unzippedFolder = $"{zip}.unzipped";
                        context.EnsureDirectoryExists(unzippedFolder);
                        context.CleanDirectory(unzippedFolder);
                        if (format == ZipFormat.Zip)
                        {
                            context.ZipUncompress(zip, unzippedFolder);
                        }
                        else
                        {
                            context.GZipUncompress(zip, unzippedFolder);
                        }
                        context.EnsureDirectoryExists(dstFolder);
                        context.CleanDirectory(dstFolder);
                        context.CopyFiles($"{unzippedFolder}/{filesToCopy}", dstFolder, true);
                        context.DeleteDirectory(unzippedFolder, new DeleteDirectorySettings
                        {
                            Recursive = true,
                            Force = true
                        });
                    }
                    break;
                }
            case ZipFormat.SevenZip:
                {
                    string exe = @"C:\Program Files\7-Zip\7z.exe";
                    if (!context.FileExists(exe))
                    {
                        exe = context.GetFullPath("7z.exe");
                    }
                    if (!context.FileExists(exe))
                    {
                        throw new InvalidOperationException("Unable to find 7z.exe in Program Files or PATH");
                    }
                    context.RunAndCheckResult(exe,
                      new ProcessSettings
                      {
                          Arguments = $@"e -o""{dstFolder}"" ""{zip}"" {filesToCopy}"
                      }
                    );
                    break;
                }
            default:
                throw new InvalidOperationException();
        }
    }

    public static string DownloadAndUnzipCompiler(this BuildContext context, string url, string binariesFolderName, string version, bool cache, ZipFormat format = ZipFormat.Zip)
    {
        var tempFileName = context.DownloadCompiler(url, binariesFolderName, version, cache);

        var unzippedFolder = $"{context.ArtifactsPath}/{binariesFolderName}/{version}";

        context.Unzip(tempFileName, unzippedFolder, null, format);
        context.Log.Information(Verbosity.Verbose, $"Unzipped {tempFileName} to {unzippedFolder}");

        return unzippedFolder;
    }

    public static string DownloadAndUnzipCompiler(this BuildContext context, string url, string binariesFolderName, string version, bool cache, string filesToCopy, ZipFormat format = ZipFormat.Zip)
    {
        var tempFileName = context.DownloadCompiler(url, binariesFolderName, version, cache);
        var binariesFolder = $"{context.SrcBinariesPath}/{binariesFolderName}/{version}";

        context.Unzip(tempFileName, binariesFolder, filesToCopy, format);

        return binariesFolder;
    }

    public static MSBuildSettings CreateCppBuildSettings(this BuildContext context)
    {
        return new MSBuildSettings()
          .UseToolVersion(MSBuildToolVersion.VS2022)
          .WithProperty("PlatformToolset", "v143")
          .WithProperty("WindowsTargetPlatformVersion", "10.0.19041.0")
          .SetConfiguration(context.BuildConfiguration)
          .SetVerbosity(Verbosity.Minimal);
    }
}
