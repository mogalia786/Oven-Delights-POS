Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient

''' <summary>
''' Service for Category -> SubCategory -> Product navigation
''' Maps to database views: v_POS_Categories, v_POS_SubCategories, v_POS_Products
''' </summary>
Public Class CategoryNavigationService
    Private ReadOnly _connectionString As String

    Public Sub New()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString").ConnectionString
    End Sub

    ''' <summary>
    ''' Load all categories with product counts
    ''' </summary>
    Public Function LoadCategories() As DataTable
        Dim dt As New DataTable()

        Dim sql As String = "
            SELECT DISTINCT
                c.CategoryID,
                c.CategoryName,
                c.DisplayOrder,
                COUNT(DISTINCT p.ProductID) AS ProductCount
            FROM Categories c
            LEFT JOIN Demo_Retail_Product p ON p.CategoryID = c.CategoryID 
                AND p.IsActive = 1
                AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'pest control')
                AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
            WHERE c.IsActive = 1
              AND LOWER(c.CategoryName) NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'pest control')
            GROUP BY c.CategoryID, c.CategoryName, c.DisplayOrder
            HAVING COUNT(DISTINCT p.ProductID) > 0
            ORDER BY c.DisplayOrder, c.CategoryName"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using

        Return dt
    End Function

    ''' <summary>
    ''' Load subcategories for a specific category
    ''' </summary>
    Public Function LoadSubCategories(categoryId As Integer) As DataTable
        Dim dt As New DataTable()

        Dim sql As String = "
            SELECT DISTINCT
                sc.SubCategoryID,
                sc.CategoryID,
                sc.SubCategoryName,
                sc.DisplayOrder,
                COUNT(DISTINCT p.ProductID) AS ProductCount
            FROM SubCategories sc
            INNER JOIN Demo_Retail_Product p ON p.SubCategoryID = sc.SubCategoryID 
                AND p.IsActive = 1
                AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
            WHERE sc.CategoryID = @CategoryID
              AND sc.IsActive = 1
            GROUP BY sc.SubCategoryID, sc.CategoryID, sc.SubCategoryName, sc.DisplayOrder
            HAVING COUNT(DISTINCT p.ProductID) > 0
            ORDER BY sc.DisplayOrder, sc.SubCategoryName"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@CategoryID", categoryId)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using

        Return dt
    End Function

    ''' <summary>
    ''' Load products for a specific subcategory and branch
    ''' Ordered by DisplayPriority (branch-specific) then alphabetically
    ''' </summary>
    Public Function LoadProducts(categoryId As Integer, subCategoryId As Integer, branchId As Integer) As DataTable
        Dim dt As New DataTable()

        Dim sql As String = "
            SELECT 
                p.ProductID,
                p.SKU AS ItemCode,
                p.Code,
                p.ProductCode,
                p.Name AS ProductName,
                p.Description,
                p.SKU,
                p.ProductID AS VariantID,
                ISNULL(p.Barcode, p.SKU) AS Barcode,
                ISNULL(price.SellingPrice, 0) AS SellingPrice,
                ISNULL(price.CostPrice, 0) AS CostPrice,
                ISNULL(p.CurrentStock, 0) AS QtyOnHand,
                0 AS ReorderLevel,
                c.CategoryName,
                sc.SubCategoryName,
                price.DisplayPriority
            FROM Demo_Retail_Product p
            INNER JOIN Categories c ON c.CategoryID = p.CategoryID
            INNER JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
            LEFT JOIN Demo_Retail_Price price ON price.ProductID = p.ProductID 
                AND price.BranchID = @BranchID
            WHERE p.CategoryID = @CategoryID
              AND p.SubCategoryID = @SubCategoryID
              AND p.BranchID = @BranchID
              AND p.IsActive = 1
              AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
            ORDER BY 
                CASE WHEN price.DisplayPriority IS NULL THEN 1 ELSE 0 END,
                price.DisplayPriority ASC,
                p.Name ASC"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@CategoryID", categoryId)
                cmd.Parameters.AddWithValue("@SubCategoryID", subCategoryId)
                cmd.Parameters.AddWithValue("@BranchID", branchId)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(dt)
                End Using
            End Using
        End Using

        Return dt
    End Function

    ''' <summary>
    ''' Search products across all categories by name or code
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
                ISNULL(p.ExternalBarcode, p.SKU) AS Barcode,
                ISNULL(price.SellingPrice, 0) AS SellingPrice,
                ISNULL(s.Quantity, 0) AS QtyOnHand,
                c.CategoryName,
                sc.SubCategoryName
            FROM Demo_Retail_Product p
            LEFT JOIN Categories c ON c.CategoryID = p.CategoryID
            LEFT JOIN SubCategories sc ON sc.SubCategoryID = p.SubCategoryID
            LEFT JOIN dbo.RetailStock s ON s.ProductID = p.ProductID 
                AND s.BranchID = @BranchID
            LEFT JOIN Demo_Retail_Price price ON price.ProductID = p.ProductID 
                AND price.BranchID = @BranchID
            WHERE p.BranchID = @BranchID
              AND p.IsActive = 1
              AND p.Category NOT IN ('ingredients', 'sub recipe', 'packaging', 'consumables', 'equipment', 'pest control')
              AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
              AND (
                  p.Name LIKE @Search 
                  OR p.ProductCode LIKE @Search 
                  OR p.SKU LIKE @Search 
                  OR ISNULL(p.ExternalBarcode, '') LIKE @Search
              )
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
End Class
