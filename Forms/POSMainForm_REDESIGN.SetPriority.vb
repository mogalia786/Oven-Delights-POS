Imports System.Windows.Forms

Partial Public Class POSMainForm_REDESIGN
    ''' <summary>
    ''' Opens priority management dialog for current subcategory
    ''' Requires supervisor authentication
    ''' </summary>
    Private Sub SetItemPriority()
        Try
            ' Check if we're viewing products (not categories or subcategories)
            If _currentView <> "products" OrElse _currentSubCategoryId = 0 Then
                MessageBox.Show("Please select a subcategory first to manage item priorities.", "No Subcategory Selected", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Authenticate supervisor
            Dim authDialog As New SupervisorAuthDialog(_connectionString)
            If authDialog.ShowDialog(Me) <> DialogResult.OK Then
                Return ' User cancelled or authentication failed
            End If

            ' Open priority management dialog
            Dim priorityDialog As New ItemPriorityManagementDialog(
                _connectionString,
                _branchID,
                _currentSubCategoryId,
                _currentSubCategoryName,
                authDialog.AuthenticatedUsername
            )

            If priorityDialog.ShowDialog(Me) = DialogResult.OK Then
                ' Refresh product display to show new priority order
                ShowProductsForSubCategory(_currentSubCategoryId, _currentSubCategoryName)
                MessageBox.Show("Item priorities updated successfully! Products are now displayed in priority order.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show($"Error managing item priorities: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
