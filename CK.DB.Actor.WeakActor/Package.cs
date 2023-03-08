using CK.Core;

namespace CK.DB.Actor.WeakActor
{
    /// <summary>
    /// Package for Actor.WeakActor
    /// </summary>
    [SqlPackage( Schema = "CK", ResourcePath = "Res" )]
    [Versions( "1.0.0" )]
    public abstract class Package : SqlPackage
    {
    }
}
