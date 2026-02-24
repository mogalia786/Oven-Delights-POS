Imports System.Data.SqlClient
Imports System.Configuration

''' <summary>
''' Accounting Service for posting proper double-entry bookkeeping transactions
''' Handles Cash on Hand vs Bank separation and Customer Ledger tracking
''' </summary>
Public Class AccountingService
    Private ReadOnly _connectionString As String
    Private ReadOnly _isEnabled As Boolean
    
    Public Sub New()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString")?.ConnectionString
        
        ' Feature flag: Set to False to disable accounting integration temporarily
        ' Set to True once proper transaction handling is implemented in calling code
        Dim enabledValue As String = ConfigurationManager.AppSettings("EnableAccountingIntegration")
        _isEnabled = If(String.IsNullOrEmpty(enabledValue), False, Boolean.Parse(enabledValue))
    End Sub
    
    ''' <summary>
    ''' Post cake order deposit - creates customer receivable and records deposit as liability
    ''' </summary>
    Public Sub PostOrderDeposit(orderNumber As String, orderID As Integer, customerID As Integer, 
                                customerName As String, accountNumber As String, 
                                totalAmount As Decimal, depositAmount As Decimal, 
                                paymentMethod As String, branchID As Integer, cashierName As String)
        If Not _isEnabled Then Return ' Accounting integration disabled
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    
                    Dim journalNumber = $"ORD-{orderNumber}"
                    
                    ' 1. Record deposit payment (Cash on Hand or Bank)
                    If paymentMethod.ToUpper() = "CASH" Then
                        ' DR: Cash on Hand
                        ' CR: Customer Deposits (Liability)
                        PostJournalEntry(conn, transaction, journalNumber, 
                            "1110", depositAmount, 0, ' Cash on Hand - Debit
                            $"Deposit received for Order {orderNumber}", 
                            "Order", orderNumber, branchID, customerID, cashierName)
                        
                        PostJournalEntry(conn, transaction, journalNumber,
                            "2120", 0, depositAmount, ' Customer Deposits - Credit
                            $"Deposit received for Order {orderNumber}",
                            "Order", orderNumber, branchID, customerID, cashierName)
                        
                        ' Record in Cash Register
                        RecordCashRegister(conn, transaction, branchID, 1, "Deposit", depositAmount, 
                                         "Cash", orderNumber, $"Cake order deposit - {customerName}", 
                                         cashierName)
                    Else
                        ' Card/EFT goes directly to Bank
                        ' DR: Bank
                        ' CR: Customer Deposits (Liability)
                        PostJournalEntry(conn, transaction, journalNumber,
                            "1120", depositAmount, 0, ' Bank - Debit
                            $"Deposit received for Order {orderNumber} via {paymentMethod}",
                            "Order", orderNumber, branchID, customerID, cashierName)
                        
                        PostJournalEntry(conn, transaction, journalNumber,
                            "2120", 0, depositAmount, ' Customer Deposits - Credit
                            $"Deposit received for Order {orderNumber}",
                            "Order", orderNumber, branchID, customerID, cashierName)
                    End If
                    
                    ' 2. Create Customer Receivable for full order amount
                    ' DR: Customer - Accounts Receivable (Asset)
                    ' CR: Customer - Deposit Paid (contra to receivable)
                    PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                     "Order", orderNumber,
                                     $"Cake order placed - Total: R{totalAmount:F2}",
                                     totalAmount, 0, branchID, cashierName)
                    
                    PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                     "Deposit", orderNumber,
                                     $"Deposit paid - R{depositAmount:F2}",
                                     0, depositAmount, branchID, cashierName)
                    
                    ' Note: POS_CustomOrders.JournalEntryNumber should be updated by the calling POS code
                    ' This service only posts to ERP accounting tables
                    
                    transaction.Commit()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Error posting order deposit accounting: {ex.Message}", ex)
        End Try
    End Sub
    
    ''' <summary>
    ''' Post order edit - adjust customer receivable for price difference
    ''' </summary>
    Public Sub PostOrderEdit(orderNumber As String, orderID As Integer, customerID As Integer,
                            customerName As String, accountNumber As String,
                            oldTotal As Decimal, newTotal As Decimal,
                            branchID As Integer, cashierName As String)
        If Not _isEnabled Then Return ' Accounting integration disabled
        Try
            Dim difference = newTotal - oldTotal
            If Math.Abs(difference) < 0.01 Then Return ' No change
            
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    
                    If difference > 0 Then
                        ' Order increased - customer owes more
                        PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                         "Edit", orderNumber,
                                         $"Order edited - Additional amount: R{difference:F2}",
                                         difference, 0, branchID, cashierName)
                    Else
                        ' Order decreased - reduce customer receivable
                        PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                         "Edit", orderNumber,
                                         $"Order edited - Reduced amount: R{Math.Abs(difference):F2}",
                                         0, Math.Abs(difference), branchID, cashierName)
                    End If
                    
                    transaction.Commit()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Error posting order edit accounting: {ex.Message}", ex)
        End Try
    End Sub
    
    ''' <summary>
    ''' Post order collection - recognize revenue and clear deposits
    ''' </summary>
    Public Sub PostOrderCollection(orderNumber As String, orderID As Integer, customerID As Integer,
                                   customerName As String, accountNumber As String,
                                   totalAmount As Decimal, depositAmount As Decimal, balanceAmount As Decimal,
                                   paymentMethod As String, branchID As Integer, cashierName As String)
        If Not _isEnabled Then Return ' Accounting integration disabled
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    
                    Dim journalNumber = $"COLL-{orderNumber}"
                    
                    ' 1. Record balance payment (if any)
                    If balanceAmount > 0 Then
                        If paymentMethod.ToUpper() = "CASH" Then
                            ' DR: Cash on Hand
                            PostJournalEntry(conn, transaction, journalNumber,
                                "1110", balanceAmount, 0,
                                $"Balance payment for Order {orderNumber}",
                                "Collection", orderNumber, branchID, customerID, cashierName)
                            
                            RecordCashRegister(conn, transaction, branchID, 1, "Sale", balanceAmount,
                                             "Cash", orderNumber, $"Cake order balance - {customerName}",
                                             cashierName)
                        Else
                            ' DR: Bank
                            PostJournalEntry(conn, transaction, journalNumber,
                                "1120", balanceAmount, 0,
                                $"Balance payment for Order {orderNumber} via {paymentMethod}",
                                "Collection", orderNumber, branchID, customerID, cashierName)
                        End If
                    End If
                    
                    ' 2. Clear Customer Deposits liability
                    ' DR: Customer Deposits (Liability)
                    PostJournalEntry(conn, transaction, journalNumber,
                        "2120", depositAmount, 0,
                        $"Clear deposit for Order {orderNumber}",
                        "Collection", orderNumber, branchID, customerID, cashierName)
                    
                    ' 3. Recognize Revenue
                    ' CR: Cake Sales Revenue
                    PostJournalEntry(conn, transaction, journalNumber,
                        "4110", 0, totalAmount,
                        $"Cake order revenue - Order {orderNumber}",
                        "Collection", orderNumber, branchID, customerID, cashierName)
                    
                    ' 4. Clear Customer Ledger
                    PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                     "Payment", orderNumber,
                                     $"Order collected - Deposit: R{depositAmount:F2}, Balance: R{balanceAmount:F2}",
                                     0, depositAmount, branchID, cashierName)
                    
                    If balanceAmount > 0 Then
                        PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                         "Payment", orderNumber,
                                         $"Balance payment - R{balanceAmount:F2}",
                                         0, balanceAmount, branchID, cashierName)
                    End If
                    
                    PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                     "Payment", orderNumber,
                                     $"Order completed - Total: R{totalAmount:F2}",
                                     0, totalAmount, branchID, cashierName)
                    
                    transaction.Commit()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Error posting order collection accounting: {ex.Message}", ex)
        End Try
    End Sub
    
    ''' <summary>
    ''' Post order cancellation - record cancellation fee revenue and process refund/payment
    ''' </summary>
    Public Sub PostOrderCancellation(orderNumber As String, orderID As Integer, customerID As Integer,
                                     customerName As String, accountNumber As String,
                                     depositAmount As Decimal, cancellationFee As Decimal,
                                     paymentMethod As String, branchID As Integer, cashierName As String)
        If Not _isEnabled Then Return ' Accounting integration disabled
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    
                    Dim journalNumber = $"CANC-{orderNumber}"
                    Dim balance = depositAmount - cancellationFee
                    
                    ' 1. Record Cancellation Fee Revenue
                    ' CR: Cancellation Fee Revenue
                    PostJournalEntry(conn, transaction, journalNumber,
                        "4130", 0, cancellationFee,
                        $"Cancellation fee for Order {orderNumber}",
                        "Cancellation", orderNumber, branchID, customerID, cashierName)
                    
                    ' 2. Clear Customer Deposits
                    ' DR: Customer Deposits
                    PostJournalEntry(conn, transaction, journalNumber,
                        "2120", depositAmount, 0,
                        $"Clear deposit for cancelled Order {orderNumber}",
                        "Cancellation", orderNumber, branchID, customerID, cashierName)
                    
                    ' 3. Process Refund or Additional Payment
                    If balance > 0 Then
                        ' Refund to customer
                        If paymentMethod.ToUpper() = "CASH" Then
                            ' CR: Cash on Hand
                            PostJournalEntry(conn, transaction, journalNumber,
                                "1110", 0, balance,
                                $"Refund for cancelled Order {orderNumber}",
                                "Cancellation", orderNumber, branchID, customerID, cashierName)
                            
                            RecordCashRegister(conn, transaction, branchID, 1, "Refund", balance,
                                             "Cash", orderNumber, $"Cancellation refund - {customerName}",
                                             cashierName)
                        Else
                            ' CR: Bank
                            PostJournalEntry(conn, transaction, journalNumber,
                                "1120", 0, balance,
                                $"Refund for cancelled Order {orderNumber} via {paymentMethod}",
                                "Cancellation", orderNumber, branchID, customerID, cashierName)
                        End If
                        
                        ' Customer Ledger
                        PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                         "Refund", orderNumber,
                                         $"Cancellation refund - R{balance:F2}",
                                         0, balance, branchID, cashierName)
                    ElseIf balance < 0 Then
                        ' Customer pays additional amount
                        Dim additionalPayment = Math.Abs(balance)
                        If paymentMethod.ToUpper() = "CASH" Then
                            ' DR: Cash on Hand
                            PostJournalEntry(conn, transaction, journalNumber,
                                "1110", additionalPayment, 0,
                                $"Additional payment for cancelled Order {orderNumber}",
                                "Cancellation", orderNumber, branchID, customerID, cashierName)
                            
                            RecordCashRegister(conn, transaction, branchID, 1, "Sale", additionalPayment,
                                             "Cash", orderNumber, $"Cancellation fee payment - {customerName}",
                                             cashierName)
                        Else
                            ' DR: Bank
                            PostJournalEntry(conn, transaction, journalNumber,
                                "1120", additionalPayment, 0,
                                $"Additional payment for cancelled Order {orderNumber} via {paymentMethod}",
                                "Cancellation", orderNumber, branchID, customerID, cashierName)
                        End If
                        
                        ' Customer Ledger
                        PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                         "Payment", orderNumber,
                                         $"Additional cancellation fee payment - R{additionalPayment:F2}",
                                         0, additionalPayment, branchID, cashierName)
                    End If
                    
                    ' 4. Clear customer receivable
                    PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                     "Cancellation", orderNumber,
                                     $"Order cancelled - Cancellation fee: R{cancellationFee:F2}",
                                     cancellationFee, 0, branchID, cashierName)
                    
                    PostCustomerLedger(conn, transaction, customerID, customerName, accountNumber,
                                     "Cancellation", orderNumber,
                                     $"Clear receivable for cancelled order",
                                     0, 0, branchID, cashierName)
                    
                    transaction.Commit()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Error posting order cancellation accounting: {ex.Message}", ex)
        End Try
    End Sub
    
    Private Sub PostJournalEntry(conn As SqlConnection, transaction As SqlTransaction,
                                journalNumber As String, accountCode As String,
                                debitAmount As Decimal, creditAmount As Decimal,
                                description As String, referenceType As String,
                                referenceID As String, branchID As Integer,
                                customerID As Integer, createdBy As String)
        
        Dim sql = "
            INSERT INTO GeneralLedger (
                JournalEntryNumber, TransactionDate, AccountID, DebitAmount, CreditAmount,
                Description, ReferenceType, ReferenceID, BranchID, CustomerID, CreatedBy
            )
            SELECT 
                @JournalNumber, GETDATE(), AccountID, @DebitAmount, @CreditAmount,
                @Description, @ReferenceType, @ReferenceID, @BranchID, @CustomerID, @CreatedBy
            FROM ChartOfAccounts
            WHERE AccountCode = @AccountCode"
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@JournalNumber", journalNumber)
            cmd.Parameters.AddWithValue("@AccountCode", accountCode)
            cmd.Parameters.AddWithValue("@DebitAmount", debitAmount)
            cmd.Parameters.AddWithValue("@CreditAmount", creditAmount)
            cmd.Parameters.AddWithValue("@Description", description)
            cmd.Parameters.AddWithValue("@ReferenceType", referenceType)
            cmd.Parameters.AddWithValue("@ReferenceID", referenceID)
            cmd.Parameters.AddWithValue("@BranchID", branchID)
            cmd.Parameters.AddWithValue("@CustomerID", If(customerID > 0, CObj(customerID), DBNull.Value))
            cmd.Parameters.AddWithValue("@CreatedBy", createdBy)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
    
    Private Sub PostCustomerLedger(conn As SqlConnection, transaction As SqlTransaction,
                                  customerID As Integer, customerName As String, accountNumber As String,
                                  transactionType As String, referenceNumber As String,
                                  description As String, debitAmount As Decimal, creditAmount As Decimal,
                                  branchID As Integer, createdBy As String)
        
        ' Get current balance
        Dim currentBalance As Decimal = 0
        Dim sqlBalance = "SELECT TOP 1 RunningBalance FROM CustomerLedger 
                         WHERE AccountNumber = @AccountNumber ORDER BY LedgerID DESC"
        Using cmd As New SqlCommand(sqlBalance, conn, transaction)
            cmd.Parameters.AddWithValue("@AccountNumber", accountNumber)
            Dim result = cmd.ExecuteScalar()
            If result IsNot Nothing Then currentBalance = CDec(result)
        End Using
        
        ' Calculate new balance
        Dim newBalance = currentBalance + debitAmount - creditAmount
        
        ' Insert ledger entry
        Dim sql = "
            INSERT INTO CustomerLedger (
                CustomerID, CustomerName, AccountNumber, TransactionDate, TransactionType,
                ReferenceNumber, Description, DebitAmount, CreditAmount, RunningBalance,
                BranchID, CreatedBy
            ) VALUES (
                @CustomerID, @CustomerName, @AccountNumber, GETDATE(), @TransactionType,
                @ReferenceNumber, @Description, @DebitAmount, @CreditAmount, @RunningBalance,
                @BranchID, @CreatedBy
            )"
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@CustomerID", customerID)
            cmd.Parameters.AddWithValue("@CustomerName", customerName)
            cmd.Parameters.AddWithValue("@AccountNumber", accountNumber)
            cmd.Parameters.AddWithValue("@TransactionType", transactionType)
            cmd.Parameters.AddWithValue("@ReferenceNumber", referenceNumber)
            cmd.Parameters.AddWithValue("@Description", description)
            cmd.Parameters.AddWithValue("@DebitAmount", debitAmount)
            cmd.Parameters.AddWithValue("@CreditAmount", creditAmount)
            cmd.Parameters.AddWithValue("@RunningBalance", newBalance)
            cmd.Parameters.AddWithValue("@BranchID", branchID)
            cmd.Parameters.AddWithValue("@CreatedBy", createdBy)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
    
    Private Sub RecordCashRegister(conn As SqlConnection, transaction As SqlTransaction,
                                  branchID As Integer, tillPointID As Integer,
                                  transactionType As String, amount As Decimal,
                                  paymentMethod As String, referenceNumber As String,
                                  description As String, cashierName As String)
        
        Dim sql = "
            INSERT INTO CashRegister (
                BranchID, TillPointID, TransactionDate, TransactionType, Amount,
                PaymentMethod, ReferenceNumber, Description, CashierID, CashierName
            ) VALUES (
                @BranchID, @TillPointID, GETDATE(), @TransactionType, @Amount,
                @PaymentMethod, @ReferenceNumber, @Description, 0, @CashierName
            )"
        
        Using cmd As New SqlCommand(sql, conn, transaction)
            cmd.Parameters.AddWithValue("@BranchID", branchID)
            cmd.Parameters.AddWithValue("@TillPointID", tillPointID)
            cmd.Parameters.AddWithValue("@TransactionType", transactionType)
            cmd.Parameters.AddWithValue("@Amount", amount)
            cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod)
            cmd.Parameters.AddWithValue("@ReferenceNumber", If(referenceNumber, DBNull.Value))
            cmd.Parameters.AddWithValue("@Description", If(description, DBNull.Value))
            cmd.Parameters.AddWithValue("@CashierName", cashierName)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
End Class
