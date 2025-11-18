Imports System.Data
Imports System.Configuration
Imports Microsoft.Data.SqlClient
Imports System.Drawing.Printing

Public Class ReceiptPreviewForm
    Inherits Form
    
    Private _connectionString As String
    Private _branchID As Integer
    Private _orderData As Dictionary(Of String, String)
    Private _fields As New Dictionary(Of String, FieldConfig)
    Private Const PAPER_WIDTH_PX As Integer = 830
    Private _printTimer As Timer
    
    Private Class FieldConfig
        Public Property Name As String
        Public Property XPos As Integer
        Public Property YPos As Integer
        Public Property FontSize As Integer
        Public Property IsBold As Boolean
        Public Property IsEnabled As Boolean
    End Class
    
    Public Sub New(branchID As Integer, orderData As Dictionary(Of String, String))
        InitializeComponent()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString")?.ConnectionString
        _branchID = branchID
        _orderData = orderData
        
        Me.Text = "Receipt Preview - Printing..."
        Me.Size = New Size(900, 950)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.None
        
        LoadTemplateConfiguration()
        SetupUI()
        StartPrintingAnimation()
    End Sub
    
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
    End Sub
    
    Private Sub LoadTemplateConfiguration()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As New SqlCommand("SELECT * FROM ReceiptTemplateConfig WHERE BranchID = @BranchID", conn)
                    cmd.Parameters.AddWithValue("@BranchID", _branchID)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim fieldName = reader("FieldName").ToString()
                            _fields(fieldName) = New FieldConfig With {
                                .Name = fieldName,
                                .XPos = Convert.ToInt32(reader("XPosition")),
                                .YPos = Convert.ToInt32(reader("YPosition")),
                                .FontSize = Convert.ToInt32(reader("FontSize")),
                                .IsBold = Convert.ToBoolean(reader("IsBold")),
                                .IsEnabled = Convert.ToBoolean(reader("IsEnabled"))
                            }
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' Use defaults if no config
            InitializeDefaultFields()
        End Try
    End Sub
    
    Private Sub InitializeDefaultFields()
        AddField("CompanyName", 10, 10, 12, True)
        AddField("CompanyTagline", 10, 30, 8, False)
        AddField("CoRegNo", 10, 50, 7, False)
        AddField("VATNumber", 10, 65, 7, False)
        AddField("ShopNo", 10, 80, 7, False)
        AddField("Address", 10, 95, 7, False)
        AddField("City", 10, 110, 7, False)
        AddField("Phone", 10, 125, 7, False)
        AddField("Email", 10, 140, 7, False)
        AddField("AccountRef", 10, 160, 7, True)
        AddField("AccountNo", 10, 185, 8, False)
        AddField("CustomerName", 10, 200, 8, False)
        AddField("Telephone", 10, 215, 8, False)
        AddField("CellNumber", 10, 230, 8, False)
        AddField("SpecialRequest", 10, 250, 8, True)
        AddField("CakeColour", 450, 50, 8, False)
        AddField("CakePicture", 450, 65, 8, False)
        AddField("CollectionDate", 450, 80, 8, False)
        AddField("CollectionDay", 450, 95, 8, False)
        AddField("CollectionTime", 450, 110, 8, False)
        AddField("OrderHeader", 10, 290, 8, True)
        AddField("OrderDetails", 10, 305, 8, False)
        AddField("ItemHeader", 10, 330, 8, True)
        AddField("ItemLine1", 10, 345, 8, False)
        AddField("Message", 10, 380, 10, True)
        AddField("Terms", 10, 650, 7, False)
        AddField("Terms2", 10, 665, 7, False)
        AddField("InvoiceTotal", 450, 650, 9, True)
        AddField("DepositPaid", 450, 670, 9, False)
        AddField("BalanceOwing", 450, 690, 9, True)
    End Sub
    
    Private Sub AddField(name As String, x As Integer, y As Integer, fontSize As Integer, isBold As Boolean)
        _fields(name) = New FieldConfig With {
            .Name = name,
            .XPos = x,
            .YPos = y,
            .FontSize = fontSize,
            .IsBold = isBold,
            .IsEnabled = True
        }
    End Sub
    
    Private Sub SetupUI()
        Dim pnlMain As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black
        }
        
        Dim lblPrinting As New Label With {
            .Text = "PRINTING...",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.Lime,
            .AutoSize = True,
            .Location = New Point(350, 20)
        }
        
        Dim pnlPaper As New Panel With {
            .Name = "pnlPaper",
            .Size = New Size(PAPER_WIDTH_PX, 850),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Location = New Point(35, 60),
            .AutoScroll = True
        }
        
        RenderReceipt(pnlPaper)
        
        pnlMain.Controls.AddRange({lblPrinting, pnlPaper})
        Me.Controls.Add(pnlMain)
    End Sub
    
    Private Sub RenderReceipt(pnlPaper As Panel)
        For Each kvp In _fields
            Dim field = kvp.Value
            If Not field.IsEnabled Then Continue For
            
            Dim text As String = GetFieldValue(field.Name)
            If String.IsNullOrEmpty(text) Then Continue For
            
            Dim lbl As New Label With {
                .Text = text,
                .Location = New Point(field.XPos, field.YPos),
                .AutoSize = True,
                .Font = New Font("Courier New", field.FontSize, If(field.IsBold, FontStyle.Bold, FontStyle.Regular)),
                .BackColor = Color.Transparent
            }
            pnlPaper.Controls.Add(lbl)
        Next
    End Sub
    
    Private Function GetFieldValue(fieldName As String) As String
        If _orderData.ContainsKey(fieldName) Then
            Return _orderData(fieldName)
        End If
        
        ' Return defaults if not in order data
        Select Case fieldName
            Case "CompanyName" : Return "Oven Delights"
            Case "CompanyTagline" : Return "YOUR TRUSTED FAMILY BAKERY"
            Case "Terms" : Return "All same day orders and cancellations will attract a R30.00 service charge"
            Case "Terms2" : Return "All changes to size, cream and date - R20.00 service charge"
            Case Else : Return ""
        End Select
    End Function
    
    Private Sub StartPrintingAnimation()
        _printTimer = New Timer With {
            .Interval = 3000
        }
        AddHandler _printTimer.Tick, Sub()
                                         _printTimer.Stop()
                                         Me.Close()
                                     End Sub
        _printTimer.Start()
    End Sub
    
    Private components As System.ComponentModel.IContainer
End Class
