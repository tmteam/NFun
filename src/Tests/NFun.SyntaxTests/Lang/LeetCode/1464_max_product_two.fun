# LeetCode 1464 — Maximum Product of Two Elements in an Array
#
# (nums[i] - 1) * (nums[j] - 1), maximised by the top two values.

fun maxProductTwo(nums):
    s = nums.sort()
    n = s.count()
    return (s[n - 1] - 1) * (s[n - 2] - 1)

@Test
fun testCanonical():
    assertEqual(maxProductTwo([3, 4, 5, 2]), 12)

@Test
fun testSecondCase():
    assertEqual(maxProductTwo([1, 5, 4, 5]), 16)

@Test
fun testWithMin():
    assertEqual(maxProductTwo([3, 7]), 12)
