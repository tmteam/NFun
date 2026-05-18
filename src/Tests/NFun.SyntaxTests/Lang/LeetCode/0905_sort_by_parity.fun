# LeetCode 0905 — Sort Array By Parity
#
# Return any permutation where even numbers come before odd. Use filter twice
# and concat — preserves original relative order within each group.

fun sortByParity(nums):
    return concat(nums.filter(rule it % 2 == 0), nums.filter(rule it % 2 != 0))

@Test
fun testCanonical():
    assertEqual(sortByParity([3, 1, 2, 4]), [2, 4, 3, 1])

@Test
fun testAlreadyOk():
    assertEqual(sortByParity([2, 4, 1, 3]), [2, 4, 1, 3])

@Test
fun testAllEven():
    assertEqual(sortByParity([2, 4, 6]), [2, 4, 6])

@Test
fun testEmpty():
    assertEqual(sortByParity([]), [])
