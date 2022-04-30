using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace RTLib;

/// <summary>
/// Information about a visit to a restaurant
/// </summary>
public class RestaurantVisit
{
    /// <summary>
    /// Unique row ID assigned by the database
    /// </summary>
    public long RestaurantVisitId { get; init; }
    
    /// <summary>
    /// UserId of the user who recorded the visit to the restaurant
    /// </summary>
    public long UserId { get; init; }
    
    /// <summary>
    /// RestaurantId of the restaurant visited
    /// </summary>
    public long RestaurantId { get; init; }
    
    /// <summary>
    /// How many minutes spend waiting for a table
    /// </summary>
    public int WaitingTimeMinutes { get; init; }
    
    /// <summary>
    /// Staff rating; an integer from 1 (bad) to 5 (great)
    /// </summary>
    public int StaffRating { get; init; }
    
    /// <summary>
    /// Food rating; an integer from 1 (bad) to 5 (great)
    /// </summary>
    public int FoodRating { get; init; }
    
    /// <summary>
    /// Date of the visit
    /// </summary>
    public DateTime VisitDate { get; init; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="restaurantVisitId"></param>
    /// <param name="userId"></param>
    /// <param name="restaurantId"></param>
    /// <param name="waitingTimeMinutes"></param>
    /// <param name="staffRating"></param>
    /// <param name="foodRating"></param>
    /// <param name="visitDate"></param>
    public RestaurantVisit(long restaurantVisitId, 
                            long userId, 
                            long restaurantId, 
                            int waitingTimeMinutes, 
                            int staffRating, 
                            int foodRating, 
                            DateTime visitDate)
    {
        RestaurantVisitId = restaurantVisitId;
        UserId = userId;
        RestaurantId = restaurantId;
        WaitingTimeMinutes = waitingTimeMinutes;
        StaffRating = staffRating;
        FoodRating = foodRating;
        VisitDate = visitDate;
    }

    /// <summary>
    /// Constructor for a DataReader
    /// </summary>
    /// <param name="rdr"></param>
    public RestaurantVisit(SqliteDataReader rdr)
        : this(GetInt64(rdr, "RestaurantVisitId"), 
                GetInt64(rdr, "UserId"), 
                GetInt64(rdr, "RestaurantId"), 
                GetInt32(rdr, "WaitingTimeMinutes"),
                GetInt32(rdr, "StaffRating"),
                GetInt32(rdr, "FoodRating"),
                GetDateTime(rdr, "VisitDate"))
    {
    }

    /// <summary>
    /// Helper method to get a long from a DataReader
    /// </summary>
    /// <param name="rdr"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    private static long GetInt64(SqliteDataReader rdr, string fieldName) => rdr.GetInt64(rdr.GetOrdinal(fieldName));

    /// <summary>
    /// Helper method to get an int from a DataReader
    /// </summary>
    /// <param name="rdr"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    private static int GetInt32(SqliteDataReader rdr, string fieldName) => rdr.GetInt32(rdr.GetOrdinal(fieldName));

    /// <summary>
    /// Helper to get a DateTime from a DataReader
    /// </summary>
    /// <param name="rdr"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    private static DateTime GetDateTime(SqliteDataReader rdr, string fieldName) => rdr.GetDateTime(rdr.GetOrdinal(fieldName));
}