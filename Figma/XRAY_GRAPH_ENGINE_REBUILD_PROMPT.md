# PROJECT X-RAY — GRAPH ENGINE REBUILD IMPLEMENTATION PROMPT

## ROLE

You are the lead engineer responsible for rebuilding the Project X-Ray semantic graph-analysis engine.

You are working inside an existing .NET application. The application already has repository ingestion, branch handling, graph persistence, API models, and frontend integration. Your job is NOT to create a toy parser and NOT to merely patch the current parser.

Your goal is to replace the weak file-level/text-based graph generation with a production-quality, semantic, multi-language dependency-analysis engine that produces an accurate, explainable, branch-specific dependency graph.

The final graph must identify REAL relationships between files, namespaces, projects, classes, interfaces, methods, frontend components, API endpoints, repositories, database objects, and external dependencies.

---

# 1. PRIMARY OBJECTIVE

Rebuild the graph-analysis pipeline so that:

Repository
→ exact branch/commit snapshot
→ project discovery
→ language analysis
→ semantic symbol extraction
→ semantic relationship resolution
→ cross-language relationship resolution
→ canonical graph aggregation
→ graph validation
→ persistence
→ X-Ray blast-radius analysis

The engine must be deterministic wherever compiler/AST/static-analysis evidence is available.

DO NOT use an LLM as the primary mechanism for constructing factual graph relationships.

The LLM/Foundry layer may later consume the graph to explain impact, infer business meaning, summarize findings, and reason about user stories.

---

# 2. IMPORTANT CONTEXT FROM THE EXISTING IMPLEMENTATION

The old Python builder had an important architectural property:

- It walked the repository first.
- It collected all source files.
- It passed repository-wide source information into language parsers.
- Parser outputs were combined.
- Nodes and edges were then stored.
- Unresolved endpoints were materialized as explicit external/unparsed nodes instead of silently dropping relationships.
- Secrets were masked before graph materialization.

The current .NET IngestionService currently:

- resolves the repository provider,
- determines the branch,
- lists files,
- reads each file,
- invokes CSharpParser / TypeScriptParser / SqlParser one file at a time,
- merges ParsedNode results using last-writer-wins,
- aggregates ParsedEdge results,
- creates GraphSnapshot / GraphNode / GraphEdge records.

The current architecture is insufficient for high-quality semantic linking because parsing a file independently does not provide project-wide semantic information.

The new architecture must preserve the good existing ingestion behavior while introducing a proper analysis engine.

---

# 3. NON-NEGOTIABLE ARCHITECTURAL RULE

DO NOT put semantic analysis logic into IngestionService.

IngestionService must become an orchestration boundary.

Target shape:

IngestionService
    ↓
RepositorySnapshotService
    ↓
AnalysisEngine
    ├── ProjectDiscovery
    ├── CSharpSemanticAnalyzer
    ├── TypeScriptAnalyzer
    ├── SqlAnalyzer
    ├── DependencyInjectionResolver
    ├── ApiContractResolver
    ├── DatabaseAccessResolver
    ├── CrossLanguageResolver
    ├── GraphAggregator
    └── GraphValidator
    ↓
GraphRepository
    ↓
GraphSnapshot

IngestionService should NOT contain:

- AST traversal
- Roslyn semantic resolution
- TypeScript symbol resolution
- SQL relationship inference
- DI resolution
- API matching
- node merging logic
- relationship classification logic
- graph traversal logic

---

# 4. FIRST TASK — INSPECT THE EXISTING CODEBASE

Before changing code:

1. Inspect the entire existing backend structure.
2. Find:
   - IngestionService
   - CSharpParser
   - TypeScriptParser
   - SqlParser
   - ParsedNode
   - ParsedEdge
   - GraphNode
   - GraphEdge
   - ComponentTypeCodes
   - GraphEdgeTypeCodes
   - GraphSnapshot
   - CodeFile
   - Branch
   - Commit
   - Repository
   - persistence/repositories
   - graph APIs
   - graph DTOs
3. Find all existing tests.
4. Identify existing NuGet packages.
5. Identify target .NET version.
6. Identify how the solution/project files are currently discovered.
7. Identify whether MSBuildWorkspace is already available.
8. Identify how the current frontend expects graph nodes/edges.
9. Identify database constraints before changing the schema.

Do not blindly replace existing contracts.

