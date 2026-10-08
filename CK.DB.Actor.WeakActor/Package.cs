using CK.Core;

namespace CK.DB.Actor.WeakActor;

/// <summary>
/// Package for Actor.WeakActor. The weak actors are the actors of the "Weak" authentication provider
/// (see <see cref="WeakActorTable"/>).
/// </summary>
[SqlPackage( Schema = "CK", ResourcePath = "Res" )]
[Versions( "1.0.0, 1.1.0" )]
[SqlObjectItem( "transform:vGroupMember, transform:vUserAuthProvider, transform:sAuthUserInfoRead" )]
public abstract class Package : SqlPackage
{
    void StObjConstruct( Actor.Package actorPackage, Auth.Package authPackage ) { }
}
