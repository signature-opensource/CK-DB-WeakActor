using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Shouldly;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.Zone.WeakActor.Tests;

public class MoveZoneTests
{
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>();

    [Test]
    public void should_move_weak_actor_to_a_new_zone_in_a_perfect_world()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget );

            var zoneIdResult = context[WeakActorTable].QuerySingle<int>
            (
                "select ZoneId from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            zoneIdResult.ShouldBe( zoneIdTarget );

            var oldProfileResult = context[WeakActorTable].QuerySingle<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneIdTarget",
                new { weakActorId, zoneIdTarget }
            );
            oldProfileResult.ShouldBe( 1 );

            var newProfileResult = context[WeakActorTable].Query<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneId",
                new { weakActorId, zoneId }
            );
            newProfileResult.ShouldBeEmpty();

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );
        }
    }

    [Test]
    public void should_throw_when_moving_a_weak_actor_to_a_new_zone_where_name_clash()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );
            WeakActorTable.Create( context, 1, weakActorName, zoneIdTarget );

            Util.Invokable( () => WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget ) )
                          .ShouldThrow<SqlDetailedException>()
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*UK_CK_tWeakActor_WeakActorName_ZoneId.*" );
        }
    }

    [Test]
    public void should_not_throw_when_moving_a_weak_actor_with_a_new_unique_name()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );
            WeakActorTable.Create( context, 1, weakActorName, zoneIdTarget );

            var displayName = context[WeakActorTable].QuerySingle<string>
            (
                "select DisplayName from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget, newWeakActorName: displayName );
        }
    }

    [Test]
    public void should_throw_when_moving_a_weak_actor_to_a_new_zone_while_in_a_group()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var groupId = GroupTable.CreateGroup( context, 1, zoneId );
            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

            Util.Invokable( () => WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget, option: WeakActorZoneMoveOption.None ) )
                          .ShouldThrow<SqlDetailedException>()
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.IsInAGroup.*" );
        }
    }

    [Test]
    public void should_not_throw_when_moving_a_weak_actor_to_a_new_zone_while_in_a_group_with_option_1_intersect()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var groupId = GroupTable.CreateGroup( context, 1, zoneId );
            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget, option: WeakActorZoneMoveOption.Intersect );


            var zoneIdResult = context[WeakActorTable].QuerySingle<int>
            (
                "select ZoneId from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            zoneIdResult.ShouldBe( zoneIdTarget );

            var oldProfileResult = context[WeakActorTable].QuerySingle<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneIdTarget",
                new { weakActorId, zoneIdTarget }
            );
            oldProfileResult.ShouldBe( 1 );

            var newProfileResult = context[WeakActorTable].Query<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneId",
                new { weakActorId, zoneId }
            );
            newProfileResult.ShouldBeEmpty();

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );

            context[WeakActorTable].Query
                                   (
                                       "select * from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId",
                                       new { weakActorId, groupId }
                                   )
                                   .ShouldBeEmpty();
        }
    }
}
