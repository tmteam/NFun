# LeetCode 1389 — Create Target Array in the Given Order
#
# Insert nums[i] at position index[i] in target.

fun createTargetArray(nums, index):
    target = []
    i = 0
    while i < nums.count():
        idx = index[i]
        target = concat(concat(target.take(idx), [nums[i]]), target.skip(idx))
        i += 1
    return target

@Test
fun testCanonical():
    assertEqual(createTargetArray([0, 1, 2, 3, 4], [0, 1, 2, 2, 1]), [0, 4, 1, 3, 2])

@Test
fun testReverse():
    assertEqual(createTargetArray([1, 2, 3, 4, 0], [0, 1, 2, 3, 0]), [0, 1, 2, 3, 4])

@Test
fun testSingle():
    assertEqual(createTargetArray([1], [0]), [1])
