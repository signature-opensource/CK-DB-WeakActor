using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.Zone.WeakActor.SimpleNaming.Tests;

public class ZoneWeakActorSimpleNamingTests
{
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>();

    [Test]
    public void display_name_should_be_unique()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var sql = "select DisplayName from CK.vWeakActor";

            for( var i = 0; i < 100; i++ )
                Populate( context );

            var weakActors = context[WeakActorTable].Query<string>( sql );
            weakActors.Should().OnlyHaveUniqueItems();
        }
    }

    private void Populate( SqlStandardCallContext context )
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
        WeakActorTable.AddIntoGroup( context, 1, group, weakActorId );
    }
}
