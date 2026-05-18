# LeetCode 0121 — Best Time to Buy and Sell Stock
#
# Given an array `prices` where prices[i] is the stock price on day i, choose
# a single day to buy and a later day to sell to maximise profit. Return the
# maximum profit, or 0 if no profit is possible.

fun maxProfit(prices):
    minPrice = prices[0]
    bestProfit = 0
    i = 1
    while i < prices.count():
        if prices[i] < minPrice:
            minPrice = prices[i]
        elif prices[i] - minPrice > bestProfit:
            bestProfit = prices[i] - minPrice
        i += 1
    return bestProfit

@Test
fun testCanonical():
    assertEqual(maxProfit([7, 1, 5, 3, 6, 4]), 5)

@Test
fun testNoProfit():
    assertEqual(maxProfit([7, 6, 4, 3, 1]), 0)

@Test
fun testMonotoneUp():
    assertEqual(maxProfit([1, 2, 3, 4, 5]), 4)

@Test
fun testSingleDay():
    assertEqual(maxProfit([42]), 0)
