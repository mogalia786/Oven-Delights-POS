Imports System.Windows.Forms

Public Class UpdateDialog
    Inherits Form

    Private lblMessage As Label
    Private progressBar As ProgressBar
    Private btnUpdate As Button
    Private btnLater As Button
    Private _updateInfo As UpdateInfo
    Private _updateService As AutoUpdateService

    Public Sub New(updateInfo As UpdateInfo, updateService As AutoUpdateService)
        _updateInfo = updateInfo
        _updateService = updateService
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Update Available"
        Me.Size = New Size(450, 200)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Message label
        lblMessage = New Label With {
            .Text = $"A new version of Overn Delights POS is available!{Environment.NewLine}{Environment.NewLine}" &
                    $"Current Version: {_updateInfo.CurrentVersion}{Environment.NewLine}" &
                    $"New Version: {_updateInfo.NewVersion}{Environment.NewLine}{Environment.NewLine}" &
                    "Would you like to update now?",
            .Location = New Point(20, 20),
            .Size = New Size(400, 80),
            .AutoSize = False
        }
        Me.Controls.Add(lblMessage)

        ' Progress bar (hidden initially)
        progressBar = New ProgressBar With {
            .Location = New Point(20, 110),
            .Size = New Size(400, 23),
            .Visible = False
        }
        Me.Controls.Add(progressBar)

        ' Update button
        btnUpdate = New Button With {
            .Text = "Update Now",
            .Location = New Point(240, 120),
            .Size = New Size(100, 30),
            .DialogResult = DialogResult.OK
        }
        AddHandler btnUpdate.Click, AddressOf BtnUpdate_Click
        Me.Controls.Add(btnUpdate)

        ' Later button
        btnLater = New Button With {
            .Text = "Later",
            .Location = New Point(350, 120),
            .Size = New Size(70, 30),
            .DialogResult = DialogResult.Cancel
        }
        Me.Controls.Add(btnLater)

        Me.AcceptButton = btnUpdate
        Me.CancelButton = btnLater
    End Sub

    Private Sub BtnUpdate_Click(sender As Object, e As EventArgs)
        Try
            ' Close this dialog first to avoid disposed object issues
            Me.DialogResult = DialogResult.OK
            Me.Close()
            
            ' Show a simple progress form
            Dim progressForm As New Form With {
                .Text = "Updating...",
                .Size = New Size(400, 150),
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .StartPosition = FormStartPosition.CenterScreen,
                .MaximizeBox = False,
                .MinimizeBox = False
            }
            
            Dim lblProgress As New Label With {
                .Text = "Downloading update, please wait...",
                .Location = New Point(20, 40),
                .Size = New Size(360, 60),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            progressForm.Controls.Add(lblProgress)
            progressForm.Show()
            Application.DoEvents()

            ' Download update synchronously
            Dim zipPath = _updateService.DownloadUpdate(Nothing)
            
            lblProgress.Text = "Installing update..."
            Application.DoEvents()

            ' Install update (this will close the app)
            _updateService.InstallUpdate(zipPath)

        Catch ex As Exception
            MessageBox.Show($"Update failed: {ex.Message}{Environment.NewLine}{Environment.NewLine}Please try again later or contact support.",
                          "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
