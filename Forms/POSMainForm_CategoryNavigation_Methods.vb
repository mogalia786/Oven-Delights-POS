''' <summary>
''' Category Navigation Methods - Add these to POSMainForm_REDESIGN.vb
''' These methods implement the Category -> SubCategory -> Product navigation flow
''' </summary>

' ============================================
' BREADCRUMB NAVIGATION
' ============================================
Private Sub Breadcrumb_Click(sender As Object, e As EventArgs)
    If _currentView = "products" Then
        ShowSubCategories(_currentCategoryId, _currentCategoryName)
    ElseIf _currentView = "subcategories" Then
        ShowCategories()
    End If
End Sub

' ============================================
' SHOW CATEGORIES (Main View)
' ============================================
Private Sub ShowCategories()
    _currentView = "categories"
    _currentCategoryId = 0
    _currentCategoryName = ""
    lblBreadcrumb.Text = "Categories"
    
    flpProducts.SuspendLayout()
    flpProducts.Controls.Clear()
    
    Try
        Dim categories = _categoryService.LoadCategories()
        
        For Each row As DataRow In categories.Rows
            Dim categoryId = CInt(row("CategoryID"))
            Dim categoryName = row("CategoryName").ToString()
            Dim productCount = CInt(row("ProductCount"))
            
            Dim btn = CreateCategoryTile(categoryId, categoryName, productCount)
            flpProducts.Controls.Add(btn)
        Next
        
    Catch ex As Exception
        MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    Finally
        flpProducts.ResumeLayout()
    End Try
End Sub

