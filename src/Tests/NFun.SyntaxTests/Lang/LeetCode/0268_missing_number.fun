# LeetCode 0268 — Missing Number
#
# `nums` contains n distinct numbers from [0, n]. Return the one that's
# missing. Constant-extra-space trick: sum of [0, n] is n*(n+1)/2; subtract
# the actual sum to recover the missing value.

fun missingNumber(nums):
    n = nums.count()
    expected = n * (n + 1) // 2
    actual = 0
    for x in nums:
        actual += x
    return expected - actual

@Test
fun testCanonical():
    assertEqual(missingNumber([3, 0, 1]), 2)

@Test
fun testZeroMissing():
    assertEqual(missingNumber([1]), 0)

@Test
fun testNMissing():
    assertEqual(missingNumber([0, 1]), 2)

@Test
fun testNineElements():
    assertEqual(missingNumber([9, 6, 4, 2, 3, 5, 7, 0, 1]), 8)
