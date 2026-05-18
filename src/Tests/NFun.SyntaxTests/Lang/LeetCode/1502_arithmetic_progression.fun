# LeetCode 1502 — Can Make Arithmetic Progression
#
# After sorting, every adjacent diff must equal arr[1] - arr[0].

fun canMakeArithmeticProgression(arr):
    s = arr.sort()
    diff = s[1] - s[0]
    i = 2
    while i < s.count():
        if s[i] - s[i - 1] != diff: return false
        i += 1
    return true

@Test
fun testCanonical():
    assertEqual(canMakeArithmeticProgression([3, 5, 1]), true)

@Test
fun testNot():
    assertEqual(canMakeArithmeticProgression([1, 2, 4]), false)

@Test
fun testTwoElements():
    assertEqual(canMakeArithmeticProgression([1, 100]), true)
