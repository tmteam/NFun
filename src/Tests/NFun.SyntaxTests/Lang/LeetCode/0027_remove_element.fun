# LeetCode 0027 — Remove Element
#
# Without in-place mutation, return the filtered array.

fun removeElement(nums, target):
    return nums.filter(rule it != target)

@Test
fun testCanonical():
    assertEqual(removeElement([3, 2, 2, 3], 3), [2, 2])

@Test
fun testAll():
    assertEqual(removeElement([1, 1, 1, 1], 1), [])

@Test
fun testNone():
    assertEqual(removeElement([1, 2, 3], 99), [1, 2, 3])

@Test
fun testEmpty():
    assertEqual(removeElement([], 1), [])
