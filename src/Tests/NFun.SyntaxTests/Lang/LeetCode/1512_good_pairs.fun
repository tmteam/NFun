# LeetCode 1512 — Number of Good Pairs
#
# Pairs (i, j) with i<j and nums[i]==nums[j]. Sort, then within each
# equal-value run of length k add k(k-1)/2.

fun numIdenticalPairs(nums):
    s = nums.sort()
    total = 0
    i = 0
    while i < s.count():
        j = i
        while j < s.count() and s[j] == s[i]:
            j += 1
        k = j - i
        total += k * (k - 1) // 2
        i = j
    return total

@Test
fun testCanonical():
    assertEqual(numIdenticalPairs([1, 2, 3, 1, 1, 3]), 4)

@Test
fun testAllSame():
    assertEqual(numIdenticalPairs([1, 1, 1, 1]), 6)

@Test
fun testNoneEqual():
    assertEqual(numIdenticalPairs([1, 2, 3]), 0)
