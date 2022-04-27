using Microsoft.Data.Sqlite;

namespace RTLib;

public class RT : IDisposable
{
    #region Private Fields

    /// <summary>
    /// Database schema
    /// </summary>
    private const string CreateDBCmds = @"CREATE TABLE IF NOT EXISTS ""Group"" (
""GroupId"" INTEGER PRIMARY KEY,
""GroupName"" TEXT UNIQUE
);
CREATE TABLE IF NOT EXISTS ""Restaurant"" (
""RestaurantId"" INTEGER PRIMARY KEY,
""RestaurantName"" TEXT UNIQUE
);
CREATE TABLE IF NOT EXISTS ""RestaurantGroup"" (
""RestaurantId"" INTEGER REFERENCES ""Restaurant""(""RestaurantId"") ON DELETE CASCADE,
""GroupId"" INTEGER REFERENCES ""Group""(""GroupId"") ON DELETE CASCADE,
CONSTRAINT ""RestaurantGroup_Key"" UNIQUE (""RestaurantId"", ""GroupId"") ON CONFLICT IGNORE
);
CREATE TABLE IF NOT EXISTS ""RestaurantVisit"" (
""RestaurantVisitId"" INTEGER PRIMARY KEY,
""UserId"" INTEGER REFERENCES ""User""(""UserId"") ON DELETE CASCADE,
""RestaurantId"" INTEGER REFERENCES ""Restaurant""(""RestaurantId"") ON DELETE CASCADE,
""WaitingTimeMinutes"" INTEGER,
""StaffRating"" INTEGER,
""FoodRating"" INTEGER,
""VisitDate"" TEXT
);
CREATE TABLE IF NOT EXISTS ""User"" (
""UserId"" INTEGER PRIMARY KEY,
""UserName"" TEXT UNIQUE
);
CREATE TABLE IF NOT EXISTS ""UserGroup"" (
""UserId"" INTEGER REFERENCES ""User""(""UserId"") ON DELETE CASCADE,
""GroupId"" INTEGER REFERENCES ""Group""(""GroupId"") ON DELETE CASCADE,
CONSTRAINT ""UserGroup_Key"" UNIQUE (""UserId"", ""GroupId"") ON CONFLICT IGNORE
);";

    /// <summary>
    /// Select the next Restaurant to eat at
    /// </summary>
    private const string PickRestaurantCmd = @"
/* All the Users in the Group */
WITH ""UsersInGroup"" (""UserId"") 
	AS (SELECT ""UserId"" FROM ""UserGroup"" WHERE ""GroupId"" = @GroupId),
/* All the Restaurants in the Group */
""RestaurantsInGroup"" (""RestaurantId"")
	AS (SELECT ""RestaurantId"" FROM ""RestaurantGroup"" WHERE ""GroupId"" = @GroupId),
/* Most recent visit by a User in the Group to each Restaurant in the Group */
""LastVisit"" (""RestaurantId"", ""VisitDate"") 
	AS (SELECT ""RestaurantId"", MAX(""VisitDate"") FROM ""RestaurantVisit"" 
		WHERE ""UserId"" IN (SELECT ""UserId"" FROM ""UsersInGroup"") 
			AND ""RestaurantId"" IN (SELECT ""RestaurantId"" FROM ""RestaurantsInGroup"")
		GROUP BY ""RestaurantId""),
/* Names of the 10 least recently visited Restaurants in the Group */
""TopTen"" (""RestaurantName"") 
	AS (SELECT ""RestaurantName"" FROM ""Restaurant""
		LEFT OUTER JOIN ""LastVisit"" 
			ON ""Restaurant"".""RestaurantId"" = ""LastVisit"".""RestaurantId""
        WHERE ""Restaurant"".""RestaurantId"" IN (SELECT ""RestaurantId"" FROM ""RestaurantsInGroup"")
        ORDER BY ""LastVisit"".""VisitDate""
		LIMIT 10)
