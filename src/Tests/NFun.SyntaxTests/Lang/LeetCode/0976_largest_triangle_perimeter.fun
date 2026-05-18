# LeetCode 0976 — Largest Perimeter Triangle
#
# Sort descending; the first triple satisfying a+b>c (with a,b,c the three
# next-largest) gives the largest valid perimeter. If none does, return 0.

fun largestPerimeter(nums):
    s = nums.sort().reverse()
    i = 0
    while i + 2 < s.count():
        if s[i + 1] + s[i + 2] > s[i]:
            return s[i] + s[i + 1] + s[i + 2]
        i += 1
    return 0

@Test
fun testCanonical():
    assertEqual(largestPerimeter([2, 1, 2]), 5)

@Test
fun testDegenerate():
    assertEqual(largestPerimeter([1, 2, 1, 10]), 0)

@Test
fun testManyChoices():
    assertEqual(largestPerimeter([3, 6, 2, 3]), 8)
