using CK.Core;
using CK.SqlServer;
using Shouldly;
using NUnit.Framework;
using System;
using System.Linq;
using static CK.Testing.MonitorTestHelper;
using Dapper;
using Microsoft.Data.SqlClient;
using static CK.DB.Zone.WeakActor.WeakActorZoneMoveOption;
using CK.Testing;

namespace CK.DB.Zone.WeakActor.Tests;

public class ZoneWeakActorTests
{
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>();

    [Test]
    public void create_twice_the_same_weak_actor_name_on_different_zone_should_not_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );

            var name = Guid.NewGuid().ToString();
            WeakActorTable.Create( context, 1, name, zoneId1 );
            Util.Invokable( () => WeakActorTable.Create( context, 1, name, zoneId2 ) )
                          .ShouldNotThrow( /*Was Should().NotThrow<SqlDetailedException>. */);
        }
    }

    [Test]
    public void should_be_unique_inside_a_zone()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var name = Guid.NewGuid().ToString();
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );
            WeakActorTable.Create( context, 1, name, zoneId1 );
            WeakActorTable.Create( context, 1, name, zoneId2 );
            Util.Invokable( () => WeakActorTable.Create( context, 1, name, zoneId1 ) )
                          .ShouldThrow<SqlDetailedException>();
        }
    }

    [Test]
    public void should_add_weak_actor_into_a_group_if_target_group_is_inside_weak_actor_zone()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var groupId = GroupTable.CreateGroup( context, 1, zoneId );
            var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );

            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );
        }
    }

    [Test]
    public void can_be_added_to_every_group_inside_zone()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var groupId1 = GroupTable.CreateGroup( context, 1, zoneId );
            var groupId2 = GroupTable.CreateGroup( context, 1, zoneId );
            var groupId3 = GroupTable.CreateGroup( context, 1, zoneId );
            var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );

            WeakActorTable.AddIntoGroup( context, 1, groupId1, weakActorId );
            WeakActorTable.AddIntoGroup( context, 1, groupId2, weakActorId );
            WeakActorTable.AddIntoGroup( context, 1, groupId3, weakActorId );
        }
    }

    [Test]
    public void name_and_zone_id_should_be_unique()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            WeakActorTable.Create( context, 1, weakActorName, zoneId );

            Util.Invokable( () => WeakActorTable.Create( context, 1, weakActorName, zoneId ) )
                          .ShouldThrow<SqlDetailedException>();
        }
    }

    [Test]
    public void two_weak_actors_can_be_added_to_the_same_zone()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActor1 = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );
            var weakActor2 = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );
            weakActor1.ShouldNotBe( weakActor2 );
        }
    }

    [Test]
    public void display_name_should_be_unique()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var sql = "select DisplayName from CK.vWeakActor";

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

            var weakActors = context[WeakActorTable].Query<string>( sql );
            weakActors.ShouldBeUnique();
        }
    }

    [Test]
    public void move_a_group_containing_a_weak_actor_with_option_0_none_should_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );

            var weakActorName1 = Guid.NewGuid().ToString();
            var weakActorName2 = Guid.NewGuid().ToString();

            var weakActorId1 = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );
            var weakActorId2 = WeakActorTable.Create( context, 1, weakActorName2, zoneId2 );

            var group = GroupTable.CreateGroup( context, 1, zoneId1 );

            WeakActorTable.AddIntoGroup( context, 1, group, weakActorId1 );

            Util.Invokable( () => GroupTable.MoveGroup( context, 1, group, zoneId2, GroupMoveOption.None ) )
                      .ShouldThrow<SqlDetailedException>()
                      .InnerException.ShouldBeOfType<SqlException>()
                      .Message.ShouldMatch( @".*Group\.UserNotInZone.*" );
        }
    }

    [Test]
    public void move_a_group_containing_a_weak_actor_with_option_1_intersect_should_not_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );

            var weakActorName = Guid.NewGuid().ToString();

            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId1 );

            var groupId = GroupTable.CreateGroup( context, 1, zoneId1 );

            var sqlCheckGroup =
            "select count(*) from CK.tActorProfile where GroupId=@groupId and ActorId=@weakActorId";
            var checkGroupParams = new { groupId, weakActorId };

            var checkGroupBefore = context[WeakActorTable].QuerySingle<int>( sqlCheckGroup, checkGroupParams );
            checkGroupBefore.ShouldBe( 0 );

            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );
            var checkGroup = context[WeakActorTable].QuerySingle<int>( sqlCheckGroup, checkGroupParams );
            checkGroup.ShouldBe( 1 );

            Util.Invokable( () => GroupTable.MoveGroup( context, 1, groupId, zoneId2, GroupMoveOption.Intersect ) )
                      .ShouldNotThrow();

            var checkGroupAfter = context[WeakActorTable].QuerySingle<int>( sqlCheckGroup, checkGroupParams );
            checkGroupAfter.ShouldBe( checkGroupBefore );

            context[WeakActorTable].QuerySingle<int>( """
                                       select count(*)
                                       from CK.tActorProfile
                                       where GroupId=@ZoneId
                                           and ActorId=@WeakActorId
                                       union
                                       select count(*)
                                       from CK.tWeakActor
                                       where WeakActorId=@WeakActorId
                                           and ZoneId=@ZoneId;
                                       """,
                                       new { ZoneId = zoneId2, WeakActorId = weakActorId } )
                                   .ShouldBe( 0 );

            context[WeakActorTable].QuerySingle<int>( """
                                       select count(*)
                                       from CK.tActorProfile
                                       where GroupId=@ZoneId
                                           and ActorId=@WeakActorId
                                       union
                                       select count(*)
                                       from CK.tWeakActor
                                       where WeakActorId=@WeakActorId
                                           and ZoneId=@ZoneId;
                                       """,
                                       new { ZoneId = zoneId1, WeakActorId = weakActorId } )
                                   .ShouldBe( 1 );
        }
    }

    [Test]
    public void move_a_group_containing_a_weak_actor_with_option_2_auto_user_registration_should_throw()
    {
        using( var context = new SqlStandardCallContext() )
        {
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );

            var weakActorName1 = Guid.NewGuid().ToString();
            var weakActorName2 = Guid.NewGuid().ToString();

            var weakActorId1 = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );
            var weakActorId2 = WeakActorTable.Create( context, 1, weakActorName2, zoneId2 );

            var group = GroupTable.CreateGroup( context, 1, zoneId1 );

            WeakActorTable.AddIntoGroup( context, 1, group, weakActorId1 );

            Util.Invokable( () => GroupTable.MoveGroup( context, 1, group, zoneId2, GroupMoveOption.AutoUserRegistration ) )
                      .ShouldThrow<SqlDetailedException>();
        }
    }

    [Test]
    public void move_group_which_is_weak_actor_zone_should_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );

            var weakActorName1 = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );

            var currentGroupId = context[WeakActorTable].QuerySingle<int>
            (
                "select GroupId from CK.tActorProfile where ActorId=@WeakActorId and GroupId!=@WeakActorId;",
                new { WeakActorId = weakActorId }
            );
            currentGroupId.ShouldBe( zoneId1 );

            Util.Invokable( () => GroupTable.MoveGroup( context, 1, zoneId1, zoneId2, GroupMoveOption.Intersect ) )
                      .ShouldThrow<SqlDetailedException>()
                      .InnerException.ShouldBeOfType<SqlException>()
                      .Message.ShouldMatch( @".*Zone\.CannotRemoveWeakActor.*" );

            var noGroupId = context[WeakActorTable].QuerySingle<int>
            (
                "select GroupId from CK.tActorProfile where ActorId=@WeakActorId and GroupId!=@WeakActorId",
                new { WeakActorId = weakActorId }
            );

            noGroupId.ShouldBe( currentGroupId );
        }
    }

    [Test]
    public void sZoneUserRemove_should_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );

            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            context[WeakActorTable].QuerySingle<int>
                                   (
                                       "select count(*) from CK.tActorProfile where ActorId=@weakActorId and GroupId=@GroupId;",
                                       new { weakActorId, GroupId = zoneId }
                                   )
                                   .ShouldBe( 1 );

            Util.Invokable( () => ZoneTable.RemoveUser( context, 1, zoneId, weakActorId ) )
                     .ShouldThrow<SqlDetailedException>();
        }
    }

    [Test]
    public void sGroupUserRemove_should_simply_remove_the_association_with_weak_actor_from_tActorProfile()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );

            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var groupId = GroupTable.CreateGroup( context, 1, zoneId );
            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

            context[WeakActorTable].QuerySingle<int>
                                   (
                                       "select count(*) from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId;",
                                       new { weakActorId, groupId }
                                   )
                                   .ShouldBe( 1 );

            GroupTable.RemoveUser( context, 1, groupId, weakActorId );

            context[WeakActorTable].QuerySingle<int>
                                   (
                                       "select count(*) from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId;",
                                       new { weakActorId, groupId }
                                   )
                                   .ShouldBe( 0 );
        }
    }

    [Test]
    public void sGroupUserRemove_which_target_a_zone_should_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );

            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            context[WeakActorTable].QuerySingle<int>
                                   (
                                       "select count(*) from CK.tActorProfile where ActorId=@weakActorId and GroupId=@GroupId;",
                                       new { weakActorId, GroupId = zoneId }
                                   )
                                   .ShouldBe( 1 );

            Util.Invokable( () => GroupTable.RemoveUser( context, 1, zoneId, weakActorId ) )
                      .ShouldThrow<SqlDetailedException>()
                      .InnerException.ShouldBeOfType<SqlException>();

            context[WeakActorTable].QuerySingle<int>
                                   (
                                       "select count(*) from CK.tActorProfile where ActorId=@weakActorId and GroupId=@GroupId;",
                                       new { weakActorId, GroupId = zoneId }
                                   )
                                   .ShouldBe( 1 );
        }
    }

    [Test]
    public void add_a_weak_actor_to_a_group_that_is_a_zone_should_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var groupZoneId = ZoneTable.CreateZone( context, 1 );

            // bypass WeakActor.OutOfZone
            GroupTable.MoveGroup( context, 1, groupZoneId, zoneId );

            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            Util.Invokable( () => WeakActorTable.AddIntoGroup( context, 1, groupZoneId, weakActorId ) )
                          .ShouldThrow<SqlDetailedException>()
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.AddToZoneForbidden.*" );
        }
    }
}
