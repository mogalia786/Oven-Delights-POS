' Decompiled from version 1.0.0.37 - Restored live FNB credentials
' Paypoint MiniPOS Cloud Gateway Integration Service
' Handles OAuth2 authentication and transaction processing

Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading.Tasks
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Namespace Overn_Delights_POS
    Public Class PaypointPaymentService
        Private ReadOnly _baseUrl As String
        Private ReadOnly _apiKey As String
        Private ReadOnly _clientId As String
        Private ReadOnly _clientSecret As String
        Private _accessToken As String
        Private _tokenExpiry As DateTime
        Private ReadOnly _httpClient As HttpClient

        Public Sub New(
            apiKey As String,
            clientId As String,
            clientSecret As String,
            Optional isTestMode As Boolean = True
        )
            _apiKey = apiKey
            _clientId = clientId
            _clientSecret = clientSecret
            _baseUrl = If(isTestMode, "https://test.figment.co.za:49410/api", "https://miniposfnb.co.za:49410/api")
            _httpClient = New HttpClient()
            _httpClient.DefaultRequestHeaders.Add(NameOf(apiKey), _apiKey)
            _httpClient.Timeout = TimeSpan.FromSeconds(120)
        End Sub

        ''' <summary>
        ''' Get or refresh OAuth2 access token
        ''' </summary>
        Private Async Function GetAccessTokenAsync() As Task(Of String)
            If Not String.IsNullOrEmpty(_accessToken) AndAlso DateTime.Compare(DateTime.Now, _tokenExpiry.AddMinutes(-5)) < 0 Then
                Return _accessToken
            End If

            Dim data = New With {
                .client_id = _clientId,
                .client_secret = _clientSecret
            }

            Dim response = Await _httpClient.PostAsync($"{_baseUrl}/oauth2/token", 
                New StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"))

            If response.IsSuccessStatusCode Then
                Dim responseObject = JsonConvert.DeserializeObject(Of JObject)(Await response.Content.ReadAsStringAsync())
                _accessToken = responseObject("access_token").ToString()
                _tokenExpiry = DateTime.Now.AddSeconds(CInt(responseObject("expires_in")))
                Return _accessToken
            End If

            Dim errorResponse = Await response.Content.ReadAsStringAsync()
            Throw New Exception($"Failed to get access token: {response.StatusCode} - {errorResponse}")
        End Function

        ''' <summary>
        ''' Start a new payment transaction
        ''' </summary>
        Public Async Function ProcessPaymentAsync(
            amount As Decimal,
            reference As String,
            Optional cartItems As DataTable = Nothing
        ) As Task(Of PaymentResult)
            Try
                Dim token = Await GetAccessTokenAsync()
                Dim amountInCents = Convert.ToInt32(amount * 100D)
                Dim productItems As New List(Of Object)()

                If cartItems IsNot Nothing AndAlso cartItems.Rows.Count > 0 Then
                    Dim itemNumber As Integer = 1
                    For Each row As DataRow In cartItems.Rows
                        Dim itemPrice As Decimal = If(IsDBNull(row("Price")), 0D, CDec(row("Price")))
                        Dim itemQty As Integer = If(IsDBNull(row("Qty")), 1, CInt(CDec(row("Qty")))
                        Dim itemTotal As Decimal = itemPrice * itemQty
                        Dim itemName As String = If(IsDBNull(row("Product")), "Item", row("Product").ToString())

                        productItems.Add(New With {
                            .itemId = itemNumber,
                            .category = 255,
                            .amount = CInt(itemTotal * 100),
                            .description = itemName.Substring(0, Math.Min(20, itemName.Length)),
                            .quantity = itemQty,
                            .unitPrice = CInt(itemPrice * 100)
                        })

                        itemNumber += 1
                    Next
                Else
                    productItems.Add(New With {
                        .itemId = 1,
                        .category = 255,
                        .amount = amountInCents,
                        .description = "Sale",
                        .quantity = 1,
                        .unitPrice = amountInCents
                    })
                End If

                Dim reconIndicator As String = $"1{DateTime.Now:HHmmss}"
                reconIndicator = reconIndicator.Substring(0, Math.Min(7, reconIndicator.Length))

                Dim transactionRequest = New With {
                    .requestType = "Settlement",
                    .reconIndicator = reconIndicator,
                    .supervisor = New String() {"S"},
                    .posIdentifier = 1,
                    .posVersion = "1.0.0",
                    .siteId = "RT08",
                    .totalAmount = amountInCents,
                    .productItems = productItems
                }

                Dim content = New StringContent(JsonConvert.SerializeObject(transactionRequest), Encoding.UTF8, "application/json")
                _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

                Dim response = Await _httpClient.PostAsync($"{_baseUrl}/transactions", content)

                If response.IsSuccessStatusCode Then
                    Dim responseBody = Await response.Content.ReadAsStringAsync()
                    Dim transactionResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)

                    Dim maskedPan As String = Nothing
                    Dim cardType As String = Nothing
                    Dim approvalCode As String = Nothing

                    If transactionResponse("transactions") IsNot Nothing Then
                        Dim transactions = transactionResponse("transactions")
                        If transactions IsNot Nothing AndAlso transactions.Type = JTokenType.Array Then
                            Dim firstTxn = transactions(0)
                            maskedPan = If(firstTxn("pan") IsNot Nothing, firstTxn("pan").ToString(), Nothing)
                            cardType = If(firstTxn("cardType") IsNot Nothing, firstTxn("cardType").ToString(), Nothing)
                            approvalCode = If(firstTxn("approvalCode") IsNot Nothing, firstTxn("approvalCode").ToString(), Nothing)
                        End If
                    End If

                    Return New PaymentResult With {
                        .IsSuccess = True,
                        .TransactionId = If(transactionResponse("transactionId") IsNot Nothing, transactionResponse("transactionId").ToString(), Nothing),
                        .Status = If(transactionResponse("status") IsNot Nothing, transactionResponse("status").ToString(), Nothing),
                        .Message = "Payment successful",
                        .AuthCode = approvalCode,
                        .CardType = cardType,
                        .CardLastFour = maskedPan,
                        .Amount = amount,
                        .RawResponse = responseBody
                    }
                Else
                    Dim errorResponse = Await response.Content.ReadAsStringAsync()
                    Dim errorObj = JsonConvert.DeserializeObject(Of JObject)(errorResponse)
                    Return New PaymentResult With {
                        .IsSuccess = False,
                        .Message = If(errorObj("error") IsNot Nothing, errorObj("error").ToString(), "Payment failed"),
                        .RawResponse = errorResponse
                    }
                End If

            Catch ex As Exception
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = ex.Message,
                    .RawResponse = ex.ToString()
                }
            End Try
        End Function

        ''' <summary>
        ''' Process a refund transaction
        ''' </summary>
        Public Async Function ProcessRefundAsync(
            amount As Decimal,
            reference As String,
            Optional receiptData As Dictionary(Of String, String) = Nothing
        ) As Task(Of PaymentResult)
            Try
                Dim token = Await GetAccessTokenAsync()
                Dim amountInCents = Convert.ToInt32(amount * 100D)

                Dim productItems As New List(Of Object)()
                productItems.Add(New With {
                    .itemId = 1,
                    .category = 255,
                    .amount = amountInCents,
                    .description = "Refund",
                    .quantity = 1,
                    .unitPrice = amountInCents
                })

                Dim reconIndicator As String = $"2{DateTime.Now:HHmmss}"
                reconIndicator = reconIndicator.Substring(0, Math.Min(7, reconIndicator.Length))

                Dim transactionRequest = New With {
                    .requestType = "Refund",
                    .reconIndicator = reconIndicator,
                    .supervisor = New String() {"R"},
                    .posIdentifier = 1,
                    .posVersion = "1.0.0",
                    .siteId = "RT08",
                    .totalAmount = amountInCents,
                    .productItems = productItems
                }

                Dim content = New StringContent(JsonConvert.SerializeObject(transactionRequest), Encoding.UTF8, "application/json")
                _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

                Dim response = Await _httpClient.PostAsync($"{_baseUrl}/transactions", content)

                If response.IsSuccessStatusCode Then
                    Dim responseBody = Await response.Content.ReadAsStringAsync()
                    Dim transactionResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)

                    Return New PaymentResult With {
                        .IsSuccess = True,
                        .TransactionId = If(transactionResponse("transactionId") IsNot Nothing, transactionResponse("transactionId").ToString(), Nothing),
                        .Status = If(transactionResponse("status") IsNot Nothing, transactionResponse("status").ToString(), Nothing),
                        .Message = "Refund successful",
                        .Amount = amount,
                        .RawResponse = responseBody
                    }
                Else
                    Dim errorResponse = Await response.Content.ReadAsStringAsync()
                    Dim errorObj = JsonConvert.DeserializeObject(Of JObject)(errorResponse)
                    Return New PaymentResult With {
                        .IsSuccess = False,
                        .Message = If(errorObj("error") IsNot Nothing, errorObj("error").ToString(), "Refund failed"),
                        .RawResponse = errorResponse
                    }
                End If

            Catch ex As Exception
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = ex.Message,
                    .RawResponse = ex.ToString()
                }
            End Try
        End Function

        ''' <summary>
        ''' Resume a transaction (for timeout handling)
        ''' </summary>
        Public Async Function ResumeTransactionAsync(transactionId As String) As Task(Of PaymentResult)
            Try
                Dim token = Await GetAccessTokenAsync()

                Dim resumeRequest = New With {
                    .transactionId = transactionId
                }

                Dim content = New StringContent(JsonConvert.SerializeObject(resumeRequest), Encoding.UTF8, "application/json")
                _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

                Dim response = Await _httpClient.PostAsync($"{_baseUrl}/transactions/resume", content)

                If response.IsSuccessStatusCode Then
                    Dim responseBody = Await response.Content.ReadAsStringAsync()
                    Dim transactionResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)

                    Return New PaymentResult With {
                        .IsSuccess = True,
                        .TransactionId = transactionId,
                        .Status = If(transactionResponse("status") IsNot Nothing, transactionResponse("status").ToString(), Nothing),
                        .Message = "Transaction resumed successfully",
                        .RawResponse = responseBody
                    }
                Else
                    Dim errorResponse = Await response.Content.ReadAsStringAsync()
                    Dim errorObj = JsonConvert.DeserializeObject(Of JObject)(errorResponse)
                    Return New PaymentResult With {
                        .IsSuccess = False,
                        .Message = If(errorObj("error") IsNot Nothing, errorObj("error").ToString(), "Resume failed"),
                        .RawResponse = errorResponse
                    }
                End If

            Catch ex As Exception
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = ex.Message,
                    .RawResponse = ex.ToString()
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

                Dim response = Await _httpClient.GetAsync($"{_baseUrl}/transactions/{transactionId}")

                If response.IsSuccessStatusCode Then
                    Dim responseBody = Await response.Content.ReadAsStringAsync()
                    Dim statusResponse = JsonConvert.DeserializeObject(Of JObject)(responseBody)

                    Return New PaymentResult With {
                        .IsSuccess = True,
                        .TransactionId = transactionId,
                        .Status = If(statusResponse("status") IsNot Nothing, statusResponse("status").ToString(), Nothing),
                        .Message = "Status retrieved successfully",
                        .RawResponse = responseBody
                    }
                Else
                    Dim errorResponse = Await response.Content.ReadAsStringAsync()
                    Dim errorObj = JsonConvert.DeserializeObject(Of JObject)(errorResponse)
                    Return New PaymentResult With {
                        .IsSuccess = False,
                        .Message = If(errorObj("error") IsNot Nothing, errorObj("error").ToString(), "Status check failed"),
                        .RawResponse = errorResponse
                    }
                End If

            Catch ex As Exception
                Return New PaymentResult With {
                    .IsSuccess = False,
                    .Message = ex.Message,
                    .RawResponse = ex.ToString()
                }
            End Try
        End Function

        ''' <summary>
        ''' Find transactions by criteria
        ''' </summary>
        Public Async Function FindTransactionsAsync(criteria As Dictionary(Of String, String)) As Task(Of List(Of PaymentResult))
            Try
                Dim token = Await GetAccessTokenAsync()
                _httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", token)

                Dim queryString = String.Join("&", criteria.Select(Function(kvp) $"{kvp.Key}={kvp.Value}"))
                Dim response = Await _httpClient.GetAsync($"{_baseUrl}/transactions/search?{queryString}")

                If response.IsSuccessStatusCode Then
                    Dim responseBody = Await response.Content.ReadAsStringAsync()
                    Dim transactions = JsonConvert.DeserializeObject(Of JArray)(responseBody)
                    Dim results As New List(Of PaymentResult)()

                    For Each txn As JToken In transactions
                        Dim txnObj = txn As JObject
                        results.Add(New PaymentResult With {
                            .IsSuccess = True,
                            .TransactionId = If(txnObj("transactionId") IsNot Nothing, txnObj("transactionId").ToString(), Nothing),
                            .Status = If(txnObj("status") IsNot Nothing, txnObj("status").ToString(), Nothing),
                            .Message = "Transaction found",
                            .RawResponse = txn.ToString()
                        })
                    Next

                    Return results
                Else
                    Return New List(Of PaymentResult)()
                End If

            Catch ex As Exception
                Return New List(Of PaymentResult)()
            End Try
        End Function

        ''' <summary>
        ''' Dispose of HTTP client
        ''' </summary>
        Public Sub Dispose()
            If _httpClient IsNot Nothing Then _httpClient.Dispose()
        End Sub
    End Class

    Public Class PaymentResult
        Public Property IsSuccess As Boolean
        Public Property TransactionId As String
        Public Property Status As String
        Public Property Message As String
        Public Property AuthCode As String
        Public Property CardType As String
        Public Property CardLastFour As String
        Public Property Amount As Decimal
        Public Property ErrorType As PaymentErrorType?
        Public Property RawResponse As String
    End Class

    Public Enum PaymentErrorType
        NetworkError
        InvalidCredentials
        InsufficientFunds
        CardDeclined
        TimeoutError
        ServerError
        UnknownError
    End Enum
End Namespace
