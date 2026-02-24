Imports System.IO
Imports System.Net
Imports System.IO.Compression
Imports System.Diagnostics
Imports System.Reflection

Public Class AutoUpdateService
    Private Const UPDATE_URL As String = "http://www.mogalia.co.za/pos/version.txt"
    Private Const DOWNLOAD_URL As String = "http://www.mogalia.co.za/pos/pos.zip"
    Private ReadOnly _currentVersion As String

    Public Sub New()
        _currentVersion = GetCurrentVersion()
    End Sub

    Private Function GetCurrentVersion() As String
        Try
            Dim version = Assembly.GetExecutingAssembly().GetName().Version
            Return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
        Catch ex As Exception
            Return "1.0.0.0"
        End Try
    End Function

    Public Function CheckForUpdates() As UpdateInfo
        Try
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            Using client As New WebClient()
                Dim serverVersion = client.DownloadString(UPDATE_URL).Trim()
                
                If IsNewerVersion(serverVersion, _currentVersion) Then
                    Return New UpdateInfo With {
                        .IsUpdateAvailable = True,
                        .CurrentVersion = _currentVersion,
                        .NewVersion = serverVersion,
                        .DownloadUrl = DOWNLOAD_URL
                    }
                End If
            End Using
        Catch ex As Exception
            ' Silently fail if update check fails (no internet, server down, etc.)
            Debug.WriteLine($"Update check failed: {ex.Message}")
        End Try

        Return New UpdateInfo With {
            .IsUpdateAvailable = False,
            .CurrentVersion = _currentVersion
        }
    End Function

    Private Function IsNewerVersion(serverVersion As String, currentVersion As String) As Boolean
        Try
            Dim server = New Version(serverVersion)
            Dim current = New Version(currentVersion)
            Return server > current
        Catch
            Return False
        End Try
    End Function

    Private Function FindExecutableFolder(rootPath As String, exeName As String) As String
        Try
            ' Check if exe is in root
            If File.Exists(Path.Combine(rootPath, exeName)) Then
                Return rootPath
            End If

            ' Search subdirectories (max 2 levels deep)
            Dim directories = Directory.GetDirectories(rootPath)
            For Each dir As String In directories
                If File.Exists(Path.Combine(dir, exeName)) Then
                    Return dir
                End If
                
                ' Check one level deeper
                Dim subDirectories = Directory.GetDirectories(dir)
                For Each subDir As String In subDirectories
                    If File.Exists(Path.Combine(subDir, exeName)) Then
                        Return subDir
                    End If
                Next
            Next
        Catch ex As Exception
            Debug.WriteLine($"Error finding executable folder: {ex.Message}")
        End Try
        
        Return Nothing
    End Function

    Public Function DownloadUpdate(progressCallback As Action(Of Integer)) As String
        Try
            Dim tempPath = Path.Combine(Path.GetTempPath(), "od_package.zip")
            
            If File.Exists(tempPath) Then
                File.Delete(tempPath)
            End If

            ' Use simple synchronous download
            Using client As New WebClient()
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                client.DownloadFile(New Uri(DOWNLOAD_URL), tempPath)
            End Using

            Return tempPath
        Catch ex As Exception
            Throw New Exception($"Failed to download update: {ex.Message}", ex)
        End Try
    End Function

    Public Sub InstallUpdate(zipPath As String)
        Try
            Dim appPath = Path.GetDirectoryName(Application.ExecutablePath)
            Dim backupPath = Path.Combine(Path.GetTempPath(), "POS_Backup_" & DateTime.Now.ToString("yyyyMMddHHmmss"))

            ' Create backup of current version
            If Directory.Exists(backupPath) Then
                Directory.Delete(backupPath, True)
            End If
            Directory.CreateDirectory(backupPath)

            ' Extract update to temp location
            Dim extractPath = Path.Combine(Path.GetTempPath(), "POS_Extract")
            If Directory.Exists(extractPath) Then
                Directory.Delete(extractPath, True)
            End If
            ZipFile.ExtractToDirectory(zipPath, extractPath)

            ' Find the actual POS folder in the extracted content
            ' Look for the folder containing Overn-Delights-POS.exe
            Dim posFolder = FindExecutableFolder(extractPath, "Overn-Delights-POS.exe")
            If String.IsNullOrEmpty(posFolder) Then
                ' Fallback: check for common folder names
                If Directory.Exists(Path.Combine(extractPath, "Release")) Then
                    posFolder = Path.Combine(extractPath, "Release")
                ElseIf Directory.Exists(Path.Combine(extractPath, "POS")) Then
                    posFolder = Path.Combine(extractPath, "POS")
                ElseIf Directory.Exists(Path.Combine(extractPath, "Overn-Delights-POS")) Then
                    posFolder = Path.Combine(extractPath, "Overn-Delights-POS")
                Else
                    ' Use root if no subfolder found
                    posFolder = extractPath
                End If
            End If

            ' Create batch file for reliable update with UAC elevation
            Dim batPath = Path.Combine(Path.GetTempPath(), "od_update.bat")
            Dim logPath = Path.Combine(Path.GetTempPath(), "od_update.log")
            
            ' Escape paths for batch file
            Dim batContent = $"@echo off
echo Update started > ""{logPath}""
timeout /t 3 /nobreak > nul
taskkill /F /IM ""Overn-Delights-POS.exe"" > nul 2>&1
timeout /t 2 /nobreak > nul
echo Process stopped >> ""{logPath}""
robocopy ""{posFolder}"" ""{appPath}"" /E /IS /IT /R:3 /W:1 > nul
if %errorlevel% leq 7 (
    echo Files copied successfully >> ""{logPath}""
) else (
    echo Copy failed with error %errorlevel% >> ""{logPath}""
)
if exist ""{zipPath}"" del /F /Q ""{zipPath}"" > nul 2>&1
if exist ""{extractPath}"" rmdir /S /Q ""{extractPath}"" > nul 2>&1
timeout /t 1 /nobreak > nul
start """" ""{Application.ExecutablePath}""
echo Update complete >> ""{logPath}""
timeout /t 2 /nobreak > nul
del ""%~f0"" > nul 2>&1
"

            File.WriteAllText(batPath, batContent)

            ' Start batch file with elevation (triggers UAC)
            Process.Start(New ProcessStartInfo With {
                .FileName = batPath,
                .Verb = "runas",
                .WindowStyle = ProcessWindowStyle.Hidden,
                .UseShellExecute = True
            })

            ' Force immediate exit to avoid ObjectDisposedException
            Environment.Exit(0)
        Catch ex As Exception
            Throw New Exception($"Failed to install update: {ex.Message}", ex)
        End Try
    End Sub
End Class

Public Class UpdateInfo
    Public Property IsUpdateAvailable As Boolean
    Public Property CurrentVersion As String
    Public Property NewVersion As String
    Public Property DownloadUrl As String
End Class
