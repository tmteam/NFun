# LeetCode 2206 — Divide Array Into Equal Pairs
#
# Every value must appear an even number of times. Sort + run length.

fun divideArray(nums):
    s = nums.sort()
    i = 0
    while i < s.count():
        j = i
        while j < s.count() and s[j] == s[i]:
            j += 1
        if (j - i) % 2 != 0: return false
        i = j
    return true

@Test
fun testYes():
    assertEqual(divideArray([3, 2, 3, 2, 2, 2]), true)

@Test
fun testNo():
    assertEqual(divideArray([1, 2, 3, 4]), false)
