Imports System.Data.SqlClient
Imports System.Configuration
Imports System.Windows.Forms

Public Class OrderSelectionDialog
    Inherits Form
    
    Private _connectionString As String
    Private _accountNumber As String
    Private _pickupDate As Date
    Private _selectedOrderID As Integer = 0
    Private dgvOrders As DataGridView

    Public ReadOnly Property SelectedOrderID As Integer
        Get
            Return _selectedOrderID
        End Get
    End Property

    Public Sub New(accountNumber As String, pickupDate As Date)
        Try
            _accountNumber = accountNumber
            _pickupDate = pickupDate
            Dim connStr = ConfigurationManager.ConnectionStrings("OvenDelightsPOSConnectionString")
            If connStr Is Nothing Then
                Throw New Exception("Connection string 'OvenDelightsPOSConnectionString' not found in configuration file.")
            End If
            _connectionString = connStr.ConnectionString
            InitializeComponent()
            AddHandler Me.Load, Sub(s, e) LoadOrders()
        Catch ex As Exception
            MessageBox.Show($"Configuration Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Throw
        End Try
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Select Order to Edit"
        Me.Size = New Size(900, 600)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.White

        Dim pnlHeader As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = ColorTranslator.FromHtml("#D2691E")
        }

        Dim lblCustomerName As New Label With {
            .Name = "lblCustomerName",
            .Text = "Loading...",
            .Font = New Font("Segoe UI", 18, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(20, 15),
            .Size = New Size(860, 35)
        }

        Dim lblOrderInfo As New Label With {
            .Name = "lblOrderInfo",
            .Text = $"Orders for pickup on {_pickupDate:dd MMMM yyyy}",
            .Font = New Font("Segoe UI", 11),
            .ForeColor = Color.FromArgb(240, 240, 240),
            .Location = New Point(20, 55),
            .Size = New Size(860, 25)
        }

        pnlHeader.Controls.AddRange({lblCustomerName, lblOrderInfo})

        Dim lblInstruction As New Label With {
            .Text = "Click on an order to edit",
            .Font = New Font("Segoe UI", 9, FontStyle.Italic),
            .ForeColor = Color.FromArgb(100, 100, 100),
            .Location = New Point(20, 115),
            .Size = New Size(860, 20)
        }

        dgvOrders = New DataGridView With {
            .Location = New Point(20, 145),
            .Size = New Size(860, 360),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .Font = New Font("Segoe UI", 10),
            .ColumnHeadersHeight = 40
        }

        dgvOrders.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#34495E")
        dgvOrders.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        dgvOrders.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        dgvOrders.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        dgvOrders.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
        dgvOrders.RowTemplate.Height = 35

        AddHandler dgvOrders.CellDoubleClick, AddressOf DgvOrders_CellDoubleClick

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Font = New Font("Segoe UI", 10),
            .Size = New Size(130, 45),
            .Location = New Point(620, 520),
            .BackColor = Color.FromArgb(200, 200, 200),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .DialogResult = DialogResult.Cancel
        }
        btnCancel.FlatAppearance.BorderSize = 0

        Dim btnSelect As New Button With {
            .Name = "btnSelect",
            .Text = "Edit Selected Order",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Size = New Size(130, 45),
            .Location = New Point(760, 520),
            .BackColor = ColorTranslator.FromHtml("#27AE60"),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnSelect.FlatAppearance.BorderSize = 0
        AddHandler btnSelect.Click, AddressOf BtnSelect_Click

        Me.Controls.AddRange({pnlHeader, lblInstruction, dgvOrders, btnCancel, btnSelect})
        Me.CancelButton = btnCancel
    End Sub

    Private Sub LoadOrders()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()

                Dim sql = "
                    SELECT 
                        OrderID,
                        OrderNumber,
                        CustomerName + ' ' + ISNULL(CustomerSurname, '') AS CustomerName,
                        ReadyTime,
                        TotalAmount,
                        DepositPaid,
                        BalanceDue
                    FROM POS_CustomOrders
                    WHERE AccountNumber = @AccountNumber 
                        AND CAST(ReadyDate AS DATE) = @ReadyDate
                    ORDER BY ReadyTime"

                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@AccountNumber", _accountNumber)
                    cmd.Parameters.AddWithValue("@ReadyDate", _pickupDate)

                    Dim dt As New DataTable()
                    Using adapter As New SqlDataAdapter(cmd)
                        adapter.Fill(dt)
                    End Using

                    If dt.Rows.Count > 0 Then
                        Dim customerName As String = dt.Rows(0)("CustomerName").ToString()
                        Dim lblCustomerName = DirectCast(Me.Controls.Find("lblCustomerName", True)(0), Label)
                        lblCustomerName.Text = customerName
                    End If

                    dgvOrders.DataSource = dt

                    dgvOrders.Columns("OrderID").Visible = False
                    dgvOrders.Columns("CustomerName").Visible = False

                    dgvOrders.Columns("OrderNumber").HeaderText = "Order #"
                    dgvOrders.Columns("OrderNumber").Width = 150

                    dgvOrders.Columns("ReadyTime").HeaderText = "Pickup Time"
                    dgvOrders.Columns("ReadyTime").Width = 120

                    dgvOrders.Columns("TotalAmount").HeaderText = "Total"
                    dgvOrders.Columns("TotalAmount").DefaultCellStyle.Format = "C2"
                    dgvOrders.Columns("TotalAmount").Width = 100

                    dgvOrders.Columns("DepositPaid").HeaderText = "Deposit"
                    dgvOrders.Columns("DepositPaid").DefaultCellStyle.Format = "C2"
                    dgvOrders.Columns("DepositPaid").Width = 100

                    dgvOrders.Columns("BalanceDue").HeaderText = "Balance"
                    dgvOrders.Columns("BalanceDue").DefaultCellStyle.Format = "C2"
                    dgvOrders.Columns("BalanceDue").DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#E74C3C")
                    dgvOrders.Columns("BalanceDue").DefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                    dgvOrders.Columns("BalanceDue").Width = 100
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DgvOrders_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex >= 0 Then
            SelectOrder()
        End If
    End Sub

    Private Sub BtnSelect_Click(sender As Object, e As EventArgs)
        SelectOrder()
    End Sub

    Private Sub SelectOrder()
        If dgvOrders.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select an order to edit.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        _selectedOrderID = CInt(dgvOrders.SelectedRows(0).Cells("OrderID").Value)
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
End Class
