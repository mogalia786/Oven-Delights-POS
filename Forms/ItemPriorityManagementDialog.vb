Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Windows.Forms

Public Class ItemPriorityManagementDialog
    Inherits Form

    Private _connectionString As String
    Private _branchID As Integer
    Private _subCategoryID As Integer
    Private _subCategoryName As String
    Private _supervisorUsername As String

    ' UI Controls
    Private dgvItems As DataGridView
    Private btnSave As Button
    Private btnCancel As Button
    Private lblInfo As Label

    Public Sub New(connectionString As String, branchID As Integer, subCategoryID As Integer, subCategoryName As String, supervisorUsername As String)
        MyBase.New()
        _connectionString = connectionString
        _branchID = branchID
        _subCategoryID = subCategoryID
        _subCategoryName = subCategoryName
        _supervisorUsername = supervisorUsername

        InitializeComponent()
        LoadItems()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Item Display Priority Management"
        Me.Size = New Size(900, 700)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True
        Me.BackColor = Color.White

        Dim yPos As Integer = 20

        ' Header
        Dim lblHeader As New Label With {
            .Text = "ITEM DISPLAY PRIORITY MANAGEMENT",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#E67E22"),
            .Location = New Point(20, yPos),
            .Size = New Size(850, 35),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblHeader)
        yPos += 45

        ' Info label
        lblInfo = New Label With {
            .Text = $"Sub-Category: {_subCategoryName} | Supervisor: {_supervisorUsername}",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = ColorTranslator.FromHtml("#2C3E50"),
            .Location = New Point(20, yPos),
            .Size = New Size(850, 25),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblInfo)
        yPos += 35

        ' Instructions
        Dim lblInstructions As New Label With {
            .Text = "Set priority numbers for items (1 = highest priority, displayed first). Items without priority are shown alphabetically at the end.",
            .Font = New Font("Segoe UI", 9),
            .Location = New Point(20, yPos),
            .Size = New Size(850, 30),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblInstructions)
        yPos += 40

        ' DataGridView
        dgvItems = New DataGridView With {
            .Location = New Point(20, yPos),
            .Size = New Size(860, 450),
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Font = New Font("Segoe UI", 10)
        }
        AddHandler dgvItems.DataBindingComplete, AddressOf DgvItems_DataBindingComplete
        Me.Controls.Add(dgvItems)
        yPos += 460

        ' Buttons
        btnSave = New Button With {
            .Text = "Save Priorities",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(150, 40),
            .Location = New Point(300, yPos),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnSave.FlatAppearance.BorderSize = 0
        AddHandler btnSave.Click, AddressOf BtnSave_Click

        btnCancel = New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .Size = New Size(150, 40),
            .Location = New Point(470, yPos),
            .BackColor = ColorTranslator.FromHtml("#E74C3C"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }
        btnCancel.FlatAppearance.BorderSize = 0
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click

        Me.Controls.AddRange({btnSave, btnCancel})
    End Sub

    Private Sub LoadItems()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql = "
                    SELECT 
                        price.PriceID,
                        drp.ProductID,
                        drp.Name AS ProductName,
                        price.DisplayPriority,
                        price.SellingPrice AS Price
                    FROM Demo_Retail_Price price
                    INNER JOIN Demo_Retail_Product drp ON price.ProductID = drp.ProductID
                    WHERE price.BranchID = @BranchID 
                    AND drp.SubCategoryID = @SubCategoryID
                    AND drp.BranchID = @BranchID
                    ORDER BY 
                        CASE WHEN price.DisplayPriority IS NULL THEN 1 ELSE 0 END,
                        price.DisplayPriority ASC,
                        drp.Name ASC"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    cmd.Parameters.AddWithValue("@SubCategoryID", _subCategoryID)

                    Dim adapter As New SqlDataAdapter(cmd)
                    Dim dt As New DataTable()
                    adapter.Fill(dt)

                    dgvItems.DataSource = dt

                    If dt.Rows.Count = 0 Then
                        MessageBox.Show("No products found in this subcategory.", "No Products", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error loading items: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DgvItems_DataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        Try
            ' Hide ID columns
            If dgvItems.Columns.Contains("PriceID") Then
                dgvItems.Columns("PriceID").Visible = False
            End If
            If dgvItems.Columns.Contains("ProductID") Then
                dgvItems.Columns("ProductID").Visible = False
            End If

            ' Configure ProductName column
            If dgvItems.Columns.Contains("ProductName") Then
                dgvItems.Columns("ProductName").HeaderText = "Product Name"
                dgvItems.Columns("ProductName").ReadOnly = True
                dgvItems.Columns("ProductName").Width = 400
            End If

            ' Configure Price column
            If dgvItems.Columns.Contains("Price") Then
                dgvItems.Columns("Price").HeaderText = "Price"
                dgvItems.Columns("Price").ReadOnly = True
                dgvItems.Columns("Price").DefaultCellStyle.Format = "C2"
                dgvItems.Columns("Price").Width = 100
            End If

            ' Configure DisplayPriority column
            If dgvItems.Columns.Contains("DisplayPriority") Then
                dgvItems.Columns("DisplayPriority").HeaderText = "Display Priority"
                dgvItems.Columns("DisplayPriority").ReadOnly = False
                dgvItems.Columns("DisplayPriority").Width = 150
                dgvItems.Columns("DisplayPriority").DefaultCellStyle.BackColor = Color.LightYellow
            End If

            ' Color rows based on priority
            For Each row As DataGridViewRow In dgvItems.Rows
                If Not row.IsNewRow AndAlso row.Cells("DisplayPriority").Value IsNot Nothing AndAlso Not IsDBNull(row.Cells("DisplayPriority").Value) Then
                    row.DefaultCellStyle.BackColor = Color.LightGreen
                End If
            Next
        Catch ex As Exception
            ' Ignore configuration errors
        End Try
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        Try
            ' Validate priority values
            For Each row As DataGridViewRow In dgvItems.Rows
                Dim priorityValue = row.Cells("DisplayPriority").Value
                If Not IsDBNull(priorityValue) AndAlso priorityValue IsNot Nothing Then
                    Dim priorityStr = priorityValue.ToString().Trim()
                    If Not String.IsNullOrEmpty(priorityStr) Then
                        Dim priority As Integer
                        If Not Integer.TryParse(priorityStr, priority) Then
                            MessageBox.Show($"Invalid priority value for '{row.Cells("ProductName").Value}'. Priority must be a number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                            Return
                        End If
                        If priority < 1 Then
                            MessageBox.Show($"Priority for '{row.Cells("ProductName").Value}' must be 1 or greater.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                            Return
                        End If
                    End If
                End If
            Next

            ' Confirm save
            Dim result = MessageBox.Show("Save display priorities for all items in this sub-category?", "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.No Then Return

            ' Save priorities
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                For Each row As DataGridViewRow In dgvItems.Rows
                    Dim priceID = CInt(row.Cells("PriceID").Value)
                    Dim priorityValue = row.Cells("DisplayPriority").Value

                    Dim sql = "UPDATE Demo_Retail_Price SET DisplayPriority = @Priority WHERE PriceID = @PriceID"
                    Using cmd As New SqlCommand(sql, conn)
                        cmd.Parameters.AddWithValue("@PriceID", priceID)

                        If IsDBNull(priorityValue) OrElse priorityValue Is Nothing OrElse String.IsNullOrWhiteSpace(priorityValue.ToString()) Then
                            cmd.Parameters.AddWithValue("@Priority", DBNull.Value)
                        Else
                            Dim priority As Integer
                            If Integer.TryParse(priorityValue.ToString().Trim(), priority) Then
                                cmd.Parameters.AddWithValue("@Priority", priority)
                            Else
                                cmd.Parameters.AddWithValue("@Priority", DBNull.Value)
                            End If
                        End If

                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End Using

            MessageBox.Show("Display priorities saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Error saving priorities: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
