Imports System.Windows.Forms

Namespace My
    Partial Friend Class MyApplication
        Private Sub MyApplication_Startup(sender As Object, e As ApplicationServices.StartupEventArgs) Handles Me.Startup
            ' Show login form
            Dim loginForm As New LoginForm()
            
            If loginForm.ShowDialog() = DialogResult.OK Then
                ' Login successful, show main POS form
                Dim posForm As New POSMainForm(loginForm.CashierID, loginForm.CashierName, loginForm.BranchID)
                posForm.ShowDialog()
            End If
            
            ' Exit application after POS closes
            Application.Exit()
        End Sub
    End Class
End Namespace
