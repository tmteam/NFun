# LeetCode 1346 — Check If N and Its Double Exist
#
# Exists (i, j), i ≠ j, with arr[i] == 2 * arr[j]? Sort and binary-search
# for the double of each element.

fun bsearch(s, target):
    lo = 0
    hi = s.count() - 1
    while lo <= hi:
        mid = (lo + hi) // 2
        if s[mid] == target: return mid
        elif s[mid] < target: lo = mid + 1
        else: hi = mid - 1
    return -1

fun checkIfExist(arr):
    s = arr.sort()
    i = 0
    while i < s.count():
        target = 2 * s[i]
        idx = bsearch(s, target)
        # When the element is zero, "double" is itself — need a second copy.
        if idx != -1 and idx != i: return true
        if s[i] == 0:
            # count zeros
            c = 0
            for x in arr:
                if x == 0: c += 1
            if c >= 2: return true
        i += 1
    return false

@Test
fun testCanonical():
    assertEqual(checkIfExist([10, 2, 5, 3]), true)

@Test
fun testNoDouble():
    assertEqual(checkIfExist([3, 1, 7, 11]), false)

@Test
fun testTwoZeros():
    assertEqual(checkIfExist([0, 0]), true)
