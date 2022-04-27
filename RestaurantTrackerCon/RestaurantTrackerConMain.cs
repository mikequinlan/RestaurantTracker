using System.Globalization;
using RTLib;

namespace RestaurantTrackerCon;

public static class RestaurantTrackerConMain
{
    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Usage();
            return;
        }

        // The first argument is the path to the database file
        var dbPath = args[0];
        
        try 
        {
            // Open the database
            using var rt = new RT(dbPath);
            rt.BeginTransaction();

            // Split out the command and additional parameters
            var cmd = args[1];
            var parms = args[2..];

            // Process the command...
            switch (cmd.ToLower())
            {
                case "add-user":
                    AddUser(rt, parms);
                    break;
                case "rename-user":
                    RenameUser(rt, parms);
                    break;
                case "delete-user":
                    DeleteUser(rt, parms);
                    break;
                case "add-restaurant":
                    AddRestaurant(rt, parms);
                    break;
                case "rename-restaurant":
                    RenameRestaurant(rt, parms);
                    break;
                case "delete-restaurant":
                    DeleteRestaurant(rt, parms);
                    break;
                case "add-group":
                    AddGroup(rt, parms);
                    break;
                case "rename-group":
                    RenameGroup(rt, parms);
                    break;
                case "delete-group":
                    DeleteGroup(rt, parms);
                    break;
                case "add-user-group":
                    AddUserGroup(rt, parms);
                    break;
                case "delete-user-group":
                    DeleteUserGroup(rt, parms);
                    break;
                case "add-restaurant-group":
                    AddRestaurantGroup(rt, parms);
                    break;
                case "delete-restaurant-group":
                    DeleteRestaurantGroup(rt, parms);
                    break;
                case "add-restaurant-visit":
                    AddRestaurantVisit(rt, parms);
                    break;
                case "pick-restaurant":
                    PickRestaurant(rt, parms);
                    break;
            }

            // Commit any changes and close the database
            rt.CommitTransaction();
        }
        catch (Exception ex)
        {
            // Unexpected error
            Console.WriteLine($"Error processing request: {ex}");
        }

        Console.WriteLine("Done");
    }

    #region User

    /// <summary>
    /// Add a User to the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void AddUser(RT rt, string[] parms)
    {
        // Check that we have 1 parameter (the User name)
        if (!CheckParmsCount(parms, 1)) return;

        var userName = parms[0];

        // Error if the User already exists
        if (rt.UserExists(userName))
        {
            Console.WriteLine($"User '{userName}' already exists in the database");
            return;
        }
        
        // Insert the User into the database
        rt.AddUser(userName);
        Console.WriteLine($"User '{userName}' is added to the database");
    }

    /// <summary>
    /// Rename a User in the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void RenameUser(RT rt, string[] parms)
    {
        // Check that we have 2 parameters, old User name and new User name
        if (!CheckParmsCount(parms, 2)) return;

        var oldUserName = parms[0];
        var newUserName = parms[1];

        // Error if the new name already exists
        if (rt.UserExists(newUserName))
        {
            Console.WriteLine($"User '{newUserName}' already exists in the database");
            return;
        }

        // Get the UserId for the user. Error if the user doesn't exist.
        var userId = rt.GetUserId(oldUserName);
        if (!userId.HasValue)
        {
            Console.WriteLine($"User '{oldUserName}' does not exist in the database");
            return;
        }
        
        // Update the database with the new name
        rt.RenameUser(userId.Value, newUserName);
        Console.WriteLine($"User '{oldUserName}' is renamed to '{newUserName}'");
    }

    /// <summary>
    /// Delete a User from the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void DeleteUser(RT rt, string[] parms)
    {
        // Check that we have 1 parameter, the User name
        if (!CheckParmsCount(parms, 1)) return;

        var userName = parms[0];

        // Get the UserId for the User. Error if the User doesn't exist
        var userId = rt.GetUserId(userName);
        if (!userId.HasValue)
        {
            Console.WriteLine($"User '{userName}' does not exist in the database");
            return;
        }

        // Delete the User from the database
        rt.DeleteUser(userId.Value);
        Console.WriteLine($"User '{userName}' is deleted from the database");
    }
    
    #endregion User

    #region Restaurant

    /// <summary>
    /// Add a Restaurant to the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void AddRestaurant(RT rt, string[] parms)
    {
        // Make sure there is one parameter, the Restaurant name
        if (!CheckParmsCount(parms, 1)) return;

        var restaurantName = parms[0];

        // Error if the Restaurant already exists in the database
        if (rt.RestaurantExists(restaurantName))
        {
            Console.WriteLine($"Restaurant '{restaurantName}' already exists in the database");
            return;
        }
        
        // Insert the Restaurant into the database
        rt.AddRestaurant(restaurantName);
        Console.WriteLine($"Restaurant '{restaurantName}' is added to the database");
    }

    /// <summary>
    /// Rename a Restaurant in the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void RenameRestaurant(RT rt, string[] parms)
    {
        // Make sure there are two parameters, the old Restaurant name and the new Restaurant name
        if (!CheckParmsCount(parms, 2)) return;

        var oldRestaurantName = parms[0];
        var newRestaurantName = parms[1];

        // Error if the new Restaurant name is already in the database
        if (rt.RestaurantExists(newRestaurantName))
        {
            Console.WriteLine($"Restaurant '{newRestaurantName}' already exists in the database");
            return;
        }

        // Get the RestaurantId. Error if the restaurant doesn't exist.
        var restaurantId = rt.GetRestaurantId(oldRestaurantName);
        if (!restaurantId.HasValue)
        {
            Console.WriteLine($"Restaurant '{oldRestaurantName}' does not exist in the database");
            return;
        }
        
        // Rename the Restaurant
        rt.RenameRestaurant(restaurantId.Value, newRestaurantName);
        Console.WriteLine($"Restaurant '{oldRestaurantName}' is renamed to '{newRestaurantName}'");
    }

    /// <summary>
    /// Delete a Restaurant from the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void DeleteRestaurant(RT rt, string[] parms)
    {
        // Make sure there is one parameter, the Restaurant name
        if (!CheckParmsCount(parms, 1)) return;

        var restaurantName = parms[0];

        // Get the RestaurantId. Error if the Restaurant doesn't exist.
        var restaurantId = rt.GetRestaurantId(restaurantName);
        if (!restaurantId.HasValue)
        {
            Console.WriteLine($"Restaurant '{restaurantName}' does not exist in the database");
            return;
        }

        // Delete the Restaurant from the database
        rt.DeleteRestaurant(restaurantId.Value);
        Console.WriteLine($"Restaurant '{restaurantName}' is deleted from the database");
    }
    
    #endregion Restaurant

    #region Group

    /// <summary>
    /// Add a Group to the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void AddGroup(RT rt, string[] parms)
    {
        // Make sure there is one parameter, the Group name
        if (!CheckParmsCount(parms, 1)) return;

        var groupName = parms[0];

        // Error if the Group is already in the database
        if (rt.GroupExists(groupName))
        {
            Console.WriteLine($"Group '{groupName}' already exists in the database");
            return;
        }
        
        // Add the Group to the database
        rt.AddGroup(groupName);
        Console.WriteLine($"Group '{groupName}' is added to the database");
    }

    /// <summary>
    /// Rename a Group in the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void RenameGroup(RT rt, string[] parms)
    {
        // Make sure there are 2 parameters, the old Group name and the new Group name
        if (!CheckParmsCount(parms, 2)) return;

        var oldGroupName = parms[0];
        var newGroupName = parms[1];

        // Error if the new Group already exists in the database
        if (rt.GroupExists(newGroupName))
        {
            Console.WriteLine($"Group '{newGroupName}' already exists in the database");
            return;
        }

        // Get the GroupId. Error if the Group doesn't exist
        var groupId = rt.GetGroupId(oldGroupName);
        if (!groupId.HasValue)
        {
            Console.WriteLine($"Group '{oldGroupName}' does not exist in the database");
            return;
        }
        
        // Rename the Group in the database
        rt.RenameGroup(groupId.Value, newGroupName);
        Console.WriteLine($"Group '{oldGroupName}' is renamed to '{newGroupName}'");
    }

    /// <summary>
    /// Delete a Group from the database
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void DeleteGroup(RT rt, string[] parms)
    {
        // Make sure there is 1 parameter, the Group name
        if (!CheckParmsCount(parms, 1)) return;

        var groupName = parms[0];

        // Get the GroupId. Error if the Group doesn't exist in the database.
        var groupId = rt.GetGroupId(groupName);
        if (!groupId.HasValue)
        {
            Console.WriteLine($"Group '{groupName}' does not exist in the database");
            return;
        }

        // Delete the Group from the database
        rt.DeleteGroup(groupId.Value);
        Console.WriteLine($"Group '{groupName}' is deleted from the database");
    }
    
    #endregion Group

    #region UserGroup

    /// <summary>
    /// Add a User to a Group
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void AddUserGroup(RT rt, string[] parms)
    {
        // Make sure there are 2 parameters, the User name and the Group name
        if (!CheckParmsCount(parms, 2)) return;

        var userName = parms[0];
        var groupName = parms[1];
        
        // Get the UserId. Error if the User doesn't exist.
        var userId = rt.GetUserId(userName);
        if (!userId.HasValue)
        {
            Console.WriteLine($"User '{userName}' does not exist in the database");
            return;
        }
        
        // Get the GroupId. Error if the Group doesn't exist.
        var groupId = rt.GetGroupId(groupName);
        if (!groupId.HasValue)
        {
            Console.WriteLine($"Group '{groupName}' does not exist in the database");
            return;
        }

        // Make sure the User isn't already a member of the Group.
        if (rt.IsUserMemberOf(userId.Value, groupId.Value))
        {
            Console.WriteLine($"User '{userName}' is already in Group '{groupName}'");
            return;
        }

        // Add the User to the Group.
        rt.AddUserToGroup(userId.Value, groupId.Value);
        Console.WriteLine($"User '{userName}' is added to Group '{groupName}'");
    }

    /// <summary>
    /// Delete a User from a Group
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void DeleteUserGroup(RT rt, string[] parms)
    {
        // Make sure there are 2 parameters, the User name and the Group name
        if (!CheckParmsCount(parms, 2)) return;

        var userName = parms[0];
        var groupName = parms[1];
        
        // Get the UserId. Error if the User doesn't exist.
        var userId = rt.GetUserId(userName);
        if (!userId.HasValue)
        {
            Console.WriteLine($"User '{userName}' does not exist in the database");
            return;
        }
        
        // Get the GroupId. Error if the Group doesn't exist.
        var groupId = rt.GetGroupId(groupName);
        if (!groupId.HasValue)
        {
            Console.WriteLine($"Group '{groupName}' does not exist in the database");
            return;
        }

        // Make sure the User is a member of the Group
        if (!rt.IsUserMemberOf(userId.Value, groupId.Value))
        {
            Console.WriteLine($"User '{userName}' is not in Group '{groupName}'");
            return;
        }

        // Remove the User from the Group
        rt.DeleteUserFromGroup(userId.Value, groupId.Value);
        Console.WriteLine($"User '{userName}' is deleted from Group '{groupName}'");
    }
    
    #endregion UserGroup

    #region RestaurantGroup

    /// <summary>
    /// Add a Restaurant to a Group
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void AddRestaurantGroup(RT rt, string[] parms)
    {
        // Make sure there are 2 parameters, the Restaurant name and the Group name.
        if (!CheckParmsCount(parms, 2)) return;

        var restaurantName = parms[0];
        var groupName = parms[1];
        
        // Get the RestaurantId. Error if the Restaurant doesn't exist.
        var restaurantId = rt.GetRestaurantId(restaurantName);
        if (!restaurantId.HasValue)
        {
            Console.WriteLine($"Restaurant '{restaurantName}' does not exist in the database");
            return;
        }
        
        // Get the GroupId. Error if the Group doesn't exist.
        var groupId = rt.GetGroupId(groupName);
        if (!groupId.HasValue)
        {
            Console.WriteLine($"Group '{groupName}' does not exist in the database");
            return;
        }

        // Make sure the Restaurant isn't already a member of the Group.
        if (rt.IsRestaurantMemberOf(restaurantId.Value, groupId.Value))
        {
            Console.WriteLine($"Restaurant '{restaurantName}' is already in Group '{groupName}'");
            return;
        }

        // Add the Restaurant to the Group
        rt.AddRestaurantToGroup(restaurantId.Value, groupId.Value);
        Console.WriteLine($"Restaurant '{restaurantName}' is added to Group '{groupName}'");
    }

    /// <summary>
    /// Delete a Restaurant from a Group
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void DeleteRestaurantGroup(RT rt, string[] parms)
    {
        // Make sure there are 2 parameters, the Restaurant name and the Group name
        if (!CheckParmsCount(parms, 2)) return;

        var restaurantName = parms[0];
        var groupName = parms[1];
        
        // Get the RestaurantId. Error if the Restaurant doesn't exist.
        var restaurantId = rt.GetRestaurantId(restaurantName);
        if (!restaurantId.HasValue)
        {
            Console.WriteLine($"Restaurant '{restaurantName}' does not exist in the database");
            return;
        }
        
        // Get the GroupId. Error if the Group doesn't exist.
        var groupId = rt.GetGroupId(groupName);
        if (!groupId.HasValue)
        {
            Console.WriteLine($"Group '{groupName}' does not exist in the database");
            return;
        }

        // Make sure the Restaurant is a member of the Group
        if (!rt.IsRestaurantMemberOf(restaurantId.Value, groupId.Value))
        {
            Console.WriteLine($"Restaurant '{restaurantName}' is not in Group '{groupName}'");
            return;
        }

        // Remove the Restaurant from the Group
        rt.DeleteRestaurantFromGroup(restaurantId.Value, groupId.Value);
        Console.WriteLine($"Restaurant '{restaurantName}' is deleted from Group '{groupName}'");
    }
    
    #endregion RestaurantGroup

    /// <summary>
    /// Record a Visit to a Restaurant
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void AddRestaurantVisit(RT rt, string[] parms)
    {
        // Make sure there are 5 or 6 parameters (User name, Restaurant name, Wait Time, Staff Rating, Food Rating and optionally Date)
        if (!CheckParmsCount(parms, 5, 6)) return;

        var userName = parms[0];
        var restaurantName = parms[1];
        var waitTimeStr = parms[2];
        var staffRatingStr = parms[3];
        var foodRatingStr = parms[4];
        var dateStr = parms.Length < 6 ? null : parms[5];

        // Get the UserId. Error if the User doesn't exist.
        var userId = rt.GetUserId(userName);
        if (!userId.HasValue)
        {
            Console.WriteLine($"User '{userName} does not exist in the database");
            return;
        }

        // Get the RestaurantId. Error if the Restaurant doesn't exist.
        var restaurantId = rt.GetRestaurantId(restaurantName);
        if (!restaurantId.HasValue)
        {
            Console.WriteLine($"Restaurant '{restaurantName}' does not exist in the database");
            return;
        }

        // Get the Wait Time. Error if not a valid number of minutes.
        var waitTime = GetInt(waitTimeStr);
        if (waitTime is null or < 0)
        {
            Console.WriteLine($"Wait time '{waitTimeStr}' is not a positive integer");
            return;
        }

        // Get the Staff Rating. Error if not a valid number from 1 to 5.
        var staffRating = GetInt(staffRatingStr);
        if (staffRating is null or < 1 or > 5)
        {
            Console.WriteLine($"Staff rating '{staffRatingStr}' is not an integer from 1 to 5");
            return;
        }

        // Get the Food Rating. Error if not a valid number from 1 to 5.
        var foodRating = GetInt(foodRatingStr);
        if (foodRating is null or < 1 or > 5)
        {
            Console.WriteLine($"Food rating '{foodRatingStr}' is not an integer from 1 to 5");
            return;
        }

        // Get the Visit Date. Default to Today. Error if not a valid date.
        var visitDate = dateStr is null ? DateTime.Today : GetDate(dateStr);
        if (visitDate is null)
        {
            Console.WriteLine($"Visit Date '{dateStr}' is not a valid date in the format M/d/yy");
            return;
        }
        
        // Add the Visit to the database
        rt.AddRestaurantVisit(userId.Value, restaurantId.Value, waitTime.Value, staffRating.Value, foodRating.Value, visitDate.Value);
        Console.WriteLine($"Visit to Restaurant '{restaurantName}' by '{userName}' on {visitDate.Value.ToString("MM/dd/yyyy")} is added to the database");
    }

    /// <summary>
    /// Select a Restaurant to eat at for a Group.
    /// </summary>
    /// <param name="rt"></param>
    /// <param name="parms"></param>
    private static void PickRestaurant(RT rt, string[] parms)
    {
        // Ensure there is 1 parameter, the Group name
        if (!CheckParmsCount(parms, 1)) return;

        var groupName = parms[0];
        
        // Get the GroupId. Error if the Group doesn't exist.
        var groupId = rt.GetGroupId(groupName);
        if (!groupId.HasValue)
        {
            Console.WriteLine($"Group '{groupName}' does not exist in the database");
            return;
        }

        // Select the Restaurant to visit. Error if there aren't any Restaurants in this Group.
        var restaurantName = rt.PickRestaurant(groupId.Value);
        if (restaurantName is null)
        {
            Console.WriteLine($"No Restaurants are in Group '{groupName}'");
            return;
        }
        
        Console.WriteLine($"Restaurant: '{restaurantName}'");
    }

    /// <summary>
    /// Ensure that the correct number of parameters have been passed.
    /// </summary>
    /// <param name="parms"></param>
    /// <param name="expected"></param>
    /// <returns>True if the correct number of parameters have been passed; False if not.</returns>
    private static bool CheckParmsCount(string[] parms, int expected)
    {
        if (parms.Length == expected) return true;
        Console.WriteLine($"Expected {expected} arguments");
        Usage();
        return false;
    }

    /// <summary>
    /// Ensure that the correct number of parameters have been passed.
    /// </summary>
    /// <param name="parms"></param>
    /// <param name="minExpected"></param>
    /// <param name="maxExpected"></param>
    /// <returns>True if the correct number of parameters have been passed; False if not.</returns>
    private static bool CheckParmsCount(string[] parms, int minExpected, int maxExpected)
    {
        if (parms.Length >= minExpected && parms.Length <= maxExpected) return true;
        Console.WriteLine($"Expected {minExpected} to {maxExpected} arguments");
        Usage();
        return false;
    }

    /// <summary>
    /// Convert a string to an integer
    /// </summary>
    /// <param name="str"></param>
    /// <returns>Value of the integer or null if the string is not a valid integer</returns>
    private static int? GetInt(string str) => int.TryParse(str, out var n) ? n : null;

    /// <summary>
    /// Convert a string to a DateTime
    /// </summary>
    /// <param name="str"></param>
    /// <returns>DateTime value or null if the string is not a valid Date</returns>
    private static DateTime? GetDate(string str) =>
        DateTime.TryParseExact(str, new[] { "M/d/yy" }, null, DateTimeStyles.None, out var d) ? d : null;

    /// <summary>
    /// Show the Usage of the application
    /// </summary>
    private static void Usage()
    {
        Console.WriteLine("RestaurantTracker dbpath [command]");
        Console.WriteLine("Commands:");
        Console.WriteLine(@"    Add-User username");
        Console.WriteLine(@"    Rename-User username newusername");
        Console.WriteLine(@"    Delete-User username");

        Console.WriteLine(@"    Add-Restaurant ""name""");
        Console.WriteLine(@"    Rename-Restaurant ""name"" ""newname""");
        Console.WriteLine(@"    Delete-Restaurant ""name""");

        Console.WriteLine(@"    Add-Group groupname");
        Console.WriteLine(@"    Rename-Group groupname newgroupname");
        Console.WriteLine(@"    Delete-Group groupname");

        Console.WriteLine(@"    Add-User-Group username groupname");
        Console.WriteLine(@"    Delete-User-Group username groupname");

        Console.WriteLine(@"    Add-Restaurant-Group ""restaurantname"" groupname");
        Console.WriteLine(@"    Delete-Restaurant-Group ""restaurantname"" groupname");

        Console.WriteLine(@"    Add-Restaurant-Visit username ""resturantname"" waittime staffrating foodrating [date]");

        Console.WriteLine(@"    Pick-Restaurant groupname");
    }
}

