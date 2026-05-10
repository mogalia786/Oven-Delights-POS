Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Configuration

''' <summary>
''' Paypoint MiniPOS Cloud Gateway Integration Service
''' Handles OAuth2 authentication and transaction processing
''' </summary>
Public Class PaypointPaymentService
    Private ReadOnly _baseUrl As String
    Private ReadOnly _apiKey As String
    Private ReadOnly _clientId As String
    Private ReadOnly _clientSecret As String
    Private _accessToken As String
    Private _tokenExpiry As DateTime
    Private ReadOnly _httpClient As HttpClient

    Public Sub New(apiKey As String, clientId As String, clientSecret As String, Optional isTestMode As Boolean = True)
        _apiKey = apiKey
        _clientId = clientId
        _clientSecret = clientSecret
        _baseUrl = If(isTestMode, "https://test.figment.co.za:49410/api", "https://prod.figment.co.za/api")
        
        _httpClient = New HttpClient()
        _httpClient.DefaultRequestHeaders.Add("apiKey", _apiKey)
        _httpClient.Timeout = TimeSpan.FromSeconds(120) ' 2 minute timeout for payment processing
    End Sub

    ''' <summary>
    ''' Get or refresh OAuth2 access token
    ''' </summary>
    Private Async Function GetAccessTokenAsync() As Task(Of String)
        ' Check if token is still valid (with 5 minute buffer)
        If Not String.IsNullOrEmpty(_accessToken) AndAlso DateTime.Now < _tokenExpiry.AddMinutes(-5) Then
            Return _accessToken
        End If

        ' Request new token
        Dim tokenRequest = New With {
            .client_id = _clientId,
            .client_secret = _clientSecret
        }

        Dim content = New StringContent(JsonConvert.SerializeObject(tokenRequest), Encoding.UTF8, "application/json")
        Dim response = Await _httpClient.PostAsync($"{_baseUrl}/oauth2/token", content)

        If response.IsSuccessStatusCode Then
            Dim responseBody = Await response.Content.ReadAsStringAsync()
            Dim tokenResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)
            
            _accessToken = tokenResponse("access_token").ToString()
            Dim expiresIn = CInt(tokenResponse("expires_in"))
            _tokenExpiry = DateTime.Now.AddSeconds(expiresIn)
            
            Return _accessToken
        Else
            Dim errorBody = Await response.Content.ReadAsStringAsync()
            Throw New Exception($"Failed to get access token: {response.StatusCode} - {errorBody}")
        End If
    End Function

    ''' <summary>
    ''' Start a new payment transaction
    ''' </summary>
    Public Async Function ProcessPaymentAsync(amount As Decimal, reference As String, Optional receiptData As Dictionary(Of String, String) = Nothing) As Task(Of PaymentResult)
        Try
            ' Get valid access token
            Dim token = Await GetAccessTokenAsync()

            ' Build transaction request
            Dim transactionRequest = New With {
                .amount = amount,
                .reference = reference,
                .receiptData = receiptData
            }

            ' Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

            ' Send transaction request
            Dim content = New StringContent(JsonConvert.SerializeObject(transactionRequest), Encoding.UTF8, "application/json")
            Dim response = Await _httpClient.PostAsync($"{_baseUrl}/transactions/transaction", content)

            Dim responseBody = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Dim transactionResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)
                
                Return New PaymentResult With {
                    .IsSuccess = True,
                    .TransactionId = transactionResponse("transactionId")?.ToString(),
                    .Status = transactionResponse("status")?.ToString(),
                    .Message = "Payment processed successfully",
                    .RawResponse = responseBody
                }
            Else
                Dim errorResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = errorResponse("error")?.ToString() ?? "Payment failed",
                    .RawResponse = responseBody
                }
            End If

        Catch ex As Exception
            Return New PaymentResult With {
                .IsSuccess = False,
                .Message = $"Payment error: {ex.Message}",
                .ErrorType = PaymentErrorType.SystemError
            }
        End Try
    End Function

    ''' <summary>
    ''' Resume a timed out transaction
    ''' </summary>
    Public Async Function ResumeTransactionAsync(transactionId As String) As Task(Of PaymentResult)
        Try
            Dim token = Await GetAccessTokenAsync()
            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

            Dim resumeRequest = New With {.transactionId = transactionId}
            Dim content = New StringContent(JsonConvert.SerializeObject(resumeRequest), Encoding.UTF8, "application/json")
            Dim response = Await _httpClient.PostAsync($"{_baseUrl}/transactions/resumeTransaction", content)

            Dim responseBody = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Dim transactionResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)
                Return New PaymentResult With {
                    .IsSuccess = True,
                    .TransactionId = transactionId,
                    .Status = transactionResponse("status")?.ToString(),
                    .Message = "Transaction resumed successfully",
                    .RawResponse = responseBody
                }
            Else
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = "Failed to resume transaction",
                    .RawResponse = responseBody
                }
            End If

        Catch ex As Exception
            Return New PaymentResult With {
                .IsSuccess = False,
                .Message = $"Resume error: {ex.Message}",
                .ErrorType = PaymentErrorType.SystemError
            }
        End Try
    End Function

    ''' <summary>
    ''' Get transaction status
    ''' </summary>
    Public Async Function GetTransactionStatusAsync(transactionId As String) As Task(Of PaymentResult)
        Try
            Dim token = Await GetAccessTokenAsync()
            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

            Dim response = Await _httpClient.GetAsync($"{_baseUrl}/transactions/status?transactionId={transactionId}")
            Dim responseBody = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Dim statusResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)
                Return New PaymentResult With {
                    .IsSuccess = True,
                    .TransactionId = transactionId,
                    .Status = statusResponse("status")?.ToString(),
                    .RawResponse = responseBody
                }
            Else
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = "Failed to get transaction status",
                    .RawResponse = responseBody
                }
            End If

        Catch ex As Exception
            Return New PaymentResult With {
                .IsSuccess = False,
                .Message = $"Status check error: {ex.Message}",
                .ErrorType = PaymentErrorType.SystemError
            }
        End Try
    End Function

    ''' <summary>
    ''' Cancel a pending transaction
    ''' </summary>
    Public Async Function CancelTransactionAsync(transactionId As String) As Task(Of PaymentResult)
        Try
            Dim token = Await GetAccessTokenAsync()
            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

            Dim response = Await _httpClient.DeleteAsync($"{_baseUrl}/transactions/cancel?transactionId={transactionId}")
            Dim responseBody = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Return New PaymentResult With {
                    .IsSuccess = True,
                    .TransactionId = transactionId,
                    .Message = "Transaction cancelled successfully",
                    .RawResponse = responseBody
                }
            Else
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = "Failed to cancel transaction",
                    .RawResponse = responseBody
                }
            End If

        Catch ex As Exception
            Return New PaymentResult With {
                .IsSuccess = False,
                .Message = $"Cancel error: {ex.Message}",
                .ErrorType = PaymentErrorType.SystemError
            }
        End Try
    End Function

    ''' <summary>
    ''' Reprint transaction receipt
    ''' </summary>
    Public Async Function ReprintReceiptAsync(transactionId As String) As Task(Of PaymentResult)
        Try
            Dim token = Await GetAccessTokenAsync()
            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

            Dim reprintRequest = New With {.transactionId = transactionId}
            Dim content = New StringContent(JsonConvert.SerializeObject(reprintRequest), Encoding.UTF8, "application/json")
            Dim response = Await _httpClient.PostAsync($"{_baseUrl}/transactions/reprintReceipt", content)

            Dim responseBody = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Return New PaymentResult With {
                    .IsSuccess = True,
                    .TransactionId = transactionId,
                    .Message = "Receipt reprinted successfully",
                    .RawResponse = responseBody
                }
            Else
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = "Failed to reprint receipt",
                    .RawResponse = responseBody
                }
            End If

        Catch ex As Exception
            Return New PaymentResult With {
                .IsSuccess = False,
                .Message = $"Reprint error: {ex.Message}",
                .ErrorType = PaymentErrorType.SystemError
            }
        End Try
    End Function

    ''' <summary>
    ''' Find transactions from database
    ''' </summary>
    Public Async Function FindTransactionAsync(searchCriteria As String) As Task(Of List(Of PaymentResult))
        Try
            Dim token = Await GetAccessTokenAsync()
            _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

            Dim response = Await _httpClient.GetAsync($"{_baseUrl}/transactions/findTransaction?criteria={searchCriteria}")
            Dim responseBody = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Dim transactions = JsonConvert.DeserializeObject(Of JArray)(responseBody)
                Dim results As New List(Of PaymentResult)

                For Each txn As JObject In transactions
                    results.Add(New PaymentResult With {
                        .IsSuccess = True,
                        .TransactionId = txn("transactionId")?.ToString(),
                        .Status = txn("status")?.ToString(),
                        .RawResponse = txn.ToString()
                    })
                Next

                Return results
            Else
                Return New List(Of PaymentResult)
            End If

        Catch ex As Exception
            Return New List(Of PaymentResult)
        End Try
    End Function

    ''' <summary>
    ''' Check API health status
    ''' </summary>
    Public Async Function CheckAPIStatusAsync() As Task(Of Boolean)
        Try
            Dim response = Await _httpClient.GetAsync($"{_baseUrl}/status")
            Return response.IsSuccessStatusCode
        Catch
            Return False
        End Try
    End Function

    Public Sub Dispose()
        _httpClient?.Dispose()
    End Sub
End Class

''' <summary>
''' Payment result model
''' </summary>
Public Class PaymentResult
    Public Property IsSuccess As Boolean
    Public Property TransactionId As String
    Public Property Status As String
    Public Property Message As String
    Public Property AuthCode As String
    Public Property CardType As String
    Public Property CardLastFour As String
    Public Property ErrorType As PaymentErrorType?
    Public Property RawResponse As String
End Class

''' <summary>
''' Payment error types
''' </summary>
Public Enum PaymentErrorType
    NetworkTimeout
    PaymentDeclined
    InsufficientFunds
    InvalidCard
    SystemError
    DuplicateTransaction
    TransactionCancelled
End Enum
