# Markdown implementation skeleton

runId: cqrs-db-split-20261020
taskId: T02-projection
skeletonRevision: 1
planRevision: 1
specRevision: 2
overviewRevision: 3

## Proposed files and signatures

Mọi file thuộc G01, worker /root/outbox_projection độc quyền.

| Path | Responsibility | Signature / contract |
| --- | --- | --- |
| Infrastructure/Synchronization/IReadModelProjectionStore.cs | Contract orchestration và row state | record ReadModelProjectionState(string EntityType, IReadOnlyDictionary<string,object?> KeyValues, IReadOnlyDictionary<string,object?>? Values); AcquireConsumerLockAsync(ct), ReadPendingAsync(int,ct), ReadCurrentStateAsync(ReadModelOutboxMessage,ct), ApplyAsync(IReadOnlyList<ReadModelProjectionState>,ct), MarkProcessedAsync(IReadOnlyList<ReadModelOutboxMessage>,ct) |
| Infrastructure/Synchronization/ReadModelOutboxProcessor.cs | Enforce lock/apply-before-ack | constructor(IReadModelProjectionStore,IOptions<ReadModelSynchronizationOptions>); Task<int> ProcessBatchAsync(CancellationToken) |
| Infrastructure/Synchronization/ReadModelProjectionSql.cs | Safe metadata commands, testable offline | constructor(DbContext); DbCommand CreateExists/CreateInsert/CreateUpdate/CreateDelete(ReadModelProjectionState); CreateIdentityInsert(IEntityType,bool); validates entity+keys/property values against context model, PK never UPDATE |
| Infrastructure/Synchronization/SqlServerReadModelConsumerLock.cs | Session application lock lifecycle | static Task<IAsyncDisposable?> AcquireAsync(DbConnection,Func<Task> closeConnection,CancellationToken); static DbCommand CreateAcquireCommand/CreateReleaseCommand(DbConnection), immutable resource constant; DisposeAsync release uncancelled+close always |
| Infrastructure/Synchronization/RelationalReadModelProjectionStore.cs | Source current-state/pending/ack; private replica transaction | constructor(VeterinaryClinicDataContext,DbContextOptions<VeterinaryClinicReadDataContext>); IReadModelProjectionStore + IDisposable/IAsyncDisposable; private replica owns no source; ApplyAsync SQLServer/SQLite only |
| Infrastructure/Synchronization/ReadModelSynchronizationWorker.cs | Scope per iteration, disabled/retry/cancel | constructor(IServiceScopeFactory,IOptions<ReadModelSynchronizationOptions>,ILogger<ReadModelSynchronizationWorker>); BackgroundService.ExecuteAsync(ct) |
| Infrastructure/Synchronization/ReadModelSynchronizationServiceCollectionExtensions.cs | Scoped/hosted DI registration | static IServiceCollection AddReadModelSynchronization(this IServiceCollection) |
| Tests/Synchronization/ProjectionProcessorTests.cs | Orchestration behavior | Mock store with strict sequence+observable ack counts/data; error/cancel/replay/no-lock/empty |
| Tests/Synchronization/ProjectionStoreTests.cs | Real isolated relational behavior | Two SQLite :memory: connections; source+replica schema test-only; identity/audit/current-state/composite/delete/rollback/invalid/pending-low-ID |
| Tests/Synchronization/ProjectionSqlTests.cs | SQLServer command safety+session lease errors | SQLServer options with inert connection string, inspect commands without opening DB; mock DbConnection/DbCommand for acquire/release/close+cancellation |
| Tests/Synchronization/ProjectionWorkerTests.cs | Disabled/retry/stop | Real DI scopes with mock store, NullLogger, bounded wait; no external DB |

## Data flow and test seams

Processor acquire lease; null/empty returns0. ReadCurrentState all messages before any target apply. Method validates business entity present in read model, exact non-null typed primary keys; detaches matching tracked business before FindAsync and detaches loaded entity after scalar extraction. Apply validates all projected states before target transaction; SQL builder gets identifiers exclusively EF mapping/provider delimiters and type-mapped parameters. Replica private instance uses same read options but no write capture/audit.

After state validation: target connection open -> transaction -> each tombstone DELETE or exists/upsert. Existing rows UPDATE only non-PK non-computed/non-rowversion scalar fields. Composite-only existing row no SQL UPDATE. New SQLServer identity row: ON -> INSERT -> OFF uncancelled. Maintain active identity table until OFF succeeds. Failure preserves original exception; rollback and dispose transaction before final OFF retry, then close/force-close session if cleanup fails. Commit batch completes before source ack. SaveChanges source changes only Outbox timestamps. All cleanup paths use uncancelled tokens and never hide original exception; poison/cancel stay pending.

Session helper accepts DbConnection + close callback to test exact lifecycle without SQLServer connections. Return -1 means busy/no work; other negative code throws. Acquisition failure closes, disposal release finally closes; release/close secondary error cannot replace an earlier operation error. Processor release failure must not mask target/ack original error: capture operation error and only propagate dispose error when no prior operation failed.

Worker uses IOptions.Enabled before creating scope. Each iteration resolves scoped processor; catches dependency/apply/ack errors then waits poll; cancellation exits. Host options validation bounds poll/batch and checks connections+MARS. Extension only registers types, no connection/schema calls.

## Review request

Xin host Skeleton1 approval và explicit G01 GroupPlan1 revalidation với skeleton1/spec2/plan1/overview3. Không thay contract hay acceptance so với Plan1/Spec2; chưa có executable scaffolding.
