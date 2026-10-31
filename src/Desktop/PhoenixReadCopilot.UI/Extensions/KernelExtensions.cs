using Microsoft.Extensions.DependencyInjection;
using RichasyKernel;

namespace PhoenixReadCopilot.UI.Extensions;

internal static class KernelExtensions
{
    /// <summary>
    /// Build the kernel.
    /// </summary>
    /// <param name="builder">Kernel builder.</param>
    /// <returns><see cref="Kernel"/>.</returns>
    /// <exception cref="InvalidOperationException">No services registered.</exception>
    public static Kernel Build(this IKernelBuilder builder)
    {
        return builder.Services is { Count: > 0 } services
            ? new Kernel(services.BuildServiceProvider())
            : throw new InvalidOperationException("No services registered.");
    }
}
