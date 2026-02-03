Imports System.Data
Imports System.Data.SqlClient
Imports System.Configuration

Public Class DayEndService
    Private ReadOnly _connectionString As String
    
    Public Sub New()
        _connectionString = ConfigurationManager.ConnectionStrings("OvenDelightsERPConnectionString")?.ConnectionString
    End Sub
    
    ''' <summary>
    ''' Check if all tills completed day-end for previous business day
    ''' Returns True if all complete, False if any incomplete
    ''' </summary>
    Public Function CheckPreviousDayComplete(ByRef incompleteTills As List(Of String)) As Boolean
        incompleteTills = New List(Of String)
        
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                ' Get yesterday's date
                Dim yesterday = DateTime.Today.AddDays(-1)
                
                ' Check for incomplete day-ends
                Dim sql = "
                    SELECT 'Till ' + CAST(tp.TillNumber AS NVARCHAR(10)) AS TillName, tp.TillPointID
                    FROM TillDayEnd tde
                    INNER JOIN TillPoints tp ON tde.TillPointID = tp.TillPointID
                    WHERE tde.BusinessDate = @Yesterday 
                    AND tde.IsDayEnd = 0"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Yesterday", yesterday)
                    
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            incompleteTills.Add(reader("TillName").ToString())
                        End While
                    End Using
                End Using
            End Using
            
            Return incompleteTills.Count = 0
            
        Catch ex As Exception
            Throw New Exception("Failed to check previous day completion: " & ex.Message, ex)
        End Try
    End Function
    
    ''' <summary>
    ''' Check if till is locked by ERP finalize (LockedByFinalize = 1 for today)
    ''' </summary>
    Public Function IsTillLocked(tillPointID As Integer) As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "
                    SELECT LockedByFinalize 
                    FROM TillDayEnd 
                    WHERE TillPointID = @TillPointID 
                    AND BusinessDate = @Today"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@TillPointID", tillPointID)
                    cmd.Parameters.AddWithValue("@Today", DateTime.Today)
                    
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        ' If LockedByFinalize = 1, till is locked
                        Return CBool(result) = True
                    End If
                End Using
            End Using
            
            Return False
            
        Catch ex As Exception
            Throw New Exception("Failed to check till lock status: " & ex.Message, ex)
        End Try
    End Function
    
    ''' <summary>
    ''' Check if current till has already completed day-end for today
    ''' </summary>
    Public Function IsTodayDayEndComplete(tillPointID As Integer) As Boolean
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "
                    SELECT IsDayEnd 
                    FROM TillDayEnd 
                    WHERE TillPointID = @TillPointID 
                    AND BusinessDate = @Today"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@TillPointID", tillPointID)
                    cmd.Parameters.AddWithValue("@Today", DateTime.Today)
                    
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        Return CBool(result)
                    End If
                End Using
            End Using
            
            Return False
            
        Catch ex As Exception
            Throw New Exception("Failed to check today's day-end status: " & ex.Message, ex)
        End Try
    End Function
    
    ''' <summary>
    ''' Create or update day-end record for today
    ''' </summary>
    Public Sub InitializeTodayDayEnd(tillPointID As Integer, cashierID As Integer, cashierName As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                ' Check if record exists
                Dim checkSql = "SELECT COUNT(*) FROM TillDayEnd WHERE TillPointID = @TillPointID AND BusinessDate = @Today"
                Using cmd As New SqlCommand(checkSql, conn)
                    cmd.Parameters.AddWithValue("@TillPointID", tillPointID)
                    cmd.Parameters.AddWithValue("@Today", DateTime.Today)
                    
                    Dim exists = CInt(cmd.ExecuteScalar()) > 0
                    
                    If Not exists Then
                        ' Create new record
                        Dim insertSql = "
                            INSERT INTO TillDayEnd (TillPointID, BusinessDate, CashierID, CashierName, IsDayEnd)
                            VALUES (@TillPointID, @Today, @CashierID, @CashierName, 0)"
                        
                        Using insertCmd As New SqlCommand(insertSql, conn)
                            insertCmd.Parameters.AddWithValue("@TillPointID", tillPointID)
                            insertCmd.Parameters.AddWithValue("@Today", DateTime.Today)
                            insertCmd.Parameters.AddWithValue("@CashierID", cashierID)
                            insertCmd.Parameters.AddWithValue("@CashierName", cashierName)
                            insertCmd.ExecuteNonQuery()
                        End Using
                    End If
                End Using
            End Using
            
        Catch ex As Exception
            Throw New Exception("Failed to initialize day-end record: " & ex.Message, ex)
        End Try
    End Sub
    
    ''' <summary>
    ''' Complete day-end for current till
    ''' </summary>
    Public Sub CompleteDayEnd(tillPointID As Integer, cashierID As Integer, 
                              totalSales As Decimal, totalCash As Decimal, totalCard As Decimal,
                              totalAccount As Decimal, totalRefunds As Decimal,
                              expectedCash As Decimal, actualCash As Decimal, notes As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "
                    UPDATE TillDayEnd 
                    SET IsDayEnd = 1,
                        DayEndTime = GETDATE(),
                        TotalSales = @TotalSales,
                        TotalCash = @TotalCash,
                        TotalCard = @TotalCard,
                        TotalAccount = @TotalAccount,
                        TotalRefunds = @TotalRefunds,
                        ExpectedCash = @ExpectedCash,
                        ActualCash = @ActualCash,
                        CashVariance = @ActualCash - @ExpectedCash,
                        PrintedReceipt = 1,
                        CompletedBy = @CashierID,
                        Notes = @Notes
                    WHERE TillPointID = @TillPointID 
                    AND BusinessDate = @Today"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@TillPointID", tillPointID)
                    cmd.Parameters.AddWithValue("@Today", DateTime.Today)
                    cmd.Parameters.AddWithValue("@CashierID", cashierID)
                    cmd.Parameters.AddWithValue("@TotalSales", totalSales)
                    cmd.Parameters.AddWithValue("@TotalCash", totalCash)
                    cmd.Parameters.AddWithValue("@TotalCard", totalCard)
                    cmd.Parameters.AddWithValue("@TotalAccount", totalAccount)
                    cmd.Parameters.AddWithValue("@TotalRefunds", totalRefunds)
                    cmd.Parameters.AddWithValue("@ExpectedCash", expectedCash)
                    cmd.Parameters.AddWithValue("@ActualCash", actualCash)
                    cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrEmpty(notes), DBNull.Value, CObj(notes)))
                    
                    Dim rowsAffected = cmd.ExecuteNonQuery()
                    If rowsAffected = 0 Then
                        Throw New Exception("Day-end record not found for today. Please contact support.")
                    End If
                End Using
            End Using
            
        Catch ex As Exception
            Throw New Exception("Failed to complete day-end: " & ex.Message, ex)
        End Try
    End Sub
    
    ''' <summary>
    ''' Supervisor override: Reset incomplete day-ends for previous day
    ''' </summary>
    Public Sub SupervisorResetPreviousDay(supervisorID As Integer, supervisorName As String)
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim yesterday = DateTime.Today.AddDays(-1)
                
                Dim sql = "
                    UPDATE TillDayEnd 
                    SET IsDayEnd = 1,
                        DayEndTime = GETDATE(),
                        CompletedBy = @SupervisorID,
                        Notes = 'Supervisor override by ' + @SupervisorName
                    WHERE BusinessDate = @Yesterday 
                    AND IsDayEnd = 0"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@Yesterday", yesterday)
                    cmd.Parameters.AddWithValue("@SupervisorID", supervisorID)
                    cmd.Parameters.AddWithValue("@SupervisorName", supervisorName)
                    
                    Dim rowsAffected = cmd.ExecuteNonQuery()
                    Debug.WriteLine($"Supervisor reset {rowsAffected} incomplete day-ends")
                End Using
            End Using
            
        Catch ex As Exception
            Throw New Exception("Failed to reset previous day: " & ex.Message, ex)
        End Try
    End Sub
    
    ''' <summary>
    ''' Get day-end summary for a specific date and till
    ''' </summary>
    Public Function GetDayEndSummary(tillPointID As Integer, businessDate As Date) As DataRow
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                
                Dim sql = "
                    SELECT tde.*, 'Till ' + CAST(tp.TillNumber AS NVARCHAR(10)) AS TillName
                    FROM TillDayEnd tde
                    INNER JOIN TillPoints tp ON tde.TillPointID = tp.TillPointID
                    WHERE tde.TillPointID = @TillPointID 
                    AND tde.BusinessDate = @BusinessDate"
                
                Using cmd As New SqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@TillPointID", tillPointID)
                    cmd.Parameters.AddWithValue("@BusinessDate", businessDate)
                    
                    Using adapter As New SqlDataAdapter(cmd)
                        Dim dt As New DataTable()
                        adapter.Fill(dt)
                        
                        If dt.Rows.Count > 0 Then
                            Return dt.Rows(0)
                        End If
                    End Using
                End Using
            End Using
            
            Return Nothing
            
        Catch ex As Exception
            Throw New Exception("Failed to get day-end summary: " & ex.Message, ex)
        End Try
    End Function
End Class
