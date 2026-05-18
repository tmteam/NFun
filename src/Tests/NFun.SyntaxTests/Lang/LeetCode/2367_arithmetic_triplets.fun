# LeetCode 2367 — Number of Arithmetic Triplets
#
# Strictly increasing array; count triplets (i<j<k) with constant diff.
# Use the `in` operator — nums has ≤200 elements per leetcode bounds.

fun arithmeticTriplets(nums, diff):
    count = 0
    for x in nums:
        if (x + diff) in nums and (x + 2 * diff) in nums:
            count += 1
    return count

@Test
fun testCanonical():
    assertEqual(arithmeticTriplets([0, 1, 4, 6, 7, 10], 3), 2)

@Test
fun testNoTriplet():
    assertEqual(arithmeticTriplets([4, 5, 6, 7, 8, 9], 2), 2)
