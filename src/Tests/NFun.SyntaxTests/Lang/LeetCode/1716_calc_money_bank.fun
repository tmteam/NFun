# LeetCode 1716 — Calculate Money in Leetcode Bank
#
# Week n contributes 1+2+…+7 + 7n  → 28 + 7n. Closed form for full weeks +
# remainder.

fun totalMoney(n):
    weeks = n // 7
    days = n % 7
    full = weeks * (28 + 7 * (weeks - 1) // 2 + 7 * (weeks - 1) - 7 * (weeks - 1) // 2) # collapse
    # Cleaner direct computation:
    total = 0
    i = 0
    while i < weeks:
        total += 28 + 7 * i
        i += 1
    j = 0
    while j < days:
        total += weeks + 1 + j
        j += 1
    return total

@Test(4, 10)
@Test(10, 37)
@Test(20, 96)
@Test(1, 1)
@Test(7, 28)
fun testTotalMoney(n, expected):
    assertEqual(totalMoney(n), expected)