Reuse existing persistence/API contracts when compatible.

If a contract is inadequate, introduce a compatibility layer or migrate it deliberately.

---

# 5. REQUIRED ENGINE PROJECT STRUCTURE

Create or reorganize the engine into a clean separation similar to:

XRay.Engine/
│
├── Abstractions/
│   ├── IAnalysisEngine.cs
│   ├── ILanguageAnalyzer.cs
│   ├── IRelationshipResolver.cs
│   ├── IGraphBuilder.cs
│   └── ISymbolIdentityProvider.cs
│
├── Models/
│   ├── RepositorySnapshot.cs
│   ├── SourceDocument.cs
│   ├── ProjectModel.cs
│   ├── SymbolNode.cs
│   ├── SemanticEdge.cs
│   ├── AnalysisResult.cs
│   └── AnalysisDiagnostic.cs
│
├── Discovery/
│   ├── RepositoryDiscovery.cs
│   ├── DotNetProjectDiscovery.cs
│   ├── TypeScriptProjectDiscovery.cs
│   └── SqlDiscovery.cs
│
├── CSharp/
│   ├── CSharpSolutionLoader.cs
│   ├── CSharpCompilationBuilder.cs
│   ├── CSharpSymbolExtractor.cs
│   ├── CSharpReferenceResolver.cs
│   ├── CSharpCallResolver.cs
│   ├── CSharpInheritanceResolver.cs
│   ├── CSharpInterfaceResolver.cs
│   ├── CSharpDiResolver.cs
│   ├── CSharpApiResolver.cs
│   └── CSharpAnalyzer.cs
│
├── TypeScript/
│   ├── TypeScriptProjectLoader.cs
│   ├── TypeScriptSymbolExtractor.cs
│   ├── TypeScriptImportResolver.cs
│   ├── ReactComponentResolver.cs
│   ├── HttpCallResolver.cs
│   └── TypeScriptAnalyzer.cs
│
├── Sql/
│   ├── SqlParser.cs
│   ├── SqlTableExtractor.cs
│   ├── SqlColumnExtractor.cs
│   ├── SqlProcedureExtractor.cs
│   └── SqlAnalyzer.cs
│
├── CrossLanguage/
│   ├── ApiContractResolver.cs
│   ├── FrontendBackendResolver.cs
│   ├── DatabaseAccessResolver.cs
│   └── CrossLanguageAnalyzer.cs
│
├── Graph/
│   ├── NodeAggregator.cs
│   ├── EdgeAggregator.cs
│   ├── GraphBuilder.cs
│   ├── GraphValidator.cs
│   └── GraphProjectionBuilder.cs
│
├── Identity/
│   ├── SymbolIdFactory.cs
│   └── PathNormalizer.cs
│
└── Analysis/
    ├── AnalysisPipeline.cs
    ├── AnalysisContext.cs
    └── AnalysisDiagnostics.cs

Adapt namespaces and project names to the existing solution.

---

# 6. REPOSITORY SNAPSHOT MUST BE FIRST-CLASS

Every analysis must be tied to:

- Project
- Repository
- Branch
- Commit
- Snapshot

Example:

CGOne
└── feature/payment-validation
    └── commit a81f32e

No graph data from another branch may contaminate the current branch graph.

Preserve the existing branch-isolated GraphSnapshot behavior.

If no commit is available, generate a deterministic content-based snapshot identifier.

The engine must support local repositories now and Azure DevOps repositories through the existing repository-provider abstraction.

---

# 7. PROJECT DISCOVERY

Before semantic parsing, discover:

## .NET

- .sln
- .slnx
- .csproj
- project references
- target frameworks
- assembly names
- namespaces
- NuGet/package references
- Directory.Build.props
- Directory.Build.targets
- global.json where relevant

## TypeScript / React

- package.json
- tsconfig.json
- tsconfig.*.json
- Vite configuration
- relevant bundler configuration
- aliases/path mappings
- source roots

## SQL

- .sql files
- migrations
- stored procedures
- views
- functions
- triggers where statically discoverable

Produce a ProjectModel representing the repository.

---

# 8. C# MUST USE SEMANTIC ANALYSIS

For C#, use Roslyn.

Do not implement the primary C# relationship engine using:

- regex
- string Contains()
- filename matching
- text search
- class-name similarity
- manually guessed relationships

Use Roslyn syntax trees + semantic models + compilations.

