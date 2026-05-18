# LeetCode 0434 — Number of Segments in a String
#
# Count maximal runs of non-space characters. A segment starts at the
# transition `space → non-space` (or at index 0 if it begins non-space).

fun countSegments(s):
    count = 0
    inSeg = false
    for c in s:
        if c == /' ':
            inSeg = false
        else:
            if not inSeg:
                count += 1
                inSeg = true
    return count

@Test('Hello, my name is John', 5)
@Test('', 0)
@Test('    ', 0)
@Test('one', 1)
@Test('  one two  three ', 3)
fun testCountSegments(s, expected):
    assertEqual(countSegments(s), expected)
