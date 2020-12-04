using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;

namespace tempnuker
{
    class Program
    {
        static DirectoryLayoutMode _currentMode = (DirectoryLayoutMode)Enum.Parse(typeof(DirectoryLayoutMode), Environment.OSVersion.Version.Major.ToString(), true);
        static string _rootUserProfilePath = string.Empty;
        static readonly List<string> _pathSubsToClean = new List<string>();
        static string _logFileName = "tempnuker.log";
        static TextWriter _textWriter;
        static bool _includeDefaultTempLocations;
        static ulong _bytesRemoved = 0;
        static uint _filesRemoved = 0;
        static uint _directoriesRemoved = 0;

        static void Main(string[] args)
        {
            ParseCommandLine(args);

            _textWriter = new StreamWriter(_logFileName, true);
            WriteLog("Temp Nuker v{0} started on {1} as {2}{3}", Assembly.GetExecutingAssembly().GetName().Version.ToString(), Environment.MachineName, DateTime.Now.ToString(), Environment.NewLine);

            if (_rootUserProfilePath == string.Empty)
                DetermineProfileRoot();

            DetermineSubDirectoryLocations((int)_currentMode);

            if (_includeDefaultTempLocations)
                DoCleanBaseTempFileLocations();

            DoCleanProfileDirectories();

            WriteLog("Clean Complete");
            WriteLog("{0} space recovered in {1} files and {2} directories!",
                String.Format("{0:2}", (FileSize)_bytesRemoved),
                _filesRemoved.ToString(),
                _directoriesRemoved.ToString()
                );


            if (_textWriter != null)
                _textWriter.Close();

            if (Debugger.IsAttached)
                Debugger.Break();
        }

        private static void DetermineProfileRoot()
        {
            DirectoryInfo di = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            _rootUserProfilePath = di.Parent.Parent.FullName;
        }

        private static void DoCleanBaseTempFileLocations()
        {
            DoRecursiveDirectoryEmpty(@"C:\windows\temp");
            DoRecursiveDirectoryEmpty(@"C:\temp");
            WriteLog();
        }

        private static void DetermineSubDirectoryLocations(int majorVer)
        {
            switch (majorVer)
            {
                case 4:
                    _currentMode = DirectoryLayoutMode.NT4;
                    break;

                case 5:
                    _currentMode = DirectoryLayoutMode.NT5;
                    _pathSubsToClean.Add(@"Local Settings\Temp");
                    _pathSubsToClean.Add(@"Local Settings\Temporary Internet Files");
                    break;

                case 6:
                    _currentMode = DirectoryLayoutMode.NT6;
                    _pathSubsToClean.Add(@"AppData\Local\Temp");
                    _pathSubsToClean.Add(@"AppData\Local\Microsoft\Windows\Temporary Internet Files");
                    _pathSubsToClean.Add(@"AppData\LocalLow\Temp");
                    _pathSubsToClean.Add(@"AppData\LocalLow\Microsoft\Internet Explorer\DOMStore");
                    _pathSubsToClean.Add(@"AppData\LocalLow\Microsoft\Silverlight");
                    _pathSubsToClean.Add(@"AppData\LocalLow\Microsoft\Windows Live\Setup");
                    _pathSubsToClean.Add(@"AppData\LocalLow\Microsoft\Silverlight");
                    break;

                default:
                    _currentMode = DirectoryLayoutMode.UNKNOWN;
                    break;
            }
        }

        static void DoCleanProfileDirectories()
        {
            if (!Directory.Exists(_rootUserProfilePath))
            {
                WriteLog("!Directory does not exist {0}", _rootUserProfilePath);
                return;
            }

            DirectoryInfo di = new DirectoryInfo(_rootUserProfilePath);
            DirectoryInfo[] subDirs = di.GetDirectories();
            foreach (DirectoryInfo subDirectory in subDirs)
            {
                WriteLog("Begin cleaning user profile for {0}", subDirectory.Name);
                HandleDirectory(subDirectory);
                WriteLog("Cleaning complete for {0}", subDirectory.Name);
                WriteLog();
            }
        }

        static void HandleDirectory(DirectoryInfo subDirectory)
        {
            foreach (string path in _pathSubsToClean)
            {
                DoRecursiveDirectoryEmpty(Path.Combine(subDirectory.FullName, path));
            }
        }

