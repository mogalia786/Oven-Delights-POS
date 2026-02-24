<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class RefundTenderDialog
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.pnlTop = New System.Windows.Forms.Panel()
        Me.lblTitle = New System.Windows.Forms.Label()
        Me.lblRefundAmountLabel = New System.Windows.Forms.Label()
        Me.lblRefundAmount = New System.Windows.Forms.Label()
        Me.lblOriginalMethodLabel = New System.Windows.Forms.Label()
        Me.lblOriginalMethod = New System.Windows.Forms.Label()
        Me.btnCash = New System.Windows.Forms.Button()
        Me.btnCard = New System.Windows.Forms.Button()
        Me.btnEFT = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.lblInstruction = New System.Windows.Forms.Label()
        Me.pnlTop.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlTop
        '
        Me.pnlTop.BackColor = System.Drawing.Color.FromArgb(CType(CType(192, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(0, Byte), Integer))
        Me.pnlTop.Controls.Add(Me.lblTitle)
        Me.pnlTop.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlTop.Location = New System.Drawing.Point(0, 0)
        Me.pnlTop.Name = "pnlTop"
        Me.pnlTop.Size = New System.Drawing.Size(600, 60)
        Me.pnlTop.TabIndex = 0
        '
        'lblTitle
        '
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New System.Drawing.Font("Segoe UI", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTitle.ForeColor = System.Drawing.Color.White
        Me.lblTitle.Location = New System.Drawing.Point(12, 12)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New System.Drawing.Size(222, 32)
        Me.lblTitle.TabIndex = 0
        Me.lblTitle.Text = "REFUND TENDER"
        '
        'lblRefundAmountLabel
        '
        Me.lblRefundAmountLabel.AutoSize = True
        Me.lblRefundAmountLabel.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblRefundAmountLabel.Location = New System.Drawing.Point(150, 80)
        Me.lblRefundAmountLabel.Name = "lblRefundAmountLabel"
        Me.lblRefundAmountLabel.Size = New System.Drawing.Size(136, 21)
        Me.lblRefundAmountLabel.TabIndex = 1
        Me.lblRefundAmountLabel.Text = "Refund Amount:"
        '
        'lblRefundAmount
        '
        Me.lblRefundAmount.AutoSize = True
        Me.lblRefundAmount.Font = New System.Drawing.Font("Segoe UI", 20.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblRefundAmount.ForeColor = System.Drawing.Color.Green
        Me.lblRefundAmount.Location = New System.Drawing.Point(300, 70)
        Me.lblRefundAmount.Name = "lblRefundAmount"
        Me.lblRefundAmount.Size = New System.Drawing.Size(101, 37)
        Me.lblRefundAmount.TabIndex = 2
        Me.lblRefundAmount.Text = "R 0.00"
        '
        'lblOriginalMethodLabel
        '
        Me.lblOriginalMethodLabel.AutoSize = True
        Me.lblOriginalMethodLabel.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblOriginalMethodLabel.Location = New System.Drawing.Point(150, 120)
        Me.lblOriginalMethodLabel.Name = "lblOriginalMethodLabel"
        Me.lblOriginalMethodLabel.Size = New System.Drawing.Size(164, 19)
        Me.lblOriginalMethodLabel.TabIndex = 3
        Me.lblOriginalMethodLabel.Text = "Original Payment Method:"
        '
        'lblOriginalMethod
        '
        Me.lblOriginalMethod.AutoSize = True
        Me.lblOriginalMethod.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblOriginalMethod.Location = New System.Drawing.Point(320, 119)
        Me.lblOriginalMethod.Name = "lblOriginalMethod"
        Me.lblOriginalMethod.Size = New System.Drawing.Size(41, 20)
        Me.lblOriginalMethod.TabIndex = 4
        Me.lblOriginalMethod.Text = "N/A"
        '
        'btnCash
        '
        Me.btnCash.BackColor = System.Drawing.Color.FromArgb(CType(CType(39, Byte), Integer), CType(CType(174, Byte), Integer), CType(CType(96, Byte), Integer))
        Me.btnCash.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCash.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCash.ForeColor = System.Drawing.Color.White
        Me.btnCash.Location = New System.Drawing.Point(50, 220)
        Me.btnCash.Name = "btnCash"
        Me.btnCash.Size = New System.Drawing.Size(150, 100)
        Me.btnCash.TabIndex = 5
        Me.btnCash.Text = "💵 CASH"
        Me.btnCash.UseVisualStyleBackColor = False
        '
        'btnCard
        '
        Me.btnCard.BackColor = System.Drawing.Color.FromArgb(CType(CType(52, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(219, Byte), Integer))
        Me.btnCard.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCard.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCard.ForeColor = System.Drawing.Color.White
        Me.btnCard.Location = New System.Drawing.Point(225, 220)
        Me.btnCard.Name = "btnCard"
        Me.btnCard.Size = New System.Drawing.Size(150, 100)
        Me.btnCard.TabIndex = 6
        Me.btnCard.Text = "💳 CARD"
        Me.btnCard.UseVisualStyleBackColor = False
        '
        'btnEFT
        '
        Me.btnEFT.BackColor = System.Drawing.Color.FromArgb(CType(CType(155, Byte), Integer), CType(CType(89, Byte), Integer), CType(CType(182, Byte), Integer))
        Me.btnEFT.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnEFT.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnEFT.ForeColor = System.Drawing.Color.White
        Me.btnEFT.Location = New System.Drawing.Point(400, 220)
        Me.btnEFT.Name = "btnEFT"
        Me.btnEFT.Size = New System.Drawing.Size(150, 100)
        Me.btnEFT.TabIndex = 7
        Me.btnEFT.Text = "🏦 EFT"
        Me.btnEFT.UseVisualStyleBackColor = False
        '
        'btnCancel
        '
        Me.btnCancel.BackColor = System.Drawing.Color.Gray
        Me.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCancel.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCancel.ForeColor = System.Drawing.Color.White
        Me.btnCancel.Location = New System.Drawing.Point(225, 340)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(150, 45)
        Me.btnCancel.TabIndex = 8
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = False
        '
        'lblInstruction
        '
        Me.lblInstruction.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblInstruction.ForeColor = System.Drawing.Color.Gray
        Me.lblInstruction.Location = New System.Drawing.Point(50, 165)
        Me.lblInstruction.Name = "lblInstruction"
        Me.lblInstruction.Size = New System.Drawing.Size(500, 40)
        Me.lblInstruction.TabIndex = 9
        Me.lblInstruction.Text = "Select the refund method to process the refund to the customer." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Recommended: Use" &
    " the original payment method."
        Me.lblInstruction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'RefundTenderDialog
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(600, 410)
        Me.Controls.Add(Me.lblInstruction)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.btnEFT)
        Me.Controls.Add(Me.btnCard)
        Me.Controls.Add(Me.btnCash)
        Me.Controls.Add(Me.lblOriginalMethod)
        Me.Controls.Add(Me.lblOriginalMethodLabel)
        Me.Controls.Add(Me.lblRefundAmount)
        Me.Controls.Add(Me.lblRefundAmountLabel)
        Me.Controls.Add(Me.pnlTop)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "RefundTenderDialog"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Refund Tender"
        Me.pnlTop.ResumeLayout(False)
        Me.pnlTop.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents pnlTop As Panel
    Friend WithEvents lblTitle As Label
    Friend WithEvents lblRefundAmountLabel As Label
    Friend WithEvents lblRefundAmount As Label
    Friend WithEvents lblOriginalMethodLabel As Label
    Friend WithEvents lblOriginalMethod As Label
    Friend WithEvents btnCash As Button
    Friend WithEvents btnCard As Button
    Friend WithEvents btnEFT As Button
    Friend WithEvents btnCancel As Button
    Friend WithEvents lblInstruction As Label
End Class
