# LeetCode 0448 — Find All Numbers Disappeared in an Array
#
# nums has length n with values in [1..n]. Return the missing ones.
# Without a hash set: sort, walk, emit gaps.

fun findDisappearedNumbers(nums):
    n = nums.count()
    seen = nums.sort()
    out = []
    expected = 1
    i = 0
    while expected <= n:
        if i >= seen.count() or seen[i] > expected:
            out = concat(out, [expected])
            expected += 1
        else:
            if seen[i] == expected:
                expected += 1
            i += 1
    return out

@Test
fun testCanonical():
    assertEqual(findDisappearedNumbers([4, 3, 2, 7, 8, 2, 3, 1]), [5, 6])

@Test
fun testNoneMissing():
    assertEqual(findDisappearedNumbers([1, 2, 3, 4, 5]), [])

@Test
fun testAllMissing():
    # n=4, all values are 1 → 2, 3, 4 missing
    assertEqual(findDisappearedNumbers([1, 1, 1, 1]), [2, 3, 4])
