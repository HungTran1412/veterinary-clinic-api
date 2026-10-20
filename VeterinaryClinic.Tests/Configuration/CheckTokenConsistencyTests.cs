using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using VeterinaryClinic.Business;
using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;
using Xunit;

namespace VeterinaryClinic.Tests.Configuration;

public class CheckTokenConsistencyTests
{
    // Kiểm tra xác thực đọc trực tiếp tài khoản vừa tạo tại Write DB trước khi replica kịp đồng bộ.
    [Fact]
    public async Task CheckToken_ReadsAuthoritativeUserWithoutReplica()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<VeterinaryClinicDataContext>().UseSqlite(connection).Options;
        await using var context = new VeterinaryClinicDataContext(options, null!);
        await context.Database.EnsureCreatedAsync();
        var user = new VcUsers
        {
            Username = "bacsi", FullName = "Bác sĩ mới", Email = "doctor@example.test", Role = "DOCTOR",
            Code = "BS01", Password = "hash", PhoneNumber = "0900000000", AvatarUrl = "avatar"
        };
        context.VcUsers.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var accessor = new Mock<IContextAccessor>();
        accessor.SetupGet(x => x.UserId).Returns(user.Id);
        var handler = new CheckTokenQueryHandler(() => accessor.Object, context);

        // Act
        var actual = await handler.Handle(new CheckTokenQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, actual.Id);
        Assert.Equal("DOCTOR", actual.Role);
        Assert.Equal("Bác sĩ mới", actual.FullName);
        Assert.Equal("bacsi", actual.UserName);
    }

    // Kiểm tra không có định danh người dùng trả null trước khi truy cập DB.
    [Fact]
    public async Task CheckToken_WithoutUserId_ReturnsNullWithoutOpeningDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<VeterinaryClinicDataContext>()
            .UseSqlite("Data Source=:memory:").Options;
        await using var context = new VeterinaryClinicDataContext(options, null!);
        var accessor = new Mock<IContextAccessor>();
        var handler = new CheckTokenQueryHandler(() => accessor.Object, context);

        // Act
        var actual = await handler.Handle(new CheckTokenQuery(), CancellationToken.None);

        // Assert
        Assert.Null(actual);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
