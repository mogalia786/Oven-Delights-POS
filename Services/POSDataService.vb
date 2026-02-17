Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient

Public Class POSDataService
    Private ReadOnly _connectionString As String
    Private ReadOnly _tablePrefix As String
    Private ReadOnly _useDemoTables As Boolean

    Public Sub New()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
        Dim useDemoSetting = If(ConfigurationManager.AppSettings("UseDemoTables"), "true")
        _useDemoTables = Boolean.Parse(useDemoSetting)
        _tablePrefix = If(_useDemoTables, "Demo_", "")
    End Sub

    Private Function GetTableName(baseName As String) As String
        Return $"{_tablePrefix}{baseName}"
    End Function

    ''' <summary>
    ''' Get all products with prices and stock for a branch
    ''' </summary>
    Public Function GetProductsWithStock(branchId As Integer) As DataTable
        Dim dt As New DataTable()
        
        Dim sql As String = $"
            SELECT 
                p.ProductID,
                p.SKU,
                p.Name AS ProductName,
                p.Category,
                p.ProductID AS VariantID,
                ISNULL(p.ExternalBarcode, p.SKU) AS Barcode,
                ISNULL(pr.SellingPrice, 0) AS SellingPrice,
                ISNULL(pr.CostPrice, 0) AS CostPrice,
                ISNULL(s.Quantity, 0) AS QtyOnHand,
                0 AS ReorderPoint,
                CASE WHEN ISNULL(s.Quantity, 0) > 0 THEN 1 ELSE 0 END AS InStock
            FROM {GetTableName("Retail_Product")} p
            LEFT JOIN {GetTableName("Retail_Price")} pr ON p.ProductID = pr.ProductID 
                AND (pr.BranchID IS NULL OR pr.BranchID = @BranchID)
                AND pr.EffectiveFrom <= GETDATE()
                AND (pr.EffectiveTo IS NULL OR pr.EffectiveTo >= GETDATE())
            LEFT JOIN {GetTableName("Retail_Stock")} s ON p.ProductID = s.ProductID 
                AND s.BranchID = @BranchID
            WHERE p.IsActive = 1 
                AND (p.BranchID = @BranchID OR p.BranchID IS NULL)
                AND (p.ProductType IN ('External', 'Internal') OR p.ProductType IS NULL)
            ORDER BY p.Category, p.Name"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@BranchID", branchId)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using

        Return dt
    End Function

    ''' <summary>
    ''' Search products by SKU, barcode, or name
    ''' </summary>
    Public Function SearchProducts(searchTerm As String, branchId As Integer) As DataTable
        Dim dt As New DataTable()
        
        Dim sql As String = "
            SELECT TOP 50
                p.ProductID,
                p.Code,
                p.ProductCode,
                p.Name AS ProductName,
                p.Description,
                p.SKU,
                p.ProductID AS VariantID,
                ISNULL(p.Barcode, p.SKU) AS Barcode,
                ISNULL(
                    (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
                     WHERE ProductID = p.ProductID AND BranchID = @BranchID 
                     ORDER BY EffectiveFrom DESC),
                    (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
                     WHERE ProductID = p.ProductID AND BranchID IS NULL 
                     ORDER BY EffectiveFrom DESC)
                ) AS SellingPrice,
                ISNULL(p.CurrentStock, 0) AS QtyOnHand
            FROM Demo_Retail_Product p
            WHERE p.BranchID = @BranchID
                AND p.IsActive = 1 
                AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'miscellaneous', 'pest control')
                AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
                AND (p.SKU LIKE @Search OR p.Name LIKE @Search OR ISNULL(p.Barcode, p.SKU) LIKE @Search OR p.Code LIKE @Search OR p.ProductCode LIKE @Search)
            ORDER BY p.Name"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@BranchID", branchId)
                cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%")
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using

        Return dt
    End Function

    ''' <summary>
    ''' Process a sale transaction
    ''' </summary>
    Public Function ProcessSale(saleData As SaleTransaction) As Integer
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Using trans = conn.BeginTransaction()
                Try
                    ' Insert sale header
                    Dim saleId As Integer
                    Dim sqlSale As String = $"
                        INSERT INTO {GetTableName("Sales")} 
                        (SaleNumber, BranchID, SaleDate, CustomerName, Subtotal, TaxAmount, DiscountAmount, TotalAmount, TenderType, CashierID)
                        VALUES (@SaleNumber, @BranchID, @SaleDate, @CustomerName, @Subtotal, @TaxAmount, @DiscountAmount, @TotalAmount, @TenderType, @CashierID);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);"

                    Using cmd As New SqlCommand(sqlSale, conn, trans)
                        cmd.Parameters.AddWithValue("@SaleNumber", saleData.SaleNumber)
                        cmd.Parameters.AddWithValue("@BranchID", saleData.BranchID)
                        cmd.Parameters.AddWithValue("@SaleDate", saleData.SaleDate)
                        cmd.Parameters.AddWithValue("@CustomerName", If(saleData.CustomerName, DBNull.Value))
                        cmd.Parameters.AddWithValue("@Subtotal", saleData.Subtotal)
                        cmd.Parameters.AddWithValue("@TaxAmount", saleData.TaxAmount)
                        cmd.Parameters.AddWithValue("@DiscountAmount", saleData.DiscountAmount)
                        cmd.Parameters.AddWithValue("@TotalAmount", saleData.TotalAmount)
                        cmd.Parameters.AddWithValue("@TenderType", saleData.TenderType)
                        cmd.Parameters.AddWithValue("@CashierID", saleData.CashierID)
                        saleId = CInt(cmd.ExecuteScalar())
                    End Using

                    ' Insert sale details and update stock
                    For Each item In saleData.Items
                        ' Insert sale detail
                        Dim sqlDetail As String = $"
                            INSERT INTO {GetTableName("SalesDetails")} 
                            (SaleID, VariantID, ProductName, SKU, Quantity, UnitPrice, LineTotal, TaxAmount)
                            VALUES (@SaleID, @VariantID, @ProductName, @SKU, @Quantity, @UnitPrice, @LineTotal, @TaxAmount)"

                        Using cmd As New SqlCommand(sqlDetail, conn, trans)
                            cmd.Parameters.AddWithValue("@SaleID", saleId)
                            cmd.Parameters.AddWithValue("@VariantID", item.VariantID)
                            cmd.Parameters.AddWithValue("@ProductName", item.ProductName)
                            cmd.Parameters.AddWithValue("@SKU", item.SKU)
                            cmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                            cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice)
                            cmd.Parameters.AddWithValue("@LineTotal", item.LineTotal)
                            cmd.Parameters.AddWithValue("@TaxAmount", item.TaxAmount)
                            cmd.ExecuteNonQuery()
                        End Using

                        ' Update stock (using Demo_Retail_Stock with ProductID)
                        Dim sqlStock As String = $"
                            UPDATE {GetTableName("Retail_Stock")}
                            SET Quantity = Quantity - @Quantity,
                                LastUpdated = GETDATE()
                            WHERE ProductID = @ProductID AND BranchID = @BranchID"

                        Using cmd As New SqlCommand(sqlStock, conn, trans)
                            cmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                            cmd.Parameters.AddWithValue("@ProductID", item.VariantID) ' VariantID is actually ProductID now
                            cmd.Parameters.AddWithValue("@BranchID", saleData.BranchID)
                            cmd.ExecuteNonQuery()
                        End Using

                        ' Get cost price for this item
                        Dim costPrice As Decimal = 0
                        Dim sqlGetCost As String = "SELECT TOP 1 ISNULL(CostPrice, 0) FROM Demo_Retail_Price WHERE ProductID = @ProductID AND BranchID = @BranchID ORDER BY EffectiveFrom DESC"
                        Using cmdCost As New SqlCommand(sqlGetCost, conn, trans)
                            cmdCost.Parameters.AddWithValue("@ProductID", item.VariantID)
                            cmdCost.Parameters.AddWithValue("@BranchID", saleData.BranchID)
                            Dim result = cmdCost.ExecuteScalar()
                            If result IsNot Nothing Then costPrice = Convert.ToDecimal(result)
                        End Using
                        
                        Dim totalCost As Decimal = costPrice * item.Quantity
                        
                        ' Log stock movement with cost
                        Dim sqlMovement As String = "
                            INSERT INTO dbo.StockMovements
                            (MaterialID, BranchID, MovementType, MovementDate, QuantityIn, QuantityOut, BalanceAfter, 
                             UnitCost, TotalValue, InventoryArea, ReferenceNumber, Notes, CreatedBy, CreatedDate)
                            VALUES (@ProductID, @BranchID, 'Sales', GETDATE(), 0, @QtyOut, 
                                    (SELECT ISNULL(QtyOnHand, 0) FROM Demo_Retail_Stock WHERE VariantID = @ProductID AND BranchID = @BranchID),
                                    @UnitCost, @TotalCost, 'Retail', @SaleNumber, 'POS Sale', @CashierID, GETDATE())"

                        Using cmd As New SqlCommand(sqlMovement, conn, trans)
                            cmd.Parameters.AddWithValue("@ProductID", item.VariantID)
                            cmd.Parameters.AddWithValue("@BranchID", saleData.BranchID)
                            cmd.Parameters.AddWithValue("@QtyOut", item.Quantity)
                            cmd.Parameters.AddWithValue("@UnitCost", costPrice)
                            cmd.Parameters.AddWithValue("@TotalCost", totalCost)
                            cmd.Parameters.AddWithValue("@SaleNumber", saleData.SaleNumber)
                            cmd.Parameters.AddWithValue("@CashierID", saleData.CashierID)
                            cmd.ExecuteNonQuery()
                        End Using
                        
                        ' Create Cost of Sales ledger entry (DR Cost of Sales, CR Inventory)
                        If totalCost > 0 Then
                            ' DR Cost of Sales
                            Dim sqlCOGS As String = "INSERT INTO GeneralLedger (BranchID, AccountID, TransactionDate, Description, Debit, Credit, ReferenceType, ReferenceID, CreatedBy, CreatedDate) " &
                                                   "VALUES (@BranchID, (SELECT AccountID FROM ChartOfAccounts WHERE AccountCode = '5000'), GETDATE(), @Description, @Amount, 0, 'Sale', @SaleID, @CashierID, GETDATE())"
                            Using cmdCOGS As New SqlCommand(sqlCOGS, conn, trans)
                                cmdCOGS.Parameters.AddWithValue("@BranchID", saleData.BranchID)
                                cmdCOGS.Parameters.AddWithValue("@Description", $"Cost of Sales - {item.ProductName}")
                                cmdCOGS.Parameters.AddWithValue("@Amount", totalCost)
                                cmdCOGS.Parameters.AddWithValue("@SaleID", saleId)
                                cmdCOGS.Parameters.AddWithValue("@CashierID", saleData.CashierID)
                                cmdCOGS.ExecuteNonQuery()
                            End Using
                            
                            ' CR Inventory
                            Dim sqlInv As String = "INSERT INTO GeneralLedger (BranchID, AccountID, TransactionDate, Description, Debit, Credit, ReferenceType, ReferenceID, CreatedBy, CreatedDate) " &
                                                  "VALUES (@BranchID, (SELECT AccountID FROM ChartOfAccounts WHERE AccountCode = '1200'), GETDATE(), @Description, 0, @Amount, 'Sale', @SaleID, @CashierID, GETDATE())"
                            Using cmdInv As New SqlCommand(sqlInv, conn, trans)
                                cmdInv.Parameters.AddWithValue("@BranchID", saleData.BranchID)
                                cmdInv.Parameters.AddWithValue("@Description", $"Inventory Reduction - {item.ProductName}")
                                cmdInv.Parameters.AddWithValue("@Amount", totalCost)
                                cmdInv.Parameters.AddWithValue("@SaleID", saleId)
                                cmdInv.Parameters.AddWithValue("@CashierID", saleData.CashierID)
                                cmdInv.ExecuteNonQuery()
                            End Using
                        End If
                    Next

                    ' Insert payment
                    Dim sqlPayment As String = $"
                        INSERT INTO {GetTableName("Payments")}
                        (SaleID, PaymentType, Amount, Reference)
                        VALUES (@SaleID, @PaymentType, @Amount, @Reference)"

                    Using cmd As New SqlCommand(sqlPayment, conn, trans)
                        cmd.Parameters.AddWithValue("@SaleID", saleId)
                        cmd.Parameters.AddWithValue("@PaymentType", saleData.TenderType)
                        cmd.Parameters.AddWithValue("@Amount", saleData.TotalAmount)
                        cmd.Parameters.AddWithValue("@Reference", If(saleData.PaymentReference, DBNull.Value))
                        cmd.ExecuteNonQuery()
                    End Using

                    trans.Commit()
                    Return saleId

                Catch ex As Exception
                    trans.Rollback()
                    Throw New Exception($"Error processing sale: {ex.Message}", ex)
                End Try
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Get categories for filtering
    ''' </summary>
    Public Function GetCategories() As List(Of String)
        Dim categories As New List(Of String)()
        
        Dim sql As String = $"
            SELECT DISTINCT Category 
            FROM {GetTableName("Retail_Product")} 
            WHERE IsActive = 1 
                AND Category IS NOT NULL
                AND (ProductType IN ('External', 'Internal') OR ProductType IS NULL)
            ORDER BY Category"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                conn.Open()
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        categories.Add(reader("Category").ToString())
                    End While
                End Using
            End Using
        End Using

        Return categories
    End Function
End Class

' Data transfer objects
Public Class SaleTransaction
    Public Property SaleNumber As String
    Public Property BranchID As Integer
    Public Property SaleDate As DateTime
    Public Property CustomerName As String
    Public Property Subtotal As Decimal
    Public Property TaxAmount As Decimal
    Public Property DiscountAmount As Decimal
    Public Property TotalAmount As Decimal
    Public Property TenderType As String
    Public Property CashierID As Integer
    Public Property PaymentReference As String
    Public Property Items As List(Of SaleItem)
End Class

Public Class SaleItem
    Public Property VariantID As Integer
    Public Property ProductName As String
    Public Property SKU As String
    Public Property Quantity As Decimal
    Public Property UnitPrice As Decimal
    Public Property LineTotal As Decimal
    Public Property TaxAmount As Decimal
End Class
