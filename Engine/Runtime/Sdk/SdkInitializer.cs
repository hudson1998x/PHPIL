namespace PHPIL.Engine.Runtime.Sdk;

public static partial class SdkInitializer
{
    public static void Init()
    {
        InitStreams();
        InitSeeds();
        InitIncludeRequire();
        InitAutoload();
        InitStrings();
    }
}
