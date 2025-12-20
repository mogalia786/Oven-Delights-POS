Imports System.Drawing
Imports System.Drawing.Imaging

Public Class BarcodeGenerator
    ''' <summary>
    ''' Generates Code 39 barcode using font (simple and reliable)
    ''' </summary>
    Public Function GenerateBarcode(text As String) As Bitmap
        Return GenerateCode39Barcode(text, 180, 60)
    End Function
    
    Public Shared Function GenerateCode39Barcode(text As String, width As Integer, height As Integer) As Bitmap
        Try
            Dim bmp As New Bitmap(width, height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.White)
                g.TextRenderingHint = Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit
                
                ' Code 39 requires start/stop character (*)
                Dim barcodeText As String = "*" & text.ToUpper() & "*"
                
                ' Try multiple Code 39 barcode fonts (common free fonts)
                Dim barcodeFonts() As String = {
                    "Free 3 of 9",           ' Most common free Code 39 font
                    "Free 3 of 9 Extended",
                    "IDAutomationC39",
                    "Code 39",
                    "3 of 9 Barcode"
                }
                
                Dim barcodeFont As Font = Nothing
                Dim fontSize As Single = 36 ' Start with large size
                
                ' Try to find an installed barcode font
                For Each fontName In barcodeFonts
                    Try
                        barcodeFont = New Font(fontName, fontSize, FontStyle.Regular)
                        If barcodeFont.Name = fontName Then
                            ' Font found and loaded successfully
                            Exit For
                        End If
                    Catch
                        ' Font not available, try next
                        Continue For
                    End Try
                Next
                
                ' If no barcode font found, use Libre Barcode 39 or fall back to regular font
                If barcodeFont Is Nothing OrElse barcodeFont.Name <> barcodeFont.OriginalFontName Then
                    ' Try Libre Barcode 39 (Google Fonts)
                    Try
                        barcodeFont = New Font("Libre Barcode 39", fontSize, FontStyle.Regular)
                    Catch
                        ' Ultimate fallback - use monospace font with message
                        barcodeFont = New Font("Courier New", 10, FontStyle.Bold)
                        g.DrawString($"BARCODE FONT NOT INSTALLED", barcodeFont, Brushes.Red, 5, 5)
                        g.DrawString($"Install 'Free 3 of 9' font", New Font("Arial", 8), Brushes.Black, 5, 20)
                        g.DrawString($"Invoice: {text}", New Font("Courier New", 12, FontStyle.Bold), Brushes.Black, 5, 35)
                        Return bmp
                    End Try
                End If
                
                ' Measure and center the barcode
                Dim barcodeSize = g.MeasureString(barcodeText, barcodeFont)
                Dim xPos As Single = (width - barcodeSize.Width) / 2
                Dim yPos As Single = 5
                
                ' Draw barcode using font
                g.DrawString(barcodeText, barcodeFont, Brushes.Black, xPos, yPos)
                
                ' Draw human-readable text below barcode
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
