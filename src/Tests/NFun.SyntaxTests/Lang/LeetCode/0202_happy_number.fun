# LeetCode 0202 — Happy Number
#
# A happy number: repeatedly replace n by the sum of squares of its digits.
# If the process ends at 1, the number is happy. Otherwise it falls into a
# cycle that never reaches 1.
#
# Without a hash set for cycle detection, use Floyd's tortoise-and-hare:
# advance one pointer one step, the other two; if there's a cycle they meet,
# and the meeting point is 1 iff the number is happy.

fun digitSquareSum(n):
    total = 0
    x = n
    while x > 0:
        d = x % 10
        total += d * d
        x = x // 10
    return total

fun isHappy(n):
    slow = n
    fast = digitSquareSum(n)
    while fast != 1 and slow != fast:
        slow = digitSquareSum(slow)
        fast = digitSquareSum(digitSquareSum(fast))
    return fast == 1

@Test(19, true)
@Test(2, false)
@Test(1, true)
@Test(7, true)
@Test(4, false)
fun testIsHappy(n, expected):
    assertEqual(isHappy(n), expected)
