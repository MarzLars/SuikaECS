namespace Suika.Scripts.Core
{
    /// <summary>
    /// Acts as a centralized provider for the auto-generated Cloud Code bindings.
    /// </summary>
    public class CloudBindingsProvider
    {
        public SuikaGameBindings SuikaGameBindings { get; } = new();
    }
}
