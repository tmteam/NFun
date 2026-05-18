# LeetCode 0387 — First Unique Character in a String
#
# Index of the first character that doesn't repeat. Without a hash counter,
# do a per-char count scan — O(n²) but tractable for the leetcode bounds.

fun firstUniqChar(s):
    n = s.count()
    i = 0
    while i < n:
        c = s[i]
        count = 0
        j = 0
        while j < n:
            if s[j] == c: count += 1
            j += 1
        if count == 1: return i
        i += 1
    return -1

@Test('leetcode', 0)
@Test('loveleetcode', 2)
@Test('aabb', -1)
@Test('z', 0)
@Test('', -1)
fun testFirstUniqChar(s, expected):
    assertEqual(firstUniqChar(s), expected)
