# LeetCode 0169 — Majority Element
#
# Find the element that appears more than ⌊n/2⌋ times. Boyer-Moore vote:
# O(n) time, O(1) extra space — no hash map needed.

fun majorityElement(nums):
    candidate = 0
    count = 0
    for x in nums:
        if count == 0: candidate = x
        if x == candidate:
            count += 1
        else:
            count -= 1
    return candidate

@Test
fun testCanonical():
    assertEqual(majorityElement([3, 2, 3]), 3)

@Test
fun testMixed():
    assertEqual(majorityElement([2, 2, 1, 1, 1, 2, 2]), 2)

@Test
fun testSingleton():
    assertEqual(majorityElement([42]), 42)
