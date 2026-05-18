# LeetCode 0747 — Largest Number At Least Twice of Others
#
# Return the index of the largest element iff it's at least twice every
# other element, else -1. One pass tracking top two values + position.

fun dominantIndex(nums):
    if nums.count() == 0: return -1
    top1 = -1
    top2 = -1
    idx = -1
    i = 0
    while i < nums.count():
        x = nums[i]
        if x > top1:
            top2 = top1
            top1 = x
            idx = i
        elif x > top2:
            top2 = x
        i += 1
    if top1 >= 2 * top2: return idx
    return -1

@Test
fun testCanonical():
    assertEqual(dominantIndex([3, 6, 1, 0]), 1)

@Test
fun testNotDominant():
    assertEqual(dominantIndex([1, 2, 3, 4]), -1)

@Test
fun testSingleton():
    assertEqual(dominantIndex([1]), 0)