Prefer loading the real .sln/.csproj through the appropriate Roslyn/MSBuild workspace when possible.

The analyzer must resolve actual symbols.

---

# 9. C# SYMBOL EXTRACTION

Extract at minimum:

- assemblies
- projects
- namespaces
- classes
- structs
- records
- interfaces
- enums
- delegates
- methods
- constructors
- properties
- fields
- events
- parameters where useful
- local functions where useful
- attributes
- generic types
- generic methods

Each symbol must contain:

- canonical ID
- kind
- name
- fully qualified name
- containing symbol
- project
- assembly
- namespace
- source file
- start line
- end line
- signature where applicable
- language
- metadata

---

# 10. C# RELATIONSHIPS

Resolve these deterministically whenever possible:

- CONTAINS
- DECLARES
- REFERENCES
- CALLS
- INSTANTIATES
- INHERITS
- IMPLEMENTS
- OVERRIDES
- OVERRIDDEN_BY
- HAS_FIELD
- HAS_PROPERTY
- DEPENDS_ON_PROJECT
- DEPENDS_ON_PACKAGE
- USES_TYPE
- USES_ATTRIBUTE

Method calls must resolve to actual Roslyn symbols.

Example:

PaymentController.Pay()
    ↓ CALLS
IPaymentService.ProcessPayment()

Do not create an edge merely because the string "ProcessPayment" occurs in both files.

---

# 11. INTERFACE RESOLUTION

Resolve:

public class PaymentService : IPaymentService

into:

PaymentService
    └── IMPLEMENTS → IPaymentService

When:

IPaymentService service

is injected into:

PaymentController

and DI registration proves:

services.AddScoped<IPaymentService, PaymentService>();

the graph should represent:

PaymentController
    └── DI_DEPENDS_ON → IPaymentService

IPaymentService
    └── DI_BINDS → PaymentService

This allows X-Ray to traverse interface-based dependencies to concrete implementations.

---

# 12. DEPENDENCY INJECTION ANALYSIS

Implement a dedicated DI resolver.

Support common patterns such as:

- AddScoped
- AddTransient
- AddSingleton
- TryAddScoped
- TryAddTransient
- TryAddSingleton
- IServiceCollection extensions where statically resolvable
- constructor injection
- factory registrations where resolvable
- keyed services where practical
- common Microsoft.Extensions.DependencyInjection patterns

Do not assume every interface has exactly one implementation.

Represent ambiguity explicitly.

Example:

IPaymentService
    ↓ POSSIBLE_DI_BINDING
PaymentService
    ↓ confidence 1.0 if proven

If multiple implementations exist:

IPaymentService
    ├── POSSIBLE_DI_BINDING → PaymentService
    └── POSSIBLE_DI_BINDING → MockPaymentService

Do not falsely select one.

---

# 13. ASP.NET API ANALYSIS

Resolve ASP.NET Core endpoints.

Understand common patterns including:

- [ApiController]
- [Route]
- [HttpGet]
- [HttpPost]
- [HttpPut]
- [HttpPatch]
- [HttpDelete]
- minimal APIs where practical
- MapGet
- MapPost
- MapPut
- MapDelete
- route groups where practical
- controller route prefixes
- HTTP method attributes
- route templates
- controller/action conventions where statically determinable

Create API endpoint nodes.

Example:

POST /api/payment

must resolve to:

PaymentController.Pay()

Relationship:

POST /api/payment
    └── ROUTES_TO → PaymentController.Pay()

---

# 14. TYPESCRIPT / REACT ANALYSIS

Use a proper TypeScript parser/compiler-based approach.

Do not rely primarily on regex.

Resolve:

- imports
- exports
- re-exports
- modules
- functions
- classes
- interfaces
- types
- enums
- React components
- hooks
- component rendering
- module aliases
- relative paths
- tsconfig path mappings
- API calls
- fetch
- axios
- common HTTP wrappers

Example:

CheckoutPage.tsx
    ├── IMPORTS → paymentApi.ts
    └── RENDERS → PaymentForm

paymentApi.ts
    └── HTTP_CALL → POST /api/payment

---

# 15. FRONTEND HTTP CALL RESOLUTION

Detect common patterns:

fetch(...)
axios.get(...)
axios.post(...)
axios.put(...)
axios.patch(...)
axios.delete(...)

