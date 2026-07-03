# LeetCode 2089 — Find Target Indices After Sorting Array
#
# Indices of `target` in nums.sort(). Two passes: count of values <target
# gives the starting index; then count occurrences for the run.

fun targetIndices(nums, target):
    fewer = 0
    equal = 0
    for x in nums:
        if x < target: fewer += 1
        elif x == target: equal += 1
    out = []
    i = 0
    while i < equal:
        out = concat(out, [fewer + i])
        i += 1
    return out

@Test
fun testCanonical():
    assertEqual(targetIndices([1, 2, 5, 2, 3], 2), [1, 2])

@Test
fun testNotPresent():
    assertEqual(targetIndices([1, 2, 5, 2, 3], 5), [4])

@Test
fun testMissing():
    assertEqual(targetIndices([1, 2, 5, 2, 3], 7), [])
