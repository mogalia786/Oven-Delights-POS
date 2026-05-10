# FNB Paypoint Payment Gateway Integration Guide

## Overview
This document contains all the necessary information for integrating FNB Paypoint payment gateway with the Oven Delights POS system.

## API Endpoints

### Production (Live)
- **Base URL**: `https://miniposfnb.co.za:49410`
- **Token Endpoint**: `/oauth2/token`
- **Transaction Endpoint**: `/transactions/transaction`
- **Status Endpoint**: `/status`

### Test Environment
- **Base URL**: `https://test.figment.co.za:49410`
- **Token Endpoint**: `/oauth2/token`
- **Transaction Endpoint**: `/transactions/transaction`
- **Status Endpoint**: `/status`

## Authentication

### 🔍 **Paypoint MiniPOS API - Card Payment Processing**

Both ERP and POS systems use the **same Paypoint MiniPOS API** for card payments:

#### **API Purpose**:
- Credit/Debit card processing via payment terminals
- Attended and unattended transaction modes
- Real-time payment authorization

#### **Authentication**:
- API Key + OAuth2 Client ID/Secret
- Token-based authentication with 30-minute expiry

### Live Credentials (Production) - From Decompiled EXE
```json
{
  "apiKey": "Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq",
  "clientId": "qEXGrBTnJQS9ZBX7bzuKnkHQfZ0UUFUX",
  "clientSecret": "j082ZT3cPyojxN9CSmdp41p7nXGLQ8zH",
  "siteId": "RT08"
}
```

### Test Credentials (From FNB) - ✅ CORRECT!
```json
{
  "apiKey": "Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq",
  "clientId": "MP7BQIe0TMxgxzhpGghkNF303zhmYnjA",
  "clientSecret": "Tf3ac4dLR9DGmBfwipmjy6tjUmLv6tma",
  "siteId": "UT02",
  "posIdentifier": 10
}
```

### 🔍 What I Found in Decompiled Code:
- **Line 916**: Live API key: `Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq`
- **Line 917**: Live Client ID: `qEXGrBTnJQS9ZBX7bzuKnkHQfZ0UUFUX`
- **Line 918**: Live Client Secret: `j082ZT3cPyojxN9CSmdp41p7nXGLQ8zH`
- **Line 156**: Hardcoded `siteId = "RT08"` in payment request

## Transaction Types

### 🏪 **Attended Transaction** (Cashier Present)
Used when cashier is present and supervisor override is available.

#### **Test Attended JSON** (From FNB Test System)
```json
{
  "requestType": "Settlement",
  "reconIndicator": "1141510",
  "supervisor": ["S"],
  "posIdentifier": 10,
  "posVersion": "1.0.0",
  "siteId": "UT02",
  "totalAmount": 185.00,
  "productItems": [
    {
      "itemId": 1,
      "category": 255,
      "amount": 10000,
      "description": "Cake Freshcream In D",
      "quantity": 1,
      "unitPrice": 10000
    }
  ]
}
```

#### **Live Attended JSON** (From Your Example)
```json
{
  "requestType": "Settlement",
  "reconIndicator": "1141510",
  "supervisor": ["S"],
  "posIdentifier": 1,
  "posVersion": "1.0.0",
  "siteId": "RT08",
  "totalAmount": 185.00,
  "productItems": [
    {
      "itemId": 1,
      "category": 255,
      "amount": 10000,
      "description": "Cake Freshcream In D",
      "quantity": 1,
      "unitPrice": 10000
    },
    {
      "itemId": 2,
      "category": 255,
      "amount": 8500,
      "description": "Carrot Cake With Cre",
      "quantity": 1,
      "unitPrice": 8500
    }
  ]
}
```

### 🤖 **Unattended Transaction** (Self-Service - TEST ONLY)
Used for self-service or automated transactions without supervisor.
**⚠️ UNATTENDED TRANSACTIONS ARE ONLY AVAILABLE IN TEST ENVIRONMENT**

#### **Test Unattended JSON** (From FNB Test System)
```json
{
  "requestType": "Settlement",
  "reconIndicator": "1141511",
  "supervisor": [],
  "posIdentifier": 10,
  "posVersion": "1.0.0",
  "siteId": "UT02",
  "totalAmount": 185.00,
  "productItems": [
    {
      "itemId": 1,
      "category": 255,
      "amount": 10000,
      "description": "Cake Freshcream In D",
      "quantity": 1,
      "unitPrice": 10000
    }
  ]
}
```

