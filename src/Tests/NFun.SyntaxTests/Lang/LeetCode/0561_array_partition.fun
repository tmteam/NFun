# LeetCode 0561 — Array Partition I
#
# Partition 2n numbers into n pairs to maximise the sum of mins. After
# sorting, picking adjacent pairs (indices 0,1; 2,3; …) is optimal — sum of
# even-indexed elements.

fun arrayPairSum(nums):
    s = nums.sort()
    total = 0
    i = 0
    while i < s.count():
        total += s[i]
        i += 2
    return total

@Test
fun testCanonical():
    assertEqual(arrayPairSum([1, 4, 3, 2]), 4)

@Test
fun testWithNegatives():
    assertEqual(arrayPairSum([6, 2, 6, 5, 1, 2]), 9)
