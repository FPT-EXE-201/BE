namespace FPT.EXE201.Application.IServices;

public interface IUserDataErasureService
{
    Task EraseUserPersonalDataAsync(Guid userId, CancellationToken ct = default);
}