Public Class PriceOverride
    Public Property NewPrice As Decimal
    Public Property SupervisorUsername As String
    Public Property OverrideDate As DateTime
    
    Public Sub New()
        OverrideDate = DateTime.Now
    End Sub
End Class
