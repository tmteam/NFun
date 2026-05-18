# LeetCode 0070 — Climbing Stairs
#
# You're climbing a staircase. It takes n steps to reach the top. Each time
# you can climb either 1 or 2 steps. How many distinct ways can you climb?
# Classic Fibonacci recurrence — iterative version avoids exponential
# blow-up of naive recursion.

fun climbStairs(n):
    if n <= 2: return n
    prev = 1
    curr = 2
    i = 3
    while i <= n:
        next = prev + curr
        prev = curr
        curr = next
        i += 1
    return curr

@Test(1, 1)
@Test(2, 2)
@Test(3, 3)
@Test(4, 5)
@Test(5, 8)
@Test(10, 89)
@Test(20, 10946)
fun testClimbStairs(n, expected):
    assertEqual(climbStairs(n), expected)
