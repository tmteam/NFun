# LeetCode 1518 — Water Bottles
#
# Closed-form: total drinks = numBottles + (numBottles - 1) // (numExchange - 1).

fun numWaterBottles(numBottles, numExchange):
    return numBottles + (numBottles - 1) // (numExchange - 1)

@Test(9, 3, 13)
@Test(15, 4, 19)
@Test(2, 3, 2)
fun testWaterBottles(b, e, expected):
    assertEqual(numWaterBottles(b, e), expected)
