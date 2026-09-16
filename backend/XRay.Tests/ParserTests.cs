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
}