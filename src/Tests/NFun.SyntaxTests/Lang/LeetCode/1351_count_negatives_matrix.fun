# LeetCode 1351 — Count Negative Numbers in a Sorted Matrix
#
# Rows AND columns sorted descending. Staircase walk from top-right:
# negative → all cells below in this column are negative (add count, move
# left); non-negative → move down. O(m + n).

fun countNegatives(grid):
    m = grid.count()
    n = grid[0].count()
    total = 0
    r = 0
    c = n - 1
    while r < m and c >= 0:
        if grid[r][c] < 0:
            total += m - r
            c -= 1
        else:
            r += 1
    return total

@Test
fun testCanonical():
    grid = [[4, 3, 2, -1],
            [3, 2, 1, -1],
            [1, 1, -1, -2],
            [-1, -1, -2, -3]]
    assertEqual(countNegatives(grid), 8)

@Test
fun testAllNegative():
    assertEqual(countNegatives([[-1]]), 1)

@Test
fun testAllPositive():
    assertEqual(countNegatives([[3, 2], [1, 0]]), 0)
