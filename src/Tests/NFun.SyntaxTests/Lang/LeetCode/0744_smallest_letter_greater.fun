# LeetCode 0744 — Find Smallest Letter Greater Than Target
#
# Letters are sorted; find the smallest one strictly greater than `target`.
# Wraps around: if none is greater, return the first letter. Binary search.

fun nextGreatestLetter(letters, target):
    lo = 0
    hi = letters.count()
    while lo < hi:
        mid = (lo + hi) // 2
        if letters[mid] <= target:
            lo = mid + 1
        else:
            hi = mid
    if lo == letters.count(): return letters[0]
    return letters[lo]

@Test
fun testCanonical():
    assertEqual(nextGreatestLetter([/'c', /'f', /'j'], /'a'), /'c')

@Test
fun testExactMatch():
    assertEqual(nextGreatestLetter([/'c', /'f', /'j'], /'c'), /'f')

@Test
fun testWrap():
    assertEqual(nextGreatestLetter([/'c', /'f', /'j'], /'j'), /'c')

@Test
fun testBeyondAll():
    assertEqual(nextGreatestLetter([/'c', /'f', /'j'], /'z'), /'c')
