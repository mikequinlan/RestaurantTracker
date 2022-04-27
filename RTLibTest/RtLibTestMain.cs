using System.Runtime.CompilerServices;
using RTLib;

const string DBPath = "/Users/mquinlan/SQLite/RestaurantTracker/RestaurantTrackerDBTest.sqlite";

PrepDBFile();

using var rt = new RT(DBPath);
ClearDB();

ValidateUser();
ValidateGroup();
ValidateRestaurant();
ValidateUserGroup();
ValidateRestaurantGroup();
ValidateRestaurantVisit();
ValidatePickRestaurant();

ClearDB();
Console.WriteLine("Ok!");

void ClearDB()
{
    foreach (var (userId, _) in rt.GetUsers()) rt.DeleteUser(userId);
    foreach (var (restaurantId, _) in rt.GetRestaurants()) rt.DeleteRestaurant(restaurantId);
    foreach (var (groupId, _) in rt.GetGroups()) rt.DeleteGroup(groupId);

    Assert(rt.GetUsers().Count == 0);
    Assert(rt.GetRestaurants().Count == 0);
    Assert(rt.GetGroups().Count == 0);
    Assert(rt.GetRestaurantVisits().Count == 0);
}

void ValidateUser()
{
    rt.AddUser("mquinlan");
    var userId = rt.GetUserId("mquinlan");
    Assert(userId is not null);
    Assert(rt.UserExists("mquinlan"));
    Assert(!rt.UserExists("whoever"));

    rt.RenameUser(userId!.Value, "Michael Quinlan");
    Assert(userId == rt.GetUserId("Michael Quinlan"));
    Assert(rt.GetUserId("mquinlan") is null);
    Assert(!rt.UserExists("mquinlan"));
    Assert(rt.GetUsers().Count == 1);
}

void ValidateGroup()
{
    rt.AddGroup("Boise Tech Lunch");
    var groupId = rt.GetGroupId("Boise Tech Lunch");
    Assert(groupId is not null);
    Assert(rt.GroupExists("Boise Tech Lunch"));
    Assert(!rt.GroupExists("whatever"));

    rt.RenameGroup(groupId!.Value, "BTL");
    Assert(groupId == rt.GetGroupId("BTL"));
    Assert(rt.GetGroupId("Boise Tech Lunch") is null);
    Assert(!rt.GroupExists("Boise Tech Lunch"));
    Assert(rt.GetGroups().Count == 1);
}

void ValidateRestaurant()
{
    rt.AddRestaurant("Louie's Pizza & Italian Restaurant");
    rt.AddRestaurant("Amano");
    rt.AddRestaurant("Chili’s Grill & Bar");
    Assert(rt.GetRestaurants().Count == 3);

    var chilisRestaurantId = rt.GetRestaurantId("Chili’s Grill & Bar");
    Assert(chilisRestaurantId is not null);
    Assert(rt.RestaurantExists("Chili’s Grill & Bar"));
    Assert(!rt.RestaurantExists("some restaurant"));

    rt.RenameRestaurant(chilisRestaurantId!.Value, "Chili's");
    Assert(chilisRestaurantId == rt.GetRestaurantId("Chili's"));
    Assert(rt.GetRestaurantId("Chili’s Grill & Bar") is null);
    Assert(!rt.RestaurantExists("Chili’s Grill & Bar"));
    Assert(rt.GetRestaurants().Count == 3);
}

void ValidateUserGroup()
{
    var groupId1 = AddNewGroup("Group1");
    var groupId2 = AddNewGroup("Group2");
    var userId1 = AddNewUser("User1");
    var userId2 = AddNewUser("User2");
    rt.AddUserToGroup(userId1, groupId1);
    rt.AddUserToGroup(userId2, groupId1);
    rt.AddUserToGroup(userId1, groupId2);
    
    Assert(rt.IsUserMemberOf(userId1, groupId1));
    Assert(rt.IsUserMemberOf(userId1, groupId2));
    Assert(rt.IsUserMemberOf(userId2, groupId1));
    Assert(!rt.IsUserMemberOf(userId2, groupId2));
    
    Assert(rt.GetAllUsersInGroup(groupId1).Count == 2);
    Assert(rt.GetAllUsersInGroup(groupId2).Count == 1);
    
    Assert(rt.GetAllGroupsForUser(userId1).Count == 2);
    Assert(rt.GetAllGroupsForUser(userId2).Count == 1);
    
    rt.DeleteUserFromGroup(userId1, groupId1);
    Assert(!rt.IsUserMemberOf(userId1, groupId1));

    Assert(rt.GetAllUsersInGroup(groupId1).Count == 1);
    Assert(rt.GetAllUsersInGroup(groupId2).Count == 1);
    
    Assert(rt.GetAllGroupsForUser(userId1).Count == 1);
    Assert(rt.GetAllGroupsForUser(userId2).Count == 1);
}

