# LeetCode 0867 — Transpose Matrix
#
# (m × n) → (n × m). Build row-by-row from the original columns.

fun transpose(matrix):
    if matrix.count() == 0: return []
    m = matrix.count()
    n = matrix[0].count()
    out = []
    j = 0
    while j < n:
        row = []
        i = 0
        while i < m:
            row = concat(row, [matrix[i][j]])
            i += 1
        out = concat(out, [row])
        j += 1
    return out

@Test
fun testCanonical():
    expected = [[1, 4, 7], [2, 5, 8], [3, 6, 9]]
    assertEqual(transpose([[1, 2, 3], [4, 5, 6], [7, 8, 9]]), expected)

@Test
fun testNonSquare():
    assertEqual(transpose([[1, 2, 3], [4, 5, 6]]), [[1, 4], [2, 5], [3, 6]])

@Test
fun testSingleRow():
    assertEqual(transpose([[1, 2, 3]]), [[1], [2], [3]])
