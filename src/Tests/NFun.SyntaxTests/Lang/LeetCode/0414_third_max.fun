# LeetCode 0414 — Third Maximum Number
#
# Return the third distinct maximum, or the maximum if fewer than three
# distinct values exist. Sort descending and walk while collecting unique
# values until we have three.

fun thirdMax(nums):
    sorted = nums.sort().reverse()
    seen = []
    for x in sorted:
        if not (x in seen):
            seen = concat(seen, [x])
            if seen.count() == 3: return x
    return seen[0]

@Test
fun testCanonical():
    assertEqual(thirdMax([3, 2, 1]), 1)

@Test
fun testFewDistinct():
    assertEqual(thirdMax([1, 2]), 2)

@Test
fun testDuplicates():
    assertEqual(thirdMax([2, 2, 3, 1]), 1)

@Test
fun testNegatives():
    assertEqual(thirdMax([1, 2, -2147483648]), -2147483648)
