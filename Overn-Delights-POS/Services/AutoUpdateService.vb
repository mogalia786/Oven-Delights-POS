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

    Public Function DownloadUpdate(progressCallback As Action(Of Integer)) As String
        Try
            Dim tempPath = Path.Combine(Path.GetTempPath(), "POS_Update.zip")
            
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
            Dim posFolder = extractPath
            If Directory.Exists(Path.Combine(extractPath, "POS")) Then
                posFolder = Path.Combine(extractPath, "POS")
            End If

            ' Create batch file to perform update after app closes
            Dim batchPath = Path.Combine(Path.GetTempPath(), "update_pos.bat")
            Dim batchContent = $"@echo off
timeout /t 2 /nobreak > nul
echo Updating Overn Delights POS...
xcopy /E /I /Y ""{posFolder}\*"" ""{appPath}\"" > nul
if exist ""{zipPath}"" del ""{zipPath}""
if exist ""{extractPath}"" rmdir /s /q ""{extractPath}""
start """" ""{Application.ExecutablePath}""
del ""%~f0"""

            File.WriteAllText(batchPath, batchContent)

            ' Start the batch file and exit the application
            Process.Start(New ProcessStartInfo With {
                .FileName = batchPath,
                .WindowStyle = ProcessWindowStyle.Hidden,
                .CreateNoWindow = True
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
