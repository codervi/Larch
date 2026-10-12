using Larch.Api.Services;
using Xunit;

namespace Larch.Tests;

public class CardOrderingTests
{
    [Fact]
    public void Drop_at_the_top()
    {
        Assert.Equal(new[] { 9, 1, 2, 3 }, CardOrdering.Place(new[] { 1, 2, 3 }, 9, 0));
    }

    [Fact]
    public void Drop_in_the_middle()
    {
        Assert.Equal(new[] { 1, 9, 2, 3 }, CardOrdering.Place(new[] { 1, 2, 3 }, 9, 1));
    }

    [Fact]
    public void Drop_at_the_end()
    {
        Assert.Equal(new[] { 1, 2, 3, 9 }, CardOrdering.Place(new[] { 1, 2, 3 }, 9, 3));
    }

    [Fact]
    public void Drop_into_an_empty_column()
    {
        Assert.Equal(new[] { 9 }, CardOrdering.Place(Array.Empty<int>(), 9, 0));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(99, 3)]
    public void Index_outside_the_list_goes_to_the_nearest_end(int index, int expectedSlot)
    {
        var result = CardOrdering.Place(new[] { 1, 2, 3 }, 9, index);
        Assert.Equal(expectedSlot, result.IndexOf(9));
    }

    [Fact]
    public void Moving_inside_the_same_column_does_not_duplicate_the_card()
    {
        // card 1 is already in the column, index 2 means "after the other two"
        var result = CardOrdering.Place(new[] { 1, 2, 3 }, 1, 2);
        Assert.Equal(new[] { 2, 3, 1 }, result);
    }

    [Fact]
    public void Moving_down_by_one_inside_the_column()
    {
        var result = CardOrdering.Place(new[] { 1, 2, 3 }, 1, 1);
        Assert.Equal(new[] { 2, 1, 3 }, result);
    }

    [Fact]
    public void Dropping_where_it_already_is_changes_nothing()
    {
        var result = CardOrdering.Place(new[] { 1, 2, 3 }, 2, 1);
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }
}
