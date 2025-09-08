# Test Azure SQL Database Connection
Write-Host "🔍 Testing KitchenWise Azure SQL Database Connection..." -ForegroundColor Cyan
Write-Host ""

# Test if API is running
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5196/api/pantry" -Method GET -ErrorAction Stop
    
    Write-Host "✅ SUCCESS! Connected to Azure SQL Database" -ForegroundColor Green
    Write-Host "📊 Database: $($response.databaseName)" -ForegroundColor Yellow
    Write-Host "📦 Total Items: $($response.totalItems)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🗃️ Pantry Items in Azure SQL Database:" -ForegroundColor Cyan
    
    if ($response.items -and $response.items.Count -gt 0) {
        foreach ($item in $response.items) {
            Write-Host "  • $($item.Name) - $($item.Quantity) $($item.Unit) ($($item.Category))" -ForegroundColor White
        }
    } else {
        Write-Host "  No items found in database" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🎉 Your Azure SQL Database is working perfectly!" -ForegroundColor Green
    Write-Host "   Database: kitchenwiseuser" -ForegroundColor Gray
    Write-Host "   Server: kitchenwise.database.windows.net" -ForegroundColor Gray
    
} catch {
    Write-Host "❌ ERROR: Could not connect to API or database" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Make sure the API is running with: dotnet run" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🌐 You can also test via Swagger UI at: http://localhost:5196/swagger" -ForegroundColor Cyan

