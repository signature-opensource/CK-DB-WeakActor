using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.Zone.WeakActor.Tests;

public class ZoneWeakActorTests
{
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>().ShouldNotBeNull();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>().ShouldNotBeNull();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>().ShouldNotBeNull();

    [Test]
     public async Task create_twice_the_same_weak_actor_name_on_different_zone_should_not_throw_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );

            var name = Guid.NewGuid().ToString();
            await Should.NotThrowAsync( async () =>
            {
                await WeakActorTable.CreateAsync( context, 1, name, zoneId1 );
                await WeakActorTable.CreateAsync( context, 1, name, zoneId2 );
            } );
        }
    }

    [Test]
     public async Task weak_actor_name_is_unique_inside_a_zone_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var name = Guid.NewGuid().ToString();
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );
            WeakActorTable.Create( context, 1, name, zoneId1 );
            WeakActorTable.Create( context, 1, name, zoneId2 );
            await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, name, zoneId1 ) );
        }
    }

    [Test]
     public async Task can_be_added_to_every_group_inside_zone_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var groupId1 = GroupTable.CreateGroup( context, 1, zoneId );
            var groupId2 = GroupTable.CreateGroup( context, 1, zoneId );
            var groupId3 = GroupTable.CreateGroup( context, 1, zoneId );
            var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );

            await GroupTable.AddMemberAsync( context, 1, groupId1, weakActorId );
            await GroupTable.AddMemberAsync( context, 1, groupId2, weakActorId );
            await GroupTable.AddMemberAsync( context, 1, groupId3, weakActorId );
        }
    }

    [Test]
     public async Task two_weak_actors_can_be_added_to_the_same_zone_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActor1 = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneId );
            var weakActor2 = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneId );
            weakActor1.ShouldNotBe( weakActor2 );
        }
    }

    [Test]
     public async Task display_name_should_be_unique_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId1 = ZoneTable.CreateZone( context, 1 );
            var zoneId2 = ZoneTable.CreateZone( context, 1 );
            var weakActorName1 = Guid.NewGuid().ToString();
            var weakActorName2 = Guid.NewGuid().ToString();
            var weakActorName3 = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName1, zoneId1 );
            await WeakActorTable.CreateAsync( context, 1, weakActorName2, zoneId1 );
            await WeakActorTable.CreateAsync( context, 1, weakActorName2, zoneId2 );
            await WeakActorTable.CreateAsync( context, 1, weakActorName3, zoneId2 );

            var group = GroupTable.CreateGroup( context, 1, zoneId1 );
            await GroupTable.AddMemberAsync( context, 1, group, weakActorId );

            var weakActors = context[WeakActorTable].Query<string>( "select DisplayName from CK.vWeakActor" );
            weakActors.ShouldBeUnique();
        }
    }


    [Test]
     public async Task ZoneRemoveMemberAsync_on_the_defining_Zone_should_throw_Async()
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

            await Should.ThrowAsync<SqlDetailedException>( () => ZoneTable.RemoveMemberAsync( context, 1, zoneId, weakActorId ) );
            await Should.ThrowAsync<SqlDetailedException>( () => GroupTable.RemoveMemberAsync( context, 1, zoneId, weakActorId ) );
        }
    }

    [Test]
     public async Task GroupMember_Add_Remove_just_work_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = await WeakActorTable.CreateAsync( context, 1, weakActorName, zoneId );

            var groupId = await GroupTable.CreateGroupAsync( context, 1, zoneId );
            await GroupTable.AddMemberAsync( context, 1, groupId, weakActorId );

            context[WeakActorTable].QuerySingle<int>
                                   (
                                       "select count(*) from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId;",
                                       new { weakActorId, groupId }
                                   )
                                   .ShouldBe( 1 );

            await GroupTable.RemoveMemberAsync( context, 1, groupId, weakActorId );

            context[WeakActorTable].QuerySingle<int>
                                   (
                                       "select count(*) from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId;",
                                       new { weakActorId, groupId }
                                   )
                                   .ShouldBe( 0 );
        }
    }

    [Test]
    public async Task add_a_weak_actor_to_a_group_that_is_not_in_its_defining_zone_should_throw_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var otherZoneId = await ZoneTable.CreateZoneAsync( context, 1 );
            await Should.ThrowAsync<SqlDetailedException>( () => GroupTable.AddMemberAsync( context, 1, otherZoneId, weakActorId ) );

            var otherGroupId = await GroupTable.CreateGroupAsync( context, 1, otherZoneId );
            await Should.ThrowAsync<SqlDetailedException>( () => GroupTable.AddMemberAsync( context, 1, otherGroupId, weakActorId ) );
        }
    }

    [TestCase( GroupMoveOption.None )]
    [TestCase( GroupMoveOption.Intersect )]
    [TestCase( GroupMoveOption.AutoUserRegistration )]
    public async Task moving_a_Group_to_another_Zone_always_removes_all_WeakActor_Async( GroupMoveOption option )
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneIdSource = ZoneTable.CreateZone( context, 1 );
            var groupId = await GroupTable.CreateGroupAsync( context, 1, zoneIdSource );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneIdSource );

            context[WeakActorTable].Query<int>( "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId", new { weakActorId, groupId } )
                .ShouldBeEmpty();

            await GroupTable.AddMemberAsync( context, 1, groupId, weakActorId );

            context[WeakActorTable].Query<int>( "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId", new { weakActorId, groupId } )
                .ShouldNotBeEmpty( "The WeakActor belongs to the group." );

            var zoneIdTarget = await ZoneTable.CreateZoneAsync( context, 1 );
            await GroupTable.MoveGroupAsync( context, 1, groupId, zoneIdTarget, option );

            context[WeakActorTable].Query<int>( "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId", new { weakActorId, groupId } )
                .ShouldBeEmpty( "The weak actor has been removed from the group." );
        }
    }

    [Test]
    public async Task should_throw_when_add_weak_actor_into_a_group_out_of_weak_actor_zone_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var weakActorZoneId = ZoneTable.CreateZone( context, 1 );
            var groupZoneId = ZoneTable.CreateZone( context, 1 );
            var groupId = GroupTable.CreateGroup( context, 1, groupZoneId );
            var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), weakActorZoneId );

            await Should.ThrowAsync<SqlDetailedException>( () => GroupTable.AddMemberAsync( context, 1, groupId, weakActorId ) );
        }
    }

}
