# LeetCode 0463 — Island Perimeter
#
# Each land cell contributes 4 to the perimeter; subtract 2 for every shared
# edge between two adjacent land cells (count once from the top/left side).

fun islandPerimeter(grid):
    rows = grid.count()
    cols = grid[0].count()
    total = 0
    i = 0
    while i < rows:
        j = 0
        while j < cols:
            if grid[i][j] == 1:
                total += 4
                if i > 0 and grid[i - 1][j] == 1: total -= 2
                if j > 0 and grid[i][j - 1] == 1: total -= 2
            j += 1
        i += 1
    return total

@Test
fun testCanonical():
    grid = [[0, 1, 0, 0],
            [1, 1, 1, 0],
            [0, 1, 0, 0],
            [1, 1, 0, 0]]
    assertEqual(islandPerimeter(grid), 16)

@Test
fun testSingleCell():
    assertEqual(islandPerimeter([[1]]), 4)

@Test
fun testRow():
    assertEqual(islandPerimeter([[1, 1, 1]]), 8)
