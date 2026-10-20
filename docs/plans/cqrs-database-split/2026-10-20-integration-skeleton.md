# Markdown skeleton tích hợp

runId: cqrs-db-split-20261020
taskId: Host-integration
skeletonRevision: 3
planRevision: 3
specRevision: 3
overviewRevision: 3

- Shared `public static class DatabaseConnectionSettings`: `GetWriteConnectionString(IConfiguration)`, `GetReadConnectionString(IConfiguration)`; private resolve(name) tìm explicit rồi legacy, không có throw.
- RedisCacheService private `CanUseReadCache` kết hợp EnableCache và !ReadModelSynchronization.Enabled, dùng trong hai GetOrCreate và ba Set. Bỏ qua Redis lấy trực tiếp factory/value; invalidate giữ nguyên.
- Stored procedure helper private `DatabaseConnectionString(bool forWrite=false)` delegating resolver. CandidateDoctor gọi true; hai query gọi mặc định false.
- Test harness xunit/Moq/SQLite .NET8 reference Infrastructure. Configuration tests dictionary IConfiguration + mock IDistributedCache, assert exact data và no cache interactions khi split, legacy cached data được giữ.
- Infrastructure `DatabaseServiceExtensions.AddClinicDatabases(IServiceCollection, IConfiguration)`: resolve connections; parse/validate target + flag; register write/read DbContext và bind options capture/worker theo contract T01/T02. SqlConnectionStringBuilder normalize server/catalog, thiếu catalog lỗi. Không open connection.
- Khi enabled và WriteConnection.MultipleActiveResultSets=true, throw lỗi cấu hình trước traffic. Sample không bật MARS. ReadConnection cần quyền DML/ALTER cho projector; Không có account chỉ đọc riêng trong scope này.
- CheckTokenQueryHandler constructor/field dùng VeterinaryClinicDataContext; test SQLite source-only user và thiếu userId, giữ mapping output. Dữ liệu hồ sơ và các query khác giữ replica eventual consistency.
- API `builder.Services.AddClinicDatabases(builder.Configuration)`; Hangfire write via Shared resolver. appsettings example explicit WriteConnection/ReadConnection cùng ReadModelSynchronization enabled/poll/batch.
- Flow: config -> validated DI -> SaveChanges capture (T01) -> background store/projector (T02) -> query read (host), cache bypass -> eventual read result. README map schema/bootstrap/monitor/rollback.

Tài liệu này chỉ mô tả signature/data flow; không chứa executable scaffolding.
