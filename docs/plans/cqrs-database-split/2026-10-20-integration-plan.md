# Plan tích hợp

runId: cqrs-db-split-20261020
taskId: Host-integration
planRevision: 3
specRevision: 3
overviewRevision: 3

Chiến lược host chọn: contract-first; hạ tầng test và cấu hình/cache trước, wiring sau shared contract; không thêm feature ngoài overview. InvocationContext giữ mode=host-managed, role=host, decisionOwner=/root. Áp dụng writing-plans và backend-unit-test theo spec đã duyệt.

## G01 — Resolver, cache và test harness

groupPlanRevision: 1; groupResultRevision: 1.

Files: `VeterinaryClinic.Shared/Configurations/DatabaseConnectionSettings.cs` mới; `VeterinaryClinic.Business/StoredProcedures/VeterinaryClinicCallStoreHelperHandler.cs` sửa call routing; `VeterinaryClinic.Business/Services/CacheService/RedisCacheService.cs` thêm guard; `VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj` mới; `VeterinaryClinic.Tests/Configuration/DatabaseConnectionSettingsTests.cs`, `VeterinaryClinic.Tests/Configuration/ReadModelCacheTests.cs` mới; solution thêm test project.

1. Tạo harness chỉ dùng package cached; Xunit.Assert, Moq và relational SQLite isolated. Thêm test cả resolver/cache trước source, xác nhận regression cache đỏ do đọc cached value khi sync enabled.
2. Resolver static GetWriteConnectionString/GetReadConnectionString: string nonempty explicit ưu tiên, fallback legacy, thiếu throw InvalidOperationException tiếng Việt; không mở connection.
3. Chuyển medical record/dashboard dùng read, candidate doctor dùng write; guard GetOrCreate/Set tránh Redis khi enabled sync. Giữ invalidation cũ.
4. Chạy test riêng G01; inspect diff không chạm unrelated.

## G02 — Wiring, config mẫu và tài liệu

groupPlanRevision: 3; groupResultRevision: 1. Phụ thuộc G01 GroupResult + T01/T02 Spec/implementation contract.

Files: `VeterinaryClinic.Infrastructure/Configurations/DatabaseServiceExtensions.cs` mới; `VeterinaryClinic.API/Program.cs` wiring; `VeterinaryClinic.API/appsettings.json.example` split config; `VeterinaryClinic.Tests/Configuration/DatabaseServiceExtensionsTests.cs` mới; `README.md` hướng dẫn. Host artifact results cùng thư mục docs/plans.

Registration validate paired explicit strings, enabled split, different server/catalog using SqlConnectionStringBuilder, range poll/batch, options no SQL connection. Add read/write DbContext; AddReadModelSynchronization khi enabled; Hangfire use write. Sample flags bật vì sample dùng DB khác nhau; actual config legacy mặc định disabled. Test registration có thể resolve options/service descriptors mà không start host.

Revision 2 bổ sung validator WriteConnection MARS=true phải bị reject khi enabled; sample MARS=false/omit và docs ReadConnection cần DML/ALTER cho identity. Nội dung G01 không đổi, groupPlanRevision=1; G02 groupPlanRevision=2.

Revision 3 bổ sung G02 groupPlanRevision=3: host đổi `CheckTokenQueryHandler` sang write context và thêm `Configuration/CheckTokenConsistencyTests.cs` SQLite, trường hợp user chỉ có source và userId thiếu. Ownership host mở rộng đúng hai file này. Tài liệu DBA cần copy cả read stored procedures, không chỉ bảng/dữ liệu. D05/D06/D07 là host technical decisions giữ hành vi hiện có sau tách DB, không đổi public API.

Test/doc schema chính xác theo T01 và lỗi/retry theo T02. Run `dotnet test ... -m:1`, `dotnet build ... -m:1`, review UTF-8 và diff/migrations. Không runtime SQL Server test, không API launch. Failed/NotRun nêu đúng nguyên nhân, không đánh dấu Done nếu check bắt buộc còn fail.

## Risk và giới hạn

Source baseline Data build pass; solution build parallel trả exit1 không errors, dùng single-node để điều tra. No real SQL Server nên tests command generation/static review không thay integration deployment. Connection server aliases không thể xác minh offline; deployment phải bảo đảm hai DB vật lý khác nhau. Credentials target cần DML phục vụ projector.

## GroupResult G01 r1 / G02 r1

G01: observed cache regression 3 fail/1 pass trước sửa; sau resolver/cache sửa 8/8 pass. G02: source wiring/guard MARS/paired connections/Hangfire/stored procedure/check-token/cache/docs được readonly reviewer GREEN. Host final `dotnet build VeterinaryClinic.Solution.sln --no-restore --verbosity quiet -m:1 -p:WarningLevel=0` Passed, 0 errors/6 package-audit warnings; `dotnet test VeterinaryClinic.Tests/VeterinaryClinic.Tests.csproj --no-restore --verbosity quiet -m:1 -p:WarningLevel=0` outside sandbox Passed88 Failed0 Skipped0 duration7s (session45574). Real capture -> store -> processor -> ack giữ ID/audit và hội tụ insert/update/delete đã pass. Diff-check và strict UTF-8 51 files pass; migration/real config không đổi. AC01/05/06/07 fulfilled. SQLServer integration NotRun theo authorized scope; không required check còn pending. ExecutionRef HOST-FULL-TEST-20261020-45574, upstream overview3/spec3/plan3/skeleton3; source/test phù hợp approved intended behavior, thêm integration test hỗ trợ scenario hiện có không đổi acceptance.
