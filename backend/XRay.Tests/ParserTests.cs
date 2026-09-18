using XRay.Parsers;

namespace XRay.Tests;

public class ParserTests
{
    [Fact]
    public void CSharpParser_ExtractsDependenciesRoutesAndTables()
    {
        var result = new CSharpParser().Parse("Controllers/OrdersController.cs", """
using Microsoft.AspNetCore.Mvc;
namespace Demo.Api;

[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public OrdersController(IOrderService service) { _service = service; }

    [HttpGet("{id}")]
    public object Get(int id) => _service.Get(id);
}
""");

        Assert.Contains(result.Nodes, node => node.ExternalKey == "CLASS:Demo.Api.OrdersController");
        Assert.Contains(result.Nodes, node => node.ExternalKey == "API:GET /api/orders/{id}");
        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.DependsOn && edge.TargetExternalKey == "CLASS:Demo.Api.IOrderService");
        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.Exposes && edge.TargetExternalKey == "CLASS:Demo.Api.OrdersController");
    }

    [Fact]
    public void TypeScriptParser_ExtractsRelativeImportsAndHttpCalls()
    {
        var result = new TypeScriptParser().Parse("src/pages/Orders.tsx", """
import { api } from '../api/client';
export function Orders() { return fetch('/api/orders', { method: 'POST' }); }
""");

        Assert.Contains(result.Nodes, node => node.ExternalKey == "MODULE:src/pages/Orders.tsx");
        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.Imports && edge.TargetExternalKey == "MODULE:src/api/client");
        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.Calls && edge.TargetExternalKey == "API:POST /api/orders");
    }

    [Fact]
    public void SqlParser_ExtractsTablesForeignKeysAndProcedures()
    {
        var result = new SqlParser().Parse("schema.sql", """
CREATE TABLE Orders (Id int);
GO
CREATE TABLE OrderLines (OrderId int, FOREIGN KEY (OrderId) REFERENCES Orders(Id));
GO
CREATE PROCEDURE GetOrders AS SELECT * FROM Orders;
""");

        Assert.Contains(result.Nodes, node => node.ExternalKey == "TABLE:Orders");
        Assert.Contains(result.Nodes, node => node.ExternalKey == "TABLE:OrderLines");
        Assert.Contains(result.Nodes, node => node.ExternalKey == "PROC:GetOrders");
        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.References && edge.TargetExternalKey == "TABLE:Orders");
    }

    [Fact]
    public void CSharpParser_NestedClassesDoNotCollideWithTopLevelClassesOfSameName()
    {
        var result = new CSharpParser().Parse("Demo.cs", """
namespace Alpha;

public class Outer
{
    public class ResponseModel { }
}

public class ResponseModel { }
""");

        var classKeys = result.Nodes
            .Select(node => node.ExternalKey)
            .Where(key => key.StartsWith("CLASS:", StringComparison.Ordinal))
            .OrderBy(key => key)
            .ToArray();

        Assert.Equal(new[]
        {
            "CLASS:Alpha.Outer",
            "CLASS:Alpha.Outer.ResponseModel",
            "CLASS:Alpha.ResponseModel"
        }, classKeys);
    }

    [Fact]
    public void CSharpParser_DoesNotTreatUnrelatedContextVariableAsDbContextAccess()
    {
        var result = new CSharpParser().Parse("Demo.cs", """
namespace Alpha;
public class DebugService
{
    public void Log()
    {
        var debugContext = GetContext();
        var value = debugContext.Payments;
    }
}
""");

        Assert.DoesNotContain(result.Edges, edge => edge.TargetExternalKey == "TABLE:Payments");
    }

    [Fact]
    public void CSharpParser_SubstitutesControllerAndActionRouteTokens()
    {
        var result = new CSharpParser().Parse("Controllers/OrdersController.cs", """
using Microsoft.AspNetCore.Mvc;
namespace Demo.Api;

[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet("[action]")]
    public object Summary() => null;
}
""");

        Assert.Contains(result.Nodes, node => node.ExternalKey == "API:GET /api/Orders/Summary");
    }

    [Fact]
    public void TypeScriptParser_ResolvesAxiosVerbAndMultiLineImport()
    {
        var result = new TypeScriptParser().Parse("src/pages/Orders.tsx", """
import {
  api
} from '../api/client';
export function submit() { return axios.post('/api/orders'); }
""");

        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.Imports && edge.TargetExternalKey == "MODULE:src/api/client");
        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.Calls && edge.TargetExternalKey == "API:POST /api/orders");
    }

    [Fact]
    public void TypeScriptParser_ResolvesDuplicateDirectoryNamesAndDynamicImports()
    {
        var result = new TypeScriptParser().Parse("src/src/nested.tsx", """
export const lazyPage = import('./lazy');
import { helper } from '../utils';
""");

        Assert.Contains(result.Edges, edge => edge.TargetExternalKey == "MODULE:src/src/lazy");
        Assert.Contains(result.Edges, edge => edge.TargetExternalKey == "MODULE:src/utils");
    }

    [Fact]
    public void SqlParser_QualifiesNonDboSchemaAndDetectsColumnLevelForeignKey()
    {
        var result = new SqlParser().Parse("schema.sql", """
CREATE TABLE [audit].[Users] (Id int);
GO
CREATE TABLE Orders (
    Id INT,
    CustomerId INT REFERENCES Customers(CustomerId)
);
""");

        Assert.Contains(result.Nodes, node => node.ExternalKey == "TABLE:audit.Users");
        Assert.Contains(result.Edges, edge => edge.EdgeTypeCode == GraphEdgeTypeCodes.References && edge.TargetExternalKey == "TABLE:Customers");
    }
}