# LeetCode 0287 — Find the Duplicate Number
#
# nums has length n+1 and each value is in [1..n] — exactly one duplicate.
# Floyd's tortoise-and-hare on the implicit "next = nums[i]" graph finds
# the cycle entry, which is the duplicate. O(n) time, O(1) space — no
# mutation or extra collection.

fun findDuplicate(nums):
    slow = nums[0]
    fast = nums[0]
    slow = nums[slow]
    fast = nums[nums[fast]]
    while slow != fast:
        slow = nums[slow]
        fast = nums[nums[fast]]
    slow = nums[0]
    while slow != fast:
        slow = nums[slow]
        fast = nums[fast]
    return slow

@Test
fun testCanonical():
    assertEqual(findDuplicate([1, 3, 4, 2, 2]), 2)

@Test
fun testThree():
    assertEqual(findDuplicate([3, 1, 3, 4, 2]), 3)

@Test
fun testRepeatedAtEnd():
    assertEqual(findDuplicate([1, 4, 4, 2, 4]), 4)
