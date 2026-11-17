namespace Reconstruction.Application.Ports;

public sealed class UnrestrictedDisasterAccess : IDisasterAccess
{
    public IReadOnlyCollection<int>? AllowedDisasters => null;
}
