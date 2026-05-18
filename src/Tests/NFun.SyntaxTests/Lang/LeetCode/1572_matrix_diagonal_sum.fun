# LeetCode 1572 — Matrix Diagonal Sum
#
# Sum the primary and secondary diagonals; subtract the center if n is odd
# (it's counted twice).

fun diagonalSum(mat):
    n = mat.count()
    total = 0
    i = 0
    while i < n:
        total += mat[i][i] + mat[i][n - 1 - i]
        i += 1
    if n % 2 == 1:
        mid = n // 2
        total -= mat[mid][mid]
    return total

@Test
fun testCanonical():
    assertEqual(diagonalSum([[1, 2, 3], [4, 5, 6], [7, 8, 9]]), 25)

@Test
fun testEvenSize():
    assertEqual(diagonalSum([[1, 1, 1, 1], [1, 1, 1, 1], [1, 1, 1, 1], [1, 1, 1, 1]]), 8)

@Test
fun testSingleCell():
    assertEqual(diagonalSum([[5]]), 5)
