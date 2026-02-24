Imports System.Configuration

Public Class CakeOrderCancelService
    Private _branchID As Integer
    Private _tillPointID As Integer
    Private _cashierID As Integer
    Private _cashierName As String
    Private _branchName As String
    Private _branchAddress As String
    Private _branchPhone As String

    Public Sub New(branchID As Integer, tillPointID As Integer, cashierID As Integer, cashierName As String, branchName As String, branchAddress As String, branchPhone As String)
        _branchID = branchID
        _tillPointID = tillPointID
        _cashierID = cashierID
        _cashierName = cashierName
        _branchName = branchName
        _branchAddress = branchAddress
        _branchPhone = branchPhone
    End Sub

    Public Sub StartCancelWorkflow()
        Try
            ' Step 1: Authenticate Retail Manager
            Dim authDialog As New RetailManagerAuthDialog()
            If authDialog.ShowDialog() <> DialogResult.OK Then
                authDialog.Dispose()
                Return ' User cancelled or auth failed
            End If
            authDialog.Dispose()

            ' Step 2: Get order lookup criteria
            Dim accountNumber As String = ""
            Dim pickupDate As Date = Date.Today

            Dim lookupDialog As New OrderLookupDialog()
            If lookupDialog.ShowDialog() = DialogResult.OK Then
                accountNumber = lookupDialog.AccountNumber
                pickupDate = lookupDialog.PickupDate
                lookupDialog.Dispose()
            Else
                lookupDialog.Dispose()
                Return ' User cancelled
            End If

            ' Step 3: Show orders and let user select
            Dim selectedOrderID As Integer = 0

            Dim selectionDialog As New OrderSelectionDialog(accountNumber, pickupDate)
            If selectionDialog.ShowDialog() = DialogResult.OK Then
                selectedOrderID = selectionDialog.SelectedOrderID
                selectionDialog.Dispose()
            Else
                selectionDialog.Dispose()
                Return ' User cancelled
            End If

            ' Step 4: Open CakeOrderFormNew in CANCEL mode
            If selectedOrderID > 0 Then
                Dim cancelForm As New CakeOrderFormNew(_branchID, _tillPointID, _cashierID, _cashierName, _branchName, _branchAddress, _branchPhone, Nothing, selectedOrderID, True)
                cancelForm.ShowDialog()
                cancelForm.Dispose()
            End If

        Catch ex As Exception
            MessageBox.Show($"Error starting cancel workflow: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
