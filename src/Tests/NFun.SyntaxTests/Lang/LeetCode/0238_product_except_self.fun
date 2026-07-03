# LeetCode 0238 — Product of Array Except Self
#
# Without using division. Two passes: build a prefix-product array and a
# suffix-product array, then multiply position by position.

fun productExceptSelf(nums):
    n = nums.count()
    if n == 0: return []
    prefix = [1]
    i = 1
    while i < n:
        prefix = concat(prefix, [prefix[i - 1] * nums[i - 1]])
        i += 1
    out = []
    suffix = 1
    j = n - 1
    while j >= 0:
        out = concat([prefix[j] * suffix], out)
        suffix = suffix * nums[j]
        j -= 1
    return out

@Test
fun testCanonical():
    assertEqual(productExceptSelf([1, 2, 3, 4]), [24, 12, 8, 6])

@Test
fun testWithZeros():
    assertEqual(productExceptSelf([-1, 1, 0, -3, 3]), [0, 0, 9, 0, 0])

@Test
fun testTwo():
    assertEqual(productExceptSelf([2, 3]), [3, 2])
