using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.Zone.WeakActor.Tests;

public sealed class InternalZoneWeakActorTests
{
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>();

    [Test]
    public void should_throw_when_add_weak_actor_into_a_group_out_of_weak_actor_zone()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var weakActorZoneId = ZoneTable.CreateZone( context, 1 );
            var groupZoneId = ZoneTable.CreateZone( context, 1 );
            var groupId = GroupTable.CreateGroup( context, 1, groupZoneId );
            var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), weakActorZoneId );

            Util.Invokable( () => WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId ) )
                          .ShouldThrow<SqlDetailedException>();
        }
    }
}
