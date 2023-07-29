using System;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;

namespace SwitchConfigHelper
{
    public static class PathProcessor
    {
        public static string ProcessPath(string path)
        {
            SessionState ss = new SessionState();
            ProviderInfo pathProvider;
            PSDriveInfo pathDrive;
            var processedPath = ss.Path.GetUnresolvedProviderPathFromPSPath(path, out pathProvider, out pathDrive);
            if (pathProvider.ImplementingType != typeof(FileSystemProvider))
            {
                throw new ArgumentException(path + " is not a filesystem path.");
            }
            else
            {
                return processedPath;
            }
        }
    }
}