Also detect wrappers such as:

api.post(...)
httpClient.post(...)
request(...)
client.request(...)

when their definitions can be resolved.

Normalize URLs.

Support:

"/api/payment"
"/api/payment/{id}"
`${baseUrl}/api/payment`

Resolve base URL configuration where statically possible.

Create:

HTTP_CALL

from frontend operation to normalized API endpoint.

---

# 16. CROSS-LANGUAGE API CONTRACT RESOLUTION

This is critical.

Connect:

React/TypeScript HTTP call
        ↓
API endpoint
        ↓
ASP.NET controller/action
        ↓
service
        ↓
repository
        ↓
database

Example expected graph:

CheckoutPage.tsx
    ↓ IMPORTS
paymentApi.ts
    ↓ HTTP_CALL
POST /api/payment
    ↓ ROUTES_TO
PaymentController.Pay()
    ↓ CALLS
IPaymentService.ProcessPayment()
    ↓ DI_BINDS
PaymentService.ProcessPayment()
    ↓ CALLS
IPaymentRepository.Save()
    ↓ IMPLEMENTS
PaymentRepository.Save()
    ↓ WRITES
Payments

This cross-language chain is a core X-Ray requirement.

---

# 17. SQL ANALYSIS

Implement a real SQL parser/AST-based analyzer where possible.

Extract:

- databases where statically identifiable
- schemas
- tables
- views
- stored procedures
- functions
- triggers
- columns
- indexes where parser support allows
- foreign keys where discoverable
- SELECT usage
- INSERT usage
- UPDATE usage
- DELETE usage
- JOIN relationships
- table aliases
- column references

Create relationships:

READS
WRITES
USES_TABLE
USES_COLUMN
EXECUTES
DEPENDS_ON

Example:

PaymentRepository.Save()
    ↓ WRITES
Payments

PaymentRepository.GetPayment()
    ↓ READS
Payments

If a query explicitly references:

Payments.Amount

create a column-level relationship when reliable.

---

# 18. DATABASE ACCESS FROM C#

Resolve common database access patterns, including where practical:

- Entity Framework Core DbSet
- LINQ queries
- Include
- Select
- Where
- Add
- AddAsync
- Update
- Remove
- SaveChanges
- FromSql
- ExecuteSql
- Dapper
- raw SQL strings
- stored procedure calls

Do not claim column-level precision when static evidence does not support it.

Use confidence levels.

---

# 19. CANONICAL SYMBOL IDENTITY

Never use only random GUIDs as semantic identity.

Database IDs may remain GUIDs.

But every semantic symbol needs a stable ExternalKey.

Examples:

csharp:type:XRay.Services.PaymentService

csharp:method:XRay.Services.PaymentService.ProcessPayment(System.Decimal)

csharp:interface:XRay.Services.IPaymentService

api:POST:/api/payment

typescript:file:src/api/paymentApi.ts

typescript:function:src/api/paymentApi.ts:processPayment

sql:table:dbo.Payments

sql:column:dbo.Payments.Amount

The ID must be deterministic for the same symbol in the same repository.

Handle overloaded methods using signatures.

---

# 20. NODE AGGREGATION

Do NOT use last-writer-wins node merging.

The current pattern:

allNodes[key] = node

must be replaced with a NodeAggregator.

If multiple analyzers discover facts about the same symbol, merge them.

Example:

PaymentService

facts:
- class
- namespace
- file
- project
- implements IPaymentService
- methods
- attributes

All facts must survive aggregation.

---

# 21. EDGE AGGREGATION

Deduplicate edges using:

source semantic ID
+
target semantic ID
+
relationship type

But do not throw away useful evidence.

If the same relationship is discovered multiple times, aggregate evidence.

Example:

CALLS edge:
source = PaymentController.Pay()
target = PaymentService.ProcessPayment()
type = CALLS

evidence:
- file
- line
- syntax location
- resolver

Confidence should represent evidence quality.

---

# 22. CONFIDENCE MODEL

Use explicit confidence.

Suggested baseline:

1.00
Compiler/semantic-model proof

0.95
Explicit framework metadata/attribute proof

0.90
Explicit DI registration proof

0.85
Strong AST/static contract resolution

0.70
Strong convention-based resolution

0.50
Heuristic resolution

< 0.50
Do not materialize as a factual relationship unless clearly classified as inferred.

Separate:

