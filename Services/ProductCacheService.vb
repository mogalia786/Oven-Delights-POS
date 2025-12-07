Imports System.Data
Imports System.Data.SqlClient
Imports System.Configuration

Public Class ProductCacheService
    Private Shared _instance As ProductCacheService
    Private Shared ReadOnly _lock As New Object()
    
    ' In-memory cache
    Private _allProducts As List(Of CachedProduct)
    Private _productsByCategory As Dictionary(Of Integer, List(Of CachedProduct))
    Private _productsBySubcategory As Dictionary(Of Integer, List(Of CachedProduct))
    Private _productsByBarcode As Dictionary(Of String, CachedProduct)
    Private _productsBySKU As Dictionary(Of String, CachedProduct)
    Private _categories As List(Of CachedCategory)
    Private _subcategories As List(Of CachedSubcategory)
    
    Private _connectionString As String
    Private _currentBranchID As Integer
    Private _isLoaded As Boolean = False
    Private _lastLoadTime As DateTime
    
    ' Singleton instance
    Public Shared ReadOnly Property Instance As ProductCacheService
        Get
            If _instance Is Nothing Then
                SyncLock _lock
                    If _instance Is Nothing Then
                        _instance = New ProductCacheService()
                    End If
                End SyncLock
            End If
            Return _instance
        End Get
    End Property
    
    Private Sub New()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString")?.ConnectionString
        _allProducts = New List(Of CachedProduct)
        _productsByCategory = New Dictionary(Of Integer, List(Of CachedProduct))
        _productsBySubcategory = New Dictionary(Of Integer, List(Of CachedProduct))
        _productsByBarcode = New Dictionary(Of String, CachedProduct)
        _productsBySKU = New Dictionary(Of String, CachedProduct)
        _categories = New List(Of CachedCategory)
        _subcategories = New List(Of CachedSubcategory)
    End Sub
    
    ''' <summary>
    ''' Load all products and categories into memory for the current branch
    ''' </summary>
    Public Sub LoadCache(branchID As Integer)
        SyncLock _lock
            Try
                _currentBranchID = branchID
                
                ' Clear existing cache
                _allProducts.Clear()
                _productsByCategory.Clear()
                _productsBySubcategory.Clear()
                _productsByBarcode.Clear()
                _productsBySKU.Clear()
                _categories.Clear()
                _subcategories.Clear()
                
                Using conn As New SqlConnection(_connectionString)
                    conn.Open()
                    
                    ' Load categories
                    LoadCategories(conn)
                    
                    ' Load subcategories
                    LoadSubcategories(conn)
                    
                    ' Load all products for this branch
                    LoadProducts(conn, branchID)
                End Using
                
                _isLoaded = True
                _lastLoadTime = DateTime.Now
                
                System.Diagnostics.Debug.WriteLine($"Cache loaded: {_allProducts.Count} products, {_categories.Count} categories, {_subcategories.Count} subcategories")
                
            Catch ex As Exception
                _isLoaded = False
                Throw New Exception("Failed to load product cache: " & ex.Message, ex)
            End Try
        End SyncLock
    End Sub
    
    Private Sub LoadCategories(conn As SqlConnection)
        Dim sql = "SELECT CategoryID, CategoryName, DisplayOrder FROM Categories WHERE IsActive = 1 ORDER BY DisplayOrder, CategoryName"
        Using cmd As New SqlCommand(sql, conn)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    _categories.Add(New CachedCategory With {
                        .CategoryID = CInt(reader("CategoryID")),
                        .CategoryName = reader("CategoryName").ToString(),
                        .DisplayOrder = If(IsDBNull(reader("DisplayOrder")), 0, CInt(reader("DisplayOrder")))
                    })
                End While
            End Using
        End Using
    End Sub
    
    Private Sub LoadSubcategories(conn As SqlConnection)
        Dim sql = "SELECT SubcategoryID, CategoryID, SubcategoryName, DisplayOrder FROM Subcategories WHERE IsActive = 1 ORDER BY DisplayOrder, SubcategoryName"
        Using cmd As New SqlCommand(sql, conn)
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    _subcategories.Add(New CachedSubcategory With {
                        .SubcategoryID = CInt(reader("SubcategoryID")),
                        .CategoryID = CInt(reader("CategoryID")),
                        .SubcategoryName = reader("SubcategoryName").ToString(),
                        .DisplayOrder = If(IsDBNull(reader("DisplayOrder")), 0, CInt(reader("DisplayOrder")))
                    })
                End While
            End Using
        End Using
    End Sub
    
    Private Sub LoadProducts(conn As SqlConnection, branchID As Integer)
        Dim sql = "
            SELECT 
                p.ProductID,
                p.SKU,
                p.Barcode,
                p.Name,
                p.CategoryID,
                p.SubCategoryID,
                p.ProductType,
                p.CurrentStock AS QtyOnHand,
                ISNULL(pr.SellingPrice, 0) AS SellingPrice,
                ISNULL(pr.CostPrice, 0) AS CostPrice,
                c.CategoryName,
                sc.SubcategoryName
            FROM Demo_Retail_Product p
            LEFT JOIN Demo_Retail_Price pr ON p.ProductID = pr.ProductID AND pr.BranchID = @branchID
            LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
            LEFT JOIN Subcategories sc ON p.SubCategoryID = sc.SubcategoryID
            WHERE p.IsActive = 1 AND p.BranchID = @branchID
            ORDER BY p.Name"
        
        Using cmd As New SqlCommand(sql, conn)
            cmd.Parameters.AddWithValue("@branchID", branchID)
            
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim product As New CachedProduct With {
                        .ProductID = CInt(reader("ProductID")),
                        .SKU = If(IsDBNull(reader("SKU")), "", reader("SKU").ToString()),
                        .Barcode = If(IsDBNull(reader("Barcode")), "", reader("Barcode").ToString()),
                        .Name = reader("Name").ToString(),
                        .CategoryID = If(IsDBNull(reader("CategoryID")), 0, CInt(reader("CategoryID"))),
                        .SubcategoryID = If(IsDBNull(reader("SubCategoryID")), 0, CInt(reader("SubCategoryID"))),
                        .ProductType = If(IsDBNull(reader("ProductType")), "", reader("ProductType").ToString()),
                        .QtyOnHand = CDec(reader("QtyOnHand")),
                        .SellingPrice = CDec(reader("SellingPrice")),
                        .CostPrice = CDec(reader("CostPrice")),
                        .CategoryName = If(IsDBNull(reader("CategoryName")), "", reader("CategoryName").ToString()),
                        .SubcategoryName = If(IsDBNull(reader("SubcategoryName")), "", reader("SubcategoryName").ToString()),
                        .TaxRate = 0D,
                        .ProductImage = Nothing
                    }
                    
                    ' Add to main list
                    _allProducts.Add(product)
                    
                    ' Index by category
                    If product.CategoryID > 0 Then
                        If Not _productsByCategory.ContainsKey(product.CategoryID) Then
                            _productsByCategory(product.CategoryID) = New List(Of CachedProduct)
                        End If
                        _productsByCategory(product.CategoryID).Add(product)
                    End If
                    
                    ' Index by subcategory
                    If product.SubcategoryID > 0 Then
                        If Not _productsBySubcategory.ContainsKey(product.SubcategoryID) Then
                            _productsBySubcategory(product.SubcategoryID) = New List(Of CachedProduct)
                        End If
                        _productsBySubcategory(product.SubcategoryID).Add(product)
                    End If
                    
                    ' Index by barcode
                    If Not String.IsNullOrEmpty(product.Barcode) Then
                        If Not _productsByBarcode.ContainsKey(product.Barcode) Then
                            _productsByBarcode(product.Barcode) = product
                        End If
                    End If
                    
                    ' Index by SKU
                    If Not String.IsNullOrEmpty(product.SKU) Then
                        If Not _productsBySKU.ContainsKey(product.SKU) Then
                            _productsBySKU(product.SKU) = product
                        End If
                    End If
                End While
            End Using
        End Using
    End Sub
    
    ''' <summary>
    ''' Get all categories
    ''' </summary>
    Public Function GetCategories() As List(Of CachedCategory)
        EnsureCacheLoaded()
        Return _categories
    End Function
    
    ''' <summary>
    ''' Get subcategories for a category
    ''' </summary>
    Public Function GetSubcategories(categoryID As Integer) As List(Of CachedSubcategory)
        EnsureCacheLoaded()
        Return _subcategories.Where(Function(s) s.CategoryID = categoryID).ToList()
    End Function
    
    ''' <summary>
    ''' Get products by category
    ''' </summary>
    Public Function GetProductsByCategory(categoryID As Integer) As List(Of CachedProduct)
        EnsureCacheLoaded()
        If _productsByCategory.ContainsKey(categoryID) Then
            Return _productsByCategory(categoryID)
        End If
        Return New List(Of CachedProduct)
    End Function
    
    ''' <summary>
    ''' Get products by subcategory
    ''' </summary>
    Public Function GetProductsBySubcategory(subcategoryID As Integer) As List(Of CachedProduct)
        EnsureCacheLoaded()
        If _productsBySubcategory.ContainsKey(subcategoryID) Then
            Return _productsBySubcategory(subcategoryID)
        End If
        Return New List(Of CachedProduct)
    End Function
    
    ''' <summary>
    ''' Search products by barcode
    ''' </summary>
    Public Function GetProductByBarcode(barcode As String) As CachedProduct
        EnsureCacheLoaded()
        If _productsByBarcode.ContainsKey(barcode) Then
            Return _productsByBarcode(barcode)
        End If
        Return Nothing
    End Function
    
    ''' <summary>
    ''' Search products by SKU
    ''' </summary>
    Public Function GetProductBySKU(sku As String) As CachedProduct
        EnsureCacheLoaded()
        If _productsBySKU.ContainsKey(sku) Then
            Return _productsBySKU(sku)
        End If
        Return Nothing
    End Function
    
    ''' <summary>
    ''' Search products by name
    ''' </summary>
    Public Function SearchProducts(searchText As String) As List(Of CachedProduct)
        EnsureCacheLoaded()
        If String.IsNullOrWhiteSpace(searchText) Then
            Return _allProducts
        End If
        
        Dim search = searchText.ToLower()
        Return _allProducts.Where(Function(p) p.Name.ToLower().Contains(search) OrElse p.SKU.ToLower().Contains(search) OrElse p.Barcode.ToLower().Contains(search)).ToList()
    End Function
    
    ''' <summary>
    ''' Get all products
    ''' </summary>
    Public Function GetAllProducts() As List(Of CachedProduct)
        EnsureCacheLoaded()
        Return _allProducts
    End Function
    
    ''' <summary>
    ''' Update stock quantity for a product (after sale)
    ''' </summary>
    Public Sub UpdateProductStock(productID As Integer, newQty As Decimal)
        SyncLock _lock
            Dim product = _allProducts.FirstOrDefault(Function(p) p.ProductID = productID)
            If product IsNot Nothing Then
                product.QtyOnHand = newQty
            End If
        End SyncLock
    End Sub
    
    ''' <summary>
    ''' Refresh cache from database
    ''' </summary>
    Public Sub RefreshCache()
        If _currentBranchID > 0 Then
            LoadCache(_currentBranchID)
        End If
    End Sub
    
    ''' <summary>
    ''' Clear cache
    ''' </summary>
    Public Sub ClearCache()
        SyncLock _lock
            _allProducts.Clear()
            _productsByCategory.Clear()
            _productsBySubcategory.Clear()
            _productsByBarcode.Clear()
            _productsBySKU.Clear()
            _categories.Clear()
            _subcategories.Clear()
            _isLoaded = False
        End SyncLock
    End Sub
    
    Private Sub EnsureCacheLoaded()
        If Not _isLoaded Then
            Throw New InvalidOperationException("Product cache not loaded. Call LoadCache(branchID) first.")
        End If
    End Sub
    
    Public ReadOnly Property IsLoaded As Boolean
        Get
            Return _isLoaded
        End Get
    End Property
    
    Public ReadOnly Property LastLoadTime As DateTime
        Get
            Return _lastLoadTime
        End Get
    End Property
    
    Public ReadOnly Property ProductCount As Integer
        Get
            Return _allProducts.Count
        End Get
    End Property
End Class

' Cache models
Public Class CachedProduct
    Public Property ProductID As Integer
    Public Property SKU As String
    Public Property Barcode As String
    Public Property Name As String
    Public Property CategoryID As Integer
    Public Property SubcategoryID As Integer
    Public Property ProductType As String
    Public Property QtyOnHand As Decimal
    Public Property SellingPrice As Decimal
    Public Property CostPrice As Decimal
    Public Property CategoryName As String
    Public Property SubcategoryName As String
    Public Property TaxRate As Double
    Public Property ProductImage As Byte()
End Class

Public Class CachedCategory
    Public Property CategoryID As Integer
    Public Property CategoryName As String
    Public Property DisplayOrder As Integer
End Class

Public Class CachedSubcategory
    Public Property SubcategoryID As Integer
    Public Property CategoryID As Integer
    Public Property SubcategoryName As String
    Public Property DisplayOrder As Integer
End Class
