# LeetCode 0028 — Find the Index of the First Occurrence in a String
#
# Return the first index where `needle` appears in `haystack`, or -1.
# Naive O(n·m) — enough for the leetcode test bounds and clearer than KMP.

fun strStr(haystack, needle):
    n = haystack.count()
    m = needle.count()
    if m == 0: return 0
    if m > n: return -1
    i = 0
    while i <= n - m:
        j = 0
        ok = true
        while j < m and ok:
            if haystack[i + j] != needle[j]: ok = false
            j += 1
        if ok: return i
        i += 1
    return -1

@Test('sadbutsad', 'sad', 0)
@Test('leetcode', 'leeto', -1)
@Test('hello', 'll', 2)
@Test('abc', '', 0)
@Test('a', 'aa', -1)
fun testStrStr(haystack, needle, expected):
    assertEqual(strStr(haystack, needle), expected)
