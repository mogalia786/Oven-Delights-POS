# Payment Gateway Configuration Reference

## Live/Test Mode Toggle

### JSON Configuration Files
- **Live**: `App_Data\pos-config-live.json`
- **Test**: `App_Data\pos-config-test.json`

### Live Configuration Format
```json
{
  "paymentGateway": {
    "mode": "live",
    "endpoints": {
      "paypoint": "https://api.paypoint.co.za/api/v1",
      "fnb": "https://www.fnb.co.za/merchant/services/eprocurement",
      "standardbank": "https://virtualcardservices.standardbank.co.za/virtualcards/api"
    },
    "credentials": {
      "paypoint": {
        "merchantId": "LIVE_MERCHANT_ID",
        "siteId": "LIVE_SITE_ID", 
        "apiKey": "LIVE_API_KEY",
        "clientSecret": "LIVE_CLIENT_SECRET"
      },
      "fnb": {
        "merchantCode": "LIVE_FNB_CODE",
        "terminalId": "LIVE_TERMINAL_ID"
      },
      "standardbank": {
        "merchantId": "LIVE_SB_MERCHANT_ID",
        "secretKey": "LIVE_SB_SECRET_KEY"
      }
    },
    "timeouts": {
      "transaction": 30000,
      "connection": 5000,
      "retry": 3
    },
    "features": {
      "allowCreditCard": true,
      "allowSplitTender": true,
      "allowCashBack": false
    }
  }
}
```

### Test Configuration Format
```json
{
  "paymentGateway": {
    "mode": "test",
    "endpoints": {
      "paypoint": "https://test-api.paypoint.co.za/api/v1",
      "fnb": "https://test.fnb.co.za/merchant/services/eprocurement", 
      "standardbank": "https://test-virtualcardservices.standardbank.co.za/virtualcards/api"
    },
    "credentials": {
      "paypoint": {
        "merchantId": "TEST_MERCHANT_ID",
        "siteId": "TEST_SITE_ID",
        "apiKey": "TEST_API_KEY", 
        "clientSecret": "TEST_CLIENT_SECRET"
      },
      "fnb": {
        "merchantCode": "TEST_FNB_CODE",
        "terminalId": "TEST_TERMINAL_ID"
      },
      "standardbank": {
        "merchantId": "TEST_SB_MERCHANT_ID",
        "secretKey": "TEST_SB_SECRET_KEY"
      }
    },
    "timeouts": {
      "transaction": 30000,
      "connection": 5000,
      "retry": 3
    },
    "features": {
      "allowCreditCard": true,
      "allowSplitTender": true,
      "allowCashBack": false
    }
  }
}
```

## Payment Gateway Keys

### Paypoint (Credit Card Processing)

#### LIVE API Credentials (PRODUCTION)
```json
{
  "paymentGateway": {
    "mode": "live",
    "endpoints": {
      "paypoint": "https://api.paypoint.co.za/api/v1"
    },
    "credentials": {
      "paypoint": {
        "merchantId": "REPLACE_WITH_YOUR_ACTUAL_LIVE_MERCHANT_ID",
        "siteId": "REPLACE_WITH_YOUR_ACTUAL_LIVE_SITE_ID", 
        "apiKey": "REPLACE_WITH_YOUR_ACTUAL_LIVE_API_KEY",
        "clientSecret": "REPLACE_WITH_YOUR_ACTUAL_LIVE_CLIENT_SECRET"
      }
    }
  }
}
```

#### 🔒 YOUR ACTUAL CREDENTIALS FORMAT:
Replace the placeholders above with your real Paypoint production credentials:

- **merchantId**: Your actual live Paypoint merchant ID
- **siteId**: Your actual live site identifier  
- **apiKey**: Your actual live API key
- **clientSecret**: Your actual live client secret

#### ✅ SECURITY REMINDER:
- Never hardcode real credentials in source code
- Store credentials securely (environment variables, secure vault, etc.)
- Use different credentials for development vs production

#### How to Get LIVE Credentials:
1. **Paypoint Dashboard**: Login to your Paypoint merchant dashboard
2. **Technical Support**: Contact Paypoint support for production credentials
3. **API Documentation**: Refer to Paypoint API documentation
4. **Security**: Never hardcode live credentials in source code

### Paypoint (Credit Card Processing)
- **Live Merchant ID**: Your production Paypoint merchant ID
- **Live Site ID**: Your production site identifier
- **Live API Key**: Your production API key
- **Live Client Secret**: Your production client secret
- **Test Merchant ID**: Test environment merchant ID
- **Test Site ID**: Test environment site identifier  
- **Test API Key**: Test environment API key
- **Test Client Secret**: Test environment client secret

### FNB (FNB EFTPOS)
- **Live Merchant Code**: Production FNB merchant code
- **Live Terminal ID**: Production FNB terminal ID
- **Test Merchant Code**: Test FNB merchant code
- **Test Terminal ID**: Test FNB terminal ID

### Standard Bank (Virtual Card Services)
- **Live Merchant ID**: Production Standard Bank merchant ID
- **Live Secret Key**: Production Standard Bank secret key
- **Test Merchant ID**: Test Standard Bank merchant ID
- **Test Secret Key**: Test Standard Bank secret key

## Credit Card Payment Flow

### 1. Card Insertion
- Customer inserts credit card
- System detects card type (Visa, Mastercard, etc.)
- Card data is masked for security (show only last 4 digits)
- Amount is pre-filled from cart total

### 2. Processing
- System validates card details
- Connects to appropriate gateway based on live/test mode
- Sends encrypted card data to payment processor
- Waits for approval/decline response

### 3. Response Handling
- **Approved**: Transaction completed, receipt printed
- **Declined**: Show decline reason, offer retry
- **Timeout**: Retry up to configured retry count
- **Error**: Log error, show user-friendly message

### 4. Split Tender Support
- Allow splitting payment between multiple methods
- Example: R100 cash + R200 credit card
- System calculates change automatically

### 5. Timeout Handling
- **Transaction Timeout**: 30 seconds max
- **Connection Timeout**: 5 seconds max  
- **Retry Logic**: Up to 3 retry attempts
- **Long Transaction**: Return to tender selection screen

## Security Notes

### Card Data Masking
- Show only: `****-****-****-1234`
- Never store full card numbers
- Clear card data immediately after processing
- Use PCI-compliant encryption for transmission

### Error Codes
- **01**: Invalid card number
- **02**: Expired card
- **03**: Insufficient funds
- **04**: Stolen card
- **05**: Do not honor
- **06**: Error
- **07**: Pick up card
- **08**: Honor only identification
- **14**: Invalid card number
- **15**: No such issuer

## Configuration Loading

### Priority Order
1. Check for `pos-config-live.json`
2. If not found, check for `pos-config-test.json`
3. If neither found, default to test mode
4. Load appropriate credentials based on mode

### Environment Detection
```vb
Dim configPath As String = If(File.Exists("App_Data\pos-config-live.json"), 
    "App_Data\pos-config-live.json", 
    "App_Data\pos-config-test.json")
```

## Integration Points

### POS Integration
- PaymentTenderForm loads config on initialization
- Passes gateway selection to payment processor
- Handles split tenders and change calculation
- Returns payment result to main POS form

### Receipt Integration
- Include last 4 digits of card on receipt
- Show authorization code if available
- Display "CREDIT CARD" as payment method
- Include transaction ID for reference

### Error Handling
- Log all payment attempts with timestamps
- Store error codes for troubleshooting
- Provide clear error messages to users
- Allow retry on temporary failures
