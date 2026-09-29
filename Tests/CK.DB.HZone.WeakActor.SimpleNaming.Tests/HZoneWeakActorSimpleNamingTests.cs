using System;
using CK.Core;
using CK.DB.Zone;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.HZone.WeakActor.SimpleNaming.Tests;

public class HZoneWeakActorSimpleNamingTests
{
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>().ShouldNotBeNull();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>().ShouldNotBeNull();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>().ShouldNotBeNull();

    [Test]
    public void display_name_should_be_unique()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var sql = "select DisplayName from CK.vWeakActor";

            for( var i = 0; i < 100; i++ )
            {
                Populate( context );
            }

            var weakActors = context[WeakActorTable].Query<string>( sql );

            weakActors.ShouldBeUnique();
        }
    }

    void Populate( SqlStandardCallContext context )
    {
        var zoneId1 = ZoneTable.CreateZone( context, 1 );
        var zoneId2 = ZoneTable.CreateZone( context, 1 );
        var weakActorName1 = Guid.NewGuid().ToString();
        var weakActorName2 = Guid.NewGuid().ToString();
        var weakActorName3 = Guid.NewGuid().ToString();
        var weakActorId = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );
        WeakActorTable.Create( context, 1, weakActorName2, zoneId1 );
        WeakActorTable.Create( context, 1, weakActorName2, zoneId2 );
        WeakActorTable.Create( context, 1, weakActorName3, zoneId2 );
        var group = GroupTable.CreateGroup( context, 1, zoneId1 );
        GroupTable.AddMember( context, 1, group, weakActorId );
    }
}
