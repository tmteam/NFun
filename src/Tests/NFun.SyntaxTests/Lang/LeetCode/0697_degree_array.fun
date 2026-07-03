# LeetCode 0697 — Degree of an Array
#
# Find the smallest window containing every occurrence of the most-frequent
# value. Without a hash counter, sort + run length.

fun findShortestSubArray(nums):
    n = nums.count()
    s = nums.sort()
    # For each unique value, count its frequency and use the original array
    # to locate first/last occurrence — works for the leetcode bounds.
    bestDegree = 0
    bestLen = n
    i = 0
    while i < n:
        j = i
        while j < n and s[j] == s[i]:
            j += 1
        freq = j - i
        if freq > bestDegree:
            bestDegree = freq
            # Find first/last occurrence of s[i] in original nums.
            first = -1
            last = -1
            k = 0
            while k < n:
                if nums[k] == s[i]:
                    if first == -1: first = k
                    last = k
                k += 1
            bestLen = last - first + 1
        elif freq == bestDegree:
            first = -1
            last = -1
            k = 0
            while k < n:
                if nums[k] == s[i]:
                    if first == -1: first = k
                    last = k
                k += 1
            if last - first + 1 < bestLen: bestLen = last - first + 1
        i = j
    return bestLen

@Test
fun testCanonical():
    assertEqual(findShortestSubArray([1, 2, 2, 3, 1]), 2)

@Test
fun testTie():
    assertEqual(findShortestSubArray([1, 2, 2, 3, 1, 4, 2]), 6)

@Test
fun testSingle():
    assertEqual(findShortestSubArray([5]), 1)
