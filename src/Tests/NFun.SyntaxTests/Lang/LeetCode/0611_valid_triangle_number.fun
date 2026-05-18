# LeetCode 0611 — Valid Triangle Number
#
# Triples (i, j, k) where the three sides form a non-degenerate triangle.
# Sort and pick the longest side; two pointers from the remaining left.

fun triangleNumber(nums):
    s = nums.sort()
    n = s.count()
    count = 0
    k = n - 1
    while k >= 2:
        i = 0
        j = k - 1
        while i < j:
            if s[i] + s[j] > s[k]:
                count += j - i
                j -= 1
            else:
                i += 1
        k -= 1
    return count

@Test
fun testCanonical():
    assertEqual(triangleNumber([2, 2, 3, 4]), 3)

@Test
fun testWithZero():
    assertEqual(triangleNumber([4, 2, 3, 4]), 4)

@Test
fun testNoneValid():
    assertEqual(triangleNumber([1, 2, 3]), 0)
