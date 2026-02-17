Imports System.Data.SqlClient
Imports System.Configuration

Public Class ViewOrdersForm
    Private ReadOnly _connString As String
    Private ReadOnly _branchId As Integer
    
    Public Sub New(branchId As Integer)
        InitializeComponent()
        _connString = ConfigurationManager.ConnectionStrings("OvenDelightsConnectionString")?.ConnectionString
        _branchId = branchId
    End Sub
    
    Private Sub ViewOrdersForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadOrders()
    End Sub
    
    Private Sub LoadOrders()
        Try
            Using conn As New SqlConnection(_connString)
                conn.Open()
                
                Dim sql As String = "
                    SELECT 
                        OrderNumber AS [Order #],
                        CustomerName + ' ' + CustomerSurname AS Customer,
                        CustomerPhone AS Phone,
                        CONVERT(VARCHAR, ReadyDate, 106) + ' ' + CONVERT(VARCHAR, ReadyTime, 108) AS [Ready Date/Time],
                        TotalAmount AS Total,
                        DepositPaid AS Deposit,
                        BalanceDue AS Balance,
                        OrderStatus AS Status
                    FROM POS_CustomOrders
                    WHERE BranchID = @branchId
                    AND OrderStatus IN ('New', 'Ready')
                    ORDER BY ReadyDate, ReadyTime"
                
                Using da As New SqlDataAdapter(sql, conn)
                    da.SelectCommand.Parameters.AddWithValue("@branchId", _branchId)
                    Dim dt As New DataTable()
                    da.Fill(dt)
                    dgvOrders.DataSource = dt
                    
                    If dgvOrders.Columns.Contains("Total") Then
                        dgvOrders.Columns("Total").DefaultCellStyle.Format = "N2"
                    End If
                    If dgvOrders.Columns.Contains("Deposit") Then
                        dgvOrders.Columns("Deposit").DefaultCellStyle.Format = "N2"
                    End If
                    If dgvOrders.Columns.Contains("Balance") Then
                        dgvOrders.Columns("Balance").DefaultCellStyle.Format = "N2"
                    End If
                    
                    For Each row As DataGridViewRow In dgvOrders.Rows
                        Dim status As String = row.Cells("Status").Value.ToString()
                        If status = "New" Then
                            row.DefaultCellStyle.BackColor = Color.LightYellow
                            row.Cells("Status").Style.BackColor = Color.Orange
                            row.Cells("Status").Style.ForeColor = Color.White
                        ElseIf status = "Ready" Then
                            row.DefaultCellStyle.BackColor = Color.LightGreen
                            row.Cells("Status").Style.BackColor = Color.Green
                            row.Cells("Status").Style.ForeColor = Color.White
                        End If
                    Next
                    
                    lblCount.Text = $"{dt.Rows.Count} orders"
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading orders: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    
    Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
        LoadOrders()
    End Sub
    
    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub
End Class