/* Randomly select the name of 1 Restaurant from the 10 least recently visited Restaurants in the Group */
SELECT ""RestaurantName"" FROM ""TopTen""
	ORDER BY RANDOM()
	LIMIT 1";

    /// <summary>
    /// Opened database connection
    /// </summary>
    private readonly SqliteConnection DBConnection;
    
    /// <summary>
    /// Current Database Transaction
    /// </summary>
    private SqliteTransaction? Transaction;
    
    /// <summary>
    /// Is this object already disposed?
    /// </summary>
    private bool IsDisposed;
    
    #endregion Private Fields
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbPath"></param>
    public RT(string dbPath)
    {
        // Open the database
        DBConnection = new SqliteConnection($"Data Source={dbPath}");
        DBConnection.Open();
                
        // Create the tables if they don't already exist
        using var command = CreateCommand(CreateDBCmds);
        command.ExecuteNonQuery();
    }

    #endregion Constructors

    #region Public Methods

    /// <summary>
    /// Create a Database Command object. Initialize the Transaction and CommandText properties.
    /// </summary>
    /// <param name="commandText"></param>
    /// <returns></returns>
    public SqliteCommand CreateCommand(string? commandText = null)
    {
        var cmd = DBConnection.CreateCommand();
        cmd.Transaction = Transaction;
        cmd.CommandText = commandText;
        return cmd;
    }

    #region Transaction

    /// <summary>
    /// Start a new database transaction. Rollback any previous in-progress transaction.
    /// </summary>
    public void BeginTransaction()
    {
        RollbackTransaction();
        Transaction = DBConnection.BeginTransaction();
    }

    /// <summary>
    /// Commit the current database transaction, if any.
    /// </summary>
    public void CommitTransaction()
    {
        Transaction?.Commit();
        Transaction = null;
    }

    /// <summary>
    /// Rollback the current database transaction, if any.
    /// </summary>
    public void RollbackTransaction()
    {
        Transaction?.Rollback();
        Transaction = null;
    }

    /// <summary>
    /// Determine if a Transaction is active
    /// </summary>
    /// <returns>True if a Transaction is active; otherwise False.</returns>
    public bool InTransaction() => Transaction != null;

    #endregion Transaction

    #region User

    /// <summary>
    /// Get a List of all the Users
    /// </summary>
    /// <returns></returns>
    public List<(long userId, string userName)> GetUsers()
    {
        using var cmd = CreateCommand(@"SELECT ""UserId"", ""UserName"" FROM ""User""");
        using var rdr = cmd.ExecuteReader();
        return DoGetIdNames(rdr).ToList();
    }

    /// <summary>
    /// Test if a User exists in the database.
    /// </summary>
    /// <param name="userName"></param>
    /// <returns>True if the User exists; False if not.</returns>
    public bool UserExists(string userName) => ObjectExists("User", userName);

    /// <summary>
    /// Get the UserId, given a User Name
    /// </summary>
    /// <param name="userName"></param>
    /// <returns>UserId or null if the User doesn't exist in the database</returns>
    public long? GetUserId(string userName) => GetObjectId("User", userName); 

    /// <summary>
    /// Add a User to the database
    /// </summary>
    /// <param name="userName"></param>
    public void AddUser(string userName)
    {
        using var cmd = CreateCommand(@"INSERT INTO ""User"" (""UserName"") VALUES (@UserName)");
        cmd.Parameters.AddWithValue("UserName", userName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Rename a User in the database
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="newUserName"></param>
    public void RenameUser(long userId, string newUserName)
    {
        using var cmd = CreateCommand(@"UPDATE ""User"" SET ""UserName"" = @NewUserName WHERE ""UserId"" = @UserId");
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("NewUserName", newUserName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Delete a User from the database
    /// </summary>
    /// <param name="userId"></param>
    public void DeleteUser(long userId)
    {
        // Delete all Restaurant Visits recorded by the User, delete the User from all Groups, and then delete the User.
        using var cmd = CreateCommand(@"DELETE FROM ""RestaurantVisit"" WHERE ""UserId"" = @UserId;
DELETE FROM ""UserGroup"" WHERE ""UserId"" = @UserId;
DELETE FROM ""User"" WHERE ""UserId"" = @UserId;");
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.ExecuteNonQuery();
    }
    
    #endregion User

    #region Restaurant

    /// <summary>
    /// Get a list of all the Restaurants
    /// </summary>
    /// <returns></returns>
    public List<(long restaurantId, string restaurantName)> GetRestaurants()
    {
        using var cmd = CreateCommand(@"SELECT ""RestaurantId"", ""RestaurantName"" FROM ""Restaurant""");
        using var rdr = cmd.ExecuteReader();
        return DoGetIdNames(rdr).ToList();
    }

    /// <summary>
    /// Test if a Restaurant exists in the database.
    /// </summary>
    /// <param name="restaurantName"></param>
    /// <returns>True if the Restaurant exists; False if not.</returns>
    public bool RestaurantExists(string restaurantName) => ObjectExists("Restaurant", restaurantName);

    /// <summary>
    /// Get the RestaurantId, given a Restaurant Name
    /// </summary>
    /// <param name="restaurantName"></param>
    /// <returns>RestaurantId or null if the Restaurant doesn't exist in the database</returns>
    public long? GetRestaurantId(string restaurantName) => GetObjectId("Restaurant", restaurantName);

    /// <summary>
    /// Add a Restaurant to the database
    /// </summary>
    /// <param name="restaurantName"></param>
    public void AddRestaurant(string restaurantName)
    {
        using var cmd = CreateCommand(@"INSERT INTO ""Restaurant"" (""RestaurantName"") VALUES (@RestaurantName)");
        cmd.Parameters.AddWithValue("RestaurantName", restaurantName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Rename a Restaurant in the database
    /// </summary>
    /// <param name="restaurantId"></param>
    /// <param name="newRestaurantName"></param>
    public void RenameRestaurant(long restaurantId, string newRestaurantName)
    {
        using var cmd = CreateCommand(@"UPDATE ""Restaurant"" SET ""RestaurantName"" = @NewRestaurantName WHERE ""RestaurantId"" = @RestaurantId");
        cmd.Parameters.AddWithValue("RestaurantId", restaurantId);
        cmd.Parameters.AddWithValue("NewRestaurantName", newRestaurantName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Delete a Restaurant from the database
    /// </summary>
    /// <param name="restaurantId"></param>
    public void DeleteRestaurant(long restaurantId)
    {
        // Delete Visits to the Restaurant, delete the Restaurant from all Groups, then delete the Restaurant.
        using var cmd = CreateCommand(@"DELETE FROM ""RestaurantVisit"" WHERE ""RestaurantId"" = @RestaurantId;
DELETE FROM ""RestaurantGroup"" WHERE ""RestaurantId"" = @RestaurantId;
DELETE FROM ""Restaurant"" WHERE ""RestaurantId"" = @RestaurantId;");
        cmd.Parameters.AddWithValue("RestaurantId", restaurantId);
        cmd.ExecuteNonQuery();
    }
    
    #endregion Restaurant

    #region Group

    /// <summary>
    /// Get a list of all the Groups
    /// </summary>
    /// <returns></returns>
    public List<(long groupId, string groupName)> GetGroups()
    {
        using var cmd = CreateCommand(@"SELECT ""GroupId"", ""GroupName"" FROM ""Group""");
        using var rdr = cmd.ExecuteReader();
        return DoGetIdNames(rdr).ToList();
    }

    /// <summary>
    /// Test if a Group exists in the database.
    /// </summary>
    /// <param name="groupName"></param>
    /// <returns>True if the Group exists; False if not.</returns>
    public bool GroupExists(string groupName) => ObjectExists("Group", groupName);

    /// <summary>
    /// Get the GroupId, given a Group Name
    /// </summary>
    /// <param name="groupName"></param>
    /// <returns>GroupId or null if the Group doesn't exist in the database</returns>
    public long? GetGroupId(string groupName) => GetObjectId("Group", groupName);

    /// <summary>
    /// Add a Group to the database
    /// </summary>
    /// <param name="groupName"></param>
    public void AddGroup(string groupName)
    {
        using var cmd = CreateCommand(@"INSERT INTO ""Group"" (""GroupName"") VALUES (@GroupName)");
        cmd.Parameters.AddWithValue("GroupName", groupName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Rename a Group in the database
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="newGroupName"></param>
    public void RenameGroup(long groupId, string newGroupName)
    {
        using var cmd = CreateCommand(@"UPDATE ""Group"" SET ""GroupName"" = @NewGroupName WHERE ""GroupId"" = @GroupId");
        cmd.Parameters.AddWithValue("GroupId", groupId);
        cmd.Parameters.AddWithValue("NewGroupName", newGroupName);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Delete a Group from the database
    /// </summary>
    /// <param name="groupId"></param>
    public void DeleteGroup(long groupId)
    {
        // Remove all Users from the Group, remove all Restaurants from the Group, delete the Group. 
        using var cmd = CreateCommand(@"DELETE FROM ""UserGroup"" WHERE ""GroupId"" = @GroupId;
DELETE FROM ""RestaurantGroup"" WHERE ""GroupId"" = @GroupId;
DELETE FROM ""Group"" WHERE ""GroupId"" = @GroupId;");
        cmd.Parameters.AddWithValue("GroupId", groupId);
        cmd.ExecuteNonQuery();
    }
    
    #endregion Group

    #region UserGroup

    /// <summary>
    /// Get a list of all the Users in a Group
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public List<long> GetAllUsersInGroup(long groupId)
    {
        using var cmd = CreateCommand(@"SELECT ""UserId"" FROM ""UserGroup"" WHERE ""GroupId"" = @GroupId");
        cmd.Parameters.AddWithValue("GroupId", groupId);
        using var rdr = cmd.ExecuteReader();
        return DoGetIds(rdr).ToList();
    }

    /// <summary>
    /// Test if a User is a member of a Group
    /// </summary>
    /// <returns>True if the User is a member of the Group; False if not.</returns>
    public bool IsUserMemberOf(long userId, long groupId)
        => IsObjectMemberOf("User", userId, groupId);

    /// <summary>
    /// Get a list of all the Groups a User belongs to
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<long> GetAllGroupsForUser(long userId)
    {
        using var cmd = CreateCommand(@"SELECT ""GroupId"" FROM ""UserGroup"" WHERE ""UserId"" = @UserId");
        cmd.Parameters.AddWithValue("UserId", userId);
        using var rdr = cmd.ExecuteReader();
        return DoGetIds(rdr).ToList();
    }

    /// <summary>
    /// Add a User to a Group
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    public void AddUserToGroup(long userId, long groupId)
    {
        using var cmd = CreateCommand(@"INSERT INTO ""UserGroup"" (""UserId"", ""GroupId"") VALUES (@UserId, @GroupId)");
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("GroupId", groupId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Delete a User from a Group
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupId"></param>
    public void DeleteUserFromGroup(long userId, long groupId)
    {
        using var cmd = CreateCommand(@"DELETE FROM ""UserGroup"" WHERE ""UserId"" = @UserId AND ""GroupId"" = @GroupId");
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("GroupId", groupId);
        cmd.ExecuteNonQuery();
    }
    
    #endregion UserGroup

    #region RestaurantGroup

    /// <summary>
    /// Get a list of all the Restaurants in a Group
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public List<long> GetAllRestaurantsInGroup(long groupId)
    {
        using var cmd = CreateCommand(@"SELECT ""RestaurantId"" FROM ""RestaurantGroup"" WHERE ""GroupId"" = @GroupId");
        cmd.Parameters.AddWithValue("GroupId", groupId);
        using var rdr = cmd.ExecuteReader();
        return DoGetIds(rdr).ToList();
    }

    /// <summary>
    /// Get a list of all the Groups a Restaurant belongs to
    /// </summary>
    /// <param name="restaurantId"></param>
    /// <returns></returns>
    public List<long> GetAllGroupsForRestaurant(long restaurantId)
    {
        using var cmd = CreateCommand(@"SELECT ""GroupId"" FROM ""RestaurantGroup"" WHERE ""RestaurantId"" = @RestaurantId");
        cmd.Parameters.AddWithValue("RestaurantId", restaurantId);
        using var rdr = cmd.ExecuteReader();
        return DoGetIds(rdr).ToList();
    }

    /// <summary>
    /// Test if a Restaurant is a member of a Group
    /// </summary>
    /// <returns>True if the Restaurant is a member of the Group; False if not.</returns>
    public bool IsRestaurantMemberOf(long restaurantId, long groupId)
        => IsObjectMemberOf("Restaurant", restaurantId, groupId);

    /// <summary>
    /// Add a Restaurant to a Group
    /// </summary>
    /// <param name="restaurantId"></param>
    /// <param name="groupId"></param>
    public void AddRestaurantToGroup(long restaurantId, long groupId)
    {
        using var cmd = CreateCommand(@"INSERT INTO ""RestaurantGroup"" (""RestaurantId"", ""GroupId"") VALUES (@RestaurantId, @GroupId)");
        cmd.Parameters.AddWithValue("RestaurantId", restaurantId);
        cmd.Parameters.AddWithValue("GroupId", groupId);
        cmd.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Delete a Restaurant from a Group
    /// </summary>
    /// <param name="restaurantId"></param>
    /// <param name="groupId"></param>
    public void DeleteRestaurantFromGroup(long restaurantId, long groupId)
    {
        using var cmd = CreateCommand(@"DELETE FROM ""RestaurantGroup"" WHERE ""RestaurantId"" = @RestaurantId AND ""GroupId"" = @GroupId");
        cmd.Parameters.AddWithValue("RestaurantId", restaurantId);
        cmd.Parameters.AddWithValue("GroupId", groupId);
        cmd.ExecuteNonQuery();
    }
    
    #endregion RestaurantGroup

    #region RestaurantVisit

    /// <summary>
    /// Get all the Restaurant Visits
    /// </summary>
    /// <returns></returns>
    public List<RestaurantVisit> GetRestaurantVisits()
    {
        using var cmd = CreateCommand(@"SELECT 
""RestaurantVisitId"", 
""UserId"", 
""RestaurantId"", 
""WaitingTimeMinutes"", 
""StaffRating"", 
""FoodRating"", 
""VisitDate""
FROM ""RestaurantVisit""");
        using var rdr = cmd.ExecuteReader();
        return DoGetRestaurantVisit(rdr).ToList();
    }

    /// <summary>
    /// Get all the Visits to a Restaurant
    /// </summary>
    /// <param name="restaurantId"></param>
    /// <returns></returns>
    public List<RestaurantVisit> GetRestaurantVisitsForRestaurant(long restaurantId)
    {
        using var cmd = CreateCommand(@"SELECT 
""RestaurantVisitId"",
""UserId"", 
""RestaurantId"", 
""WaitingTimeMinutes"", 
""StaffRating"", 
""FoodRating"", 
""VisitDate""
FROM ""RestaurantVisit"" WHERE ""RestaurantId"" = @RestaurantId");
        cmd.Parameters.AddWithValue("RestaurantId", restaurantId);
        using var rdr = cmd.ExecuteReader();
        return DoGetRestaurantVisit(rdr).ToList();
    }

    /// <summary>
    /// Get all the Restaurant Visits by a User
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<RestaurantVisit> GetRestaurantVisitsByUser(long userId)
    {
        using var cmd = CreateCommand(@"SELECT 
""RestaurantVisitId"",
""UserId"", 
""RestaurantId"", 
""WaitingTimeMinutes"", 
""StaffRating"", 
""FoodRating"", 
""VisitDate""
FROM ""RestaurantVisit"" WHERE ""UserId"" = @UserId");
        cmd.Parameters.AddWithValue("UserId", userId);
        using var rdr = cmd.ExecuteReader();
        return DoGetRestaurantVisit(rdr).ToList();
    }

    /// <summary>
    /// Record a visit to a Restaurant 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="restaurantId"></param>
    /// <param name="waitTime"></param>
    /// <param name="staffRating"></param>
    /// <param name="foodRating"></param>
    /// <param name="visitDate"></param>
    public void AddRestaurantVisit(long userId, long restaurantId, int waitTime, int staffRating, int foodRating, DateTime visitDate)
    {
        // Add the Visit to the database
        using var cmd = CreateCommand(@"INSERT INTO ""RestaurantVisit""
 (""UserId"", ""RestaurantId"", ""WaitingTimeMinutes"", ""StaffRating"", ""FoodRating"", ""VisitDate"")
 VALUES (@UserId, @RestaurantId, @WaitingTimeMinutes, @StaffRating, @FoodRating, @VisitDate)");
        cmd.Parameters.AddWithValue("UserId", userId);
        cmd.Parameters.AddWithValue("RestaurantId", restaurantId);
        cmd.Parameters.AddWithValue("WaitingTimeMinutes", waitTime);
        cmd.Parameters.AddWithValue("StaffRating", staffRating);
        cmd.Parameters.AddWithValue("FoodRating", foodRating);
        cmd.Parameters.AddWithValue("VisitDate", visitDate);
        cmd.ExecuteNonQuery();
    }

    #endregion RestaurantVisit

    /// <summary>
    /// Select a Restaurant to eat at for a Group.
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns>Restaurant name or null if there are no restaurants in the group</returns>
    public string? PickRestaurant(long groupId)
    {
        using var cmd = CreateCommand(PickRestaurantCmd);
        cmd.Parameters.AddWithValue("GroupId", groupId);
        using var rdr = cmd.ExecuteReader();
        if (!rdr.HasRows) return null;
        rdr.Read();
        return rdr.GetString(0);
    }

    /// <summary>
    /// Dispose the object. Rollback any in-progress transaction. Close and dispose the database connection.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed) return;
        RollbackTransaction();
        DBConnection.Close();
        DBConnection.Dispose();
        IsDisposed = true;
    }
    
    #endregion Public Methods

    #region Private Methods
    
    /// <summary>
    /// Test if an object (User, Restaurant, or Group) exists in the database.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectName"></param>
    /// <returns>True if the object exists; False if not.</returns>
    private bool ObjectExists(string objectType, string objectName)
    {
        using var cmd = CreateCommand($@"SELECT COUNT(*) FROM ""{objectType}"" WHERE ""{objectType}Name"" = @Value");
        cmd.Parameters.AddWithValue("Value", objectName);
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count != 0;
    }

    /// <summary>
    /// Test if an Object (User or Restaurant) is a member of a Group.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectId"></param>
    /// <param name="groupId"></param>
    /// <returns>True if the object is a member of the Group; False if not.</returns>
    private bool IsObjectMemberOf(string objectType, long objectId, long groupId)
    {
        using var cmd = CreateCommand($@"SELECT COUNT(*) FROM ""{objectType}Group"" WHERE ""{objectType}Id"" = @ObjectId AND ""GroupId"" = @GroupId");
        cmd.Parameters.AddWithValue("ObjectId", objectId);
        cmd.Parameters.AddWithValue("GroupId", groupId);
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count != 0;
    }

    /// <summary>
    /// Get an Object (User, Restaurant, or Group) ID given the Object Name.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectName"></param>
    /// <returns>Object ID or null if the Object doesn't exist</returns>
    private long? GetObjectId(string objectType, string objectName)
    {
        using var cmd = CreateCommand($@"SELECT ""{objectType}Id"" FROM ""{objectType}"" WHERE ""{objectType}Name"" = @ObjectName LIMIT 1");
        cmd.Parameters.AddWithValue("ObjectName", objectName);
        using var rdr = cmd.ExecuteReader();
        if (!rdr.HasRows) return null;
        rdr.Read();
        return rdr.GetInt64(0);
    }
    
    /// <summary>
    /// Get all the Object (User, Restaurant, or Group) Ids from a DataReader.
    /// </summary>
    /// <param name="rdr"></param>
    /// <returns></returns>
    private static IEnumerable<long> DoGetIds(SqliteDataReader rdr)
    {
        while (rdr.Read())
        {
            yield return rdr.GetInt64(0);
        }
    }
        
    /// <summary>
    /// Get all the Object (User, Restaurant, or Group) Ids and Names from a DataReader.
    /// </summary>
    /// <param name="rdr"></param>
    /// <returns></returns>
    private static IEnumerable<(long, string)> DoGetIdNames(SqliteDataReader rdr)
    {
        while (rdr.Read())
        {
            yield return (rdr.GetInt64(0), rdr.GetString(1));
        }
    }

    /// <summary>
    /// Get all the RestaurantVisit rows from a DataReader
    /// </summary>
    /// <param name="rdr"></param>
    /// <returns></returns>
    private static IEnumerable<RestaurantVisit> DoGetRestaurantVisit(SqliteDataReader rdr)
    {
        while (rdr.Read())
        {
            yield return new RestaurantVisit(rdr);
        }
    }
    
    #endregion Private Methods
}