        static void DoRecursiveDirectoryEmpty(string directoryName)
        {
            WriteLog("-Checking {0}", directoryName);
            if (!Directory.Exists(directoryName))
            {
                WriteLog("-!Directory does not exist {0}", directoryName);
                return;
            }

            DirectoryInfo di = new DirectoryInfo(directoryName);

            try
            {
                FileInfo[] subFiles = di.GetFiles();
                foreach (FileInfo subFile in subFiles)
                {
                    try
                    {
                        SetFileAttributes(subFile);
                        DoDeleteFile(subFile);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        try
                        {
                            AddFullAccessPermission(subFile.FullName);
                            DoDeleteFile(subFile);
                        }
                        catch (Exception e)
                        {
                            WriteLog("-!Failed to delete file {0}", subFile.FullName);
                            WriteExceptionToConsole(e);
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLog("-!Failed to delete file {0}", subFile.FullName);
                        WriteExceptionToConsole(e);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLog("-!Failed to iterate files in {0}", directoryName);
                WriteExceptionToConsole(e);
            }

            try
            {
                DirectoryInfo[] subDirs = di.GetDirectories();
                foreach (DirectoryInfo subDirectory in subDirs)
                {
                    DoRecursiveDirectoryEmpty(subDirectory.FullName);
                    try
                    {
                        subDirectory.Delete();
                        _directoriesRemoved += 1;
                        WriteLog("-Sucessfully deleted directory {0}", subDirectory.FullName);
                    }
                    catch (Exception e)
                    {
                        WriteLog("-!Failed to delete directory {0}", subDirectory.FullName);
                        WriteExceptionToConsole(e);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLog("-!Failed to iterate directories in {0}", directoryName);
                WriteExceptionToConsole(e);
            }

            WriteLog("-Cleaned: {0}", directoryName);
        }

        private static void SetFileAttributes(FileInfo file)
        {
            file.Attributes &= ~(FileAttributes.System | FileAttributes.Hidden | FileAttributes.ReadOnly);
        }

        private static void DoDeleteFile(FileInfo subFile)
        {
            subFile.Delete();
            _bytesRemoved += (ulong)subFile.Length;
            _filesRemoved += 1;
            WriteLog("-Sucessfully deleted file {0}, ({1})", subFile.FullName, String.Format("{0:2}", (FileSize)((ulong)subFile.Length)));
        }

        static void AddFullAccessPermission(string fileName)
        {
            string account = WindowsIdentity.GetCurrent().Name;
            FileSystemRights rights = FileSystemRights.FullControl;
            AccessControlType controlType = AccessControlType.Allow;

            FileSecurity fSecurity = File.GetAccessControl(fileName);
            fSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, controlType));
            File.SetAccessControl(fileName, fSecurity);
        }

        static void WriteExceptionToConsole(Exception e)
        {
            WriteLog("--!" + e.Message.Trim());
        }

        private static void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string param = args[i].Substring(1);
                string[] parSplit = param.Split(new string[1] { " -" }, StringSplitOptions.None);
                try
                {
                    switch (parSplit[0].ToLower())
                    {
                        case "o":
                        case "os":
                            _currentMode = (DirectoryLayoutMode)Enum.Parse(typeof(DirectoryLayoutMode), parSplit[1], true);
                            break;
                        case "p":
                        case "profileroot":
                            _rootUserProfilePath = parSplit[1];
                            break;
                        case "l":
                        case "logfile":
                            _logFileName = parSplit[1];
                            break;
                        case "t":
                        case "include-default-temp":
                            _includeDefaultTempLocations = true;
                            break;
                        default:
                            PrintHelp();
                            break;
                    }
                }

                catch { }
            }
        }

        static void WriteLog()
        {
            WriteLog(string.Empty);
        }

        static void WriteLog(string log, params string[] parms)
        {
            string outLog = string.Format(log, parms);

            if (!string.IsNullOrEmpty(_logFileName) && _textWriter != null)
            {
                _textWriter.WriteLine(outLog);
            }

            Console.WriteLine(outLog);
        }

        private static void PrintHelp()
        {
            Console.Write(string.Format(@"
TempNuker v{0}

Command line parameters are:
tempnuker.exe [OPTIONS]

Options parameters are:
-os    -o         Force Specific OS type. 
                  Valid Options are NT5 NT6 (-os=NT5)
                  **-os and -profileroot MUST be used together!**
                  
-profileroot  -p  Force a specific profile root.
                  This is used to clean a remote or offline profile tree.
                  (-profileroot=D:\profiles)
                  **-profileroot and -os MUST be used together!**
                  
                  
-logfile   -l     Use this to enable a log file of output. (-logfile=file.txt)
                  As of v1.4 this defaults to .\tempnuker.log

-include-default-temp   -t    
                  Include default temp locations. (C:\temp, C:\windows\temp)
                  Defaults to OFF unless NO parameters are given,
                  then defaults to ON.
EXAMPLE:

tempnuker.exe -os=NT5 -profileroot=""D:\Documents and Settings\"" -logfile=""C:\Log Files\tempnuker.txt""

tempnuker.exe -o=NT5 -p=""D:\Documents and Settings\"" -l=""C:\Log Files\tempnuker.txt"" -t

DEFAULT:
                  If executed with no parameters it will attempt to autodetect
                  the current system version and profile directories, and it
                  will clean the default temp directories as well. Logging 
                  is enabled by default. 

", Assembly.GetExecutingAssembly().GetName().Version));
            Environment.Exit(-1);
        }
    }
}