- VERIFIED
- INFERRED
- UNKNOWN
- EXTERNAL

Never present inferred relationships as proven facts.

---

# 23. EDGE PROVENANCE

Every edge should retain, where possible:

- resolver name
- source file
- source line
- source column
- evidence snippet or normalized evidence
- confidence
- runtime/static classification
- analysis version

Example:

CALLS

source:
PaymentController.Pay()

target:
PaymentService.ProcessPayment()

resolver:
RoslynInvocationResolver

file:
Controllers/PaymentController.cs

line:
47

confidence:
1.0

This will later power X-Ray's "Traceable Evidence" UI.

---

# 24. EXTERNAL / UNRESOLVED NODES

Never silently discard unresolved endpoints.

If:

A → B

is discovered but B cannot be resolved, create:

B
type = EXTERNAL / UNKNOWN
resolved = false

Preserve the edge.

This follows a useful behavior from the previous Python builder.

Do not fabricate metadata for unresolved nodes.

---

# 25. GRAPH VALIDATION

Before persistence, validate:

- no dangling edge
- no duplicate semantic node
- no duplicate semantic edge
- no accidental self-loop unless explicitly allowed
- branch isolation
- snapshot isolation
- canonical ID validity
- relationship type validity
- source/target existence
- confidence range
- file path normalization
- line numbers valid when present
- external node classification

Produce diagnostics.

Example:

AnalysisDiagnostics:
- unresolved symbols
- unresolved API endpoints
- parse failures
- compilation failures
- missing project references
- ambiguous DI registrations
- unsupported constructs
- SQL parsing failures

---

# 26. GRAPH SHOULD BE MULTI-LEVEL

Do not create only a flat file graph.

Support hierarchy:

Repository
    ↓
Solution
    ↓
Project
    ↓
Assembly
    ↓
Namespace
    ↓
Type
    ↓
Method
    ↓
Statement / API / DB object

The UI can later project this into a simpler view.

The underlying graph should retain detailed semantic nodes.

---

# 27. GRAPH PROJECTION FOR UI

Do NOT reduce the internal graph merely because the UI would be too large.

Create a GraphProjectionBuilder.

Given a selected node:

PaymentService.ProcessPayment()

allow projection:

- depth 1
- depth 2
- depth 3

and relationship filters:

CALLS
IMPLEMENTS
DI_BINDS
READS
WRITES
ROUTES_TO
HTTP_CALL
IMPORTS

The UI receives a useful projection while the underlying graph remains detailed.

---

# 28. X-RAY BLAST RADIUS

Once the graph is accurate, implement reverse dependency traversal.

Given:

PaymentService.ProcessPayment()

find:

- direct callers
- indirect callers
- controllers
- API endpoints
- frontend callers
- database dependencies
- tests
- configuration dependencies

Classify:

CRITICAL
RISKY
SAFE
UNKNOWN

Do not mix graph construction and risk scoring.

Graph = facts.

X-Ray analysis = interpretation of those facts.

---

# 29. INCREMENTAL ANALYSIS

The architecture must support later incremental analysis.

For a new commit:

old snapshot
    ↓
git diff
    ↓
changed files
    ↓
changed symbols
    ↓
affected symbols
    ↓
reanalyze affected region
    ↓
new graph snapshot

Do not design the engine assuming the entire repository must always be reparsed forever.

However:

FIRST implementation may perform full analysis.

Build interfaces so incremental analysis can be added without rewriting the engine.

---

# 30. SECURITY / SECRET HANDLING

Preserve the old behavior:

Secrets must not enter the graph/vector context as raw values.

Mask secrets before analysis artifacts are persisted where appropriate.

Detect:

- connection strings
- API keys
- access tokens
- JWT secrets
- passwords
- private keys
- common secret patterns

Never log secret values.

---

# 31. PERFORMANCE REQUIREMENTS

Do not implement an O(files × files) comparison engine.

Use:

- project-level compilation
- symbol indexes
- dictionaries keyed by canonical symbol ID
- path indexes
- API indexes
- SQL object indexes
- import indexes
- DI binding indexes

Build indexes once and resolve references against them.

Parallelize only where thread safety is guaranteed.

Do not sacrifice semantic correctness for premature parallelism.

---

# 32. ERROR HANDLING

One broken file must not destroy the entire graph.

A parser/compiler failure should produce diagnostics and continue where possible.

