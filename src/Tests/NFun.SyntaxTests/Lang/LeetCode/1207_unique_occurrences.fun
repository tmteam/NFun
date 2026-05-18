# LeetCode 1207 — Unique Number of Occurrences
#
# Each value's occurrence-count must be unique. Sort, run-length, then sort
# the counts and check for duplicates.

fun uniqueOccurrences(arr):
    s = arr.sort()
    counts = []
    i = 0
    while i < s.count():
        j = i
        while j < s.count() and s[j] == s[i]:
            j += 1
        counts = concat(counts, [j - i])
        i = j
    counts = counts.sort()
    i = 1
    while i < counts.count():
        if counts[i] == counts[i - 1]: return false
        i += 1
    return true

@Test
fun testCanonical():
    assertEqual(uniqueOccurrences([1, 2, 2, 1, 1, 3]), true)

@Test
fun testNotUnique():
    assertEqual(uniqueOccurrences([1, 2]), false)

@Test
fun testMixed():
    assertEqual(uniqueOccurrences([-3, 0, 1, -3, 1, 1, 1, -3, 10, 0]), true)
