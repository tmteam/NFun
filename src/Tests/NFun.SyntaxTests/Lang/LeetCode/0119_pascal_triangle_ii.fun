# LeetCode 0119 — Pascal's Triangle II
#
# Return row k (0-indexed). Build row by row, O(k²) like #0118 but discard
# previous rows along the way.

fun nextRow(prev):
    n = prev.count()
    out = [1]
    i = 1
    while i < n:
        out = concat(out, [prev[i - 1] + prev[i]])
        i += 1
    return concat(out, [1])

fun getRow(rowIndex):
    row = [1]
    i = 0
    while i < rowIndex:
        row = nextRow(row)
        i += 1
    return row

@Test
fun testRow0():
    assertEqual(getRow(0), [1])

@Test
fun testRow1():
    assertEqual(getRow(1), [1, 1])

@Test
fun testRow2():
    assertEqual(getRow(2), [1, 2, 1])

@Test
fun testRow3():
    assertEqual(getRow(3), [1, 3, 3, 1])

@Test
fun testRow4():
    assertEqual(getRow(4), [1, 4, 6, 4, 1])

@Test
fun testRow5():
    assertEqual(getRow(5), [1, 5, 10, 10, 5, 1])
