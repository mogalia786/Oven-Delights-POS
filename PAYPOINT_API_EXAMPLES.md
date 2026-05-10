# Paypoint API Examples

## Credit Card Transaction Request

### API Endpoint
- **Live**: `https://api.paypoint.co.za/api/v1/transactions`
- **Test**: `https://test-api.paypoint.co.za/api/v1/transactions`

### Request Headers
```http
POST /api/v1/transactions HTTP/1.1
Host: api.paypoint.co.za
Content-Type: application/json
Authorization: Bearer YOUR_API_KEY
```

### Request Body Example
```json
{
  "merchantId": "LIVE_MERCHANT_ID",
  "siteId": "LIVE_SITE_ID", 
  "amount": 25000,
  "currency": "ZAR",
  "cardNumber": "5123456789012345",
  "cardExpiry": "2025-12",
  "cardHolder": "JOHN DOE",
  "reference": "CAKE00120250508123456",
  "description": "POS Sale - CAKE001 - 2025-05-08 12:34:56"
}
```

### Response Examples

#### ✅ Successful Transaction
```json
{
  "success": true,
  "transactionId": "TXN123456789",
  "approvalCode": "APPROVED",
  "authCode": "123456",
  "responseCode": "00",
  "message": "Transaction approved successfully"
}
```

#### ❌ Failed Transaction
```json
{
  "success": false,
  "transactionId": null,
  "approvalCode": "DECLINED",
  "authCode": null,
  "responseCode": "05",
  "message": "Insufficient funds",
  "error": {
    "code": "05",
    "description": "Insufficient funds in account"
  }
}
```

#### ⏰ Timeout Response
```json
{
  "success": false,
  "transactionId": null,
  "approvalCode": "TIMEOUT",
  "authCode": null,
  "responseCode": "68",
  "message": "Transaction timed out"
}
```

## VB.NET Implementation Example

### WebClient Request
```vb
Dim paymentRequest As New With {
    .merchantId = _paypointMerchantId,
    .siteId = _paypointSiteId,
    .amount = _cardAmount * 100,
    .currency = "ZAR",
    .cardNumber = _cardMaskedPan,
    .cardExpiry = _cardExpiry,
    .cardHolder = "POS Customer",
    .reference = _branchPrefix & Now.ToString("yyyyMMddHHmmss"),
    .description = "POS Sale - " & _branchPrefix & Now.ToString("yyyy-MM-dd HH:mm:ss")
}

Dim jsonRequest As String = JsonConvert.SerializeObject(paymentRequest)

Using client As New WebClient()
    client.Headers.Add("Content-Type", "application/json")
    client.Headers.Add("Authorization", "Bearer " & _paypointApiKey)
    
    Dim responseBytes As Byte() = client.UploadData(jsonRequest, apiUrl)
    Dim responseJson As String = System.Text.Encoding.UTF8.GetString(responseBytes)
    
    Dim response = JsonConvert.DeserializeObject(responseJson)
```

### Response Handling
```vb
If response("success") Then
    ' Payment successful
    _cardApprovalCode = response("approvalCode").ToString()
    _transactionId = response("transactionId").ToString()
    
    MessageBox.Show("✅ PAYMENT APPROVED" & vbCrLf & 
                 "Amount: R" & _cardAmount.ToString("N2") & vbCrLf &
                 "Auth Code: " & _cardApprovalCode,
                 "Payment Successful")
Else
    ' Payment failed
    Dim errorMessage As String = response("message").ToString()
    MessageBox.Show("❌ PAYMENT FAILED" & vbCrLf & 
                 "Error: " & errorMessage,
                 "Payment Error")
End If
```

## Error Codes Reference

| Code | Description                     | Action Required              |
|------|--------------------------------|---------------------------|
| 01   | Invalid card number              | Re-enter card details    |
| 02   | Expired card                   | Use different card      |
| 03   | Insufficient funds              | Use cash or retry       |
| 04   | Stolen card                   | Keep card, call security |
| 05   | Do not honor                  | Use different card      |
| 06   | Error                        | Retry transaction       |
| 07   | Pick up card                   | Keep card, call bank   |
| 08   | Honor only identification     | Verify with bank       |
| 14   | Invalid card number              | Re-enter card details    |
| 15   | No such issuer                | Use different card      |

## Security Requirements

### Card Data Masking
- Show only: `****-****-****-1234`
- Never store full card numbers in memory or logs
- Clear card data immediately after processing
- Use HTTPS for all API communications

### Timeout Configuration
- **Transaction timeout**: 30 seconds maximum
- **Connection timeout**: 5 seconds maximum
- **Retry logic**: Up to 3 retry attempts with exponential backoff
- **Long transaction handling**: Return to tender selection screen

## Integration Notes

### Authentication
- Use Bearer token authentication
- API key must be kept secure and never exposed in client-side code
- Rotate API keys regularly in production environment

### Receipt Integration
- Include last 4 digits of card number: `****-1234`
- Show authorization code if available: `Auth: 123456`
- Display payment method: "CREDIT CARD"
- Include transaction ID for reference: `TXN123456789`

### Testing

#### Test Card Numbers
```vb
' For testing only - remove in production!
_testCards = {
    {"4242424242424242", "5123456789012345", "6011111111111116"}
}
```
