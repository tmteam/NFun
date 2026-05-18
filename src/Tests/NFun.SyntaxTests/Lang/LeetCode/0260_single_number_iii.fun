# LeetCode 0260 — Single Number III
#
# Every element appears twice except two distinct singletons. XOR everything
# to get a^b. A lowest set bit of (a^b) tells us where a and b differ; split
# the array on that bit and XOR each half separately.

fun lowestBit(x):
    return x & -x

fun singleNumberIII(nums):
    xorAll = 0
    for x in nums:
        xorAll = xorAll ^ x
    bit = lowestBit(xorAll)
    a = 0
    b = 0
    for x in nums:
        if (x & bit) == 0:
            a = a ^ x
        else:
            b = b ^ x
    if a < b: return [a, b]
    return [b, a]

@Test
fun testCanonical():
    assertEqual(singleNumberIII([1, 2, 1, 3, 2, 5]), [3, 5])

@Test
fun testNegatives():
    assertEqual(singleNumberIII([-1, 0]), [-1, 0])

@Test
fun testThreePairsTwoSingles():
    assertEqual(singleNumberIII([1, 1, 4, 4, 7, 9, 9, 11]), [7, 11])
