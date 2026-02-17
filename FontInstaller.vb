Imports System.Runtime.InteropServices
Imports System.IO

Public Class FontInstaller
    ''' <summary>
    ''' Installs the Free 3 of 9 barcode font if not already installed
    ''' Call this from Application_Startup or Main form Load event
    ''' </summary>
    Public Shared Sub InstallBarcodeFont()
        Try
            ' Check if font is already installed
            Using testFont As New Font("Free 3 of 9", 12, FontStyle.Regular)
                If testFont.Name = "Free 3 of 9" Then
                    ' Font already installed
                    Return
                End If
            End Using
        Catch
            ' Font not installed, proceed with installation
        End Try

        Try
            ' Get font file from application directory
            Dim appPath As String = Application.StartupPath
            Dim fontFile As String = Path.Combine(appPath, "3OF9_NEW.TTF")

            ' Check if font file exists in application directory
            If Not File.Exists(fontFile) Then
                ' Try Resources folder
                fontFile = Path.Combine(appPath, "Resources", "3OF9_NEW.TTF")
                If Not File.Exists(fontFile) Then
                    Return ' Font file not found, use fallback rendering
                End If
            End If

            ' Install font
            Dim fontsFolder As String = Environment.GetFolderPath(Environment.SpecialFolder.Fonts)
            Dim destFile As String = Path.Combine(fontsFolder, "3OF9_NEW.TTF")

            ' Copy font file to Fonts folder
            If Not File.Exists(destFile) Then
                File.Copy(fontFile, destFile, True)

                ' Register font in registry (requires admin rights)
                Try
                    Using key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", True)
                        If key IsNot Nothing Then
                            key.SetValue("Free 3 of 9 (TrueType)", "3OF9_NEW.TTF", Microsoft.Win32.RegistryValueKind.String)
                        End If
                    End Using
                Catch ex As UnauthorizedAccessException
                    ' Not running as admin - font copied but not registered
                    ' Will still work for current user
                End Try

                ' Notify Windows that fonts have changed
                SendMessage(HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero)
            End If

        Catch ex As Exception
            ' Installation failed - application will use fallback rendering
            Debug.WriteLine($"Font installation failed: {ex.Message}")
        End Try
    End Sub

    ' Windows API declarations for font change notification
    Private Const WM_FONTCHANGE As Integer = &H1D
    Private Shared ReadOnly HWND_BROADCAST As New IntPtr(&HFFFF)

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function
End Class
