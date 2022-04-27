using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace RTLib;

/// <summary>
/// Information about a visit to a restaurant
/// </summary>
public class RestaurantVisit
{
    public long RestaurantVisitId { get; init; }
    public long UserId { get; init; }
    public long RestaurantId { get; init; }
    public int WaitingTimeMinutes { get; init; }
    public int StaffRating { get; init; }
    public int FoodRating { get; init; }
    public DateTime VisitDate { get; init; }

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

    private static long GetInt64(SqliteDataReader rdr, string fieldName) => rdr.GetInt64(rdr.GetOrdinal(fieldName));

    private static int GetInt32(SqliteDataReader rdr, string fieldName) => rdr.GetInt32(rdr.GetOrdinal(fieldName));

    private static DateTime GetDateTime(SqliteDataReader rdr, string fieldName) => rdr.GetDateTime(rdr.GetOrdinal(fieldName));
}