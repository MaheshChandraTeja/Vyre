using Vyre.App.Models;

namespace Vyre.App.Services;

public interface IDoctorService
{
    Task<DoctorStatus> GetStatusAsync(CancellationToken cancellationToken);
}