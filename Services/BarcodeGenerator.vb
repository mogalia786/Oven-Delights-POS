Imports System.Drawing
Imports System.Drawing.Imaging

Public Class BarcodeGenerator
    ''' <summary>
    ''' Generates EAN-13 barcode for order numbers (same format as product barcodes)
    ''' Example: "6000009" -> Padded to "0000006000009" -> EAN-13 barcode
    ''' </summary>
    Public Function GenerateBarcode(text As String) As Bitmap
        Return GenerateCode39Barcode(text, 180, 60)
    End Function
    
    Public Shared Function GenerateCode39Barcode(text As String, width As Integer, height As Integer) As Bitmap
        Try
            ' Code 39 encoding pattern (no font required - pure graphics)
            ' Each character is encoded as 9 bars (5 black, 4 white)
            ' Narrow bar = 1 unit, Wide bar = 3 units
            Dim code39 As New Dictionary(Of Char, String) From {
                {"0"c, "101001101101"}, {"1"c, "110100101011"}, {"2"c, "101100101011"},
                {"3"c, "110110010101"}, {"4"c, "101001101011"}, {"5"c, "110100110101"},
                {"6"c, "101100110101"}, {"7"c, "101001011011"}, {"8"c, "110100101101"},
                {"9"c, "101100101101"}, {"A"c, "110101001011"}, {"B"c, "101101001011"},
                {"C"c, "110110100101"}, {"D"c, "101011001011"}, {"E"c, "110101100101"},
                {"F"c, "101101100101"}, {"G"c, "101010011011"}, {"H"c, "110101001101"},
                {"I"c, "101101001101"}, {"J"c, "101011001101"}, {"K"c, "110101010011"},
                {"L"c, "101101010011"}, {"M"c, "110110101001"}, {"N"c, "101011010011"},
                {"O"c, "110101101001"}, {"P"c, "101101101001"}, {"Q"c, "101010110011"},
                {"R"c, "110101011001"}, {"S"c, "101101011001"}, {"T"c, "101011011001"},
                {"U"c, "110010101011"}, {"V"c, "100110101011"}, {"W"c, "110011010101"},
                {"X"c, "100101101011"}, {"Y"c, "110010110101"}, {"Z"c, "100110110101"},
                {"-"c, "100101011011"}, {"."c, "110010101101"}, {" "c, "100110101101"},
                {"*"c, "100101101101"}, {"$"c, "100100100101"}, {"/"c, "100100101001"},
                {"+"c, "100101001001"}, {"%"c, "101001001001"}
            }
            
            Dim bmp As New Bitmap(width, height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.White)
                g.SmoothingMode = Drawing2D.SmoothingMode.None
                
                ' Add start/stop character (*)
                Dim barcodeText As String = "*" & text.ToUpper() & "*"
                
                ' Calculate bar width (narrow bar)
                Dim totalUnits As Integer = 0
                For Each c As Char In barcodeText
                    If code39.ContainsKey(c) Then
                        totalUnits += 15 ' Each char = 12 units + 3 unit gap
                    End If
                Next
                
                Dim barWidth As Single = (width - 20) / totalUnits ' 10px margins
                If barWidth < 1 Then barWidth = 1
                
                Dim xPos As Single = 10 ' Start with left margin
                Dim barHeight As Single = height - 20 ' Leave space for text
                
                ' Draw each character
                For Each c As Char In barcodeText
                    If code39.ContainsKey(c) Then
                        Dim pattern As String = code39(c)
                        
                        ' Draw bars according to pattern
                        For i As Integer = 0 To pattern.Length - 1
                            Dim isBlack As Boolean = (i Mod 2 = 0)
                            Dim isWide As Boolean = (pattern(i) = "1"c)
                            Dim currentBarWidth As Single = If(isWide, barWidth * 3, barWidth)
                            
                            If isBlack Then
                                g.FillRectangle(Brushes.Black, xPos, 5, currentBarWidth, barHeight)
                            End If
                            
                            xPos += currentBarWidth
                        Next
                        
                        ' Add inter-character gap (narrow white bar)
                        xPos += barWidth
                    End If
                Next
                
                ' Draw human-readable text below
                Dim textFont As New Font("Arial", 8, FontStyle.Regular)
                Dim textSize = g.MeasureString(text, textFont)
                g.DrawString(text, textFont, Brushes.Black, (width - textSize.Width) / 2, height - 14)
            End Using
            
            Return bmp
        Catch ex As Exception
            ' Fallback: return error image
            Dim bmp As New Bitmap(width, height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.White)
                Dim errorFont As New Font("Arial", 10, FontStyle.Bold)
                g.DrawString($"Barcode Error: {text}", errorFont, Brushes.Black, 5, height / 2)
            End Using
            Return bmp
        End Try
    End Function
End Class
