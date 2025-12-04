namespace Hapvida.ExternalIntegration.UnitTest;

public class BaseTest : IDisposable
{
    public BaseTest()
    {
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
    }

    public virtual void Dispose()
    {
        // Implementação base do dispose
        GC.SuppressFinalize(this);
    }
}

