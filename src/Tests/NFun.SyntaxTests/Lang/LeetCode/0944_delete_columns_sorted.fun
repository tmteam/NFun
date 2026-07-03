# LeetCode 0944 — Delete Columns to Make Sorted
#
# Count columns where the chars are not in non-decreasing order.

fun minDeletionSize(strs):
    if strs.count() == 0: return 0
    cols = strs[0].count()
    deletions = 0
    j = 0
    while j < cols:
        i = 1
        while i < strs.count():
            if strs[i][j] < strs[i - 1][j]:
                deletions += 1
                i = strs.count()  # break out — count this column once
            i += 1
        j += 1
    return deletions

@Test
fun testCanonical():
    assertEqual(minDeletionSize(['cba', 'daf', 'ghi']), 1)

@Test
fun testAllSorted():
    assertEqual(minDeletionSize(['a', 'b']), 0)

@Test
fun testAllBad():
    assertEqual(minDeletionSize(['zyx', 'wvu', 'tsr']), 3)
