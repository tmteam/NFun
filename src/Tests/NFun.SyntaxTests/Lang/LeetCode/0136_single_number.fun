# LeetCode 0136 — Single Number
#
# Given a non-empty array of integers where every element appears twice
# except one, find that single element. Linear time, constant extra space.
# XOR trick: a^a == 0, a^0 == a, so XORing all elements cancels every pair
# and leaves the lonely value.

fun singleNumber(nums):
    result = 0
    for x in nums:
        result = result ^ x
    return result

@Test
fun testCanonical():
    assertEqual(singleNumber([2, 2, 1]), 1)

@Test
fun testFiveElements():
    assertEqual(singleNumber([4, 1, 2, 1, 2]), 4)

@Test
fun testSingleton():
    assertEqual(singleNumber([42]), 42)
