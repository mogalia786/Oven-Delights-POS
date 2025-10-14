Imports System.Windows.Forms

Namespace My
    Partial Friend Class MyApplication
        Private Sub MyApplication_Startup(sender As Object, e As ApplicationServices.StartupEventArgs) Handles Me.Startup
            ' Show login form
            Dim loginForm As New LoginForm()
            
            If loginForm.ShowDialog() = DialogResult.OK Then
                ' Store values before form is disposed
                Dim cashierID As Integer = loginForm.CashierID
                Dim cashierName As String = loginForm.CashierName
                Dim branchID As Integer = loginForm.BranchID

                ' Login successful, show main POS form
                ' Using the REDESIGN form with all modern code
                Dim posForm As New POSMainForm_REDESIGN(cashierID, cashierName, branchID)
                posForm.ShowDialog()
            End If
            
            ' Exit application after POS closes
            Environment.Exit(0)
        End Sub
    End Class
End Namespace
