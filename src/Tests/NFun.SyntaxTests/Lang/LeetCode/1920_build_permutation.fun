# LeetCode 1920 — Build Array from Permutation

fun buildArray(nums):
    return nums.map(rule nums[it])

@Test
fun testCanonical():
    assertEqual(buildArray([0, 2, 1, 5, 3, 4]), [0, 1, 2, 4, 5, 3])

@Test
fun testReverse():
    assertEqual(buildArray([5, 0, 1, 2, 3, 4]), [4, 5, 0, 1, 2, 3])

@Test
fun testIdentity():
    assertEqual(buildArray([0, 1, 2]), [0, 1, 2])
