"""Parser and linker tests against the demo application."""

from __future__ import annotations

from app.contracts.graph import NodeType, RelationshipType, node_id

CSHARP_NODES = [
    ("PaymentValidator", NodeType.SERVICE),
    ("IPaymentValidator", NodeType.INTERFACE),
    ("PaymentService", NodeType.SERVICE),
    ("IPaymentService", NodeType.INTERFACE),
    ("PaymentRepository", NodeType.CLASS),
    ("PaymentsController", NodeType.CLASS),
    ("OrderService", NodeType.SERVICE),
    ("OrdersController", NodeType.CLASS),
    ("UserService", NodeType.SERVICE),
    ("InventoryService", NodeType.SERVICE),
    ("CgOneDbContext", NodeType.CLASS),
]


def test_declared_types_are_nodes(demo_graph):
    store, _ = demo_graph
    for name, node_type in CSHARP_NODES:
        assert store.has_node(node_id(node_type, name)), f"missing node for {name}"


def test_di_registration_creates_binding(demo_graph):
    store, _ = demo_graph
    edges = [
        e
        for e in store.edges()
        if e.relationship is RelationshipType.BINDS
        and e.source == node_id(NodeType.INTERFACE, "IPaymentService")
        and e.target == node_id(NodeType.SERVICE, "PaymentService")
    ]
    assert edges, "IPaymentService -> PaymentService binding not found"
    assert any(e.rule_id == "csharp.di_registration" for e in edges)
    assert max(e.confidence for e in edges) >= 0.95


def test_constructor_injection_edges(demo_graph):
    store, _ = demo_graph
    edge = next(
        (
            e
            for e in store.edges()
            if e.source == node_id(NodeType.SERVICE, "PaymentService")
            and e.target == node_id(NodeType.INTERFACE, "IPaymentValidator")
            and e.rule_id == "csharp.ctor_injection"
        ),
        None,
    )
    assert edge is not None
    assert edge.source_file.endswith("PaymentService.cs")
    assert edge.line and edge.line > 0


def test_controller_exposes_api_endpoint(demo_graph):
    store, _ = demo_graph
    api = node_id(NodeType.API, "POST /api/payments")
    assert store.has_node(api)
    node = store.get_node(api)
    assert node.attributes.get("resolved") == "true"
    assert node.attributes.get("handler") == "PaymentsController.Create"
    assert any(
        e.target == node_id(NodeType.CLASS, "PaymentsController")
        and e.relationship is RelationshipType.EXPOSES
        for e in store.outgoing(api)
    )


def test_route_parameters_are_normalised(demo_graph):
    store, _ = demo_graph
    assert store.has_node(node_id(NodeType.API, "GET /api/payments/{}"))
    assert store.has_node(node_id(NodeType.API, "GET /api/users/{}"))


def test_ef_dbset_creates_table_nodes(demo_graph):
    store, _ = demo_graph
    table = node_id(NodeType.TABLE, "dbo.paymenttransactions")
    assert store.has_node(table)
    writes = [
        e
        for e in store.incoming(table)
        if e.source == node_id(NodeType.CLASS, "PaymentRepository")
    ]
    assert writes, "PaymentRepository should reference PaymentTransactions"
    assert any(e.relationship is RelationshipType.WRITES for e in writes)


def test_sql_foreign_keys(demo_graph):
    store, _ = demo_graph
    orders = node_id(NodeType.TABLE, "dbo.orders")
    users = node_id(NodeType.TABLE, "dbo.users")
    assert any(
        e.target == users and e.rule_id == "sql.foreign_key" for e in store.outgoing(orders)
    )


def test_typescript_imports_and_http_calls(demo_graph):
    store, _ = demo_graph
    page = node_id(NodeType.FRONTEND_COMPONENT, "CgOne.Demo.Web/src/pages/CheckoutPage.tsx")
    module = node_id(NodeType.FRONTEND_MODULE, "CgOne.Demo.Web/src/api/paymentApi.ts")
    assert store.has_node(page)
    assert store.has_node(module)
    assert any(
        e.target == module and e.relationship is RelationshipType.IMPORTS
        for e in store.outgoing(page)
    )
    assert any(
        e.target == node_id(NodeType.API, "POST /api/payments")
        and e.relationship is RelationshipType.CALLS
        for e in store.outgoing(module)
    )


def test_full_frontend_to_database_chain(demo_graph):
    """The chain that makes the whole product credible."""
    store, _ = demo_graph
    page = node_id(NodeType.FRONTEND_COMPONENT, "CgOne.Demo.Web/src/pages/CheckoutPage.tsx")
    table = node_id(NodeType.TABLE, "dbo.paymenttransactions")
    path = store.find_path(page, table, max_depth=12)
    assert path is not None, "CheckoutPage must reach PaymentTransactions"
    hops = [f"{e.source} -{e.relationship.value}-> {e.target}" for e in path]
    assert any("PaymentService" in hop for hop in hops), hops
    assert any("PaymentRepository" in hop for hop in hops), hops


def test_no_secret_leaks_into_graph(demo_graph):
    store, report = demo_graph
    assert report.files_failed == 0
    for node in store.nodes():
        assert "REDACTED" not in node.id
