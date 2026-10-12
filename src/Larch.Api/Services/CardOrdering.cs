namespace Larch.Api.Services;

// Cards keep their order in a Position number (0, 1, 2 ...) per column.
// When a card is dropped somewhere, the simplest way to stay correct is to work out the new
// list of ids for the column and then number it again from zero. A column rarely has more
// than a few dozen cards, so rewriting the numbers is cheap and there are never gaps or duplicates.
public static class CardOrdering
{
    // Takes the ids that are in the target column now (without the moving card),
    // puts the moving card at the wanted index and returns the new order.
    // An index that is too small or too large is moved to the nearest end.
    public static List<int> Place(IEnumerable<int> idsInColumn, int movingId, int index)
    {
        var list = idsInColumn.Where(id => id != movingId).ToList();
        list.Insert(Math.Clamp(index, 0, list.Count), movingId);
        return list;
    }
}
