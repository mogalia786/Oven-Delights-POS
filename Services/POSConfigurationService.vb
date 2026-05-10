Imports System.IO
Imports Newtonsoft.Json

Public Class POSConfigurationService
    Private Shared _instance As POSConfigurationService
    Private Shared ReadOnly _lock As New Object()
    
    Private _config As POSConfiguration
    Private _configFilePath As String
    
    Public Shared ReadOnly Property Instance As POSConfigurationService
        Get
            If _instance Is Nothing Then
                SyncLock _lock
                    If _instance Is Nothing Then
                        _instance = New POSConfigurationService()
                    End If
                End SyncLock
            End If
            Return _instance
        End Get
    End Property
    
    Private Sub New()
        _configFilePath = Path.Combine(Application.StartupPath, "pos_config.json")
        LoadConfiguration()
    End Sub
    
    Public Sub LoadConfiguration()
        Try
            If File.Exists(_configFilePath) Then
                Dim json = File.ReadAllText(_configFilePath)
                _config = JsonConvert.DeserializeObject(Of POSConfiguration)(json)
            Else
                _config = New POSConfiguration()
                SaveConfiguration()
            End If
        Catch ex As Exception
            _config = New POSConfiguration()
        End Try
    End Sub
    
    Public Sub SaveConfiguration()
        Try
            Dim json = JsonConvert.SerializeObject(_config, Formatting.Indented)
            File.WriteAllText(_configFilePath, json)
        Catch ex As Exception
            ' Log error but continue
        End Try
    End Sub
    
    Public Function IsCreditCardEnabled() As Boolean
        Return _config.EnableCreditCardPayments
    End Function
    
    Public Sub SetCreditCardEnabled(enabled As Boolean)
        _config.EnableCreditCardPayments = enabled
        SaveConfiguration()
    End Sub
    
    Public Function IsTestMode() As Boolean
        Return _config.TestMode
    End Function
    
    Public Sub SetTestMode(testMode As Boolean)
        _config.TestMode = testMode
        SaveConfiguration()
    End Sub
    
    Public Function GetPaymentGatewayCredentials() As PaymentGatewayCredentials
        Return _config.PaymentGateway
    End Function
    
    Public Sub SetPaymentGatewayCredentials(credentials As PaymentGatewayCredentials)
        _config.PaymentGateway = credentials
        SaveConfiguration()
    End Sub
End Class

Public Class POSConfiguration
    Public Property EnableCreditCardPayments As Boolean = True
    Public Property TestMode As Boolean = False
    Public Property PaymentGateway As New PaymentGatewayCredentials
End Class

Public Class PaymentGatewayCredentials
    Public Property ApiKey As String = ""
    Public Property SecretKey As String = ""
    Public Property MerchantId As String = ""
    Public Property Environment As String = "production" ' production or test
End Class
