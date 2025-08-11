namespace MatchMaking.Worker.Constants;

public static class LuaScripts
{
    public const string LPopNItemsIfExist =
        """
            local key = KEYS[1]
            local n = tonumber(ARGV[1])
            local len = redis.call('LLEN', key)
            if len < n then
                return nil
            end
            local items = {}
            for i = 1, n do
                table.insert(items, redis.call('LPOP', key))
            end
            return items
        """;
}