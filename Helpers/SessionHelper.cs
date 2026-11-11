namespace RaceDay.Web.Helpers
{
    /// <summary>
    /// One place that knows how login state and roles are kept in the session.
    /// </summary>
    public static class SessionHelper
    {
        public const string TokenKey = "JWToken";
        public const string UserIdKey = "UserId";
        public const string UserNameKey = "UserName";
        public const string UserRoleKey = "UserRole";

        public static string Token(this ISession session) => session.GetString(TokenKey) ?? string.Empty;

        public static bool IsLoggedIn(this ISession session) => !string.IsNullOrEmpty(session.GetString(TokenKey));

        public static int? GetUserId(this ISession session) =>
            int.TryParse(session.GetString(UserIdKey), out var id) && id > 0 ? id : null;

        public static string Role(this ISession session) => session.GetString(UserRoleKey) ?? string.Empty;

        // "Admin" is accepted too so accounts created before the roles were renamed still work
        public static bool IsOrganiser(this ISession session) =>
            session.IsLoggedIn() &&
            (session.Role().Equals("Organiser", StringComparison.OrdinalIgnoreCase) ||
             session.Role().Equals("Admin", StringComparison.OrdinalIgnoreCase));

        public static bool IsParticipant(this ISession session) =>
            session.IsLoggedIn() && !session.IsOrganiser();
    }

    public static class Ordinal
    {
        // 1 -> 1st, 2 -> 2nd, 3 -> 3rd, 11 -> 11th, 47 -> 47th
        public static string Of(int number)
        {
            var lastTwo = number % 100;
            if (lastTwo is >= 11 and <= 13) return $"{number}th";

            return (number % 10) switch
            {
                1 => $"{number}st",
                2 => $"{number}nd",
                3 => $"{number}rd",
                _ => $"{number}th"
            };
        }
    }
}
