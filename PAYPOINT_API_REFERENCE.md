# Paypoint MiniPOS Cloud Gateway API Reference

**Version:** 1.0  
**Contact:** Marcel Zuur  
**Base URL (Test):** `https://test.figment.co.za:49410/api`  
**Base URL (Production):** `https://prod.figment.co.za/api`  
**API Key:** `Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq`

---

## Table of Contents
1. [Authentication](#authentication)
2. [API Status](#api-status)
3. [Transactions (Mandatory)](#transactions-mandatory)
4. [Transactions (Optional)](#transactions-optional)
5. [Request/Response Models](#requestresponse-models)
6. [Error Codes](#error-codes)
7. [Integration Guide](#integration-guide)

---

## Authentication

### OAuth2 Token Generation

**Endpoint:** `POST /oauth2/token`

**Description:** Create a new OAuth2 access token for API authentication.

**Headers:**
```
Content-Type: application/json
apiKey: Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq
```

**Request Body:**
```json
{
  "client_id": "string",
  "client_secret": "string"
}
```

**Response (200 - Success):**
```json
{
  "access_token": "227a0d096b445fcbec61a0e3c17ec901ba274a71f52467d1e5ee82f715cf1284",
  "token_type": "Bearer",
  "expires_in": 1800
}
```

**Response Codes:**
- `200` - Successfully generated OAuth token
- `400` - Invalid/Missing field or JSON
- `403` - API key is missing or invalid
- `404` - API key is missing or invalid

**Token Usage:**
- Token expires in 1800 seconds (30 minutes)
- Include in subsequent requests: `Authorization: Bearer {access_token}`
- Refresh token before expiry to maintain session

---

## API Status

### Health Check

**Endpoint:** `GET /status`

**Description:** Check API health and availability.

**Response (200):**
```json
{
  "status": "OK"
}
```

---

## Transactions (Mandatory)

### 1. Start New Transaction

**Endpoint:** `POST /transactions/transaction`

**Description:** Initiates a new payment transaction. Supports three types:
- **Settlement** - Standard card payment transaction
- **Refund** - Refund a previous transaction
- **CashAdvance** - Cash back with no items

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {access_token}
apiKey: Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq
```

**Request Body:**
```json
{
  "siteId": "UT02",
  "requestType": "Settlement",
  "reconIndicator": "1234567",
  "posIdentifier": 10,
  "posVersion": "1.8.5.3",
  "totalAmount": 1000,
  "operatorId": 107010,
  "operatorName": "Marcel",
  "shiftNo": 1,
  "slipNo": 1,
  "supervisor": ["S"],
  "cashBackAmount": 0,
  "budgetPeriod": 0,
  "productItems": [
    {
      "itemId": 1,
      "category": 255,
      "amount": 1000,
      "barCode": "600123456100",
      "description": "1L CocaCola",
      "quantity": 1,
      "unitPrice": 1000,
      "rebate": 0
    }
  ]
}
```

**Request Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `siteId` | string | Yes | Unique site identifier (max 15 chars) |
| `requestType` | string | Yes | Transaction type: `Settlement`, `Refund`, `CashAdvance` |
| `reconIndicator` | string | No | Unique request identifier (max 7 chars) |
| `posIdentifier` | number | Yes | Till number (max 3 digits) |
| `posVersion` | string | Yes | POS software version (max 40 chars) |
| `totalAmount` | integer | Yes | Total amount in cents (max 10 digits) |
| `operatorId` | number | No | Cashier identifier (max 7 digits) |
| `operatorName` | string | No | Cashier name (max 20 chars) |
| `shiftNo` | number | No | Shift number (max 4 digits) |
| `slipNo` | number | No | Slip number (max 4 digits) |
| `supervisor` | array | No | Supervisor override options |
| `cashBackAmount` | integer | No | Cash back amount in cents (must be < totalAmount) |
| `budgetPeriod` | number | No | Budget period if applicable (max 2 digits) |
| `productItems` | array | Yes | Array of product items |

**Product Item Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `itemId` | number | Yes | Item number (starting from 1) |
| `category` | number | Yes | Product category (max 3 digits) |
| `amount` | integer | Yes | Item amount in cents |
| `barCode` | string | No | Product barcode (max 13 chars) |
| `description` | string | No | Product description (max 20 chars) |
| `quantity` | number | Yes | Product quantity (max 7 digits) |
| `unitPrice` | integer | Yes | Unit price in cents |
| `rebate` | integer | No | Item discount in cents |

**Response (200 - Success):**
```json
{
  "applicationSender": "VBSPOS",
  "requestType": "Settlement",
  "resultCode": "0000",
  "resultSubCode": "001",
  "posIdentifier": 10,
  "reconIndicator": "1234567",
  "totalAmount": 1000,
  "operatorId": 107010,
  "operatorName": "Marcel",
  "supervisor": ["S"],
  "cashBackAmount": 1000,
  "budgetPeriod": 0,
  "printTemplate": "Y",
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
  ],
  "server": {
    "serial": 2644,
    "version": "4.20A"
  },
  "merchant": {
    "number": "600115",
    "terminalId": "600115",
    "name": "Engen Dev",
    "type": "R"
  },
  "printLines": {
    "textLine": [
      {
        "text": "PAN: 457896xxxxxxx391        Visa Card  "
      }
    ],
    "terminalId": false
  }
}
```

**Response Codes:**
- `200` - Transaction successful
- `400` - Invalid/Missing field or JSON
- `401` - API key is missing or invalid
- `402` - Transaction declined
- `403` - Not authorised
- `404` - Transaction not found
- `500` - Internal System Error

**Response (402 - Declined):**
```json
{
  "error": "Incorrect PIN",
  "resultCode": "1000",
  "resultSubCode": "201",
  "posIdentifier": 10,
  "totalAmount": 1000,
  "server": {
    "serial": "2644",
    "version": "4.20A"
  },
  "merchant": {
    "name": "MiniPOS POSAPI",
    "number": "086553",
    "terminalId": "991230",
    "type": "R"
  },
  "reconIndicator": "1234567",
  "transaction": {
    "pan": "528497xxxxxx5593",
    "cardName": "Credit",
    "cardType": "006",
    "date": "2015-12-23 08:10:00",
    "track2": "528497xxxxxx75593=170520100000000000000",
    "expiry": "1705",
    "sequence": "003165",
    "approvalCode": "176593",
    "uti": "99123001-0000-0000-0000-151223165901",
    "batch": "000076",
    "record": "001",
    "sPDHSeqNo": "003165",
    "flags": "60024001",
    "signature": "N",
    "hotcardSeq": "2000500",
    "indicators": "221IH"
  },
  "printTemplate": "R"
}
```

---

### 2. Resume Transaction

**Endpoint:** `POST /transactions/resumeTransaction`

**Description:** Continue a transaction that was timed out by the API but is still being processed by the MiniPOS.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `siteId` | string | Yes | Unique site identifier |
| `posIdentifier` | number | Yes | Till number |
| `reconIndicator` | string | Yes | Original transaction recon indicator |

**Example Request:**
```
POST /transactions/resumeTransaction?siteId=UT02&posIdentifier=10&reconIndicator=1234567
```

**Response:** Same as Start Transaction response

**Response Codes:**
- `200` - Transaction successful
- `400` - Invalid/Missing field or JSON
- `401` - API key is missing or invalid
- `402` - Transaction declined
- `403` - Not authorised
- `404` - Transaction not found
- `500` - Internal System Error

---

## Transactions (Optional)

### 3. Reprint Receipt

**Endpoint:** `POST /transactions/reprintReceipt`

**Description:** Reprint a transaction receipt.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `siteId` | string | Yes | Unique site identifier |
| `posIdentifier` | number | Yes | Till number |
| `operatorId` | string | No | Operator ID |
| `reconIndicator` | string | Yes | Transaction recon indicator |

**Example Request:**
```
POST /transactions/reprintReceipt?siteId=UT02&posIdentifier=10&reconIndicator=1234567
```

**Response Codes:**
- `200` - Transaction successful
- `202` - Transaction in progress (e.g., "Waiting for cashier")
- `400` - Invalid/Missing field or JSON
- `401` - API key is missing or invalid
- `402` - Transaction declined
- `403` - Not authorised
- `404` - Transaction not found
- `500` - Internal System Error

**Response (202 - In Progress):**
```json
{
  "error": "Waiting for cashier",
  "resultCode": "3015",
  "posIdentifier": 10,
  "reconIndicator": "1234567"
}
```

---

### 4. Get Transaction Status

**Endpoint:** `GET /transactions/status`

**Description:** Query the status of an active transaction. Vital for handling communication issues.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `siteId` | string | Yes | Unique site identifier |
| `posIdentifier` | number | Yes | Till number |
| `operatorId` | string | No | Operator ID |
| `reconIndicator` | string | Yes | Transaction recon indicator |

**Example Request:**
```
GET /transactions/status?siteId=UT02&posIdentifier=10&reconIndicator=1234567
```

**Response:** Same as Start Transaction response

**Response Codes:**
- `200` - Transaction successful
- `202` - Transaction in progress
- `400` - Invalid/Missing field or JSON
- `401` - API key is missing or invalid
- `402` - Transaction declined
- `403` - Not authorised
- `404` - Transaction not found
- `500` - Internal System Error

---

### 5. Find Transaction

**Endpoint:** `GET /transactions/findTransaction`

**Description:** Read transactions from the transaction database with filtering options.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `siteId` | string | Yes | Unique site identifier |
| `posIdentifier` | number | Yes | Till number |
| `mode` | string | Yes | Filter mode: `TillNo`, `OperatorNo`, `ReceiptNo`, `BatchNo`, `ReconIndicator` |
| `value` | string | Yes | Filter value |
| `recordNo` | string | No | Offset for returned transaction (0 = most recent) |

**Example Request:**
```
GET /transactions/findTransaction?siteId=UT02&posIdentifier=10&mode=ReconIndicator&value=1234567&recordNo=0
```

**Response:** Same as Start Transaction response

**Response Codes:**
- `200` - Transaction successful
- `400` - Invalid/Missing field or JSON
- `401` - API key is missing or invalid
- `403` - Not authorised
- `404` - Transaction not found
- `500` - Internal System Error

---

### 6. Cancel Transaction

**Endpoint:** `DELETE /transactions/cancel`

**Description:** Removes a transaction request from the MiniPOS queue. Only succeeds if the transaction is still idle and not being processed.

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `siteId` | string | Yes | Unique site identifier |
| `posIdentifier` | number | Yes | Till number |
| `reconIndicator` | string | Yes | Original transaction recon indicator |

**Example Request:**
```
DELETE /transactions/cancel?siteId=UT02&posIdentifier=10&reconIndicator=1234567
```

**Response (200 - Success):**
```json
{
  "resultCode": "0000",
  "posIdentifier": 10,
  "reconIndicator": "1234567"
}
```

**Response Codes:**
- `200` - Transaction successfully cancelled
- `400` - Invalid/Missing field or JSON
- `401` - API key is missing or invalid
- `403` - Not authorised
- `404` - Transaction not found
- `500` - Internal System Error

---

## Request/Response Models

### Transaction Request Model

```json
{
  "siteId": "string (max 15)",
  "requestType": "Settlement|Refund|CashAdvance",
  "reconIndicator": "string (max 7)",
  "posIdentifier": "number (max 3)",
  "posVersion": "string (max 40)",
  "totalAmount": "integer (max 10) - cents",
  "operatorId": "number (max 7)",
  "operatorName": "string (max 20)",
  "shiftNo": "number (max 4)",
  "slipNo": "number (max 4)",
  "supervisor": ["string"],
  "cashBackAmount": "integer (max 8) - cents",
  "budgetPeriod": "number (max 2)",
  "productItems": [
    {
      "itemId": "number (min 1)",
      "category": "number (max 3)",
      "amount": "integer (max 10) - cents",
      "barCode": "string (max 13)",
      "description": "string (max 20)",
      "quantity": "number (max 7)",
      "unitPrice": "integer (max 10) - cents",
      "rebate": "integer (max 10) - cents"
    }
  ]
}
```

### Transaction Response Model

```json
{
  "applicationSender": "string (max 7)",
  "requestType": "Settlement|Refund|CashAdvance",
  "resultCode": "string (4 chars)",
  "resultSubCode": "string (4 chars)",
  "posIdentifier": "number (max 3)",
  "reconIndicator": "string (max 7)",
  "totalAmount": "integer (max 8) - cents",
  "operatorId": "number (max 7)",
  "operatorName": "string (max 20)",
  "supervisor": ["string"],
  "cashBackAmount": "integer (max 8) - cents",
  "budgetPeriod": "number (max 2)",
  "printTemplate": "string (1 char) - Y/N/R",
  "transactions": [
    {
      "date": "ISO 8601 datetime",
      "pan": "string (8-19) - Obfuscated card number",
      "expiry": "string (4) - YYMM",
      "cardType": "string (3)",
      "sequence": "string (6) - Receipt number (STAN)",
      "batch": "string (6)",
      "record": "string (10)",
      "approvalCode": "string (6)",
      "flags": "string (8)",
      "indicators": "string (16)",
      "uti": "string (36) - Unique Trace Number",
      "track2": "string (79)",
      "spdhSeqNo": "string (8)",
      "signature": "string (1) - Y/N",
      "hotcardSeq": "string (7)"
    }
  ],
  "server": {
    "serial": "number (max 6)",
    "version": "string (max 6)"
  },
  "merchant": {
    "number": "string (max 6)",
    "terminalId": "string (max 6)",
    "name": "string (max 16)",
    "type": "string (1) - R/S/P/F/C"
  },
  "printLines": {
    "textLine": [
      {
        "text": "string (max 40)"
      }
    ],
    "terminalId": "boolean"
  }
}
```

---

## Error Codes

### Result Codes

| Code | Description |
|------|-------------|
| `0000` | Transaction successful |
| `1000` | Transaction declined (see subCode for details) |
| `3015` | Transaction in progress (waiting for cashier) |
| `3050` | Transaction not found |

### Result Sub-Codes

| Sub-Code | Description |
|----------|-------------|
| `001` | Success |
| `201` | Incorrect PIN |
| `050` | Not found, please retry |

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| `200` | Success |
| `202` | Accepted (transaction in progress) |
| `400` | Bad Request (invalid/missing field or JSON) |
| `401` | Unauthorized (API key missing or invalid) |
| `402` | Payment Required (transaction declined) |
| `403` | Forbidden (not authorised) |
| `404` | Not Found (transaction not found) |
| `500` | Internal Server Error |

---

## Integration Guide

### 1. Authentication Flow

```
1. Call POST /oauth2/token with client_id and client_secret
2. Receive access_token (valid for 30 minutes)
3. Store token and expiry time
4. Include token in all subsequent requests: Authorization: Bearer {token}
5. Refresh token before expiry (recommended: 5 minutes before)
```

### 2. Payment Transaction Flow

```
1. Build transaction request with product items
2. Generate unique reconIndicator (max 7 chars)
3. POST /transactions/transaction
4. Handle response:
   - 200: Success - Print receipt, complete sale
   - 202: In Progress - Poll status or wait
   - 402: Declined - Show error, cancel sale
   - 404: Not Found - Retry or cancel
   - 500: System Error - Log and retry
5. If timeout occurs, use POST /transactions/resumeTransaction
6. Store transaction details in local database
```

### 3. Error Handling

```
Network Timeout:
- Use GET /transactions/status to check transaction state
- If still pending, use POST /transactions/resumeTransaction
- If completed, retrieve transaction details

Transaction Declined:
- Display decline reason to cashier
- Offer retry option
- Log decline for reporting

System Error:
- Retry up to 3 times with exponential backoff
- If persistent, switch to offline mode (if supported)
- Log error for investigation
```

### 4. Receipt Printing

```
1. Parse printLines from response
2. Print textLine array in order
3. Include transaction details:
   - PAN (obfuscated)
   - Card type
   - Approval code
   - Sequence number
   - Date/time
4. Use printTemplate flag:
   - Y: Print full receipt
   - N: No receipt
   - R: Reprint required
```

### 5. Reconciliation

```
Daily:
1. Use GET /transactions/findTransaction with mode=BatchNo
2. Retrieve all transactions for current batch
3. Compare with local database
4. Generate settlement report
5. Identify discrepancies

Per Transaction:
1. Store reconIndicator in local database
2. Link to POS invoice number
3. Use for refunds and lookups
```

### 6. Testing

**Test Environment:**
- Base URL: `https://test.figment.co.za:49410/api`
- API Key: `Q7w30FOnntfiLzJuKKJrKqVqXg9BHPCq`

**Test Scenarios:**
1. Successful payment (amount: 1000 cents = R10.00)
2. Declined payment (test with invalid PIN)
3. Timeout and resume
4. Transaction cancellation
5. Receipt reprint
6. Transaction lookup

---

## Important Notes

1. **Amounts:** All amounts are in **cents** (e.g., R10.00 = 1000)
2. **reconIndicator:** Must be unique per transaction (max 7 characters)
3. **posIdentifier:** Till number, must be ≥ 21 for cancellation support
4. **Token Expiry:** OAuth token expires in 30 minutes - refresh proactively
5. **PAN Security:** Card numbers are obfuscated (first 6 + last 4 digits only)
6. **Timeout Handling:** Always implement resume logic for network issues
7. **Idempotency:** Use reconIndicator to prevent duplicate transactions
8. **Logging:** Log all requests/responses for debugging and reconciliation

---

## Support

**Contact:** Marcel Zuur  
**Documentation:** OpenAPI 3.0 Specification  
**Environment:** Test and Production available

---

*Last Updated: December 17, 2025*