#### **Live Environment - NO UNATTENDED TRANSACTIONS**
❌ **Live production only supports attended transactions**
- All live transactions require supervisor presence
- `"supervisor": ["S"]` is mandatory for live environment
- No self-service option available in production

### 🔑 **Key Differences**:
- **Attended**: `"supervisor": ["S"]` - Requires supervisor override (both test & live)
- **Unattended**: `"supervisor": []` - No supervisor required (TEST ONLY)
- **Test vs Live**: Different siteId and posIdentifier values
- **reconIndicator**: Must be unique per transaction (max 7 chars)
- **⚠️ Critical**: Unattended mode only works in test environment

## Transaction Types

### 1. Attended Transaction
Used when cashier is present and supervisor override is available.

#### JSON Request Example
```json
{
  "requestType": "Settlement",
  "reconIndicator": "1141510",
  "supervisor": ["S"],
  "posIdentifier": 1,
  "posVersion": "1.0.0",
  "siteId": "RT08",
  "totalAmount": 185.00,
  "productItems": [
    {
      "itemId": 1,
      "category": 255,
      "amount": 10000,
      "description": "Cake Freshcream In D",
      "quantity": 1,
      "unitPrice": 10000
    },
    {
      "itemId": 2,
      "category": 255,
      "amount": 8500,
      "description": "Carrot Cake With Cre",
      "quantity": 1,
      "unitPrice": 8500
    }
  ]
}
```

#### Key Fields
- `supervisor`: `["S"]` - Supervisor override required
- `reconIndicator`: Unique identifier (max 7 chars)
- `totalAmount`: Decimal format with 2 decimal places
- `amount`: Integer cents (R100.00 = 10000)
- `unitPrice`: Integer cents for each item

### 2. Unattended Transaction
Used for self-service or automated transactions without supervisor.

#### JSON Request Example
```json
{
  "requestType": "Settlement",
  "reconIndicator": "1141511",
  "supervisor": [],
  "posIdentifier": 1,
  "posVersion": "1.0.0",
  "siteId": "RT08",
  "totalAmount": 185.00,
  "productItems": [
    {
      "itemId": 1,
      "category": 255,
      "amount": 10000,
      "description": "Cake Freshcream In D",
      "quantity": 1,
      "unitPrice": 10000
    }
  ]
}
```

#### Key Differences from Attended
- `supervisor`: `[]` - Empty array, no supervisor required
- `reconIndicator`: Must be different from attended transactions

## Response Formats

### Successful Transaction
```json
{
  "requestType": "Settlement",
  "resultCode": "0000",
  "resultSubCode": "001",
  "posIdentifier": 1,
  "reconIndicator": "1141510",
  "totalAmount": 185.00,
  "transactions": [
    {
      "date": "2025-12-17T00:26:17.387Z",
      "pan": "528497xxxxxx5593",
      "expiry": "1705",
      "cardType": "006",
      "sequence": "003165",
      "batch": "000076",
      "record": "001",
      "approvalCode": "176593",
      "flags": "60024001",
      "indicators": "221IH ",
      "uti": "99123001-0000-0000-0000-151223165901",
      "track2": ";528497xxxxxx75593=170520100000000000000?",
      "spdhSeqNo": "003165",
      "signature": "N",
      "hotcardSeq": "2000500"
    }
  ]
}
```

### Declined Transaction
```json
{
  "requestType": "Settlement",
  "resultCode": "0001",
  "resultSubCode": "002",
  "posIdentifier": 1,
  "reconIndicator": "1141510",
  "totalAmount": 185.00
}
```

## Error Codes

| Code | Description | Action Required |
|------|-------------|-----------------|
| 0000 | Success | Transaction approved |
| 0001 | Declined | Ask for different payment method |
| 0002 | Timeout | Retry transaction |
| 0003 | System Error | Contact support |
| 0004 | Invalid Amount | Check amount and retry |
| 0005 | Invalid Card | Ask for different card |

## POS Integration

### Configuration Files

