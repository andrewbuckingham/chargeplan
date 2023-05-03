public static class LinqExtensions
{
    public static T OnlyOne<T>(this IEnumerable<T> items, Func<T,string?> id, string name)
    {
        Func<T, bool> predicate = f => id(f) == name;

        int count = items.Count(predicate);

        if (count > 1) throw new InvalidStateException($"There are too many {typeof(T).Name} called {name}");
        if (count == 0) throw new InvalidStateException($"There is no {typeof(T).Name} called {name}");

        return items.Single(predicate);
    }
}