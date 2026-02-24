Imports System.Data.SqlClient
Imports System.Configuration

Public Class RefundTenderDialog
    Private ReadOnly _amount As Decimal
    Private ReadOnly _originalPaymentMethod As String
    Private ReadOnly _isPayment As Boolean
    Private _selectedRefundMethod As String = ""
    
    Public Property RefundMethod As String
        Get
            Return _selectedRefundMethod
        End Get
        Private Set(value As String)
            _selectedRefundMethod = value
        End Set
    End Property
    
    Public Sub New(amount As Decimal, originalPaymentMethod As String, Optional isPayment As Boolean = False)
        InitializeComponent()
        _amount = amount
        _originalPaymentMethod = originalPaymentMethod
        _isPayment = isPayment
    End Sub
    
    Private Sub RefundTenderDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Update form title and labels based on payment or refund
        If _isPayment Then
            Me.Text = "Process Payment"
            lblTitle.Text = "Customer Payment"
            lblRefundAmount.Text = _amount.ToString("C2")
            lblOriginalMethod.Text = _originalPaymentMethod
        Else
            Me.Text = "Process Refund"
            lblTitle.Text = "Customer Refund"
            lblRefundAmount.Text = _amount.ToString("C2")
            lblOriginalMethod.Text = _originalPaymentMethod
        End If
        
        ' Highlight the original payment method button
        HighlightOriginalMethod()
    End Sub
    
    Private Sub HighlightOriginalMethod()
        Select Case _originalPaymentMethod.ToUpper()
            Case "CASH"
                btnCash.BackColor = Color.FromArgb(46, 204, 113)
                btnCash.Text = "💵 CASH" & vbCrLf & "(Original Method)"
            Case "CARD", "CREDIT CARD", "DEBIT CARD"
                btnCard.BackColor = Color.FromArgb(46, 204, 113)
                btnCard.Text = "💳 CARD" & vbCrLf & "(Original Method)"
            Case "EFT"
                btnEFT.BackColor = Color.FromArgb(46, 204, 113)
                btnEFT.Text = "🏦 EFT" & vbCrLf & "(Original Method)"
        End Select
    End Sub
    
    Private Sub btnCash_Click(sender As Object, e As EventArgs) Handles btnCash.Click
        _selectedRefundMethod = "Cash"
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
    
    Private Sub btnCard_Click(sender As Object, e As EventArgs) Handles btnCard.Click
        _selectedRefundMethod = "Card"
        
        ' Show card processing confirmation
        Dim actionText = If(_isPayment, "payment", "refund")
        Dim result = MessageBox.Show(
            $"Process card {actionText} of {_amount.ToString("C2")}?{vbCrLf}{vbCrLf}" &
            $"Please ensure the card terminal is ready for {actionText} processing.",
            $"Card {If(_isPayment, "Payment", "Refund")}",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)
        
        If result = DialogResult.Yes Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End If
    End Sub
    
    Private Sub btnEFT_Click(sender As Object, e As EventArgs) Handles btnEFT.Click
        _selectedRefundMethod = "EFT"
        
        ' Show EFT processing instructions
        Dim actionText = If(_isPayment, "Payment", "Refund")
        MessageBox.Show(
            $"EFT {actionText}: {_amount.ToString("C2")}{vbCrLf}{vbCrLf}" &
            $"Please process the EFT {actionText.ToLower()} manually and confirm when complete.",
            $"EFT {actionText}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
        
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
    
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