' ============================================
' SHOW SUBCATEGORIES
' ============================================
Private Sub ShowSubCategories(categoryId As Integer, categoryName As String)
    _currentView = "subcategories"
    _currentCategoryId = categoryId
    _currentCategoryName = categoryName
    lblBreadcrumb.Text = $"Categories > {categoryName}"
    
    flpProducts.SuspendLayout()
    flpProducts.Controls.Clear()
    
    Try
        Dim subcategories = _categoryService.LoadSubCategories(categoryId)
        
        If subcategories.Rows.Count = 0 Then
            ' No subcategories - load products directly from this category
            ShowProductsDirectlyFromCategory(categoryId, categoryName)
            Return
        Else
            For Each row As DataRow In subcategories.Rows
                Dim subCategoryId = CInt(row("SubCategoryID"))
                Dim subCategoryName = row("SubCategoryName").ToString()
                Dim productCount = CInt(row("ProductCount"))
                
                Dim btn = CreateSubCategoryTile(subCategoryId, subCategoryName, productCount)
                flpProducts.Controls.Add(btn)
            Next
        End If
        
    Catch ex As Exception
        MessageBox.Show($"Error loading subcategories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    Finally
        flpProducts.ResumeLayout()
    End Try
End Sub

' ============================================
' SHOW PRODUCTS DIRECTLY FROM CATEGORY (NO SUBCATEGORIES)
' ============================================
Private Sub ShowProductsDirectlyFromCategory(categoryId As Integer, categoryName As String)
    _currentView = "products"
    _currentSubCategoryId = 0
    _currentSubCategoryName = ""
    lblBreadcrumb.Text = $"Categories > {categoryName}"
    
    flpProducts.SuspendLayout()
    flpProducts.Controls.Clear()
    
    Try
        ' Load ALL products from this category (regardless of SubCategoryID)
        Dim sql As String = "
            SELECT 
                p.ProductID,
                p.SKU AS ProductCode,
                p.Name AS ProductName,
                ISNULL(
                    (SELECT TOP 1 SellingPrice FROM Demo_Retail_Price 
                     WHERE ProductID = p.ProductID AND BranchID = @BranchID 
                     ORDER BY EffectiveFrom DESC),
                    0
                ) AS SellingPrice,
                ISNULL(
                    (SELECT TOP 1 Quantity FROM RetailStock 
                     WHERE ProductID = p.ProductID AND BranchID = @BranchID),
                    0
                ) AS QtyOnHand
            FROM Demo_Retail_Product p
            WHERE p.CategoryID = @CategoryID
              AND p.IsActive = 1
              AND (p.ProductType = 'External' OR p.ProductType = 'Internal')
            ORDER BY p.Name"
        
        Dim products As New DataTable()
        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@CategoryID", categoryId)
                cmd.Parameters.AddWithValue("@BranchID", _branchID)
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(products)
                End Using
            End Using
        End Using
        
        If products.Rows.Count = 0 Then
            Dim lblNoProducts As New Label With {
                .Text = $"No products found in {categoryName} (CategoryID: {categoryId}, BranchID: {_branchID})",
                .Font = New Font("Segoe UI", 16, FontStyle.Italic),
                .ForeColor = _ironGold,
                .AutoSize = True,
                .Padding = New Padding(20)
            }
            flpProducts.Controls.Add(lblNoProducts)
        Else
            For Each row As DataRow In products.Rows
                Dim productId = CInt(row("ProductID"))
                Dim productName = row("ProductName").ToString()
                Dim price = If(IsDBNull(row("SellingPrice")), 0D, CDec(row("SellingPrice")))
                Dim stock = If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
                Dim productCode = row("ProductCode").ToString()
                
                Dim btn = CreateProductTileNew(productId, productCode, productName, price, stock)
                flpProducts.Controls.Add(btn)
            Next
        End If
        
    Catch ex As Exception
        MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    Finally
        flpProducts.ResumeLayout()
    End Try
End Sub

' ============================================
' SHOW PRODUCTS
' ============================================
Private Sub ShowProductsForSubCategory(subCategoryId As Integer, subCategoryName As String)
    _currentView = "products"
    _currentSubCategoryId = subCategoryId
    _currentSubCategoryName = subCategoryName
    lblBreadcrumb.Text = $"Categories > {_currentCategoryName} > {subCategoryName}"
    
    flpProducts.SuspendLayout()
    flpProducts.Controls.Clear()
    
    Try
        Dim products = _categoryService.LoadProducts(_currentCategoryId, subCategoryId, _branchID)
        
        If products.Rows.Count = 0 Then
            Dim lblNoProducts As New Label With {
                .Text = $"No products found in {subCategoryName}",
                .Font = New Font("Segoe UI", 16, FontStyle.Italic),
                .ForeColor = _ironGold,
                .AutoSize = True,
                .Padding = New Padding(20)
            }
            flpProducts.Controls.Add(lblNoProducts)
        Else
            For Each row As DataRow In products.Rows
                Dim productId = CInt(row("ProductID"))
                Dim productName = row("ProductName").ToString()
                Dim price = If(IsDBNull(row("SellingPrice")), 0D, CDec(row("SellingPrice")))
                Dim stock = If(IsDBNull(row("QtyOnHand")), 0D, CDec(row("QtyOnHand")))
                Dim productCode = row("ProductCode").ToString()
                
                Dim btn = CreateProductTileNew(productId, productCode, productName, price, stock)
                flpProducts.Controls.Add(btn)
            Next
        End If
        
    Catch ex As Exception
        MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    Finally
        flpProducts.ResumeLayout()
    End Try
End Sub

' ============================================
' CREATE CATEGORY TILE (Iron Man Red)
' ============================================
Private Function CreateCategoryTile(categoryId As Integer, categoryName As String, productCount As Integer) As Button
    Dim btn As New Button With {
        .Text = $"{categoryName}{vbCrLf}({productCount} items)",
        .Size = New Size(100, 70),
        .Font = New Font("Segoe UI", 10, FontStyle.Bold),
        .ForeColor = Color.White,
        .BackColor = _ironRed,
        .FlatStyle = FlatStyle.Flat,
        .Cursor = Cursors.Hand,
        .Tag = categoryId,
        .Margin = New Padding(5)
    }
    btn.FlatAppearance.BorderSize = 0
    
    AddHandler btn.Click, Sub() ShowSubCategories(categoryId, categoryName)
    AddHandler btn.MouseEnter, Sub() btn.BackColor = ControlPaint.Light(_ironRed, 0.2)
    AddHandler btn.MouseLeave, Sub() btn.BackColor = _ironRed
    
    Return btn
End Function

' ============================================
' CREATE SUBCATEGORY TILE (Iron Man Blue)
' ============================================
Private Function CreateSubCategoryTile(subCategoryId As Integer, subCategoryName As String, productCount As Integer) As Button
    Dim btn As New Button With {
        .Text = $"{subCategoryName}{vbCrLf}({productCount} items)",
        .Size = New Size(100, 70),
        .Font = New Font("Segoe UI", 10, FontStyle.Bold),
        .ForeColor = Color.White,
        .BackColor = _ironBlue,
        .FlatStyle = FlatStyle.Flat,
        .Cursor = Cursors.Hand,
        .Tag = subCategoryId,
        .Margin = New Padding(5)
    }
    btn.FlatAppearance.BorderSize = 0
    
    AddHandler btn.Click, Sub() ShowProductsForSubCategory(subCategoryId, subCategoryName)
    AddHandler btn.MouseEnter, Sub() btn.BackColor = ControlPaint.Light(_ironBlue, 0.2)
    AddHandler btn.MouseLeave, Sub() btn.BackColor = _ironBlue
    
    Return btn
End Function

' ============================================
' CREATE PRODUCT TILE (Iron Man Gold)
' ============================================
Private Function CreateProductTileNew(productId As Integer, productCode As String, productName As String, price As Decimal, stock As Decimal) As Button
    Dim btn As New Button With {
        .Text = $"{productName}{vbCrLf}R {price:N2}",
        .Size = New Size(100, 70),
        .Font = New Font("Segoe UI", 9, FontStyle.Bold),
        .ForeColor = _ironDark,
        .BackColor = _ironGold,
        .FlatStyle = FlatStyle.Flat,
        .Cursor = Cursors.Hand,
        .Tag = New With {productId, productCode, productName, price, stock},
        .Margin = New Padding(5)
    }
    btn.FlatAppearance.BorderSize = 0
    
    AddHandler btn.Click, Sub() AddProductToCartFromTile(productId, productCode, productName, price, stock)
    AddHandler btn.MouseEnter, Sub() btn.BackColor = ControlPaint.Light(_ironGold, 0.2)
    AddHandler btn.MouseLeave, Sub() btn.BackColor = _ironGold
    
    Return btn
End Function

' ============================================
' ADD PRODUCT TO CART FROM TILE
' ============================================
Private Sub AddProductToCartFromTile(productId As Integer, productCode As String, productName As String, price As Decimal, stock As Decimal)
    ' Check stock
    If stock <= 0 Then
        MessageBox.Show($"{productName} is out of stock!", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Return
    End If
    
    ' Add to cart with quantity 1 (or show quantity dialog)
    AddToCart(productId, productCode, productName, price, 1)
End Sub
