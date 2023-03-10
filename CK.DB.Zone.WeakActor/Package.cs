using CK.Core;

namespace CK.DB.Zone.WeakActor
{
    /// <summary>
    /// Package for Zone.WeakActor
    /// </summary>
    [SqlPackage( ResourcePath = "Res", ResourceType = typeof(Package))]
    [Versions( "1.0.0" )]
    public abstract class Package : CK.DB.Actor.WeakActor.Package
    {
        public new WeakActorTable WeakActorTable => base.WeakActorTable as WeakActorTable;
    }
}