#### Live Configuration (`App_Data/pos-config-live.json`)
```json
{
  "paymentGateway": {
    "mode": "live",
    "endpoints": {
      "paypoint": {
        "baseUrl": "https://miniposfnb.co.za:49410",
        "tokenEndpoint": "/oauth2/token",
        "transactionEndpoint": "/transactions/transaction"
      }
    },
    "credentials": {
      "paypoint": {
        "apiKey": "Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq",
        "clientId": "qEXGrBTnJQS9ZBX7bzuKnkHQfZ0UUFUX",
        "clientSecret": "j082ZT3cPyojxN9CSmdp41p7nXGLQ8zH",
        "siteId": "RT08"
      }
    }
  }
}
```

#### Test Configuration (`App_Data/pos-config-test.json`)
```json
{
  "paymentGateway": {
    "mode": "test",
    "endpoints": {
      "paypoint": {
        "baseUrl": "https://test.figment.co.za:49410",
        "tokenEndpoint": "/oauth2/token",
        "transactionEndpoint": "/transactions/transaction"
      }
    },
    "credentials": {
      "paypoint": {
        "apiKey": "Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq",
        "clientId": "E84OOE",
        "clientSecret": "621NZsDknRDWjqf8sKhyH0ktjPXtbsr4",
        "siteId": "TEST_RT08"
      }
    }
  }
}
```

### Implementation Steps

1. **Load Configuration**: Read live or test config based on environment
2. **Get OAuth Token**: POST to `/oauth2/token` with client credentials
3. **Build Transaction JSON**: Use cart items to build productItems array
4. **Send Transaction**: POST to `/transactions/transaction` with Bearer token
5. **Process Response**: Handle success/declined/error responses
6. **Update Receipt**: Include card details and approval code

### Code Example
```vb
' Build product items from cart
Dim productItems As New List(Of Object)
Dim itemId As Integer = 1

For Each row As DataRow In _cartItems.Rows
    productItems.Add(New With {
        .itemId = itemId,
        .category = 255,
        .amount = CDec(row("Total")) * 100,
        .description = row("Description").ToString().Substring(0, Math.Min(20, row("Description").ToString().Length)),
        .quantity = CInt(row("Quantity")),
        .unitPrice = CDec(row("UnitPrice")) * 100
    })
    itemId += 1
Next

' Build payment request
Dim paymentRequest As New With {
    .requestType = "Settlement",
    .reconIndicator = Now.ToString("HHmmss").Substring(0, Math.Min(7, Now.ToString("HHmmss").Length)),
    .supervisor = If(isAttended, New String() {"S"}, New String() {}),
    .posIdentifier = 1,
    .posVersion = "1.0.0",
    .siteId = If(isLiveMode, "RT08", "TEST_RT08"),
    .totalAmount = _cardAmount,
    .productItems = productItems
}
```

## Security Notes

- **Never hardcode credentials** in source code
- **Use different credentials** for test and production
- **Rotate API keys** regularly
- **Log transactions** for audit trail
- **Mask card numbers** in logs and receipts
- **Use HTTPS** for all API calls

## Testing Checklist

- [ ] Test attended transactions with supervisor override
- [ ] Test unattended transactions without supervisor
- [ ] Test successful payment flow
- [ ] Test declined payment scenarios
- [ ] Test timeout and retry logic
- [ ] Test receipt printing with card details
- [ ] Verify different card types (Visa, Mastercard, Amex)
- [ ] Test split payments (cash + card)
- [ ] Test refund processing
- [ ] Verify end-to-end transaction logging

## Troubleshooting

### Common Issues
1. **Invalid Credentials**: Check API key and client secret
2. **Timeout Errors**: Increase timeout values or retry
3. **Amount Errors**: Ensure amounts are in correct format (cents for items, decimal for total)
4. **Supervisor Required**: Use attended format for cashier-present transactions
5. **Network Issues**: Check internet connectivity to FNB endpoints

### Support Contacts
- **FNB Paypoint Support**: Contact for API issues
- **Technical Support**: For POS integration issues
- **Emergency**: For production payment failures

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-05-09 | Initial implementation with FNB Paypoint integration |
| 1.0.1 | 2025-05-09 | Added test vs live credential separation |
| 1.0.2 | 2025-05-09 | Updated JSON format to match FNB specifications |
