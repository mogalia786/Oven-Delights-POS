' ========================================
' EDIT CAKE ORDER FEATURE INTEGRATION
' ========================================
' Add this code to POSMainForm_REDESIGN.vb

' 1. ADD THIS METHOD TO THE CLASS (place near other shortcut button handlers)
Private Sub EditCakeOrder()
    Try
        ' Get branch details
        Dim branchDetails = GetBranchDetails()
        
        ' Create and start edit workflow
        Dim editService As New CakeOrderEditService(
            _branchID, 
            _tillPointID, 
            _cashierID, 
            _cashierName,
            branchDetails.Name, 
            branchDetails.Address, 
            branchDetails.Phone
        )
        
        editService.StartEditWorkflow()
        
    Catch ex As Exception
        MessageBox.Show($"Error starting edit workflow: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Try
End Sub

' 2. ADD THIS BUTTON TO CreateShortcutButtons() METHOD
' Find the CreateShortcutButtons method and add this button:

Dim btnEditCakeOrder As New Button With {
    .Text = "✏️ EDIT CAKE ORDER (F11)",
    .Font = New Font("Segoe UI", 10, FontStyle.Bold),
    .Size = New Size(200, 70),
    .BackColor = ColorTranslator.FromHtml("#E67E22"),
    .ForeColor = Color.White,
    .FlatStyle = FlatStyle.Flat,
    .Cursor = Cursors.Hand,
    .Location = New Point(10, 5)  ' Adjust position as needed
}
btnEditCakeOrder.FlatAppearance.BorderSize = 0
AddHandler btnEditCakeOrder.Click, Sub() EditCakeOrder()
pnlShortcuts.Controls.Add(btnEditCakeOrder)

' 3. ADD KEYBOARD SHORTCUT HANDLER
' In the form's KeyDown event or ProcessCmdKey override:

Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
    If keyData = Keys.F11 Then
        EditCakeOrder()
        Return True
    End If
    Return MyBase.ProcessCmdKey(msg, keyData)
End Function

' ========================================
' ALTERNATIVE: If CreateShortcutButtons doesn't exist, add buttons directly in InitializeComponent
' ========================================

' In InitializeComponent method, after pnlShortcuts is created:

' Edit Cake Order Button
Dim btnEditCakeOrder As New Button With {
    .Text = "✏️ EDIT CAKE ORDER (F11)",
    .Font = New Font("Segoe UI", 10, FontStyle.Bold),
    .Size = New Size(200, 70),
    .BackColor = ColorTranslator.FromHtml("#E67E22"),
    .ForeColor = Color.White,
    .FlatStyle = FlatStyle.Flat,
    .Cursor = Cursors.Hand
}
btnEditCakeOrder.FlatAppearance.BorderSize = 0
AddHandler btnEditCakeOrder.Click, Sub() EditCakeOrder()

' Add to shortcuts panel
pnlShortcuts.Controls.Add(btnEditCakeOrder)

' ========================================
' NOTES:
' ========================================
' - Button color #E67E22 is orange (matches cake/order theme)
' - F11 shortcut key for quick access
' - Button should be placed in footer shortcuts panel
' - Adjust Size and Location based on available space
' - Ensure GetBranchDetails() method exists or replace with equivalent
