using Aegis.Application.Features.Users;
using Aegis.Contracts.Administration;
using Aegis.Infrastructure.Authorization;

namespace Aegis.UnitTests.Application.Features.Users;

[Trait("Category", "ApplicationTests")]
[Trait("Feature", "Users")]
public sealed class UserMutationUseCaseTests
{
    [Fact]
    public async Task Create_update_and_delete_preserve_tenant_scope_and_return_mutation_snapshot()
    {
        var store = new InMemoryRbacStore();
        var create = new CreateUserUseCase(store);
        var update = new UpdateUserUseCase(store);
        var delete = new DeleteUserUseCase(store);

        var created = await create.ExecuteAsync(
            "tenant-a",
            new CreateUserRequestDto("user:alice", "old@example.test", "Alice"));
        await create.ExecuteAsync(
            "tenant-b",
            new CreateUserRequestDto("user:alice", "other@example.test", "Other Alice"));

        var updated = await update.ExecuteAsync(
            "tenant-a",
            "user:alice",
            new UpdateUserRequestDto("new@example.test", "Alice Updated"));

        Assert.NotNull(updated);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal("new@example.test", updated.Email);
        Assert.Equal("Alice Updated", updated.DisplayName);
        Assert.True(await delete.ExecuteAsync("tenant-a", "user:alice"));
        Assert.False(await delete.ExecuteAsync("tenant-a", "user:alice"));
        Assert.NotNull(await store.GetUserAsync("tenant-b", "user:alice"));
    }

    [Fact]
    public async Task Mutations_reject_blank_tenant_before_persistence()
    {
        var store = new InMemoryRbacStore();
        var create = new CreateUserUseCase(store);

        await Assert.ThrowsAsync<ArgumentException>(() => create.ExecuteAsync(
            " ",
            new CreateUserRequestDto("user:alice", null, null)));

        Assert.Empty(await store.GetUsersAsync(" "));
    }

    [Fact]
    public async Task Update_returns_null_when_tenant_scoped_user_does_not_exist()
    {
        var useCase = new UpdateUserUseCase(new InMemoryRbacStore());

        var result = await useCase.ExecuteAsync(
            "tenant-a",
            "user:missing",
            new UpdateUserRequestDto("missing@example.test", null));

        Assert.Null(result);
    }
}
