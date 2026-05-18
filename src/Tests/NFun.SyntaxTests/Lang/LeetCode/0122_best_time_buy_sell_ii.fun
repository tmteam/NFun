# LeetCode 0122 — Best Time to Buy and Sell Stock II
#
# Multiple buy/sell transactions are allowed. Optimal: sum every positive
# day-over-day delta — equivalent to capturing every upswing.

fun maxProfit(prices):
    total = 0
    i = 1
    while i < prices.count():
        if prices[i] > prices[i - 1]:
            total += prices[i] - prices[i - 1]
        i += 1
    return total

@Test
fun testCanonical():
    assertEqual(maxProfit([7, 1, 5, 3, 6, 4]), 7)

@Test
fun testMonotone():
    assertEqual(maxProfit([1, 2, 3, 4, 5]), 4)

@Test
fun testNoProfit():
    assertEqual(maxProfit([7, 6, 4, 3, 1]), 0)

@Test
fun testSingleDay():
    assertEqual(maxProfit([42]), 0)
