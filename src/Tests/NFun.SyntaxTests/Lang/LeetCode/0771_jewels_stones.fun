# LeetCode 0771 — Jewels and Stones
#
# Count characters in `stones` that appear in `jewels`. Both are strings —
# nested membership check is plenty for the leetcode bounds.

fun isJewel(c, jewels):
    i = 0
    while i < jewels.count():
        if jewels[i] == c: return true
        i += 1
    return false

fun numJewelsInStones(jewels, stones):
    count = 0
    for c in stones:
        if isJewel(c, jewels): count += 1
    return count

@Test('aA', 'aAAbbbb', 3)
@Test('z', 'ZZ', 0)
@Test('', 'anything', 0)
@Test('abc', '', 0)
fun testNumJewels(jewels, stones, expected):
    assertEqual(numJewelsInStones(jewels, stones), expected)