void ValidateRestaurantGroup()
{
    var groupId1 = AddNewGroup("Group1");
    var groupId2 = AddNewGroup("Group2");
    var restaurantId1 = AddNewRestaurant("Restaurant1");
    var restaurantId2 = AddNewRestaurant("Restaurant2");
    rt.AddRestaurantToGroup(restaurantId1, groupId1);
    rt.AddRestaurantToGroup(restaurantId2, groupId1);
    rt.AddRestaurantToGroup(restaurantId1, groupId2);
    
    Assert(rt.IsRestaurantMemberOf(restaurantId1, groupId1));
    Assert(rt.IsRestaurantMemberOf(restaurantId1, groupId2));
    Assert(rt.IsRestaurantMemberOf(restaurantId2, groupId1));
    Assert(!rt.IsRestaurantMemberOf(restaurantId2, groupId2));
    
    Assert(rt.GetAllRestaurantsInGroup(groupId1).Count == 2);
    Assert(rt.GetAllRestaurantsInGroup(groupId2).Count == 1);
    
    Assert(rt.GetAllGroupsForRestaurant(restaurantId1).Count == 2);
    Assert(rt.GetAllGroupsForRestaurant(restaurantId2).Count == 1);
    
    rt.DeleteRestaurantFromGroup(restaurantId1, groupId1);
    Assert(!rt.IsRestaurantMemberOf(restaurantId1, groupId1));

    Assert(rt.GetAllRestaurantsInGroup(groupId1).Count == 1);
    Assert(rt.GetAllRestaurantsInGroup(groupId2).Count == 1);
    
    Assert(rt.GetAllGroupsForRestaurant(restaurantId1).Count == 1);
    Assert(rt.GetAllGroupsForRestaurant(restaurantId2).Count == 1);
}

void ValidateRestaurantVisit()
{
    ClearDB();

    var userIds = AddUsers(2);
    var restaurantIds = AddRestaurants(2);

    var date1 = new DateTime(2001, 1, 1);
    var date2 = new DateTime(2002, 2, 2);

    rt.AddRestaurantVisit(userIds[0], restaurantIds[0], 3600, 1, 1, date1);
    rt.AddRestaurantVisit(userIds[0], restaurantIds[1], 5, 5, 5, date2);
    rt.AddRestaurantVisit(userIds[1], restaurantIds[0], 7200, 2, 3, date1);
    
    Assert(rt.GetRestaurantVisits().Count == 3);
    Assert(rt.GetRestaurantVisitsForRestaurant(restaurantIds[0]).Count == 2);
    Assert(rt.GetRestaurantVisitsForRestaurant(restaurantIds[1]).Count == 1);
    Assert(rt.GetRestaurantVisitsByUser(userIds[0]).Count == 2);
    Assert(rt.GetRestaurantVisitsByUser(userIds[1]).Count == 1);
}

void ValidatePickRestaurant()
{
    ClearDB();

    var userIds = AddUsers(2);
    var groupIds = AddGroups(2);
    var restaurantIds = AddRestaurants(10);
    
    rt.AddUserToGroup(userIds[0], groupIds[0]);
    rt.AddUserToGroup(userIds[1], groupIds[1]);
    
    foreach (var restaurantId in restaurantIds) rt.AddRestaurantToGroup(restaurantId, groupIds[0]);
    
    var date1 = new DateTime(2001, 1, 1);
    var date2 = new DateTime(2002, 2, 2);
    rt.AddRestaurantVisit(userIds[0], restaurantIds[0], 0, 0, 0, date1);
    rt.AddRestaurantVisit(userIds[0], restaurantIds[2], 0, 0, 0, date2);

    var restaurantName1 = rt.PickRestaurant(groupIds[1]);
    Assert(restaurantName1 is null);

    var restaurantName2 = rt.PickRestaurant(groupIds[0]);
    Assert(restaurantName2 is not null);
}

long AddNewUser(string userName)
{
    rt.BeginTransaction();
    var userId = rt.GetUserId(userName);
    if (userId.HasValue) rt.DeleteUser(userId.Value);
    rt.AddUser(userName);
    userId = rt.GetUserId(userName);
    rt.CommitTransaction();
    return userId!.Value;
}

long AddNewRestaurant(string restaurantName)
{
    rt.BeginTransaction();
    var restaurantId = rt.GetRestaurantId(restaurantName);
    if (restaurantId.HasValue) rt.DeleteRestaurant(restaurantId.Value);
    rt.AddRestaurant(restaurantName);
    restaurantId = rt.GetRestaurantId(restaurantName);
    rt.CommitTransaction();
    return restaurantId!.Value;
}

long AddNewGroup(string groupName)
{
    rt.BeginTransaction();
    var groupId = rt.GetGroupId(groupName);
    if (groupId.HasValue) rt.DeleteGroup(groupId.Value);
    rt.AddGroup(groupName);
    groupId = rt.GetGroupId(groupName);
    rt.CommitTransaction();
    return groupId!.Value;
}

long[] AddUsers(int count)
{
    return Add().ToArray();
    IEnumerable<long> Add()
    {
        for (var n = 0; n < count; n++)
        {
            yield return AddNewUser($"User {n}");
        }
    }
}

long[] AddGroups(int count)
{
    return Add().ToArray();
    IEnumerable<long> Add()
    {
        for (var n = 0; n < count; n++)
        {
            yield return AddNewGroup($"Group {n}");
        }
    }
}

long[] AddRestaurants(int count)
{
    return Add().ToArray();
    IEnumerable<long> Add()
    {
        for (var n = 0; n < count; n++)
        {
            yield return AddNewRestaurant($"Restaurant {n}");
        }
    }
}

void Assert(bool cond, [CallerArgumentExpression("cond")] string? expr = null)
{
    if (!cond)
    {
        throw new Exception($"Error: '{expr}' is not True");
    }
}

void PrepDBFile()
{
    try
    {
        File.Delete(DBPath);
    }
    catch
    {
        // ignored
    }

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DBPath)!);
    }
    catch
    {
        // ignored
    }
}