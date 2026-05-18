# LeetCode 1748 — Sum of Unique Elements
#
# Sum values that appear exactly once. Sort + run-length.

fun sumOfUnique(nums):
    s = nums.sort()
    total = 0
    i = 0
    while i < s.count():
        j = i
        while j < s.count() and s[j] == s[i]:
            j += 1
        if j - i == 1: total += s[i]
        i = j
    return total

@Test
fun testCanonical():
    assertEqual(sumOfUnique([1, 2, 3, 2]), 4)

@Test
fun testAllDup():
    assertEqual(sumOfUnique([1, 1, 1, 1, 1]), 0)

@Test
fun testAllUnique():
    assertEqual(sumOfUnique([1, 2, 3, 4, 5]), 15)
