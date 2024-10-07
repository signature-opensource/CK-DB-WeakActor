using CK.Core;

namespace CK.DB.HZone.WeakActor;

/// <summary>
/// Package for HZone.WeakActor
/// </summary>
[SqlPackage( ResourcePath = "Res", ResourceType = typeof( Package ) )]
[Versions( "1.0.0" )]
// [SqlObjectItem("transform:vWeakActor")]
public abstract class Package : Zone.WeakActor.Package
{
    // void StObjConstruct( Zone.WeakActor.Package zone, Actor.WeakActor.Package weakActor ) { }

    public new WeakActorTable WeakActorTable => base.WeakActorTable as WeakActorTable;

}