Distinguish:

FAILED
PARTIAL
COMPLETED

Do not mark a graph as fully correct if compilation failed significantly.

---

# 33. TEST STRATEGY

Create unit + integration + golden graph tests.

At minimum create a synthetic fixture repository:

TestProject/
├── Web/
│   ├── Controllers/PaymentController.cs
│   └── Program.cs
├── Core/
│   ├── IPaymentService.cs
│   └── PaymentService.cs
├── Data/
│   ├── IPaymentRepository.cs
│   └── PaymentRepository.cs
├── Frontend/
│   ├── CheckoutPage.tsx
│   └── paymentApi.ts
└── Database/
    └── Payments.sql

Expected graph:

CheckoutPage.tsx
    ↓ IMPORTS
paymentApi.ts
    ↓ HTTP_CALL
POST /api/payment
    ↓ ROUTES_TO
PaymentController.Pay()
    ↓ CALLS
IPaymentService.ProcessPayment()
    ↓ DI_BINDS
PaymentService.ProcessPayment()
    ↓ CALLS
IPaymentRepository.Save()
    ↓ IMPLEMENTS
PaymentRepository.Save()
    ↓ WRITES
dbo.Payments

Tests must assert actual semantic IDs and relationship types.

---

# 34. GOLDEN TEST AGAINST THE OLD PYTHON ENGINE

If old Python output/test fixtures are available:

1. Run the old engine against fixture repositories.
2. Capture nodes and edges.
3. Run the new .NET engine.
4. Compare semantic relationships.
5. Do not blindly copy Python inaccuracies.
6. Preserve relationships proven correct by the old engine.
7. Add new relationships that Roslyn can prove.
8. Produce a compatibility report:

OLD_ONLY
NEW_ONLY
COMMON
CONFLICTING

The goal is not identical implementation.

The goal is equal or better semantic accuracy.

---

# 35. GRAPH QUALITY METRICS

Create measurable metrics.

At minimum:

- files discovered
- files parsed
- parse failure ratio
- symbols discovered
- verified edges
- inferred edges
- unresolved references
- unresolved APIs
- DI bindings
- API endpoints
- frontend HTTP calls
- SQL objects
- cross-language links
- duplicate nodes prevented
- duplicate edges prevented

Also calculate:

semantic_edge_ratio =
verified semantic edges / total edges

cross_language_resolution_ratio =
resolved cross-language links / detected cross-language candidates

These metrics should appear in diagnostics.

---

# 36. DO NOT ACCEPT THESE IMPLEMENTATIONS

Reject implementations that primarily use:

- Regex to identify C# method calls
- string Contains()
- filename matching
- class-name matching
- one-file-at-a-time parsing with no semantic linking
- random GUIDs as semantic identities
- last-writer-wins node merging
- generic USES edges for everything
- silently discarded unresolved references
- LLM-generated factual graph edges
- manually hardcoded project-specific relationships
- graph generation directly inside controller/service endpoints

---

# 37. BACKWARD COMPATIBILITY

The existing frontend and database should continue working where possible.

Map:

new SymbolNode
    ↓
existing GraphNode

new SemanticEdge
    ↓
existing GraphEdge

If the existing schema lacks fields for:

- evidence
- resolver
- qualified name
- symbol kind
- source column
- analysis version

add fields through a controlled migration.

Do not break existing branch/snapshot behavior.

---

# 38. DATABASE PERSISTENCE

The engine should not know Entity Framework details.

Use:

AnalysisEngine
    ↓
AnalysisResult
    ↓
GraphRepository
    ↓
EF Core
    ↓
MSSQL

Keep the engine domain-oriented.

---

# 39. API CONTRACT

Expose analysis status similar to:

POST /projects/{projectId}/ingestion

GET /projects/{projectId}/branches

GET /projects/{projectId}/graph

GET /projects/{projectId}/graph/nodes/{nodeId}

GET /projects/{projectId}/graph/nodes/{nodeId}/dependencies

GET /projects/{projectId}/graph/nodes/{nodeId}/dependents

GET /projects/{projectId}/graph/trace

Do not make the frontend know how Roslyn works.

---

# 40. IMPLEMENTATION ORDER

Implement in this exact sequence.

## Phase 1

Engine contracts and models.

Deliver:

