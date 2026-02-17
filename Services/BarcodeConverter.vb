Imports System.Text.RegularExpressions

Public Class BarcodeConverter
    ''' <summary>
    ''' Converts alphanumeric invoice/order numbers to 13-digit numeric barcodes and back
    ''' Example: "O-OD200-000033" <-> "2000000000033"
    ''' </summary>
    
    ' Branch code mapping (2 digits)
    Private Shared ReadOnly BranchCodes As New Dictionary(Of String, String) From {
        {"JHB", "01"}, {"CPT", "02"}, {"DBN", "03"}, {"PE", "04"},
        {"BFN", "05"}, {"PLK", "06"}, {"NLS", "07"}, {"KIM", "08"},
        {"OD200", "20"}, {"OD201", "21"}, {"OD202", "22"}
    }
    
    ' Reverse mapping
    Private Shared ReadOnly ReverseBranchCodes As Dictionary(Of String, String) = 
        BranchCodes.ToDictionary(Function(x) x.Value, Function(x) x.Key)
    
    ' Document type codes (1 digit)
    Private Shared ReadOnly DocTypeCodes As New Dictionary(Of String, String) From {
        {"INV", "1"}, {"O", "2"}, {"RET", "3"}, {"REF", "4"}
    }
    
    Private Shared ReadOnly ReverseDocTypeCodes As Dictionary(Of String, String) = 
        DocTypeCodes.ToDictionary(Function(x) x.Value, Function(x) x.Key)
    
    ''' <summary>
    ''' Convert invoice/order number to 13-digit barcode
    ''' Format: [DocType:1][Branch:2][Sequence:10]
    ''' Example: "O-OD200-000033" -> "2200000000033"
    ''' </summary>
    Public Shared Function ToBarcode(invoiceNumber As String) As String
        Try
            ' Parse invoice number: O-OD200-000033 or INV-JHB-00123
            Dim parts = invoiceNumber.Split("-"c)
            If parts.Length < 3 Then Return invoiceNumber.PadLeft(13, "0"c)
            
            Dim docType = parts(0).Trim().ToUpper()
            Dim branch = parts(1).Trim().ToUpper()
            Dim sequence = parts(2).Trim()
            
            ' Get codes
            Dim docCode = If(DocTypeCodes.ContainsKey(docType), DocTypeCodes(docType), "9")
            Dim branchCode = If(BranchCodes.ContainsKey(branch), BranchCodes(branch), "99")
            
            ' Extract numeric sequence (remove leading zeros)
            Dim numericSequence = Regex.Replace(sequence, "[^0-9]", "")
            
            ' Build 13-digit barcode: [DocType:1][Branch:2][Sequence:10]
            Dim barcode = docCode & branchCode & numericSequence.PadLeft(10, "0"c)
            
            ' Ensure exactly 13 digits
            If barcode.Length > 13 Then
                barcode = barcode.Substring(0, 13)
            ElseIf barcode.Length < 13 Then
                barcode = barcode.PadLeft(13, "0"c)
            End If
            
            Return barcode
            
        Catch ex As Exception
            ' Fallback: just use numeric portion
            Dim numeric = Regex.Replace(invoiceNumber, "[^0-9]", "")
            Return numeric.PadLeft(13, "0"c)
        End Try
    End Function
    
    ''' <summary>
    ''' Convert 13-digit barcode back to invoice/order number
    ''' Example: "2200000000033" -> "O-OD200-000033"
    ''' </summary>
    Public Shared Function FromBarcode(barcode As String) As String
        Try
            If barcode.Length <> 13 Then Return barcode
            
            ' Parse barcode: [DocType:1][Branch:2][Sequence:10]
            Dim docCode = barcode.Substring(0, 1)
            Dim branchCode = barcode.Substring(1, 2)
            Dim sequence = barcode.Substring(3, 10)
            
            ' Lookup codes
            Dim docType = If(ReverseDocTypeCodes.ContainsKey(docCode), ReverseDocTypeCodes(docCode), "UNK")
            Dim branch = If(ReverseBranchCodes.ContainsKey(branchCode), ReverseBranchCodes(branchCode), "XX")
            
            ' Remove leading zeros from sequence but keep at least 1 digit
            Dim seqNum = Convert.ToInt64(sequence).ToString().PadLeft(6, "0"c)
            
            ' Rebuild invoice number
            Return $"{docType}-{branch}-{seqNum}"
            
        Catch ex As Exception
            Return barcode
        End Try
    End Function
    
    ''' <summary>
    ''' Add a new branch code mapping
    ''' </summary>
    Public Shared Sub AddBranchCode(branchName As String, code As String)
        If Not BranchCodes.ContainsKey(branchName) Then
            BranchCodes.Add(branchName, code)
            ReverseBranchCodes.Add(code, branchName)
        End If
    End Sub
End Class