- AnalysisContext
- RepositorySnapshot
- SourceDocument
- SymbolNode
- SemanticEdge
- AnalysisResult
- diagnostics
- canonical IDs

## Phase 2

Repository/project discovery.

Deliver:

- .sln discovery
- .csproj discovery
- TS project discovery
- SQL discovery

## Phase 3

Roslyn C# semantic engine.

Deliver:

- solution loading
- compilation
- symbols
- declarations
- references
- calls
- inheritance
- interfaces
- constructors
- properties
- fields

## Phase 4

DI resolver.

## Phase 5

ASP.NET API resolver.

## Phase 6

Node/edge aggregation and validation.

## Phase 7

TypeScript/React semantic analysis.

## Phase 8

Frontend → API resolver.

## Phase 9

SQL analyzer.

## Phase 10

Backend → SQL resolver.

## Phase 11

Complete cross-language graph.

## Phase 12

Graph projection API.

## Phase 13

Blast-radius traversal.

## Phase 14

Incremental analysis.

## Phase 15

Vector DB / Knowledge Graph integration.

## Phase 16

Foundry/LLM reasoning layer.

---

# 41. DEFINITION OF DONE

The rebuild is NOT complete when the code compiles.

It is complete only when:

1. A real .NET repository can be analyzed.
2. .sln/.csproj project relationships are detected.
3. C# classes/interfaces/methods are semantically resolved.
4. C# method calls resolve to actual symbols.
5. interface implementations are detected.
6. DI registrations are resolved where statically provable.
7. ASP.NET routes resolve to controller actions/minimal APIs.
8. TypeScript imports resolve.
9. React component relationships are detected.
10. frontend HTTP calls resolve.
11. frontend calls connect to backend API endpoints.
12. backend database operations connect to SQL objects where statically provable.
13. canonical node identities prevent duplicate semantic nodes.
14. edge types are meaningful and specific.
15. evidence is retained.
16. unresolved relationships remain explicitly unresolved/external.
17. graph validation catches corruption.
18. branch isolation works.
19. graph snapshots work.
20. existing frontend graph APIs continue working.
21. golden tests pass.
22. graph quality metrics are available.
23. X-Ray can traverse the graph to determine blast radius.
24. the graph is materially more detailed and semantically accurate than the current implementation.

---

# 42. EXPECTED FINAL RESULT

For a real application, the graph should look conceptually like:

Frontend:

CheckoutPage
    ↓ RENDERS
PaymentForm
    ↓ CALLS
paymentApi.processPayment()
    ↓ HTTP_CALL
POST /api/payment

Backend:

POST /api/payment
    ↓ ROUTES_TO
PaymentController.Pay()
    ↓ CALLS
IPaymentService.ProcessPayment()
    ↓ DI_BINDS
PaymentService.ProcessPayment()
    ↓ CALLS
IPaymentRepository.Save()
    ↓ IMPLEMENTS
PaymentRepository.Save()

Database:

PaymentRepository.Save()
    ↓ WRITES
dbo.Payments
    ↓ USES_COLUMN
dbo.Payments.Amount

Tests:

PaymentServiceTests
    ↓ TESTS
PaymentService.ProcessPayment()

Configuration:

Program.cs
    ↓ DI_BINDS
IPaymentService → PaymentService

This is the minimum quality bar for the new X-Ray engine.

---

# 43. WORKING STYLE

Do not attempt the entire implementation in one uncontrolled change.

Work phase-by-phase.

For every phase:

1. Inspect existing code.
2. Explain the intended change briefly.
3. Implement.
4. Compile.
5. Run relevant tests.
6. Show files changed.
7. Show important design decisions.
8. Show remaining limitations.
9. Do not move to the next phase until the current phase is functional.

When something cannot be resolved statically:

- do not fabricate it;
- record it as UNKNOWN/INFERRED/EXTERNAL;
- retain evidence explaining why.

Prioritize correctness and traceability over graph size.

---

# 44. CRITICAL PRINCIPLE

The goal is NOT:

"Generate as many graph edges as possible."

The goal is:

"Generate the most accurate, explainable dependency graph possible from deterministic source evidence."

A graph with 2,000 correct edges is better than a graph with 20,000 guessed edges.

The X-Ray product depends on the graph being trustworthy because blast-radius analysis, security analysis, user-story impact analysis, reports, and LLM reasoning will all depend on it.

Build the engine accordingly.
